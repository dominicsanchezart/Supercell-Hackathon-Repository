using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MapLineRenderer : MonoBehaviour
{
	[Header("Line Style")]
	public float lineWidth = 0.05f;
	public Color activeColor = new Color(1f, 1f, 1f, 0.8f);
	public Color dimmedColor = new Color(0.4f, 0.4f, 0.4f, 0.3f);

	[Header("Pulse (active lines only)")]
	public float pulseSpeed = 2f;
	public float pulseMin = 0.4f;
	public float pulseMax = 1f;

	LineRenderer lr;
	bool isActive;

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
		isActive = active;

		if (!active)
		{
			// Dimmed lines get static color, no pulse
			if (lr != null)
			{
				lr.startColor = dimmedColor;
				lr.endColor = dimmedColor;
			}
		}
	}

	void Update()
	{
		if (!isActive || lr == null) return;

		// Pulse brightness between pulseMin and pulseMax
		float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
		float intensity = Mathf.Lerp(pulseMin, pulseMax, t);

		Color pulsed = activeColor * intensity;
		pulsed.a = Mathf.Lerp(activeColor.a * pulseMin, activeColor.a, t);

		lr.startColor = pulsed;
		lr.endColor = pulsed;
	}
}
