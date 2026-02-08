using UnityEngine;

/// <summary>
/// Selectable patron thumbnail slot for the patron select screen.
/// Displays a small portrait with a colored border that highlights when selected.
///
/// Follows the project's OnMouseDown callback pattern (like OptionCard, CardView).
///
/// Setup (prefab):
///   Root — PatronSlot (this script) + BoxCollider2D
///     ├── Border    — SpriteRenderer (tinted with patron color when selected)
///     └── Portrait  — SpriteRenderer (patron thumbnail)
/// </summary>
public class PatronSlot : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private SpriteRenderer portraitSprite;
	[SerializeField] private SpriteRenderer borderSprite;
	[SerializeField] private Collider2D slotCollider;

	[Header("Appearance")]
	[SerializeField] private Color unselectedBorderColor = new Color(0.3f, 0.3f, 0.3f, 1f);
	[SerializeField] private float unselectedPortraitAlpha = 0.5f;

	[Header("Hover")]
	[SerializeField] private float hoverScale = 1.1f;
	[SerializeField] private float hoverSmooth = 12f;

	private System.Action<int> _onSelected;
	private int _index;
	private Color _patronColor;
	private bool _isSelected;
	private bool _isHovered;
	private Vector3 _baseScale;

	void Awake()
	{
		_baseScale = transform.localScale;
	}

	/// <summary>
	/// Configures the slot with patron data. Called by PatronSelectController on Start.
	/// </summary>
	public void Setup(PatronData patron, int index, System.Action<int> onSelected)
	{
		_index = index;
		_onSelected = onSelected;
		_patronColor = patron.patronColor;

		if (portraitSprite != null)
			portraitSprite.sprite = patron.portrait;

		SetSelected(false);
	}

	/// <summary>
	/// Toggles the selected visual state (border color + portrait brightness).
	/// </summary>
	public void SetSelected(bool selected)
	{
		_isSelected = selected;

		if (borderSprite != null)
			borderSprite.color = selected ? _patronColor : unselectedBorderColor;

		if (portraitSprite != null)
		{
			Color c = portraitSprite.color;
			c.a = selected ? 1f : unselectedPortraitAlpha;
			portraitSprite.color = c;
		}
	}

	void Update()
	{
		float targetScale = _isHovered ? hoverScale : 1f;
		Vector3 target = _baseScale * targetScale;
		transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * hoverSmooth);
	}

	void OnMouseEnter()
	{
		_isHovered = true;
	}

	void OnMouseExit()
	{
		_isHovered = false;
	}

	void OnMouseDown()
	{
		_onSelected?.Invoke(_index);
	}
}
