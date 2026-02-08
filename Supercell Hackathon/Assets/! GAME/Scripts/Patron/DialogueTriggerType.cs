/// <summary>
/// Types of game moments that trigger patron dialogue.
/// Used to build context strings for NeoCortex AI.
/// </summary>
public enum DialogueTriggerType
{
	CombatStart,
	MidCombatQuip,
	CombatEnd,
	RewardCardChosen,
	BossEncounterStart,
	EventNodeEntered
}
