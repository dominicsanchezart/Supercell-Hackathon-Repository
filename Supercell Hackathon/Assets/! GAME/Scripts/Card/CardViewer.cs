using System.Collections.Generic;
using UnityEngine;

public class CardViewer : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private CardData[] cardPrefabs;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardRoot;

    [Header("Layout")]
    [SerializeField] private int cardsPerRow = 4;
    [SerializeField] private float verticalSpacing = 1.2f;
    [SerializeField] private float horizontalSpacing = 1f;

    [Header("Scrolling")]
    [SerializeField] private float scrollSpeed = 5f;
    [SerializeField] private float minScroll = 2f;
    [SerializeField] private float maxScroll = 50f;

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.35f;
    [SerializeField] private float hoverSmooth = 15f;
    [SerializeField] private LayerMask cardLayer;
    [SerializeField] private int hoverSortingBoost = 1000;

    [Header("Sorting")]
    [SerializeField] private int viewerBaseSortingOrder = 10000;

    private Camera cam;
    private Transform hoveredCard;
    private Vector3 hoveredOriginalScale;
    private int hoveredOriginalSorting;
    private CardView hoveredCardView;
    private float verticalScroll;

    private readonly List<GameObject> spawnedCards = new();

    public System.Action onHideCards;

    #region Unity Methods

    private void Awake() => cam = Camera.main;

    // private void Start() => DisplayCards(cardPrefabs);

    private void Update()
    {
        ScrollCards();
        HandleHover();

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			HideCards();
		}
    }

    #endregion

    #region Card Display

    [ContextMenu("Reset Display")]
    private void ResetDisplay() => DisplayCards(cardPrefabs);

    public void DisplayCards(CardData[] cards)
    {
        ClearCards();

        for (int i = 0; i < cards.Length; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardRoot);
            spawnedCards.Add(cardObj);

            cardObj.GetComponent<Card>().SetCardData(cards[i]);

            // Set high sorting order so viewer cards render on top of everything
            var cardView = cardObj.GetComponent<CardView>();
            if (cardView != null)
                cardView.SetSortingOrder(viewerBaseSortingOrder + i);

            int row = i / cardsPerRow;
            int column = i % cardsPerRow;

            cardObj.transform.localPosition = new Vector3(
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
        if (columnsUsed <= 1) return;

        float xOffset = (columnsUsed - 1) * horizontalSpacing * 0.5f;

        foreach (var card in spawnedCards)
        {
            Vector3 p = card.transform.localPosition;
            p.x -= xOffset;
            card.transform.localPosition = p;
        }
    }

    public void HideCards()
    {
        ClearCards();
        onHideCards?.Invoke();
    }

    private void ClearCards()
    {
        EndHover();

        foreach (var card in spawnedCards)
            Destroy(card);

        spawnedCards.Clear();
        cardRoot.localPosition = Vector3.zero;
        verticalScroll = 0f;
    }

    #endregion

    #region Scrolling

    private void ScrollCards()
    {
        if (spawnedCards.Count == 0) return;

        float input = -Input.mouseScrollDelta.y;
        if (Mathf.Abs(input) < 0.01f) return;

        verticalScroll = Mathf.Clamp(verticalScroll + input * scrollSpeed, minScroll, maxScroll);
        cardRoot.localPosition = new Vector3(0f, verticalScroll, 0f);
    }

    #endregion

    #region Hover

    private void HandleHover()
    {
        if (!cam || spawnedCards.Count == 0) return;

        Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Collider2D hit = Physics2D.OverlapPoint(mouseWorld, cardLayer);

        if (hit)
        {
            Transform card = hit.transform;
            if (hoveredCard != card) BeginHover(card);
        }
        else EndHover();

        if (hoveredCard) UpdateHoveredCard();
    }

    private void BeginHover(Transform card)
    {
        EndHover();

        hoveredCard = card;
        hoveredOriginalScale = card.localScale;

        hoveredCardView = card.GetComponent<CardView>();
        if (hoveredCardView == null)
            hoveredCardView = card.GetComponentInParent<CardView>();

        if (hoveredCardView != null)
        {
            // Read current base order from first sprite to restore later
            SpriteRenderer sr = card.GetComponentInChildren<SpriteRenderer>();
            hoveredOriginalSorting = sr != null ? sr.sortingOrder : 0;
            hoveredCardView.SetSortingOrder(hoveredOriginalSorting + hoverSortingBoost);
        }
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
        if (!hoveredCard) return;

        // Guard against destroyed cards (e.g. HideCards called while hovering)
        if (hoveredCard != null)
        {
            hoveredCard.localScale = hoveredOriginalScale;

            if (hoveredCardView != null)
                hoveredCardView.SetSortingOrder(hoveredOriginalSorting);
        }

        hoveredCard = null;
        hoveredCardView = null;
    }

    #endregion
}
