using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI slot for a single item in the shop.
/// Displays card info, price, and handles buy interaction.
/// </summary>
public class ShopCardSlot : MonoBehaviour
{
	[Header("UI References")]
	public Image cardImage;
	public TextMeshProUGUI cardNameText;
	public TextMeshProUGUI priceText;
	public TextMeshProUGUI descriptionText;
	public TextMeshProUGUI energyCostText;
	public GameObject saleTag;
	public GameObject soldOverlay;
	public Button buyButton;

	[Header("Colors")]
	public Color affordableColor = Color.white;
	public Color tooExpensiveColor = new Color(0.7f, 0.3f, 0.3f, 1f);
	public Color saleColor = new Color(0.2f, 0.9f, 0.2f, 1f);

	ShopItem shopItem;
	ShopView shopView;

	public void Initialize(ShopItem item, ShopView owner)
	{
		shopItem = item;
		shopView = owner;

		if (item.slotType == ShopItem.SlotType.CardRemoval)
		{
			SetupAsRemoval(item);
		}
		else
		{
			SetupAsCard(item);
		}

		if (soldOverlay != null)
			soldOverlay.SetActive(false);

		if (saleTag != null)
			saleTag.SetActive(item.isOnSale);

		if (buyButton != null)
			buyButton.onClick.AddListener(OnBuyClicked);

		RefreshPriceDisplay();
	}

	void SetupAsCard(ShopItem item)
	{
		if (item.card == null) return;

		if (cardNameText != null)
			cardNameText.text = item.card.cardName;

		if (descriptionText != null)
			descriptionText.text = item.card.GetDescription();

		if (energyCostText != null)
			energyCostText.text = item.card.baseEnergyCost.ToString();

		// Use card's first visual layer as display image
		if (cardImage != null && item.card.layer0 != null)
			cardImage.sprite = item.card.layer0;
	}

	void SetupAsRemoval(ShopItem item)
	{
		if (cardNameText != null)
			cardNameText.text = "Remove a Card";

		if (descriptionText != null)
			descriptionText.text = "Remove one card from your deck permanently.";

		if (energyCostText != null)
			energyCostText.text = "";

		if (cardImage != null)
			cardImage.gameObject.SetActive(false);
	}

	public void RefreshPriceDisplay()
	{
		if (shopItem == null || shopItem.isSold) return;

		int displayPrice = shopItem.GetDisplayPrice();

		if (priceText != null)
		{
			priceText.text = $"{displayPrice}g";

			// Color based on affordability
			int playerGold = RunManager.Instance != null ? RunManager.Instance.State.gold : 0;

			if (shopItem.isOnSale)
				priceText.color = saleColor;
			else if (playerGold >= displayPrice)
				priceText.color = affordableColor;
			else
				priceText.color = tooExpensiveColor;
		}
	}

	void OnBuyClicked()
	{
		if (shopItem == null || shopItem.isSold) return;
		if (shopView != null)
			shopView.OnSlotClicked(this, shopItem);
	}

	public void MarkSold()
	{
		shopItem.isSold = true;

		if (soldOverlay != null)
			soldOverlay.SetActive(true);

		if (buyButton != null)
			buyButton.interactable = false;
	}

	public ShopItem GetShopItem() => shopItem;
}
