using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-contained card viewer overlay. Parented to the Main Camera so it
/// persists across scenes. Consumers grab it at runtime via CardViewer.Instance.
///
/// Setup: Create a child hierarchy on the Main Camera:
///   Camera (Main)
///     └─ CardViewer          ← this script
///         ├─ Backdrop        ← SpriteRenderer (dark overlay)
///         └─ CardRoot        ← empty Transform (cards spawned here)
/// </summary>
public class CardViewer : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private CardData[] cardPrefabs;
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private Transform cardRoot;

    [Header("Backdrop")]
    [Tooltip("Dark overlay behind the card viewer. Managed automatically — no need to toggle externally.")]
    [SerializeField] private GameObject backdrop;
    [Tooltip("Sorting layer for the backdrop sprite. Should be below the viewer cards layer (e.g. 'Deck View BG').")]
    [SerializeField] private string backdropSortingLayer = "Deck View BG";

    // Found at runtime each time the viewer opens — each scene has its own overlay canvas
    private GraphicRaycaster _sceneRaycaster;

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
    [Tooltip("Sorting layer for viewer cards. Must match a layer defined in Project Settings (e.g. 'Deck View').")]
    [SerializeField] private string viewerSortingLayer = "Deck View";
    [SerializeField] private int viewerBaseSortingOrder = 0;

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

    /// <summary>
    /// Finds the CardViewer parented to the Main Camera.
    /// Every scene's camera should have a CardViewer child.
    /// </summary>
    public static CardViewer Instance
    {
        get
        {
            Camera cam = Camera.main;
            if (cam == null) return null;
            return cam.GetComponentInChildren<CardViewer>();
        }
    }

    #region Unity Methods

    private void Awake()
    {
        _cam = Camera.main;

        // Assign sorting layer to backdrop sprite
        if (backdrop != null)
        {
            var backdropSR = backdrop.GetComponent<SpriteRenderer>();
            if (backdropSR != null)
                backdropSR.sortingLayerName = backdropSortingLayer;

            backdrop.SetActive(false);
        }
    }

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

        // Show backdrop
        if (backdrop != null)
            backdrop.SetActive(true);

        // Find and disable the current scene's overlay raycaster so Physics2D can reach cards
        _sceneRaycaster = FindAnyObjectByType<GraphicRaycaster>();
        if (_sceneRaycaster != null)
            _sceneRaycaster.enabled = false;

        for (int i = 0; i < cards.Length; i++)
        {
            GameObject cardObj = Instantiate(cardPrefab, cardRoot);
            spawnedCards.Add(cardObj);

            cardObj.GetComponent<Card>().SetCardData(cards[i]);

            // Place cards on the Deck View sorting layer so they render above game content.
            // Space each card by 50 sorting orders so their per-card SpriteMask custom
            // ranges (canvasSortingLayer=20 + MASK_LEEWAY=10) don't overlap each other.
            var cardView = cardObj.GetComponent<CardView>();
            if (cardView != null)
            {
                cardView.SetSortingLayer(viewerSortingLayer);
                cardView.SetSortingOrder(viewerBaseSortingOrder + i * 50);
            }

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

        // Hide backdrop and re-enable the scene's overlay raycaster
        if (backdrop != null)
            backdrop.SetActive(false);
        if (_sceneRaycaster != null)
        {
            _sceneRaycaster.enabled = true;
            _sceneRaycaster = null;
        }

        onHideCards?.Invoke();
    }

    /// <summary>
    /// Clears all subscriber callbacks. Call this when a scene consumer is destroyed
    /// to prevent stale delegates since CardViewer lives on the camera across scenes.
    /// </summary>
    public void ClearCallbacks()
    {
        onHideCards = null;
        onCardSelected = null;
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

        // Content extends downward from cardRoot origin:
        //   Top row:    y = 0
        //   Bottom row: y = -(rowCount-1) * verticalSpacing
        int rowCount = Mathf.CeilToInt((float)spawnedCards.Count / Mathf.Max(cardsPerRow, 1));
        float contentHeight = (rowCount - 1) * verticalSpacing;

        float halfView = _cam != null ? _cam.orthographicSize : 5f;

        // cardRoot starts at local (0,0) relative to camera center.
        // Top row is at y=0 (camera center). Negative scroll pushes content down.
        // Positive scroll pushes content up so bottom rows become visible.

        // Min scroll: let the top row drop a bit below center (slight overscroll)
        float minScroll = -(halfView * 0.35f);

        // Max scroll: bring the bottom row up to ~center of the screen
        // Bottom row is at y = -contentHeight in local space
        // To bring it to center: need scroll = contentHeight
        // Add a small margin so it's comfortably visible, not edge-clipped
        float maxScroll = Mathf.Max(0f, contentHeight + halfView * 0.35f);

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
