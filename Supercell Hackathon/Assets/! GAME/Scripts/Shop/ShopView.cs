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

	[Header("Confirmation")]
	public GameObject confirmPanel;
	public TextMeshProUGUI confirmText;
	public Button confirmYesButton;
	public Button confirmNoButton;

	// Card viewer (found at runtime from Main Camera child)
	private CardViewer cardViewer;

	List<ShopCardSlot> slots = new();
	List<ShopItem> shopItems;
	ShopItem pendingPurchase;
	ShopCardSlot pendingSlot;
	bool isRemovalOpen;

	/// <summary>
	/// When true, all card slot clicks and hover are disabled.
	/// Set by popups (confirm panel, removal panel).
	/// Checked by ShopCardSlot.OnMouseDown().
	/// </summary>
	public bool IsBlocked { get; private set; }

	public void Initialize(List<ShopItem> items)
	{
		// Find the CardViewer on the Main Camera
		cardViewer = CardViewer.Instance;

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

		// Wire CardViewer callbacks for card removal
		if (cardViewer != null)
		{
			cardViewer.onHideCards += OnRemovalViewerClosed;
			cardViewer.onCardSelected += OnRemovalCardClicked;
		}

		if (confirmPanel != null)
			confirmPanel.SetActive(false);
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

	// ─── Card Removal (via CardViewer) ────────────────────────────

	void ShowRemovalPanel()
	{
		if (cardViewer == null) return;

		IsBlocked = true;
		isRemovalOpen = true;

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		// Feed the player's deck into CardViewer — it handles layout, scroll, hover, backdrop
		cardViewer.DisplayCards(state.deck.ToArray());
	}

	void HideRemovalPanel()
	{
		if (cardViewer != null)
			cardViewer.HideCards();
	}

	/// <summary>
	/// Called when a card is clicked inside the CardViewer during removal mode.
	/// </summary>
	void OnRemovalCardClicked(int cardIndex)
	{
		RunState state = RunManager.Instance?.State;
		if (state == null) return;
		if (cardIndex < 0 || cardIndex >= state.deck.Count) return;

		// Find the removal shop item to charge gold
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

		// Deduct gold and remove the card
		state.gold -= cost;
		state.deck.RemoveAt(cardIndex);
		state.cardRemoveCount++;

		if (removalSlot != null)
			removalSlot.MarkSold();

		// Close the viewer and refresh shop
		HideRemovalPanel();
		RefreshGoldDisplay();
		RefreshAllPrices();
	}

	/// <summary>
	/// Called when the CardViewer is closed (ESC key or HideCards).
	/// Restores shop interaction.
	/// </summary>
	void OnRemovalViewerClosed()
	{
		if (!isRemovalOpen) return;
		isRemovalOpen = false;

		// Backdrop and raycaster are managed by CardViewer itself
		IsBlocked = false;
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

	private void OnDestroy()
	{
		// Clear callbacks so the persistent CardViewer doesn't hold stale delegates
		if (cardViewer != null)
			cardViewer.ClearCallbacks();
	}
}
