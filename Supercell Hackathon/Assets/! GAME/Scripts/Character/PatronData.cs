using UnityEngine;

/// <summary>
/// Identifies which passive ability a patron grants.
/// Used by Arena to apply passive effects at the correct hook points.
/// </summary>
public enum PatronPassive
{
	None,
	BleedOut,          // Wrath — gain 2 Fury when taking card damage
	PerfectForm,       // Pride — +1 energy + heal 2 if no damage taken last turn
	EmergencyProtocol  // Ruin — first time below 50% HP: gain 5 Guard + 2 Dodge + Draw 2
}

/// <summary>
/// Defines a Patron (Pact) the player can choose at run start.
/// Bundles faction identity, character stats, starter deck, and passive info.
///
/// Create one per patron: Assets > Create > Scriptable Objects > Patron Data
///
/// Design doc patrons:
///   Cinder King  (Wrath) — lower HP, Fury + Burn focus, passive: Bleed Out
///   Gilded Serpent (Pride) — Heal + Poison + Gold focus, passive: Perfect Form
///   Stitch Prophet (Ruin) — Guard + Dodge + Weaken focus, passive: Emergency Protocol
/// </summary>
[CreateAssetMenu(fileName = "New Patron Data", menuName = "Scriptable Objects/Patron Data")]
public class PatronData : ScriptableObject
{
	[Header("Identity")]
	[Tooltip("Display name shown in patron select (e.g. 'Cinder King').")]
	public string patronName;
	[Tooltip("Which card faction this patron belongs to.")]
	public CardFaction faction;
	[Tooltip("Short flavor tagline (e.g. 'Pact of Wrath').")]
	public string pactTitle;
	[Tooltip("Which passive ability this patron grants.")]
	public PatronPassive passive;
	[TextArea]
	[Tooltip("Description of the patron's passive ability.")]
	public string passiveDescription;

	[Header("Visuals")]
	[Tooltip("Portrait sprite for patron select screen.")]
	public Sprite portrait;
	[Tooltip("Color associated with this patron (for UI tinting).")]
	public Color patronColor = Color.white;
	[Tooltip("9-sliced border frame sprite for this patron's dialogue box.")]
	public Sprite dialogueFrameSprite;
	[Tooltip("Short gameplay keywords shown on patron select (e.g. 'Damage - Fury - Burn').")]
	public string selectKeywords;

	[Header("Character Stats")]
	[Tooltip("CharacterData asset defining base HP, energy, draw size for this patron.")]
	public CharacterData characterData;

	[Header("Starter Deck")]
	[Tooltip("Deck asset containing this patron's starting cards.")]
	public Deck starterDeck;

	[Header("Starting Resources")]
	[Tooltip("Gold the player starts with.")]
	public int startingGold = 0;
}
