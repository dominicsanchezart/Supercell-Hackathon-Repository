using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

/// <summary>
/// One-click editor tool that populates the three patron starter deck assets
/// (Deck_Wrath, Deck_Pride, Deck_Ruin) with balanced 12-card starting decks.
///
/// Each deck follows the Slay the Spire starter deck philosophy:
///   - Simple, low-cost cards that teach the faction's mechanics
///   - A mix of attacks and defense
///   - 2-3 faction-specific cards to introduce the patron's playstyle
///   - Enough neutral cards to provide a solid baseline
///
/// Usage: Tools > Decks > Assign Starter Decks
/// </summary>
public class AssignStarterDecks
{
	// ─── Card Asset Paths ────────────────────────────────────────
	private const string CARDS = "Assets/! GAME/Data/CardData";
	private const string DECKS = "Assets/! GAME/Data/Decks/StartingDecks";

	// Neutral
	private const string STRIKE     = CARDS + "/Neutral/Card_Strike.asset";
	private const string BLOCK      = CARDS + "/Neutral/Card_Block.asset";
	private const string HEAVY_SLAM = CARDS + "/Neutral/Card_HeavySlam.asset";
	private const string RESPITE    = CARDS + "/Neutral/Card_Respite.asset";

	// Wrath
	private const string CRIMSON_SLASH   = CARDS + "/Wrath/Card_CrimsonSlash.asset";
	private const string FLAME_BURST     = CARDS + "/Wrath/Card_FlameBurst.asset";
	private const string RECKLESS_STRIKE = CARDS + "/Wrath/Card_RecklessStrike.asset";
	private const string IMMOLATION      = CARDS + "/Wrath/Card_Immolation.asset";

	// Pride
	private const string SERPENTS_KISS   = CARDS + "/Pride/Card_SerpentsKiss.asset";
	private const string OPULENT_REMEDY  = CARDS + "/Pride/Card_OpulentRemedy.asset";
	private const string COILED_STRIKE   = CARDS + "/Pride/Card_CoiledStrike.asset";
	private const string CALCULATED_RISK = CARDS + "/Pride/Card_CalculatedRisk.asset";

	// Ruin
	private const string ENFEEBLE             = CARDS + "/Ruin/Card_Enfeeble.asset";
	private const string EMERGENCY_PLATING    = CARDS + "/Ruin/Card_EmergencyPlating.asset";
	private const string CORRUPTION_SPREADS   = CARDS + "/Ruin/Card_CorruptionSpreads.asset";
	private const string STRUCTURAL_COMPRIMISE = CARDS + "/Ruin/Card_StructuralComprimise.asset";

	[MenuItem("Tools/Decks/Assign Starter Decks")]
	public static void AssignAll()
	{
		AssignWrathDeck();
		AssignPrideDeck();
		AssignRuinDeck();

		AssetDatabase.SaveAssets();
		AssetDatabase.Refresh();

		Debug.Log("[Decks] Assigned starter decks for all three patrons.");
	}

	// ─── WRATH STARTER DECK (Cinder King) ────────────────────────
	// Theme: Aggressive — Fury stacking, Burn application, self-damage for power
	// 12 cards: 4x Strike, 3x Block, 1x Crimson Slash, 1x Flame Burst, 1x Immolation, 1x Reckless Strike, 1x Heavy Slam
	//
	// Strategy: Hit hard, build Fury with Crimson Slash, apply Burn with Flame Burst + Immolation,
	// Reckless Strike teaches the self-damage-for-power theme. Heavy Slam for burst.
	// 3x Block for survivability since self-damage is baked into the faction.
	// Synergizes with Bleed Out passive (gain Fury when taking damage).
	private static void AssignWrathDeck()
	{
		Deck deck = AssetDatabase.LoadAssetAtPath<Deck>(DECKS + "/Deck_Wrath.asset");
		if (deck == null)
		{
			Debug.LogError("[Decks] Could not find Deck_Wrath at: " + DECKS);
			return;
		}

		deck.cards = new List<CardData>
		{
			Load(STRIKE),
			Load(STRIKE),
			Load(STRIKE),
			Load(STRIKE),
			Load(BLOCK),
			Load(BLOCK),
			Load(BLOCK),
			Load(CRIMSON_SLASH),     // 1 cost — 12 damage + 2 Fury (teaches Fury mechanic)
			Load(FLAME_BURST),       // 1 cost — 8 damage + 3 Burn (teaches Burn mechanic)
			Load(IMMOLATION),        // 1 cost — 5 Burn to enemy, 3 self-damage (Burn focus + risk)
			Load(RECKLESS_STRIKE),   // 1 cost — 15 damage but 4 self-damage (teaches risk/reward)
			Load(HEAVY_SLAM),        // 2 cost — 20 damage (big finisher)
		};

		EditorUtility.SetDirty(deck);
		Debug.Log($"[Decks] Wrath starter deck: {deck.cards.Count} cards assigned.");
	}

	// ─── PRIDE STARTER DECK (Gilded Serpent) ─────────────────────
	// Theme: Poison + sustain + gold generation
	// 12 cards: 4x Strike, 2x Block, 2x Serpent's Kiss, 1x Opulent Remedy, 1x Calculated Risk, 1x Coiled Strike, 1x Respite
	//
	// Strategy: Apply Poison with 2x Serpent's Kiss (also heals 3 each), sustain with Opulent Remedy
	// (heal 8 + 5 gold), Calculated Risk for gold + draw. Coiled Strike for conditional burst
	// when enemy is poisoned. Respite for cycling. Attrition + economy playstyle.
	// Synergizes with Perfect Form passive (rewarded for taking no damage).
	private static void AssignPrideDeck()
	{
		Deck deck = AssetDatabase.LoadAssetAtPath<Deck>(DECKS + "/Deck_Pride.asset");
		if (deck == null)
		{
			Debug.LogError("[Decks] Could not find Deck_Pride at: " + DECKS);
			return;
		}

		deck.cards = new List<CardData>
		{
			Load(STRIKE),
			Load(STRIKE),
			Load(STRIKE),
			Load(STRIKE),
			Load(BLOCK),
			Load(BLOCK),
			Load(SERPENTS_KISS),      // 1 cost — 4 Poison + 3 Heal (teaches Poison + sustain)
			Load(SERPENTS_KISS),      // 1 cost — second copy to reliably apply Poison
			Load(OPULENT_REMEDY),     // 1 cost — Heal 8 + 5 Gold (teaches gold generation)
			Load(CALCULATED_RISK),    // 1 cost — 5 Gold + Draw 1 (teaches economy + cycling)
			Load(COILED_STRIKE),      // 1 cost — 10 damage, +15 if poisoned (teaches synergy)
			Load(RESPITE),            // 1 cost — Draw 2 (card advantage)
		};

		EditorUtility.SetDirty(deck);
		Debug.Log($"[Decks] Pride starter deck: {deck.cards.Count} cards assigned.");
	}

	// ─── RUIN STARTER DECK (Stitch Prophet) ──────────────────────
	// Theme: Weaken + Guard + Dodge — defensive control
	// 12 cards: 4x Strike, 2x Block, 1x Enfeeble, 1x Emergency Plating, 1x Structural Compromise,
	//           1x Corruption Spreads, 1x Respite, 1x Heavy Slam
	//
	// Strategy: Enfeeble applies 3 Weaken + draws a card (teaches Weaken + cycling).
	// Emergency Plating gives 12 Guard + 1 Dodge (teaches layered defense).
	// Structural Compromise gives 3 Weaken + 8 Guard (offensive debuff + defense in one).
	// Corruption Spreads deals 10 damage + 2 Weaken (offensive Weaken application).
	// Respite for draw. Defensive/control playstyle, synergizes with Emergency Protocol passive.
	private static void AssignRuinDeck()
	{
		Deck deck = AssetDatabase.LoadAssetAtPath<Deck>(DECKS + "/Deck_Ruin.asset");
		if (deck == null)
		{
			Debug.LogError("[Decks] Could not find Deck_Ruin at: " + DECKS);
			return;
		}

		deck.cards = new List<CardData>
		{
			Load(STRIKE),
			Load(STRIKE),
			Load(STRIKE),
			Load(STRIKE),
			Load(BLOCK),
			Load(BLOCK),
			Load(ENFEEBLE),              // 1 cost — 3 Weaken + Draw 1 (teaches Weaken + cycling)
			Load(EMERGENCY_PLATING),     // 1 cost — 12 Guard + 1 Dodge (teaches layered defense)
			Load(STRUCTURAL_COMPRIMISE), // 1 cost — 3 Weaken + 8 Guard (hybrid debuff/defense)
			Load(CORRUPTION_SPREADS),    // 2 cost — 10 damage + 2 Weaken (offensive control)
			Load(RESPITE),               // 1 cost — Draw 2 (card advantage)
			Load(HEAVY_SLAM),            // 2 cost — 20 damage (finisher)
		};

		EditorUtility.SetDirty(deck);
		Debug.Log($"[Decks] Ruin starter deck: {deck.cards.Count} cards assigned.");
	}

	// ─── Helpers ─────────────────────────────────────────────────

	private static CardData Load(string path)
	{
		CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
		if (card == null)
			Debug.LogWarning($"[Decks] Card not found at: {path}");
		return card;
	}
}
