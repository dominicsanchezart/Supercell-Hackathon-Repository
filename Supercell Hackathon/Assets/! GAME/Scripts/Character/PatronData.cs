using UnityEngine;

/// <summary>
/// Defines a Patron (Pact) the player can choose at run start.
/// Bundles faction identity, character stats, starter deck, and passive info.
///
/// Create one per patron: Assets > Create > Scriptable Objects > Patron Data
///
/// Design doc patrons:
///   Cinder King  (Wrath) — lower HP, Fury + Burn focus, passive: Bleed Out
///   Gilded Serpent (Pride) — Heal + Poison + Gold focus, passive: Perfect Form
///   Stitch Prophet (Ruin) — Guard + Dodge + Weaken focus, passive: Adaptive Biology
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
	[TextArea]
	[Tooltip("Description of the patron's passive ability.")]
	public string passiveDescription;

	[Header("Visuals")]
	[Tooltip("Portrait sprite for patron select screen.")]
	public Sprite portrait;
	[Tooltip("Color associated with this patron (for UI tinting).")]
	public Color patronColor = Color.white;

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
