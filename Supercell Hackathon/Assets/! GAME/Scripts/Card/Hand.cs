using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    public List<CardData> cardsInHand = new();
    public List<CardData> drawPile = new();
    public List<CardData> discardPile = new();



	[Header("Bounds")]
	public float maxHandWidth = 10f;

	[Header("Curve Shape")]
	public float sideLift = 0.6f;
	public float curveExponent = 1.6f;

    [Header("Layout")]
    public float spacing = 1.5f;
    public float curveAmount = 0.4f;
    public float depthSpacing = 0.01f;

    [Header("Selection")]
    public float selectedLift = 1.2f;
    public float selectedScale = 1.3f;
    public float scrollCooldown = 0.1f;

	[Header("Selection Rotation")]
	public float maxLookRotation = 15f;
	public float lookRotationSmooth = 12f;

    public Transform handRoot;

    readonly List<CardView> cards = new();
    int selectedIndex = 0;
    float lastScrollTime;
	public int selectedSortingBoost = 1000;
	float scrollIndex;



	private void OnValidate()
	{
		RefreshLayout();
	}

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

        selectedIndex = Mathf.Clamp(selectedIndex, 0, cards.Count - 1);
		scrollIndex = selectedIndex;
        RefreshLayout();
    }

	void Update()
	{
		HandleScroll();
		UpdateSelectedCardRotation();
	}

	void UpdateSelectedCardRotation()
	{
		if (selectedIndex < 0 || selectedIndex >= cards.Count) return;

		CardView card = cards[selectedIndex];
		if (!card) return;

		Camera cam = Camera.main;
		if (!cam) return;

		Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
		// mouseWorld.z = card.transform.position.z;

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
		if (Mathf.Abs(scrollDelta) < 0.001f)
			return;

		const float scrollSensitivity = 1.0f;

		// Accumulate scroll into continuous index
		scrollIndex -= scrollDelta * scrollSensitivity;

		// Clamp to valid range
		scrollIndex = Mathf.Clamp(scrollIndex, 0f, cards.Count - 1);

		// Snap to nearest index WITHOUT skipping
		int newIndex = Mathf.FloorToInt(scrollIndex + 0.5f);

		if (newIndex != selectedIndex)
		{
			selectedIndex = newIndex;
			RefreshLayout();
		}
	}

    // ---- PUBLIC API ----

    public void AddCard(CardView card)
    {
        card.transform.SetParent(handRoot);
        card.owner = this;
        cards.Add(card);

        selectedIndex = cards.Count - 1;
        RefreshLayout();
    }

    public void RemoveCard(CardView card)
    {
        int index = cards.IndexOf(card);
        if (index == -1) return;

        cards.RemoveAt(index);
        Destroy(card.gameObject);

        selectedIndex = Mathf.Clamp(selectedIndex, 0, cards.Count - 1);
        RefreshLayout();
    }

	public void OnCardClicked(CardView card)
	{
		int index = cards.IndexOf(card);
		if (index == -1)
			return;

		// If this card is not selected, just select it
		if (index != selectedIndex)
		{
			selectedIndex = index;
			scrollIndex = selectedIndex; // keep scroll in sync
			RefreshLayout();
			return;
		}

		// Card is already selected — confirm/use (optional)
		OnCardConfirmed(card);
	}

	void OnCardConfirmed(CardView card) // change to draw later on
	{
		// Placeholder for "play card" logic
		Debug.Log("Confirmed card: " + card.name);

		// Example behavior (remove when ready):
		// RemoveCard(card);
	}

    // ---- LAYOUT ----

	void RefreshLayout()
	{
		int count = cards.Count;
		if (count == 0)
			return;

		float halfWidth = maxHandWidth * 0.5f;

		// Count cards on each side of selected
		int leftCount = selectedIndex;
		int rightCount = count - selectedIndex - 1;

		// Compute spacing per side (clamped)
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

			card.transform.rotation = Quaternion.identity;

			int visualIndex = i - selectedIndex;
			int distance = Mathf.Abs(visualIndex);

			// Choose spacing based on side
			float sideSpacing = visualIndex < 0 ? leftSpacing : rightSpacing;

			// Normalized distance for curve
			float t = maxDistance > 0 ? (float)distance / maxDistance : 0f;

			// Curve profile
			float curveY = Mathf.Pow(t, curveExponent) * sideLift;

			// Position
			float x = visualIndex * sideSpacing;
			float y = curveY;
			float z = -distance * depthSpacing;

			Vector3 pos = new Vector3(x, y, z);
			Vector3 scale = Vector3.one;

			// Sorting: closer to center above farther
			int baseOrder = (count - distance) * 10;

			// Selected card override
			bool isSelected = (i == selectedIndex);

			if (isSelected)
			{
				pos.y += selectedLift;
				scale = Vector3.one * selectedScale;
				baseOrder += selectedSortingBoost;
			}

			card.SetMasked(isSelected);

			// Card cardData = card.GetComponent<Card>() 
			// 	?? card.GetComponentOrInChildren<Card>();

			// if (cardData != null)
			// {
			// 	if (isSelected)
			// 		cardData.EnableMask();
			// 	else
			// 		cardData.DisableMask();
			// }

			card.SetTarget(pos, scale);
			card.SetSortingOrder(baseOrder);
		}
	}
}