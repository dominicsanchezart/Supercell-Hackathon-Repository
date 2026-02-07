using UnityEngine;

/// <summary>
/// Optional condition that must be satisfied for an individual card action to fire.
/// When set to None the action always resolves.
/// </summary>
public enum CardCondition
{
	/// <summary>No condition – action always fires.</summary>
	None,

	/// <summary>Action fires only if the target enemy is dead (useful after a damage action).</summary>
	KillEnemy,

	/// <summary>Action fires only if the caster's HP is at or below the threshold.</summary>
	BelowHealthValue,

	/// <summary>Action fires only if the caster has >= threshold stacks of a status effect.</summary>
	StatusEffectThreshold,

	/// <summary>Action fires only if the most recently discarded card matches a card faction.</summary>
	DiscardedCardFaction
}
