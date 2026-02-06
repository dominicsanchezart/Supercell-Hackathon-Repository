using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
	public CharacterInfo characterInfo;
    public List<CardData> cardsInHand = new();
    public List<CardData> drawPile = new();
    public List<CardData> discardPile = new();
    public List<CardData> exhaustPile = new();


	[Header("Prefabs")]
	[SerializeField] private GameObject cardPrefab;
	[SerializeField] private bool isPlayer;
	[SerializeField] private Arena arena;

	[Header("Bounds")]
	public float maxHandWidth = 10f;

	[Header("Curve Shape")]
	public float sideLift = 0.6f;
	public float curveExponent = 1.6f;
	public float smoothSpeed = 12f;

    [Header("Layout")]
    public float spacing = 1.5f;
    public float depthSpacing = 0.01f;

    [Header("Selection")]
    public float selectedLift = 1.2f;
    public float selectedScale = 1.3f;

	[Header("Selection Rotation")]
	public float maxLookRotation = 15f;
	public float lookRotationSmooth = 12f;

    public Transform handRoot;

    readonly List<CardView> cards = new();
    int selectedIndex = 0;
	public int selectedSortingBoost = 1000;
	float scrollIndex;
	const int SORTING_STRIDE = 100;



    void Awake()
    {
        if (!handRoot)
            handRoot = transform;

        cards.Clear();

        for (int i = 0; i < handRoot.childCount; i++)
        {
            CardView card = handRoot.GetChild(i).GetComponent<CardView>();
            if (!card) continue;

            card.owner = this;
            cards.Add(card);
        }

        selectedIndex = cards.Count > 0 ? cards.Count / 2 : 0;
		scrollIndex = selectedIndex;
    }

	private void Start()
	{
		StartBattle();
	}

	void Update()
	{
		HandleScroll();
		UpdateSelectedCardRotation();
		RefreshLayout();
	}

	public void StartBattle()
	{
		drawPile.Clear();
		discardPile.Clear();
		exhaustPile.Clear();
		cardsInHand.Clear();

		foreach (CardData card in characterInfo.GetInventory().deck)
			drawPile.Add(card);
	}

	[ContextMenu("Test Draw Hand")]
	public void StartTurn()
	{
		characterInfo.ResetEnergy();
		DrawNewHand();
	}

	[ContextMenu("Test Discard Hand")]
	public void EndTurn()
	{
		DiscardHand();
	}

	#region Card Logic

	public void DrawCardFromDeck()
	{
		if (drawPile.Count == 0)
		{
			if (discardPile.Count == 0)
				return;

			ReshuffleDiscardIntoDraw();
		}

		CardData data = drawPile[0];
		drawPile.RemoveAt(0);
		cardsInHand.Add(data);

		SpawnCardView(data);
	}

	public void UseCard()
	{
		if (selectedIndex < 0 || selectedIndex >= cards.Count)
			return;

		CardView view = cards[selectedIndex];
		Card card = view.GetComponent<Card>();
		CardData data = card.cardData;

		if (characterInfo.GetEnergy() < data.baseEnergyCost) return;

		characterInfo.SpendEnergy(data.baseEnergyCost);

		ResolveCardActions(data);

		cardsInHand.Remove(data);
		discardPile.Add(data);

		RemoveCard(view);
	}

	void ResolveCardActions(CardData data)
	{
		arena.ResolveAction(data.actionType1, data.action1Value, isPlayer);

		if (data.actionType2 != CardActionType.None)
			arena.ResolveAction(data.actionType2, data.action2Value, isPlayer);

		if (data.actionType3 != CardActionType.None)
			arena.ResolveAction(data.actionType2, data.action2Value, isPlayer);
	}

	public void DrawNewHand()
	{
		int cardsToDraw = characterInfo._data.baseDrawSize;

		while (cardsToDraw > 0)
		{
			if (drawPile.Count == 0)
			{
				if (discardPile.Count == 0)
					break;

				ReshuffleDiscardIntoDraw();
			}

			DrawCardFromDeck();
			cardsToDraw--;
		}
	}

	public void DiscardHand()
	{
		for (int i = cards.Count - 1; i >= 0; i--)
		{
			CardView view = cards[i];
			Card card = view.GetComponent<Card>();

			discardPile.Add(card.cardData);

			RemoveCard(view);
		}

		cardsInHand.Clear();
	}

	public void RemoveRandomCard()
	{
		if (cards.Count == 0)
			return;

		int index = Random.Range(0, cards.Count);
		CardView view = cards[index];
		Card card = view.GetComponent<Card>();

		cardsInHand.Remove(card.cardData);
		discardPile.Add(card.cardData);

		RemoveCard(view);
	}

	public void ExhaustCard()
	{
		// cardsInHand.Remove(card);
		// exhaustPile.Add(card);
	}

	public void ReshuffleDiscardIntoDraw()
	{
		for (int i = 0; i < discardPile.Count; i++)
			drawPile.Add(discardPile[i]);

		discardPile.Clear();

		for (int i = drawPile.Count - 1; i > 0; i--)
		{
			int j = Random.Range(0, i + 1);
			(drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
		}
	}

	void SpawnCardView(CardData data)
	{
		GameObject go = Instantiate(cardPrefab, handRoot);
		CardView view = go.GetComponent<CardView>();
		Card card = go.GetComponent<Card>();

		card.SetCardData(data);
		AddCard(view);
	}

	#endregion

	void UpdateSelectedCardRotation()
	{
		if (selectedIndex < 0 || selectedIndex >= cards.Count) return;

		CardView card = cards[selectedIndex];
		if (!card) return;

		Camera cam = Camera.main;
		if (!cam) return;

		Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);

		Vector3 localOffset = mouseWorld - card.transform.position;

		Vector2 normalized = new Vector2(Mathf.Clamp(localOffset.x, -1f, 1f), Mathf.Clamp(localOffset.y, -1f, 1f));

		float rotX = normalized.y * maxLookRotation;
		float rotY = -normalized.x * maxLookRotation;

		Quaternion targetRot = Quaternion.Euler(rotX, rotY, 0f);

		card.transform.localRotation = Quaternion.Slerp(card.transform.localRotation, targetRot, Time.deltaTime * lookRotationSmooth);
	}

	void HandleScroll()
	{
		if (cards.Count <= 1)
			return;

		float scrollDelta = Input.mouseScrollDelta.y;
		
		if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
			scrollDelta = 1f;
		else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
			scrollDelta = -1f;

		if (Mathf.Abs(scrollDelta) < 0.001f)
			return;

		scrollIndex -= scrollDelta;

		scrollIndex = Mathf.Clamp(scrollIndex, 0f, cards.Count - 1);

		int newIndex = Mathf.FloorToInt(scrollIndex + 0.5f);

		if (newIndex != selectedIndex)
		{
			selectedIndex = newIndex;
		}
	}

    public void AddCard(CardView card)
    {
        card.transform.SetParent(handRoot);
        card.owner = this;
        cards.Add(card);

        selectedIndex = cards.Count - 1;
    }

    public void RemoveCard(CardView card)
    {
        int index = cards.IndexOf(card);
        if (index == -1) return;

        cards.RemoveAt(index);
        Destroy(card.gameObject);

        selectedIndex = Mathf.Clamp(selectedIndex, 0, cards.Count - 1);
    }

	// public void OnCardClicked(CardView card)
	// {
	// 	int index = cards.IndexOf(card);
	// 	if (index == -1)
	// 		return;

	// 	// If this card is not selected, just select it
	// 	if (index != selectedIndex)
	// 	{
	// 		selectedIndex = index;
	// 		scrollIndex = selectedIndex; // keep scroll in sync
	// 		RefreshLayout();
	// 		return;
	// 	}

	// 	// Card is already selected — confirm/use (optional)
	// 	OnCardConfirmed(card);
	// }

	// void OnCardConfirmed(CardView card) // change to draw later on
	// {
	// 	// Placeholder for "play card" logic
	// 	Debug.Log("Confirmed card: " + card.name);

	// 	// Example behavior (remove when ready):
	// 	// RemoveCard(card);
	// }

    // ---- LAYOUT ----

	void RefreshLayout()
	{
		int count = cards.Count;
		if (count == 0)
			return;

		float halfWidth = maxHandWidth * 0.5f;

		int leftCount = selectedIndex;
		int rightCount = count - selectedIndex - 1;

		float leftSpacing = leftCount > 0
			? Mathf.Min(spacing, halfWidth / leftCount)
			: spacing;

		float rightSpacing = rightCount > 0
			? Mathf.Min(spacing, halfWidth / rightCount)
			: spacing;

		int maxDistance = Mathf.Max(leftCount, rightCount);

		for (int i = 0; i < count; i++)
		{
			CardView card = cards[i];

			int visualIndex = i - selectedIndex;
			int distance = Mathf.Abs(visualIndex);

			float sideSpacing = visualIndex < 0 ? leftSpacing : rightSpacing;

			float t = maxDistance > 0 ? (float)distance / maxDistance : 0f;
			float curveY = Mathf.Pow(t, curveExponent) * sideLift;

			float x = visualIndex * sideSpacing;
			float y = curveY;
			float z = -distance * depthSpacing;

			Vector3 pos = new Vector3(x, y, z);
			Vector3 scale = Vector3.one;

			// ---- SORTING ----
			int baseOrder = (count - distance) * SORTING_STRIDE;

			bool isSelected = (i == selectedIndex);
			if (isSelected)
			{
				pos.y += selectedLift;
				scale = Vector3.one * selectedScale;
				baseOrder += selectedSortingBoost;
			}

			card.SetMasked(isSelected);

			card.transform.localPosition = Vector3.Lerp(
				card.transform.localPosition,
				pos,
				Time.deltaTime * smoothSpeed
			);

			card.transform.localScale = Vector3.Lerp(
				card.transform.localScale,
				scale,
				Time.deltaTime * smoothSpeed
			);

			card.SetSortingOrder(baseOrder);
		}
	}
}