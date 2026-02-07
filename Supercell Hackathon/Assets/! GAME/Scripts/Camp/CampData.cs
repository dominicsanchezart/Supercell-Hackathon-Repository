using UnityEngine;

/// <summary>
/// Configuration for the Camp encounter.
/// Create via: Assets > Create > Scriptable Objects > Camp Data
/// </summary>
[CreateAssetMenu(fileName = "New Camp Data", menuName = "Scriptable Objects/Camp Data")]
public class CampData : ScriptableObject
{
	[Header("Rest")]
	[Tooltip("Fraction of max HP healed when resting (0.30 = 30%).")]
	[Range(0f, 1f)] public float healPercent = 0.30f;
	[Tooltip("Icon displayed on the Rest option card.")]
	public Sprite restIcon;

	[Header("Upgrade")]
	[Tooltip("Icon displayed on the Upgrade option card.")]
	public Sprite upgradeIcon;
}
