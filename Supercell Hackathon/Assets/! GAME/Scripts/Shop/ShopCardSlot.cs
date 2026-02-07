using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// A single slot in the shop. Spawns the real Card.prefab to display visuals,
/// then overlays price / sale / sold UI on top.
/// Handles its own hover via OnMouseEnter/OnMouseExit (Collider2D).
/// </summary>
public class ShopCardSlot : MonoBehaviour
{
	[Header("Card Display")]
	[Tooltip("The actual Card.prefab used in battle — spawned here for visuals.")]
	public GameObject cardPrefab;
	[Tooltip("Child transform where the Card.prefab instance is parented.")]
	public Transform cardAnchor;
	[Tooltip("Scale applied to the spawned card (adjust to fit the slot).")]
	public float cardScale = 1f;

	[Header("Removal Fallback (no card to show)")]
	[Tooltip("Root object for the text-only removal display. Disabled for card slots.")]
	public GameObject removalDisplay;
	public TextMeshProUGUI removalTitleText;
	public TextMeshProUGUI removalDescText;

	[Header("Price & Status")]
	public TextMeshProUGUI priceText;
	public GameObject saleTag;
	public GameObject soldOverlay;

	[Header("Interaction")]
	public Collider2D slotCollider;

	[Header("Hover")]
	public float hoverScale = 1.1f;
	public float hoverSmooth = 12f;

	[Header("Colors")]
	public Color affordableColor = Color.white;
	public Color tooExpensiveColor = new Color(0.7f, 0.3f, 0.3f, 1f);
	public Color saleColor = new Color(0.2f, 0.9f, 0.2f, 1f);

	ShopItem shopItem;
	ShopView shopView;
	GameObject spawnedCard;

	// Hover state
	bool isHovered;
	Vector3 baseScale;

	public void Initialize(ShopItem item, ShopView owner)
	{
		shopItem = item;
		shopView = owner;
		baseScale = transform.localScale;

		if (item.slotType == ShopItem.SlotType.CardRemoval)
			SetupAsRemoval(item);
		else
			SetupAsCard(item);

		if (soldOverlay != null)
			soldOverlay.SetActive(false);

		if (saleTag != null)
			saleTag.SetActive(item.isOnSale);

		RefreshPriceDisplay();
	}

	void Update()
	{
		// Smooth hover scale
		Vector3 target = isHovered ? baseScale * hoverScale : baseScale;
		transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * hoverSmooth);
	}

	void SetupAsCard(ShopItem item)
	{
		if (removalDisplay != null)
			removalDisplay.SetActive(false);

		if (item.card == null) return;

		if (cardPrefab != null && cardAnchor != null)
		{
			spawnedCard = Object.Instantiate(cardPrefab, cardAnchor);
			spawnedCard.transform.localPosition = Vector3.zero;
			spawnedCard.transform.localRotation = Quaternion.identity;
			spawnedCard.transform.localScale = Vector3.one * cardScale;

			Card card = spawnedCard.GetComponent<Card>();
			if (card != null)
				card.SetCardData(item.card);

			Collider2D cardCol = spawnedCard.GetComponent<Collider2D>();
			if (cardCol != null)
				cardCol.enabled = false;

			CardView cardView = spawnedCard.GetComponent<CardView>();
			if (cardView != null)
				cardView.enabled = false;
		}
	}

	void SetupAsRemoval(ShopItem item)
	{
		if (removalDisplay != null)
			removalDisplay.SetActive(true);

		if (removalTitleText != null)
			removalTitleText.text = "Remove a Card";

		if (removalDescText != null)
			removalDescText.text = "Remove one card\nfrom your deck.";
	}

	public void RefreshPriceDisplay()
	{
		if (shopItem == null || shopItem.isSold) return;

		int displayPrice = shopItem.GetDisplayPrice();

		if (priceText != null)
		{
			priceText.text = $"{displayPrice}g";

			int playerGold = RunManager.Instance != null ? RunManager.Instance.State.gold : 0;

			if (shopItem.isOnSale)
				priceText.color = saleColor;
			else if (playerGold >= displayPrice)
				priceText.color = affordableColor;
			else
				priceText.color = tooExpensiveColor;
		}
	}

	// ─── Input ────────────────────────────────────────────────────

	void OnMouseEnter()
	{
		if (shopView != null && shopView.IsBlocked) return;
		if (shopItem != null && shopItem.isSold) return;
		isHovered = true;
	}

	void OnMouseExit()
	{
		isHovered = false;
	}

	void OnMouseDown()
	{
		if (shopView != null && shopView.IsBlocked) return;
		OnClicked();
	}

	public void OnClicked()
	{
		if (shopItem == null || shopItem.isSold) return;
		if (shopView != null)
			shopView.OnSlotClicked(this, shopItem);
	}

	public void MarkSold()
	{
		shopItem.isSold = true;
		isHovered = false;

		if (soldOverlay != null)
			soldOverlay.SetActive(true);

		if (spawnedCard != null)
		{
			SpriteRenderer[] renderers = spawnedCard.GetComponentsInChildren<SpriteRenderer>();
			for (int i = 0; i < renderers.Length; i++)
			{
				Color c = renderers[i].color;
				c.a = 0.35f;
				renderers[i].color = c;
			}
		}

		if (slotCollider != null)
			slotCollider.enabled = false;
	}

	public ShopItem GetShopItem() => shopItem;

	void OnDestroy()
	{
		if (spawnedCard != null)
			Destroy(spawnedCard);
	}
}
