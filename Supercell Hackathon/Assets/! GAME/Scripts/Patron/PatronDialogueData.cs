using UnityEngine;

/// <summary>
/// Stores all scripted dialogue lines for a single patron, organized by trigger type.
/// Create one asset per patron: Assets > Create > Scriptable Objects > Patron Dialogue Data
///
/// Lines are picked randomly without repeating until the entire pool is exhausted.
/// Cold/Warm affinity tiers can use separate line pools if needed (future extension).
/// </summary>
[CreateAssetMenu(fileName = "New Patron Dialogue Data", menuName = "Scriptable Objects/Patron Dialogue Data")]
public class PatronDialogueData : ScriptableObject
{
	[Header("Combat Start")]
	[Tooltip("One line when a new fight begins. Sets the tone. Always scripted.")]
	[TextArea] public string[] combatStartLines;

	[Header("Mid-Combat Quips — Positive")]
	[Tooltip("Player deals 15+ damage in a single turn.")]
	[TextArea] public string[] bigDamageLines;
	[Tooltip("High status stacks on enemy (5+ poison/burn/weaken).")]
	[TextArea] public string[] highStatusLines;

	[Header("Mid-Combat Quips — Negative")]
	[Tooltip("Player drops below 25% HP.")]
	[TextArea] public string[] lowHPLines;

	[Header("Combat End")]
	[Tooltip("Normal victory lines.")]
	[TextArea] public string[] victoryLines;
	[Tooltip("Victory with <20% HP remaining (close call).")]
	[TextArea] public string[] closeCallVictoryLines;

	[Header("Reward Card")]
	[Tooltip("Chosen card matches this patron's faction (approval).")]
	[TextArea] public string[] approvalLines;
	[Tooltip("Chosen card doesn't match this patron's faction (disapproval).")]
	[TextArea] public string[] disapprovalLines;

	[Header("Boss Encounter (NeoCortex fallback)")]
	[TextArea] public string[] bossStartFallbackLines;

	[Header("Event Node (NeoCortex fallback)")]
	[TextArea] public string[] eventFallbackLines;

	[Header("Rival Card Played (NeoCortex fallback)")]
	[Tooltip("Player played a card from a rival faction in combat.")]
	[TextArea] public string[] rivalCardPlayedLines;

	[Header("Card Purchased (NeoCortex fallback)")]
	[Tooltip("Player bought a card at the shop — reaction varies by faction match.")]
	[TextArea] public string[] rivalCardPurchasedLines;
	[TextArea] public string[] loyalCardPurchasedLines;
}
