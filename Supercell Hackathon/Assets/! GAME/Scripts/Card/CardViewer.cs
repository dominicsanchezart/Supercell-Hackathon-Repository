using System.Collections.Generic;
using UnityEngine;

public class CardViewer : MonoBehaviour
{
	[SerializeField] private CardData[] cardPrefabs;
    [Header("Setup")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardRoot;

    [Header("Layout")]
	[SerializeField] private int cardsPerRow = 4;
    [SerializeField] private float verticalSpacing = 1.2f;
    [SerializeField] private float horizontalSpacing = 1.0f;

    [Header("Scrolling")]
	[SerializeField] private float scrollSpeed = 5f;
	private float verticalScroll;

	[Header("Hover")]
	[SerializeField] private float hoverScale = 1.35f;
	[SerializeField] private float hoverSmooth = 15f;
	[SerializeField] private LayerMask cardLayer;
	[SerializeField] private int hoverSortingBoost = 1000;

	private Camera cam;
	private Transform hoveredCard;
	private Vector3 hoveredOriginalScale;
	private int hoveredOriginalSorting;

    private readonly List<GameObject> spawnedCards = new();



	private void Awake()
	{
		cam = Camera.main;
	}

	private void Start()
	{
		DisplayCards(cardPrefabs);
	}

	[ContextMenu("Reset Display")]
	private void ResetDisplay()
	{
		DisplayCards(cardPrefabs);
	}

	private void Update()
	{
		ScrollCards();
		HandleHover();
	}

	private void HandleHover()
	{
		Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
		mouseWorld.z = 0f;

		Collider2D hit = Physics2D.OverlapPoint(mouseWorld, cardLayer);

		if (hit)
		{
			Transform card = hit.transform;
			if (hoveredCard != card)
				BeginHover(card);
		}
		else
		{
			EndHover();
		}

		if (hoveredCard)
			UpdateHoveredCard();
	}

	private void BeginHover(Transform card)
	{
		EndHover();

		hoveredCard = card;
		hoveredOriginalScale = card.localScale;

		SpriteRenderer sr = card.GetComponentInChildren<SpriteRenderer>();
		hoveredOriginalSorting = sr.sortingOrder;
		sr.sortingOrder += hoverSortingBoost;
	}

	private void UpdateHoveredCard()
	{
		hoveredCard.localScale = Vector3.Lerp(
			hoveredCard.localScale,
			hoveredOriginalScale * hoverScale,
			Time.deltaTime * hoverSmooth
		);
	}

	private void EndHover()
	{
		if (!hoveredCard)
			return;

		hoveredCard.localScale = hoveredOriginalScale;

		SpriteRenderer sr = hoveredCard.GetComponentInChildren<SpriteRenderer>();
		sr.sortingOrder = hoveredOriginalSorting;

		hoveredCard = null;
	}
	
	public void DisplayCards(CardData[] cards)
	{
		HideCards();

		for (int i = 0; i < cards.Length; i++)
		{
			GameObject card = Instantiate(cardPrefab, cardRoot);
			spawnedCards.Add(card);

			card.GetComponent<Card>().SetCardData(cards[i]);

			int row    = i / cardsPerRow;
			int column = i % cardsPerRow;

			card.transform.localPosition = new Vector3(
				column * horizontalSpacing,
				-row * verticalSpacing,
				0f
			);
		}

		CenterGridHorizontally(cards.Length);
	}

	private void CenterGridHorizontally(int cardCount)
	{
		int columnsUsed = Mathf.Min(cardsPerRow, cardCount);
		if (columnsUsed <= 1)
			return;

		float totalWidth = (columnsUsed - 1) * horizontalSpacing;
		float xOffset = totalWidth * 0.5f;

		for (int i = 0; i < spawnedCards.Count; i++)
		{
			Vector3 p = spawnedCards[i].transform.localPosition;
			p.x -= xOffset;
			spawnedCards[i].transform.localPosition = p;
		}
	}

	public void HideCards()
	{
		foreach (var card in spawnedCards)
			Destroy(card);

		spawnedCards.Clear();
		cardRoot.localPosition = Vector3.zero;
	}

	private void ScrollCards()
	{
		float input = -Input.mouseScrollDelta.y;
		if (Mathf.Abs(input) < 0.01f) return;

		verticalScroll += input * scrollSpeed;
		verticalScroll = Mathf.Clamp(verticalScroll, 2f, 50f);
		cardRoot.localPosition = new Vector3(0f, verticalScroll, 0f);
	}
}