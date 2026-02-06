using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class MapNodeView : MonoBehaviour
{
	[Header("Smooth Motion")]
	public float smoothSpeed = 12f;

	[Header("Sprites")]
	[SerializeField] SpriteRenderer iconRenderer;
	[SerializeField] SpriteRenderer backgroundRenderer;

	[Header("Background Colors")]
	public Color bgAvailableColor = Color.white;
	public Color bgUnavailableColor = new Color(0.5f, 0.5f, 0.5f, 0.7f);
	public Color bgCompletedColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

	[Header("Icon Colors")]
	public Color iconAvailableColor = new Color(0.15f, 0.15f, 0.15f, 1f);
	public Color iconUnavailableColor = new Color(0.35f, 0.35f, 0.35f, 0.5f);
	public Color iconCompletedColor = new Color(0.2f, 0.2f, 0.2f, 0.35f);

	[Header("Pulsate")]
	public float pulsateSpeed = 2f;
	public float pulsateAmount = 0.15f;

	[HideInInspector] public MapView owner;

	MapNodeData nodeData;
	bool isAvailable;
	bool isCompleted;
	Vector3 baseScale;
	Color targetBgColor;
	Color targetIconColor;

	public void Initialize(MapNodeData data, Sprite icon, Sprite background)
	{
		nodeData = data;

		if (iconRenderer != null && icon != null)
			iconRenderer.sprite = icon;

		if (backgroundRenderer != null && background != null)
			backgroundRenderer.sprite = background;

		baseScale = transform.localScale;
		UpdateVisuals();
	}

	public void SetAvailability(bool available, bool completed)
	{
		isAvailable = available;
		isCompleted = completed;
		UpdateVisuals();
	}

	void UpdateVisuals()
	{
		if (isCompleted)
		{
			targetBgColor = bgCompletedColor;
			targetIconColor = iconCompletedColor;
		}
		else if (isAvailable)
		{
			targetBgColor = bgAvailableColor;
			targetIconColor = iconAvailableColor;
		}
		else
		{
			targetBgColor = bgUnavailableColor;
			targetIconColor = iconUnavailableColor;
		}
	}

	void Update()
	{
		// Lerp colors separately
		if (backgroundRenderer != null)
		{
			backgroundRenderer.color = Color.Lerp(
				backgroundRenderer.color,
				targetBgColor,
				Time.deltaTime * smoothSpeed
			);
		}

		if (iconRenderer != null)
		{
			iconRenderer.color = Color.Lerp(
				iconRenderer.color,
				targetIconColor,
				Time.deltaTime * smoothSpeed
			);
		}

		// Pulsate if available and not completed
		if (isAvailable && !isCompleted)
		{
			float pulse = 1f + Mathf.Sin(Time.time * pulsateSpeed) * pulsateAmount;
			transform.localScale = baseScale * pulse;
		}
		else
		{
			transform.localScale = Vector3.Lerp(
				transform.localScale,
				baseScale,
				Time.deltaTime * smoothSpeed
			);
		}
	}

	void OnMouseDown()
	{
		if (owner != null)
			owner.OnNodeClicked(this);
	}

	public MapNodeData GetNodeData()
	{
		return nodeData;
	}
}
