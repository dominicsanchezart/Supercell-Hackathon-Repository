using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main shop UI controller. Spawns card slots in a two-row layout:
///   Top row:    Patron + Neutral cards (6 slots)
///   Bottom row: Items + Card Removal  (3 slots)
/// Handles buy/remove flow and the card removal deck picker.
/// Uses the real Card.prefab for all card displays.
///
/// UI panels (confirm, removal, gold, leave) should live on a single
/// Screen Space - Overlay Canvas so buttons work reliably.
/// Cards are world-space sprites clicked via OnMouseDown + Collider2D.
/// When a popup is open, IsBlocked = true prevents card clicks and hover.
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
	public float slotSpacing = 2.8f;
	[Tooltip("Y position of the top row (patron + neutral cards).")]
	public float topRowY = 1.5f;
	[Tooltip("Y position of the bottom row (items + card removal).")]
	public float bottomRowY = -2.5f;

	[Header("Card Removal")]
	public GameObject removalPanel;
	public Transform removalCardContainer;
	[Tooltip("Fallback prefab if Card.prefab is null.")]
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

	List<ShopCardSlot> slots = new();
	List<ShopItem> shopItems;
	ShopItem pendingPurchase;
	ShopCardSlot pendingSlot;

	/// <summary>
	/// When true, all card slot clicks and hover are disabled.
	/// Set by popups (confirm panel, removal panel).
	/// Checked by ShopCardSlot.OnMouseDown().
	/// </summary>
	public bool IsBlocked { get; private set; }

	public void Initialize(List<ShopItem> items)
	{
		shopItems = items;
		IsBlocked = false;
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

		LayoutSlots();
	}

	/// <summary>
	/// Two-row layout:
	///   Top row:    PatronCard + NeutralCard slots
	///   Bottom row: ItemCard + CardRemoval slots
	/// Each row is independently centered.
	/// </summary>
	void LayoutSlots()
	{
		if (slots.Count == 0) return;

		List<ShopCardSlot> topRow = new();
		List<ShopCardSlot> bottomRow = new();

		for (int i = 0; i < slots.Count; i++)
		{
			ShopItem item = shopItems[i];
			if (item.slotType == ShopItem.SlotType.ItemCard ||
				item.slotType == ShopItem.SlotType.CardRemoval)
			{
				bottomRow.Add(slots[i]);
			}
			else
			{
				topRow.Add(slots[i]);
			}
		}

		LayoutRow(topRow, topRowY);
		LayoutRow(bottomRow, bottomRowY);
	}

	void LayoutRow(List<ShopCardSlot> row, float y)
	{
		int count = row.Count;
		if (count == 0) return;

		float totalWidth = (count - 1) * slotSpacing;
		float startX = -totalWidth * 0.5f;

		for (int i = 0; i < count; i++)
		{
			row[i].transform.localPosition = new Vector3(
				startX + i * slotSpacing,
				y,
				0f
			);
		}
	}

	// ─── Buy Flow ─────────────────────────────────────────────────

	public void OnSlotClicked(ShopCardSlot slot, ShopItem item)
	{
		if (IsBlocked) return;
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

		IsBlocked = true;
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
		IsBlocked = false;
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

		IsBlocked = true;
		removalPanel.SetActive(true);

		// Clear existing cards in the removal panel
		if (removalCardContainer != null)
		{
			foreach (Transform child in removalCardContainer)
				Destroy(child.gameObject);
		}

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		for (int i = 0; i < state.deck.Count; i++)
		{
			CardData cardData = state.deck[i];
			int index = i;

			GameObject cardObj = null;

			if (cardPrefab != null)
			{
				cardObj = Instantiate(cardPrefab, removalCardContainer);

				Card card = cardObj.GetComponent<Card>();
				if (card != null)
					card.SetCardData(cardData);

				CardView cardView = cardObj.GetComponent<CardView>();
				if (cardView != null)
					cardView.enabled = false;

				cardObj.transform.localScale = Vector3.one * 0.75f;
			}
			else if (removalCardPrefab != null)
			{
				cardObj = Instantiate(removalCardPrefab, removalCardContainer);

				TextMeshProUGUI nameText = cardObj.GetComponentInChildren<TextMeshProUGUI>();
				if (nameText != null)
					nameText.text = cardData.cardName;
			}

			if (cardObj == null) continue;

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

			Collider2D col2d = cardObj.GetComponent<Collider2D>();
			if (col2d != null)
			{
				RemovalCardClick click = cardObj.AddComponent<RemovalCardClick>();
				click.Initialize(this, index);
			}
		}
	}

	void HideRemovalPanel()
	{
		if (removalPanel != null)
			removalPanel.SetActive(false);

		IsBlocked = false;
	}

	/// <summary>
	/// Called by RemovalCardClick when a card in the removal panel is clicked.
	/// </summary>
	public void OnRemoveCardSelected(int deckIndex)
	{
		RunState state = RunManager.Instance?.State;
		if (state == null) return;
		if (deckIndex < 0 || deckIndex >= state.deck.Count) return;

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

		state.gold -= cost;
		state.deck.RemoveAt(deckIndex);
		state.cardRemoveCount++;

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
		if (IsBlocked) return;

		if (RunManager.Instance != null)
			RunManager.Instance.OnEncounterComplete();
	}
}
