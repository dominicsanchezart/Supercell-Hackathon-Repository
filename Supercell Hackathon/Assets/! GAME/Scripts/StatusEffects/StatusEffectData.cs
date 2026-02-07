using System;
using UnityEngine;

[CreateAssetMenu(fileName = "StatusEffectData", menuName = "Scriptable Objects/Status Effect Data")]
public class StatusEffectData : ScriptableObject
{
	[Serializable]
	public struct StatusVisual
	{
		public StatusEffects status;
		public Sprite icon;
		public Color tint;
	}

	public StatusVisual[] statusVisuals;

	/// <summary>
	/// Also include Empower since it's a buff that appears on the HUD
	/// but isn't in the StatusEffects enum.
	/// </summary>
	public Sprite empowerIcon;
	public Color empowerTint = Color.yellow;

	/// <summary>
	/// Look up the visual for a given status enum value.
	/// </summary>
	public bool TryGetVisual(StatusEffects status, out Sprite icon, out Color tint)
	{
		foreach (var v in statusVisuals)
		{
			if (v.status == status)
			{
				icon = v.icon;
				tint = v.tint;
				return true;
			}
		}

		icon = null;
		tint = Color.white;
		return false;
	}
}
