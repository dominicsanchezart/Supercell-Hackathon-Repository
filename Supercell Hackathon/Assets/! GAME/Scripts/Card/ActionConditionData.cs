using UnityEngine;

/// <summary>
/// Data block that describes an optional condition for a single card action slot.
/// Attach one of these to each action in CardData.
/// </summary>
[System.Serializable]
public struct ActionConditionData
{
	public CardCondition condition;

	[Tooltip("HP threshold (BelowHealthValue) or stack count (StatusEffectThreshold).")]
	public int threshold;

	[Tooltip("Which status effect to check (only used by StatusEffectThreshold).")]
	public StatusEffectType statusEffect;

	[Tooltip("Which card faction to match (only used by DiscardedCardFaction).")]
	public CardFaction cardFaction;
}
