using UnityEngine;

/// <summary>
/// Defines a complete enemy encounter: character stats/visuals + deck.
/// Create one asset per enemy type (e.g. Goblin, Skeleton, Dragon).
/// Assigned to map nodes or picked randomly by RunManager when entering combat.
///
/// Does NOT modify CharacterData or CharacterInfo — Arena applies this at runtime
/// by swapping the enemy's _data reference and assigning the deck to its Inventory.
/// </summary>
[CreateAssetMenu(fileName = "EnemyPreset", menuName = "Scriptable Objects/Enemy Preset")]
public class EnemyPreset : ScriptableObject
{
	[Header("Identity")]
	[Tooltip("Display name shown in combat (e.g. 'Goblin Scout', 'Bone Warden').")]
	public string enemyName;

	[Header("Stats & Visuals")]
	[Tooltip("CharacterData ScriptableObject defining HP, energy, draw size, sprites, etc.")]
	public CharacterData characterData;

	[Header("Deck")]
	[Tooltip("The deck this enemy uses in combat.")]
	public Deck deck;

	[Header("Difficulty (Optional)")]
	[Tooltip("Rough difficulty tier for filtering. 0 = easiest, higher = harder.")]
	public int difficultyTier;
}
