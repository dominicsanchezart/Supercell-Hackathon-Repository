using System.Collections;
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
	[SerializeField] private bool showCardVisuals = true;
	

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

    [Header("Drag to Play")]
    public float dragPlayThreshold = 2.0f;
    public float dragReturnSpeed = 20f;

    [Header("Audio")]
    [SerializeField] private AudioClip drawCardSound;
    [SerializeField] private AudioClip pickupCardSound;
    [SerializeField] private AudioClip playAttackCardSound;
    [SerializeField] private AudioClip playNonAttackCardSound;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float drawCardDelay = 0.15f;
    [SerializeField] private Vector2 drawPitchRange = new(0.9f, 1.1f);

    [Header("Burn")]
    [SerializeField] private int burnDamagePerCard = 2;
    [SerializeField] private int maxBurnCards = 5;

    [Header("Sorting")]
    public int selectedSortingBoost = 1000;
    private const int SORTING_STRIDE = 100;

    // Drag state
    private bool _isDragging;
    private Vector3 _dragStartWorldPos;
    private CardView _draggedCard;

    // The card currently being played — excluded from random removal effects
    // so it doesn't accidentally discard/exhaust/destroy itself mid-resolution.
    private CardData _cardBeingPlayed;

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
		if (!showCardVisuals) return;

		if (arena != null && arena.IsViewingCards)
		{
			DisableAllCardColliders();
			return;
		}

		HandleDragInput();

		if (!_isDragging)
		{
			HandleScroll();
			UpdateSelectedCardRotation();
			RefreshLayout();
		}
	}

	private void DisableAllCardColliders()
	{
		foreach (var card in cards)
		{
			var col = card.GetComponentInChildren<Collider2D>();
			if (col != null) col.enabled = false;
		}
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
        StartCoroutine(DrawNewHandRoutine());
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

        if (drawCardSound != null && audioSource != null)
        {
            audioSource.pitch = Random.Range(drawPitchRange.x, drawPitchRange.y);
            audioSource.PlayOneShot(drawCardSound);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(clip);
        }
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

    private IEnumerator DrawNewHandRoutine()
    {
        int cardsToDraw = characterInfo._data.baseDrawSize;

        while (cardsToDraw > 0)
        {
            if (drawPile.Count == 0 && discardPile.Count == 0) break;

            DrawCardFromDeck();
            cardsToDraw--;

            if (cardsToDraw > 0)
                yield return new WaitForSeconds(drawCardDelay);
        }

        ApplyBurnToCards();
        RefreshCardDescriptions();
    }

	public void UseCard()
	{
		if (!showCardVisuals || !IsValidSelection())
			return;

		CardView view = cards[selectedIndex];
		CardData data = view.GetComponent<Card>().cardData;

		if (characterInfo.GetEnergy() < data.baseEnergyCost) return;

		// Play appropriate sound based on card type
		PlaySound(data.cardType == CardType.Attack ? playAttackCardSound : playNonAttackCardSound);

		characterInfo.SpendEnergy(data.baseEnergyCost);

		// Burning card deals damage to the player who plays it
		if (view.isBurning)
		{
			characterInfo.TakeDamage(burnDamagePerCard);
			Debug.Log($"Burned card played! {characterInfo._data.name} takes {burnDamagePerCard} burn damage.");
		}

		_cardBeingPlayed = data;
		ResolveCardActions(data);
		_cardBeingPlayed = null;

		// Track patron affinity from played card factions
		if (isPlayer)
			PatronAffinityTracker.OnCardPlayed(data);

		// Notify listeners which type of card was played (for sprite swaps)
		characterInfo.NotifyCardPlayed(data.cardType);

		cardsInHand.Remove(data);
		discardPile.Add(data);
		RemoveCard(view);

		// Refresh remaining cards since buffs may have changed
		RefreshCardDescriptions();
	}

    private void ResolveCardActions(CardData data)
    {
        // Use CardModifiers to get final values with buffs/debuffs applied
        CardModifiers.GetModifiedValues(data, characterInfo, out int mod1, out int mod2, out int mod3);

        if (IsActionConditionMet(data.action1Condition))
        {
            arena.ResolveAction(data.actionType1, mod1, data.actionTarget1, isPlayer);
            if (data.actionType1 == CardActionType.DamagePerStack || data.actionType1 == CardActionType.HealPerStack)
                characterInfo.ResetStatusEffect(data.action1StatusEffect);
        }

        if (data.actionType2 != CardActionType.None && IsActionConditionMet(data.action2Condition))
        {
            arena.ResolveAction(data.actionType2, mod2, data.actionTarget2, isPlayer);
            if (data.actionType2 == CardActionType.DamagePerStack || data.actionType2 == CardActionType.HealPerStack)
                characterInfo.ResetStatusEffect(data.action2StatusEffect);
        }

        if (data.actionType3 != CardActionType.None && IsActionConditionMet(data.action3Condition))
        {
            arena.ResolveAction(data.actionType3, mod3, data.actionTarget3, isPlayer);
            if (data.actionType3 == CardActionType.DamagePerStack || data.actionType3 == CardActionType.HealPerStack)
                characterInfo.ResetStatusEffect(data.action3StatusEffect);
        }
    }

    /// <summary>
    /// Evaluates whether a per-action condition is satisfied.
    /// </summary>
    private bool IsActionConditionMet(ActionConditionData cond)
    {
        switch (cond.condition)
        {
            case CardCondition.None:
                return true;

            case CardCondition.KillEnemy:
                return !arena.GetOpponent(isPlayer).IsAlive();

            case CardCondition.BelowHealthValue:
                return characterInfo.GetHealth() <= cond.threshold;

            case CardCondition.StatusEffectThreshold:
                return characterInfo.GetStatusEffectStacks(cond.statusEffect) >= cond.threshold;

            case CardCondition.EnemyStatusEffectThreshold:
                return arena.GetOpponent(isPlayer).GetStatusEffectStacks(cond.statusEffect) >= cond.threshold;

            case CardCondition.DiscardedCardFaction:
                if (discardPile.Count == 0) return false;
                var last = discardPile[discardPile.Count - 1];
                return last.cardFaction1 == cond.cardFaction || last.cardFaction2 == cond.cardFaction;

            default:
                return true;
        }
    }

    /// <summary>
    /// Refreshes the description text on all cards in hand to reflect current buffs.
    /// </summary>
    public void RefreshCardDescriptions()
    {
        if (!showCardVisuals) return;

        foreach (var view in cards)
        {
            var card = view.GetComponent<Card>();
            if (card != null)
                card.RefreshDescription(characterInfo);
        }
    }

    public void DiscardHand()
    {
        foreach (var data in cardsInHand)
            discardPile.Add(data);

        for (int i = cards.Count - 1; i >= 0; i--)
            RemoveCard(cards[i]);

        cardsInHand.Clear();
    }

    /// <summary>
    /// Immediately removes all visual cards from hand without discarding.
    /// Used when battle ends to clean up the hand display.
    /// </summary>
    public void ClearHand()
    {
        for (int i = cards.Count - 1; i >= 0; i--)
            RemoveCard(cards[i]);

        cardsInHand.Clear();
    }

    public void RemoveRandomCard()
    {
        if (cardsInHand.Count == 0) return;

        // Build a list of eligible indices, excluding the card currently being played
        List<int> eligible = new();
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            if (cardsInHand[i] != _cardBeingPlayed)
                eligible.Add(i);
        }
        if (eligible.Count == 0) return;

        int index = eligible[Random.Range(0, eligible.Count)];
        CardData data = cardsInHand[index];

        cardsInHand.RemoveAt(index);
        discardPile.Add(data);

        // Remove visual card if it exists
        if (index < cards.Count)
            RemoveCard(cards[index]);
    }

    public void ExhaustCard()
    {
        if (cardsInHand.Count == 0) return;

        int index;
        if (showCardVisuals && IsValidSelection())
        {
            // Player selects which card to exhaust
            index = selectedIndex;
            // Safety: don't exhaust the card being played
            if (cardsInHand[index] == _cardBeingPlayed)
                return;
        }
        else
        {
            // Pick a random card, excluding the one being played
            List<int> eligible = new();
            for (int i = 0; i < cardsInHand.Count; i++)
            {
                if (cardsInHand[i] != _cardBeingPlayed)
                    eligible.Add(i);
            }
            if (eligible.Count == 0) return;
            index = eligible[Random.Range(0, eligible.Count)];
        }

        CardData data = cardsInHand[index];
        cardsInHand.RemoveAt(index);
        exhaustPile.Add(data);

        if (index < cards.Count)
            RemoveCard(cards[index]);
    }

    /// <summary>
    /// Destroys a random card from hand permanently (removed from the run entirely).
    /// </summary>
    public void DestroyCard()
    {
        if (cardsInHand.Count == 0) return;

        // Build a list of eligible indices, excluding the card currently being played
        List<int> eligible = new();
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            if (cardsInHand[i] != _cardBeingPlayed)
                eligible.Add(i);
        }
        if (eligible.Count == 0) return;

        int index = eligible[Random.Range(0, eligible.Count)];
        CardData data = cardsInHand[index];

        cardsInHand.RemoveAt(index);
        // Card is NOT added to discard or exhaust — it's gone forever

        if (index < cards.Count)
            RemoveCard(cards[index]);

        Debug.Log($"Card '{data.cardName}' was destroyed permanently!");
    }

    /// <summary>
    /// Sets random cards in hand on fire based on burn stacks. Max 5 cards.
    /// Each card set on fire consumes 1 burn stack.
    /// </summary>
    public void ApplyBurnToCards()
    {
        int burnStacks = characterInfo.GetBurn();
        if (burnStacks <= 0 || cards.Count == 0) return;

        int cardsToIgnite = Mathf.Min(burnStacks, maxBurnCards, cards.Count);

        // Build list of non-burning card indices
        List<int> available = new();
        for (int i = 0; i < cards.Count; i++)
        {
            if (!cards[i].isBurning)
                available.Add(i);
        }

        int actualIgnited = Mathf.Min(cardsToIgnite, available.Count);

        // Shuffle and pick
        for (int i = available.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (available[i], available[j]) = (available[j], available[i]);
        }

        for (int i = 0; i < actualIgnited; i++)
        {
            cards[available[i]].SetBurning(true);
        }

        characterInfo.ConsumeBurn(actualIgnited);
    }

    public bool TryPlayRandomCard()
    {
        if (cardsInHand.Count == 0) return false;

        List<int> playable = new();
        for (int i = 0; i < cardsInHand.Count; i++)
        {
            if (characterInfo.GetEnergy() >= cardsInHand[i].baseEnergyCost)
                playable.Add(i);
        }

        if (playable.Count == 0) return false;

        int index = playable[Random.Range(0, playable.Count)];
        CardData data = cardsInHand[index];

        // Play appropriate sound based on card type
        PlaySound(data.cardType == CardType.Attack ? playAttackCardSound : playNonAttackCardSound);

        characterInfo.SpendEnergy(data.baseEnergyCost);

        _cardBeingPlayed = data;
        ResolveCardActions(data);
        _cardBeingPlayed = null;

        // Notify listeners which type of card was played (for sprite swaps)
        characterInfo.NotifyCardPlayed(data.cardType);

        // Card actions may have altered the hand — find the card by reference instead of stale index
        int removeIndex = cardsInHand.IndexOf(data);
        if (removeIndex >= 0)
        {
            cardsInHand.RemoveAt(removeIndex);
            discardPile.Add(data);
        }

        Debug.Log($"Enemy played: {data.cardName}");
        return true;
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
		if (!showCardVisuals)
			return;

		GameObject go = Instantiate(cardPrefab, handRoot);
		var view = go.GetComponent<CardView>();
		var card = go.GetComponent<Card>();

		card.SetCardData(data);
		AddCard(view);
	}

    #endregion

    #region Card Selection & Input

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = Mathf.Abs(Camera.main.transform.position.z);
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;
        return worldPos;
    }

    private void HandleDragInput()
    {
        if (Camera.main == null) return;

        Vector3 mouseWorld = GetMouseWorldPosition();

        // Start drag on click
        if (Input.GetMouseButtonDown(0) && !_isDragging)
        {
            if (!isPlayer)
            {
                return;
            }
            if (!arena._isPlayerTurn)
            {
                return;
            }

            if (cards.Count == 0 || !IsValidSelection()) return;

            // Check if the selected card was clicked
            CardView selectedCard = cards[selectedIndex];
            Collider2D col = selectedCard.GetComponentInChildren<Collider2D>();
            if (col == null) return;

            Collider2D[] hits = Physics2D.OverlapPointAll(mouseWorld);
            bool hitSelected = false;
            foreach (var hit in hits)
            {
                CardView view = hit.GetComponentInParent<CardView>();
                if (view == selectedCard)
                {
                    hitSelected = true;
                    break;
                }
            }

            if (!hitSelected)
            {
                return;
            }

            _isDragging = true;
            _draggedCard = selectedCard;
            _dragStartWorldPos = selectedCard.transform.position;
        }

        // Follow mouse while dragging
        if (_isDragging && Input.GetMouseButton(0))
        {
            Vector3 pos = mouseWorld;
            pos.z = _draggedCard.transform.position.z;
            _draggedCard.transform.position = pos;
            _draggedCard.transform.localRotation = Quaternion.identity;
        }

        // Release - play card if dragged high enough
        if (_isDragging && Input.GetMouseButtonUp(0))
        {
            float dragDelta = _draggedCard.transform.position.y - _dragStartWorldPos.y;

            if (dragDelta >= dragPlayThreshold)
            {
                UseCard();
                arena.CheckBattleOver();
            }

            // Card snaps back via RefreshLayout on next frame
            _isDragging = false;
            _draggedCard = null;
        }
    }

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

            // Disable collider on non-selected cards so they can't be hovered/picked by mouse
            var col = card.GetComponentInChildren<Collider2D>();
            if (col != null) col.enabled = isSelected;

            // Don't override position of the card being dragged
            if (_isDragging && card == _draggedCard)
            {
                card.transform.localScale = Vector3.Lerp(card.transform.localScale, targetScale, Time.deltaTime * smoothSpeed);
                card.SetSortingOrder(baseOrder);
                continue;
            }

            card.transform.localPosition = Vector3.Lerp(card.transform.localPosition, targetPos, Time.deltaTime * smoothSpeed);
            card.transform.localScale = Vector3.Lerp(card.transform.localScale, targetScale, Time.deltaTime * smoothSpeed);
            card.SetSortingOrder(baseOrder);
        }
    }

    #endregion
}