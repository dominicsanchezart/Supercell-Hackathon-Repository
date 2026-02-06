using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main shop UI controller. Spawns card slots, handles buy/remove flow.
/// </summary>
public class ShopView : MonoBehaviour
{
	[Header("References")]
	public Transform cardSlotContainer;
	public GameObject cardSlotPrefab;
	public TextMeshProUGUI goldDisplay;
	public Button leaveButton;

	[Header("Card Removal")]
	public GameObject removalPanel;
	public Transform removalCardContainer;
	public GameObject removalCardPrefab;
	public Button cancelRemovalButton;

	[Header("Confirmation")]
	public GameObject confirmPanel;
	public TextMeshProUGUI confirmText;
	public Button confirmYesButton;
	public Button confirmNoButton;

	List<ShopCardSlot> slots = new();
	List<ShopItem> shopItems;
	ShopItem pendingPurchase;
	ShopCardSlot pendingSlot;

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
				slot.Initialize(shopItems[i], this);
				slots.Add(slot);
			}
		}
	}

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

	// --- Card Removal ---

	void ShowRemovalPanel()
	{
		if (removalPanel == null) return;

		removalPanel.SetActive(true);

		// Clear existing
		if (removalCardContainer != null)
		{
			foreach (Transform child in removalCardContainer)
				Destroy(child.gameObject);
		}

		RunState state = RunManager.Instance?.State;
		if (state == null || removalCardPrefab == null) return;

		// Spawn a button for each card in the deck
		for (int i = 0; i < state.deck.Count; i++)
		{
			CardData card = state.deck[i];
			int index = i;

			GameObject obj = Instantiate(removalCardPrefab, removalCardContainer);

			// Set up the card display
			TextMeshProUGUI nameText = obj.GetComponentInChildren<TextMeshProUGUI>();
			if (nameText != null)
				nameText.text = card.cardName;

			Button btn = obj.GetComponent<Button>();
			if (btn != null)
				btn.onClick.AddListener(() => OnRemoveCardSelected(index));
		}
	}

	void HideRemovalPanel()
	{
		if (removalPanel != null)
			removalPanel.SetActive(false);
	}

	void OnRemoveCardSelected(int deckIndex)
	{
		RunState state = RunManager.Instance?.State;
		if (state == null) return;
		if (deckIndex < 0 || deckIndex >= state.deck.Count) return;

		// Find the removal slot
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
