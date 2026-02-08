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

    [Header("Hover")]
    [SerializeField] private float hoverScale = 1.35f;
    [SerializeField] private float hoverSmooth = 15f;
    [SerializeField] private LayerMask cardLayer;
    [SerializeField] private int hoverSortingBoost = 1000;

    [Header("Sorting")]
    [SerializeField] private int viewerBaseSortingOrder = 10000;

    public Camera _cam;
    private Transform hoveredCard;
    private Vector3 hoveredOriginalScale;
    private int hoveredOriginalSorting;
    private CardView hoveredCardView;
    private float verticalScroll;

    private readonly List<GameObject> spawnedCards = new();

    public System.Action onHideCards;

    /// <summary>
    /// Optional selection callback. When set, clicking a card in the viewer
    /// fires this with the card's index in the displayed array.
    /// Used by the shop's card removal flow.
    /// </summary>
    public System.Action<int> onCardSelected;

    #region Unity Methods

    private void Awake() => _cam = Camera.main;

    // private void Start() => DisplayCards(cardPrefabs);

    private void Update()
    {
        // Guard: if camera was destroyed (e.g. scene unloading), try to re-acquire
        if (_cam == null)
        {
            _cam = Camera.main;
            if (_cam == null) return; // No camera — skip this frame
        }

        ScrollCards();
        HandleHover();
        HandleClick();

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
        {
            if (card != null)
                Destroy(card);
        }

        spawnedCards.Clear();

        if (cardRoot != null)
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

        // Content extends downward: row centers go from y=0 to y=-(rows-1)*spacing
        int rowCount = Mathf.CeilToInt((float)spawnedCards.Count / Mathf.Max(cardsPerRow, 1));
        float contentBottom = (rowCount - 1) * verticalSpacing;

        // Full viewport height the camera can see
        float viewportHeight = _cam != null ? _cam.orthographicSize * 2f : 10f;

        // How far below the cardRoot origin the camera bottom sits
        float cardRootWorldY = cardRoot.parent != null
            ? cardRoot.parent.position.y
            : cardRoot.position.y;
        float camBottomY = _cam != null
            ? _cam.transform.position.y - _cam.orthographicSize
            : cardRootWorldY - 5f;
        float visibleBelow = cardRootWorldY - camBottomY;

        // How far above the cardRoot origin the camera top sits
        float camTopY = _cam != null
            ? _cam.transform.position.y + _cam.orthographicSize
            : cardRootWorldY + 5f;
        float visibleAbove = camTopY - cardRootWorldY;

        // Allow scrolling down (negative) so top row can reach center of viewport
        // Top row is at y=0 relative to cardRoot; let it drop to ~middle of screen
        float topPadding = Mathf.Max(0f, visibleAbove - viewportHeight * 0.35f);
        float minScroll = -topPadding;

        // Allow scrolling up (positive) so bottom row can reach center of viewport
        float bottomPadding = viewportHeight * 0.35f;
        float maxScroll = Mathf.Max(0f, contentBottom - visibleBelow + bottomPadding);

        verticalScroll = Mathf.Clamp(verticalScroll + input * scrollSpeed, minScroll, maxScroll);
        cardRoot.localPosition = new Vector3(0f, verticalScroll, 0f);
    }

    #endregion

    #region Selection

    private void HandleClick()
    {
        if (onCardSelected == null) return;
        if (!_cam || spawnedCards.Count == 0) return;
        if (!Input.GetMouseButtonDown(0)) return;

        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;

        Collider2D hit = Physics2D.OverlapPoint(mouseWorld, cardLayer);
        if (hit == null) return;

        // Find which spawned card was clicked
        Transform clicked = hit.transform;
        for (int i = 0; i < spawnedCards.Count; i++)
        {
            if (spawnedCards[i] == null) continue;
            if (spawnedCards[i].transform == clicked ||
                clicked.IsChildOf(spawnedCards[i].transform))
            {
                onCardSelected.Invoke(i);
                return;
            }
        }
    }

    #endregion

    #region Hover

    private void HandleHover()
    {
        if (!_cam || spawnedCards.Count == 0) return;

        Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
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
