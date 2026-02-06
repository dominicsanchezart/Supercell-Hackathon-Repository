using System.Collections.Generic;
using UnityEngine;

public class Hand : MonoBehaviour
{
    [Header("References")]
    public CharacterInfo characterInfo;
    public Arena arena;
    public Transform handRoot;

    [Header("Prefabs")]
    [SerializeField] private GameObject cardPrefab;
    [SerializeField] private bool isPlayer;

    [Header("Hand Layout")]
    public float maxHandWidth = 10f;
    public float sideLift = 0.6f;
    public float curveExponent = 1.6f;
    public float smoothSpeed = 12f;
    public float spacing = 1.5f;
    public float depthSpacing = 0.01f;

    [Header("Selection")]
    public float selectedLift = 1.2f;
    public float selectedScale = 1.3f;
    public float maxLookRotation = 15f;
    public float lookRotationSmooth = 12f;

    [Header("Sorting")]
    public int selectedSortingBoost = 1000;
    private const int SORTING_STRIDE = 100;

    // Card collections
    public List<CardData> cardsInHand = new();
    public List<CardData> drawPile = new();
    public List<CardData> discardPile = new();
    public List<CardData> exhaustPile = new();

    // Runtime state
    private readonly List<CardView> cards = new();
    private int selectedIndex;
    private float scrollIndex;

    #region Unity Methods

    private void Awake()
    {
        if (!handRoot) handRoot = transform;

        cards.Clear();
        foreach (Transform child in handRoot)
        {
            if (child.TryGetComponent<CardView>(out var card))
            {
                card.owner = this;
                cards.Add(card);
            }
        }

        selectedIndex = cards.Count > 0 ? cards.Count / 2 : 0;
        scrollIndex = selectedIndex;
    }

    private void Start() => StartBattle();

    private void Update()
    {
        HandleScroll();
        UpdateSelectedCardRotation();
        RefreshLayout();
    }

    #endregion

    #region Battle Methods

    public void StartBattle()
    {
        drawPile.Clear();
        discardPile.Clear();
        exhaustPile.Clear();
        cardsInHand.Clear();

        drawPile.AddRange(characterInfo.GetInventory().deck);
    }

    [ContextMenu("Test Draw Hand")]
    public void StartTurn()
    {
        characterInfo.ResetEnergy();
        DrawNewHand();
    }

    [ContextMenu("Test Discard Hand")]
    public void EndTurn() => DiscardHand();

    #endregion

    #region Card Logic

    public void DrawCardFromDeck()
    {
        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0) return;
            ReshuffleDiscardIntoDraw();
        }

        CardData data = drawPile[0];
        drawPile.RemoveAt(0);
        cardsInHand.Add(data);
        SpawnCardView(data);
    }

    public void DrawNewHand()
    {
        int cardsToDraw = characterInfo._data.baseDrawSize;

        while (cardsToDraw > 0)
        {
            if (drawPile.Count == 0 && discardPile.Count == 0) break;

            DrawCardFromDeck();
            cardsToDraw--;
        }
    }

    public void UseCard()
    {
        if (!IsValidSelection()) return;

        CardView view = cards[selectedIndex];
        CardData data = view.GetComponent<Card>().cardData;

        if (characterInfo.GetEnergy() < data.baseEnergyCost) return;

        characterInfo.SpendEnergy(data.baseEnergyCost);
        ResolveCardActions(data);

        cardsInHand.Remove(data);
        discardPile.Add(data);
        RemoveCard(view);
    }

    private void ResolveCardActions(CardData data)
    {
        arena.ResolveAction(data.actionType1, data.action1Value, isPlayer);

        if (data.actionType2 != CardActionType.None)
            arena.ResolveAction(data.actionType2, data.action2Value, isPlayer);

        if (data.actionType3 != CardActionType.None)
            arena.ResolveAction(data.actionType3, data.action3Value, isPlayer); // fixed typo
    }

    public void DiscardHand()
    {
        for (int i = cards.Count - 1; i >= 0; i--)
        {
            var view = cards[i];
            discardPile.Add(view.GetComponent<Card>().cardData);
            RemoveCard(view);
        }

        cardsInHand.Clear();
    }

    public void RemoveRandomCard()
    {
        if (cards.Count == 0) return;

        int index = Random.Range(0, cards.Count);
        var card = cards[index].GetComponent<Card>();

        cardsInHand.Remove(card.cardData);
        discardPile.Add(card.cardData);
        RemoveCard(cards[index]);
    }

    public void ExhaustCard()
    {
        if (!IsValidSelection()) return;

        var card = cards[selectedIndex].GetComponent<Card>();
        cardsInHand.Remove(card.cardData);
        exhaustPile.Add(card.cardData);
    }

    private void ReshuffleDiscardIntoDraw()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();

        for (int i = drawPile.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (drawPile[i], drawPile[j]) = (drawPile[j], drawPile[i]);
        }
    }

    private void SpawnCardView(CardData data)
    {
        GameObject go = Instantiate(cardPrefab, handRoot);
        var view = go.GetComponent<CardView>();
        var card = go.GetComponent<Card>();
        card.SetCardData(data);
        AddCard(view);
    }

    #endregion

    #region Card Selection & Input

    private void HandleScroll()
    {
        if (cards.Count <= 1) return;

        float scrollDelta = Input.mouseScrollDelta.y;

        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow)) scrollDelta = 1f;
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow)) scrollDelta = -1f;

        if (Mathf.Abs(scrollDelta) < 0.001f) return;

        scrollIndex = Mathf.Clamp(scrollIndex - scrollDelta, 0f, cards.Count - 1);
        int newIndex = Mathf.RoundToInt(scrollIndex);

        if (newIndex != selectedIndex) selectedIndex = newIndex;
    }

    private void UpdateSelectedCardRotation()
    {
        if (!IsValidSelection()) return;
        if (Camera.main == null) return;

        var card = cards[selectedIndex];
        Vector3 localOffset = Camera.main.ScreenToWorldPoint(Input.mousePosition) - card.transform.position;
        Vector2 normalized = new Vector2(Mathf.Clamp(localOffset.x, -1f, 1f), Mathf.Clamp(localOffset.y, -1f, 1f));

        Quaternion targetRot = Quaternion.Euler(normalized.y * maxLookRotation, -normalized.x * maxLookRotation, 0f);
        card.transform.localRotation = Quaternion.Slerp(card.transform.localRotation, targetRot, Time.deltaTime * lookRotationSmooth);
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
        if (!cards.Remove(card)) return;
        Destroy(card.gameObject);
        selectedIndex = Mathf.Clamp(selectedIndex, 0, cards.Count - 1);
    }

    private bool IsValidSelection() => selectedIndex >= 0 && selectedIndex < cards.Count;

    #endregion

    #region Layout

    private void RefreshLayout()
    {
        int count = cards.Count;
        if (count == 0) return;

        float halfWidth = maxHandWidth * 0.5f;
        int leftCount = selectedIndex;
        int rightCount = count - selectedIndex - 1;

        float leftSpacing = leftCount > 0 ? Mathf.Min(spacing, halfWidth / leftCount) : spacing;
        float rightSpacing = rightCount > 0 ? Mathf.Min(spacing, halfWidth / rightCount) : spacing;
        int maxDistance = Mathf.Max(leftCount, rightCount);

        for (int i = 0; i < count; i++)
        {
            var card = cards[i];
            int visualIndex = i - selectedIndex;
            int distance = Mathf.Abs(visualIndex);

            float sideSpacing = visualIndex < 0 ? leftSpacing : rightSpacing;
            float t = maxDistance > 0 ? (float)distance / maxDistance : 0f;
            float curveY = Mathf.Pow(t, curveExponent) * sideLift;

            Vector3 targetPos = new(visualIndex * sideSpacing, curveY, -distance * depthSpacing);
            Vector3 targetScale = Vector3.one;

            int baseOrder = (count - distance) * SORTING_STRIDE;
            bool isSelected = i == selectedIndex;
            if (isSelected)
            {
                targetPos.y += selectedLift;
                targetScale *= selectedScale;
                baseOrder += selectedSortingBoost;
            }

            card.SetMasked(isSelected);
            card.transform.localPosition = Vector3.Lerp(card.transform.localPosition, targetPos, Time.deltaTime * smoothSpeed);
            card.transform.localScale = Vector3.Lerp(card.transform.localScale, targetScale, Time.deltaTime * smoothSpeed);
            card.SetSortingOrder(baseOrder);
        }
    }

    #endregion
}