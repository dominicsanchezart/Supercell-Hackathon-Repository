using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MapLineRenderer : MonoBehaviour
{
	[Header("Line Style")]
	public float lineWidth = 0.05f;
	public Color activeColor = new Color(1f, 1f, 1f, 0.8f);
	public Color dimmedColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);

	LineRenderer lr;
	Color targetColor;

	void Awake()
	{
		lr = GetComponent<LineRenderer>();
	}

	public void Initialize(Vector3 start, Vector3 end, bool isOnActivePath)
	{
		if (lr == null)
			lr = GetComponent<LineRenderer>();

		lr.positionCount = 2;
		lr.SetPosition(0, start);
		lr.SetPosition(1, end);

		lr.startWidth = lineWidth;
		lr.endWidth = lineWidth;

		lr.useWorldSpace = false;
		lr.sortingOrder = -1;

		SetActivePath(isOnActivePath);
	}

	public void SetActivePath(bool active)
	{
		targetColor = active ? activeColor : dimmedColor;

		if (lr != null)
		{
			lr.startColor = targetColor;
			lr.endColor = targetColor;
		}
	}
}
