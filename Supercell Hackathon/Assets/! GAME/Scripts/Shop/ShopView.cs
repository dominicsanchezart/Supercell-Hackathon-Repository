using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main shop UI controller. Spawns card slots in a horizontal row,
/// handles buy/remove flow, and manages the card removal deck picker.
/// Uses the real Card.prefab for all card displays.
/// </summary>
public class ShopView : MonoBehaviour
{
	[Header("References")]
	public Transform cardSlotContainer;
	public GameObject cardSlotPrefab;
	public TextMeshProUGUI goldDisplay;
	public Button leaveButton;

	[Header("Card Prefab (passed to slots)")]
	[Tooltip("The actual Card.prefab — passed to each ShopCardSlot for spawning visuals.")]
	public GameObject cardPrefab;

	[Header("Slot Layout")]
	[Tooltip("Horizontal distance between each shop slot.")]
	public float slotSpacing = 2.2f;
	[Tooltip("Vertical offset for the slot row.")]
	public float slotRowY = 0f;

	[Header("Card Removal")]
	public GameObject removalPanel;
	public Transform removalCardContainer;
	[Tooltip("Card.prefab reference — removal panel also spawns real cards.")]
	public GameObject removalCardPrefab;
	public Button cancelRemovalButton;
	[Tooltip("Spacing between cards in the removal grid.")]
	public float removalSpacing = 2f;
	[Tooltip("Cards per row in the removal grid.")]
	public int removalCardsPerRow = 5;
	[Tooltip("Vertical spacing between rows in the removal grid.")]
	public float removalRowSpacing = 2.8f;

	[Header("Confirmation")]
	public GameObject confirmPanel;
	public TextMeshProUGUI confirmText;
	public Button confirmYesButton;
	public Button confirmNoButton;

	[Header("Hover")]
	[Tooltip("Scale multiplier when hovering a slot.")]
	public float hoverScale = 1.1f;
	public float hoverSmooth = 12f;
	public LayerMask shopLayer;

	List<ShopCardSlot> slots = new();
	List<ShopItem> shopItems;
	ShopItem pendingPurchase;
	ShopCardSlot pendingSlot;

	// Hover state
	Transform hoveredSlot;
	Vector3 hoveredOriginalScale;

	public void Initialize(List<ShopItem> items)
	{
		shopItems = items;
		ClearSlots();
		SpawnSlots();
		RefreshGoldDisplay();

		if (leaveButton != null)
			leaveButton.onClick.AddListener(OnLeaveClicked);

		if (confirmNoButton != null)
			confirmNoButton.onClick.AddListener(HideConfirmation);

		if (confirmYesButton != null)
			confirmYesButton.onClick.AddListener(OnConfirmPurchase);

		if (cancelRemovalButton != null)
			cancelRemovalButton.onClick.AddListener(HideRemovalPanel);

		if (confirmPanel != null)
			confirmPanel.SetActive(false);

		if (removalPanel != null)
			removalPanel.SetActive(false);
	}

	void Update()
	{
		HandleHover();
	}

	// ─── Slot Spawning & Layout ───────────────────────────────────

	void ClearSlots()
	{
		for (int i = 0; i < slots.Count; i++)
		{
			if (slots[i] != null)
				Destroy(slots[i].gameObject);
		}
		slots.Clear();
	}

	void SpawnSlots()
	{
		if (cardSlotPrefab == null || cardSlotContainer == null) return;

		for (int i = 0; i < shopItems.Count; i++)
		{
			GameObject obj = Instantiate(cardSlotPrefab, cardSlotContainer);
			ShopCardSlot slot = obj.GetComponent<ShopCardSlot>();
			if (slot != null)
			{
				// Pass the card prefab reference so the slot can spawn real cards
				if (slot.cardPrefab == null)
					slot.cardPrefab = cardPrefab;

				slot.Initialize(shopItems[i], this);
				slots.Add(slot);
			}
		}

		// Position slots in a centered horizontal row
		LayoutSlots();
	}

	void LayoutSlots()
	{
		int count = slots.Count;
		if (count == 0) return;

		float totalWidth = (count - 1) * slotSpacing;
		float startX = -totalWidth * 0.5f;

		for (int i = 0; i < count; i++)
		{
			slots[i].transform.localPosition = new Vector3(
				startX + i * slotSpacing,
				slotRowY,
				0f
			);
		}
	}

	// ─── Hover ────────────────────────────────────────────────────

	void HandleHover()
	{
		if (Camera.main == null) return;

		Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
		mouseWorld.z = 0f;

		Collider2D hit = Physics2D.OverlapPoint(mouseWorld, shopLayer);

		if (hit != null)
		{
			ShopCardSlot slot = hit.GetComponentInParent<ShopCardSlot>();
			if (slot != null)
			{
				Transform slotTransform = slot.transform;
				if (hoveredSlot != slotTransform)
				{
					EndHover();
					hoveredSlot = slotTransform;
					hoveredOriginalScale = slotTransform.localScale;
				}
			}
			else
			{
				EndHover();
			}
		}
		else
		{
			EndHover();
		}

		if (hoveredSlot != null)
		{
			hoveredSlot.localScale = Vector3.Lerp(
				hoveredSlot.localScale,
				hoveredOriginalScale * hoverScale,
				Time.deltaTime * hoverSmooth
			);
		}
	}

	void EndHover()
	{
		if (hoveredSlot == null) return;
		hoveredSlot.localScale = hoveredOriginalScale;
		hoveredSlot = null;
	}

	// ─── Buy Flow ─────────────────────────────────────────────────

	public void OnSlotClicked(ShopCardSlot slot, ShopItem item)
	{
		if (item.isSold) return;

		int displayPrice = item.GetDisplayPrice();
		int playerGold = RunManager.Instance != null ? RunManager.Instance.State.gold : 0;

		if (playerGold < displayPrice)
		{
			Debug.Log("Not enough gold!");
			return;
		}

		if (item.slotType == ShopItem.SlotType.CardRemoval)
		{
			ShowRemovalPanel();
			return;
		}

		// Show confirmation for card purchases
		pendingPurchase = item;
		pendingSlot = slot;
		ShowConfirmation(item);
	}

	void ShowConfirmation(ShopItem item)
	{
		if (confirmPanel == null) return;

		confirmPanel.SetActive(true);

		if (confirmText != null)
		{
			string name = item.card != null ? item.card.cardName : "this item";
			int price = item.GetDisplayPrice();
			confirmText.text = $"Buy {name} for {price}g?";
		}
	}

	void HideConfirmation()
	{
		if (confirmPanel != null)
			confirmPanel.SetActive(false);

		pendingPurchase = null;
		pendingSlot = null;
	}

	void OnConfirmPurchase()
	{
		if (pendingPurchase == null) return;

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		int cost = pendingPurchase.GetDisplayPrice();
		if (state.gold < cost) return;

		// Deduct gold
		state.gold -= cost;

		// Add card to deck
		if (pendingPurchase.card != null)
			state.deck.Add(pendingPurchase.card);

		// Mark as sold
		if (pendingSlot != null)
			pendingSlot.MarkSold();

		HideConfirmation();
		RefreshGoldDisplay();
		RefreshAllPrices();
	}

	// ─── Card Removal ─────────────────────────────────────────────

	void ShowRemovalPanel()
	{
		if (removalPanel == null) return;

		removalPanel.SetActive(true);

		// Clear existing cards in the removal panel
		if (removalCardContainer != null)
		{
			foreach (Transform child in removalCardContainer)
				Destroy(child.gameObject);
		}

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		// Spawn a real Card.prefab for each card in the player's deck
		for (int i = 0; i < state.deck.Count; i++)
		{
			CardData cardData = state.deck[i];
			int index = i;

			// Spawn the card visual
			GameObject cardObj = null;

			if (cardPrefab != null)
			{
				// Use real Card.prefab
				cardObj = Instantiate(cardPrefab, removalCardContainer);

				Card card = cardObj.GetComponent<Card>();
				if (card != null)
					card.SetCardData(cardData);

				// Disable the hand-related CardView so it doesn't interfere
				CardView cardView = cardObj.GetComponent<CardView>();
				if (cardView != null)
					cardView.enabled = false;

				// Scale down slightly for the grid
				cardObj.transform.localScale = Vector3.one * 0.75f;
			}
			else if (removalCardPrefab != null)
			{
				// Fallback: simple text button
				cardObj = Instantiate(removalCardPrefab, removalCardContainer);

				TextMeshProUGUI nameText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
				if (nameText != null)
					nameText.text = cardData.cardName;
			}

			if (cardObj == null) continue;

			// Position in a grid
			int row = i / removalCardsPerRow;
			int col = i % removalCardsPerRow;

			int colsThisRow = Mathf.Min(removalCardsPerRow, state.deck.Count - row * removalCardsPerRow);
			float rowWidth = (colsThisRow - 1) * removalSpacing;
			float startX = -rowWidth * 0.5f;

			cardObj.transform.localPosition = new Vector3(
				startX + col * removalSpacing,
				-row * removalRowSpacing,
				0f
			);

			// Make clickable — use existing Collider2D on the Card.prefab, or add one
			Collider2D col2d = cardObj.GetComponent<Collider2D>();
			if (col2d != null)
			{
				// Add a RemovalCardClick helper to route the click
				RemovalCardClick click = cardObj.AddComponent<RemovalCardClick>();
				click.Initialize(this, index);
			}
		}
	}

	void HideRemovalPanel()
	{
		if (removalPanel != null)
			removalPanel.SetActive(false);
	}

	/// <summary>
	/// Called by RemovalCardClick when a card in the removal panel is clicked.
	/// </summary>
	public void OnRemoveCardSelected(int deckIndex)
	{
		RunState state = RunManager.Instance?.State;
		if (state == null) return;
		if (deckIndex < 0 || deckIndex >= state.deck.Count) return;

		// Find the removal shop slot
		ShopItem removalItem = null;
		ShopCardSlot removalSlot = null;
		for (int i = 0; i < shopItems.Count; i++)
		{
			if (shopItems[i].slotType == ShopItem.SlotType.CardRemoval && !shopItems[i].isSold)
			{
				removalItem = shopItems[i];
				removalSlot = slots[i];
				break;
			}
		}

		if (removalItem == null) return;

		int cost = removalItem.GetDisplayPrice();
		if (state.gold < cost) return;

		// Deduct gold and remove card
		state.gold -= cost;
		state.deck.RemoveAt(deckIndex);
		state.cardRemoveCount++;

		// Mark removal as sold (one-use per shop)
		if (removalSlot != null)
			removalSlot.MarkSold();

		HideRemovalPanel();
		RefreshGoldDisplay();
		RefreshAllPrices();
	}

	// ─── UI Helpers ───────────────────────────────────────────────

	void RefreshGoldDisplay()
	{
		if (goldDisplay == null) return;

		int gold = RunManager.Instance != null ? RunManager.Instance.State.gold : 0;
		goldDisplay.text = $"{gold}g";
	}

	void RefreshAllPrices()
	{
		for (int i = 0; i < slots.Count; i++)
		{
			if (slots[i] != null)
				slots[i].RefreshPriceDisplay();
		}
	}

	void OnLeaveClicked()
	{
		if (RunManager.Instance != null)
			RunManager.Instance.OnEncounterComplete();
	}
}
