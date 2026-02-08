using UnityEngine;
using TMPro;

/// <summary>
/// Reusable, skinnable card-shaped UI element for presenting choices.
/// Not a real game card — it's an option that looks like a card.
///
/// Used across multiple systems:
///   Camp      — Rest / Upgrade options
///   Shop      — Remove a Card slot
///   Rewards   — Post-battle card picks
///   Treasure  — Loot choices
///   Events    — Branching story options
///
/// Visual structure (all children in prefab):
///   Background  — SpriteRenderer (card frame, tinted per skin)
///   Icon        — SpriteRenderer (campfire, anvil, trash icon, etc.)
///   Title       — TextMeshPro (e.g. "Rest", "Upgrade")
///   Description — TextMeshPro (e.g. "Heal 30% of max HP")
///
/// Interaction follows the ShopCardSlot pattern:
///   OnMouseEnter/Exit  — hover scale animation
///   OnMouseDown        — fires onClicked callback
///   SetInteractable()  — grey out + disable collider
/// </summary>
public class OptionCard : MonoBehaviour
{
	[Header("Display")]
	public SpriteRenderer backgroundSprite;
	[Tooltip("Optional 9-sliced frame border rendered on top of the background. Does not affect interaction.")]
	public SpriteRenderer frameSprite;
	public SpriteRenderer iconSprite;
	public TextMeshPro titleText;
	public TextMeshPro descriptionText;

	[Header("Hover")]
	public float hoverScale = 1.1f;
	public float hoverSmooth = 12f;

	[Header("Interaction")]
	public Collider2D cardCollider;

	[Header("Disabled Appearance")]
	[Tooltip("Color applied to background when disabled.")]
	public Color disabledTint = new Color(0.4f, 0.4f, 0.4f, 0.7f);

	/// <summary>
	/// Fired when the player clicks this option card.
	/// Subscribers receive a reference to the clicked card.
	/// </summary>
	public System.Action<OptionCard> onClicked;

	bool interactable = true;
	bool isHovered;
	Vector3 baseScale;
	Color originalTint;
	Color originalFrameTint;

	void Awake()
	{
		baseScale = transform.localScale;

		if (backgroundSprite != null)
			originalTint = backgroundSprite.color;

		if (frameSprite != null)
			originalFrameTint = frameSprite.color;
	}

	// ─── Skinning ─────────────────────────────────────────────────

	/// <summary>
	/// Configure the card's visuals. Call this after instantiation or on scene start.
	/// </summary>
	public void Setup(string title, string description, Sprite icon = null, Color? tint = null)
	{
		if (titleText != null)
			titleText.text = title;

		if (descriptionText != null)
			descriptionText.text = description;

		if (iconSprite != null)
		{
			if (icon != null)
			{
				iconSprite.sprite = icon;
				iconSprite.enabled = true;
			}
			else
			{
				iconSprite.enabled = false;
			}
		}

		if (backgroundSprite != null && tint.HasValue)
		{
			backgroundSprite.color = tint.Value;
			originalTint = tint.Value;
		}
	}

	// ─── Interactability ──────────────────────────────────────────

	/// <summary>
	/// Enable or disable this option card. Disabled cards are greyed out
	/// and do not respond to hover or clicks.
	/// </summary>
	public void SetInteractable(bool value)
	{
		interactable = value;

		if (cardCollider != null)
			cardCollider.enabled = value;

		if (!value)
		{
			isHovered = false;

			if (backgroundSprite != null)
				backgroundSprite.color = disabledTint;

			if (frameSprite != null)
			{
				Color c = originalFrameTint;
				c.a = 0.4f;
				frameSprite.color = c;
			}

			if (iconSprite != null)
			{
				Color c = iconSprite.color;
				c.a = 0.4f;
				iconSprite.color = c;
			}

			if (titleText != null)
			{
				Color c = titleText.color;
				c.a = 0.5f;
				titleText.color = c;
			}

			if (descriptionText != null)
			{
				Color c = descriptionText.color;
				c.a = 0.5f;
				descriptionText.color = c;
			}
		}
		else
		{
			if (backgroundSprite != null)
				backgroundSprite.color = originalTint;

			if (frameSprite != null)
				frameSprite.color = originalFrameTint;

			if (iconSprite != null)
			{
				Color c = iconSprite.color;
				c.a = 1f;
				iconSprite.color = c;
			}

			if (titleText != null)
			{
				Color c = titleText.color;
				c.a = 1f;
				titleText.color = c;
			}

			if (descriptionText != null)
			{
				Color c = descriptionText.color;
				c.a = 1f;
				descriptionText.color = c;
			}
		}
	}

	public bool IsInteractable => interactable;

	// ─── Hover & Click ────────────────────────────────────────────

	void Update()
	{
		Vector3 target = isHovered ? baseScale * hoverScale : baseScale;
		transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * hoverSmooth);
	}

	void OnMouseEnter()
	{
		if (!interactable) return;
		isHovered = true;
	}

	void OnMouseExit()
	{
		isHovered = false;
	}

	void OnMouseDown()
	{
		if (!interactable) return;
		onClicked?.Invoke(this);
	}
}
