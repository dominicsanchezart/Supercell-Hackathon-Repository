using UnityEngine;

/// <summary>
/// Selectable patron thumbnail slot for the patron select screen.
/// Structured like the patron dialogue box with 3 layers:
///   Background (per-pact art, masked) → Portrait (masked) → Frame (9-sliced border on top)
///
/// The background and portrait are masked inside the frame interior using a SpriteMask child.
/// The frame renders on top, unmasked, providing the border.
///
/// Slots without patron data appear locked (blacked out, no interaction).
///
/// Follows the project's OnMouseDown callback pattern (like OptionCard, CardView).
///
/// Setup (prefab):
///   Root — PatronSlot (this script) + BoxCollider2D
///     ├── Background  — SpriteRenderer (per-pact art, Mask Interaction: Visible Inside Mask)
///     ├── Portrait    — SpriteRenderer (patron thumbnail, Mask Interaction: Visible Inside Mask)
///     ├── Frame       — SpriteRenderer (9-sliced border, Mask Interaction: None — renders on top)
///     └── Mask        — SpriteMask (square/rect matching the interior area of the frame)
/// </summary>
public class PatronSlot : MonoBehaviour
{
	[Header("References")]
	[Tooltip("Interior background — swapped per pact from PatronData.pactBackground, masked inside frame.")]
	[SerializeField] private SpriteRenderer backgroundSprite;
	[Tooltip("Patron portrait thumbnail — masked inside the frame.")]
	[SerializeField] private SpriteRenderer portraitSprite;
	[Tooltip("9-sliced frame border — swapped per patron from PatronData.dialogueFrameSprite. Renders on top, unmasked.")]
	[SerializeField] private SpriteRenderer frameSprite;
	[Tooltip("SpriteMask child that clips background + portrait inside the frame interior.")]
	[SerializeField] private SpriteMask interiorMask;
	[SerializeField] private Collider2D slotCollider;

	[Header("Sorting")]
	[Tooltip("Base sorting order. Each slot auto-offsets by index * 10 to isolate masks.")]
	[SerializeField] private int baseSortingOrder = 0;
	[Tooltip("Sorting layer for all sprites.")]
	[SerializeField] private string sortingLayerName = "Default";

	/// <summary>
	/// Spacing between each slot's sorting order range so masks don't bleed across slots.
	/// Background = base, Portrait = base+1, Frame = base+2. Mask covers base to base+1.
	/// </summary>
	private const int SORT_ORDER_STRIDE = 10;

	[Header("Appearance")]
	[Tooltip("Portrait alpha when unselected (dimmed).")]
	[SerializeField] private float unselectedPortraitAlpha = 0.5f;
	[Tooltip("Background alpha when unselected (dimmed).")]
	[SerializeField] private float unselectedBackgroundAlpha = 0.4f;
	[Tooltip("Frame tint when unselected (greyed out).")]
	[SerializeField] private Color unselectedFrameTint = new Color(0.4f, 0.4f, 0.4f, 1f);

	[Header("Locked Appearance")]
	[Tooltip("Background color when locked.")]
	[SerializeField] private Color lockedBackgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.9f);
	[Tooltip("Frame color when locked.")]
	[SerializeField] private Color lockedFrameColor = new Color(0.15f, 0.15f, 0.15f, 1f);
	[Tooltip("Portrait color when locked (dark/blacked out).")]
	[SerializeField] private Color lockedPortraitColor = new Color(0.05f, 0.05f, 0.05f, 1f);

	[Header("Scale")]
	[Tooltip("Uniform scale multiplier for the entire slot. Adjust to fit the select screen layout.")]
	[SerializeField] private float slotScale = 1f;

	[Header("Hover")]
	[SerializeField] private float hoverScale = 1.1f;
	[SerializeField] private float hoverSmooth = 12f;

	private System.Action<int> _onSelected;
	private int _index;
	private Color _patronColor;
	private bool _isSelected;
	private bool _isHovered;
	private bool _isLocked;
	private Vector3 _baseScale;

	void Awake()
	{
		transform.localScale *= slotScale;
		_baseScale = transform.localScale;
	}

	/// <summary>
	/// Assigns unique sorting orders per slot index so each slot's SpriteMask
	/// only affects its own background + portrait, preventing bleed between neighbors.
	///
	/// Each slot gets a range of SORT_ORDER_STRIDE (10) orders:
	///   Background = base + index*10
	///   Portrait   = base + index*10 + 1
	///   Frame      = base + index*10 + 2  (unmasked, renders on top)
	///   Mask range = [base + index*10, base + index*10 + 1]
	/// </summary>
	private void ApplySortingForIndex(int index)
	{
		int slotBase = baseSortingOrder + index * SORT_ORDER_STRIDE;

		if (backgroundSprite != null)
		{
			backgroundSprite.sortingLayerName = sortingLayerName;
			backgroundSprite.sortingOrder = slotBase;
			backgroundSprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
		}

		if (portraitSprite != null)
		{
			portraitSprite.sortingLayerName = sortingLayerName;
			portraitSprite.sortingOrder = slotBase + 1;
			portraitSprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
		}

		if (frameSprite != null)
		{
			frameSprite.sortingLayerName = sortingLayerName;
			frameSprite.sortingOrder = slotBase + 2;
			frameSprite.maskInteraction = SpriteMaskInteraction.None;
		}

		// Constrain the mask to only affect this slot's sorting order range
		if (interiorMask != null)
		{
			interiorMask.isCustomRangeActive = true;
			interiorMask.frontSortingLayerID = SortingLayer.NameToID(sortingLayerName);
			interiorMask.backSortingLayerID = SortingLayer.NameToID(sortingLayerName);
			interiorMask.frontSortingOrder = slotBase + 1; // covers background + portrait
			interiorMask.backSortingOrder = slotBase - 1;
		}
	}

	/// <summary>
	/// Configures the slot with patron data. Called by PatronSelectController on Start.
	/// Swaps the frame sprite, background art, and portrait per patron.
	/// </summary>
	public void Setup(PatronData patron, int index, System.Action<int> onSelected)
	{
		_index = index;
		_onSelected = onSelected;
		_isLocked = false;
		_patronColor = patron.patronColor;

		// Isolate this slot's sorting orders so its mask doesn't bleed into neighbors
		ApplySortingForIndex(index);

		// Swap frame to patron's dialogue frame sprite (9-sliced)
		if (frameSprite != null && patron.dialogueFrameSprite != null)
			frameSprite.sprite = patron.dialogueFrameSprite;

		// Set portrait
		if (portraitSprite != null)
			portraitSprite.sprite = patron.portrait;

		// Swap background to patron's pact background art (masked inside frame)
		if (backgroundSprite != null && patron.pactBackground != null)
			backgroundSprite.sprite = patron.pactBackground;

		if (slotCollider != null)
			slotCollider.enabled = true;

		SetSelected(false);
	}

	/// <summary>
	/// Configures the slot as locked (no patron data, coming soon teaser).
	/// Dark background, dim frame, blacked-out portrait, no interaction.
	/// </summary>
	public void SetupLocked(int index = 0)
	{
		_isLocked = true;
		_onSelected = null;

		// Isolate sorting even for locked slots to prevent mask bleed
		ApplySortingForIndex(index);

		if (backgroundSprite != null)
			backgroundSprite.color = lockedBackgroundColor;

		if (frameSprite != null)
			frameSprite.color = lockedFrameColor;

		if (portraitSprite != null)
			portraitSprite.color = lockedPortraitColor;

		if (slotCollider != null)
			slotCollider.enabled = false;
	}

	/// <summary>
	/// Toggles the selected visual state.
	/// Selected: patron color on frame, full brightness on portrait + background.
	/// Unselected: grey frame, dimmed portrait + background.
	/// No-op if the slot is locked.
	/// </summary>
	public void SetSelected(bool selected)
	{
		if (_isLocked) return;

		_isSelected = selected;

		// Frame — patron color when selected, greyed when not
		if (frameSprite != null)
		{
			Color fc = selected ? _patronColor : unselectedFrameTint;
			fc.a = 1f;
			frameSprite.color = fc;
		}

		// Background — full brightness when selected, dimmed when not
		if (backgroundSprite != null)
		{
			Color bg = Color.white;
			bg.a = selected ? 1f : unselectedBackgroundAlpha;
			backgroundSprite.color = bg;
		}

		// Portrait — full brightness when selected, dimmed when not
		if (portraitSprite != null)
		{
			Color c = Color.white;
			c.a = selected ? 1f : unselectedPortraitAlpha;
			portraitSprite.color = c;
		}
	}

	void Update()
	{
		if (_isLocked) return;

		float targetScale = _isHovered ? hoverScale : 1f;
		Vector3 target = _baseScale * targetScale;
		transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * hoverSmooth);
	}

	void OnMouseEnter()
	{
		if (_isLocked) return;
		_isHovered = true;
	}

	void OnMouseExit()
	{
		_isHovered = false;
	}

	void OnMouseDown()
	{
		if (_isLocked) return;
		_onSelected?.Invoke(_index);
	}
}
