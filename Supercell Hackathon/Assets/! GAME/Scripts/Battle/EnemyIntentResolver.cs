using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Analyses future enemy cards and produces a simple intent label
/// (Aggressive, Defensive, Aggressive & Defensive, Buff, Debuff).
/// </summary>
public static class EnemyIntentResolver
{
	/// <summary>
	/// Peeks at the top cards of the enemy's draw pile (+ discard if needed)
	/// and returns a predicted intent for the enemy's upcoming turn.
	/// </summary>
	public static EnemyIntent Resolve(Hand enemyHand)
	{
		List<CardData> predicted = PeekNextHand(enemyHand);
		if (predicted.Count == 0)
			return EnemyIntent.Unknown;

		return ClassifyHand(predicted);
	}

	/// <summary>
	/// Returns the list of cards the enemy is likely to draw next turn
	/// without modifying the actual draw/discard piles.
	/// </summary>
	public static List<CardData> PeekNextHand(Hand enemyHand)
	{
		int drawSize = enemyHand.characterInfo._data.baseDrawSize;
		List<CardData> peek = new List<CardData>();

		// First pull from draw pile
		for (int i = 0; i < Mathf.Min(drawSize, enemyHand.drawPile.Count); i++)
			peek.Add(enemyHand.drawPile[i]);

		// If draw pile is insufficient, the discard would be reshuffled.
		// We can't know the exact shuffle order, so just grab from the discard pile.
		if (peek.Count < drawSize && enemyHand.discardPile.Count > 0)
		{
			int remaining = drawSize - peek.Count;
			for (int i = 0; i < Mathf.Min(remaining, enemyHand.discardPile.Count); i++)
				peek.Add(enemyHand.discardPile[i]);
		}

		return peek;
	}

	/// <summary>
	/// Classifies a collection of cards into a single EnemyIntent label
	/// based on the types of actions present.
	/// Uses proportional weighting so the *dominant* category wins,
	/// rather than any single action pulling the whole hand towards it.
	/// </summary>
	public static EnemyIntent ClassifyHand(List<CardData> cards)
	{
		int aggressive = 0;
		int defensive = 0;
		int buff = 0;
		int debuff = 0;

		foreach (CardData card in cards)
		{
			TallyAction(card.actionType1, card.actionTarget1, ref aggressive, ref defensive, ref buff, ref debuff);

			if (card.actionType2 != CardActionType.None)
				TallyAction(card.actionType2, card.actionTarget2, ref aggressive, ref defensive, ref buff, ref debuff);

			if (card.actionType3 != CardActionType.None)
				TallyAction(card.actionType3, card.actionTarget3, ref aggressive, ref defensive, ref buff, ref debuff);
		}

		int total = aggressive + defensive + buff + debuff;
		if (total == 0)
			return EnemyIntent.Unknown;

		float aggRatio = (float)aggressive / total;
		float defRatio = (float)defensive / total;
		float bufRatio = (float)buff / total;
		float debRatio = (float)debuff / total;

		// Threshold: a category must hold at least this share to count as significant
		const float threshold = 0.25f;

		bool sigAgg = aggRatio >= threshold;
		bool sigDef = defRatio >= threshold;
		bool sigBuf = bufRatio >= threshold;
		bool sigDeb = debRatio >= threshold;

		// Mixed aggressive + defensive only when both are significant
		if (sigAgg && sigDef)
			return EnemyIntent.AggressiveDefensive;

		// Otherwise pick whichever category has the highest share
		if (aggRatio >= defRatio && aggRatio >= bufRatio && aggRatio >= debRatio && sigAgg)
			return EnemyIntent.Aggressive;

		if (defRatio >= aggRatio && defRatio >= bufRatio && defRatio >= debRatio && sigDef)
			return EnemyIntent.Defensive;

		if (debRatio >= aggRatio && debRatio >= defRatio && debRatio >= bufRatio && sigDeb)
			return EnemyIntent.Debuff;

		if (sigBuf)
			return EnemyIntent.Buff;

		// Fallback: return whatever has the most tallies
		if (aggressive >= defensive && aggressive >= buff && aggressive >= debuff)
			return EnemyIntent.Aggressive;
		if (defensive >= buff && defensive >= debuff)
			return EnemyIntent.Defensive;
		if (debuff >= buff)
			return EnemyIntent.Debuff;

		return EnemyIntent.Buff;
	}

	private static void TallyAction(
		CardActionType type, ActionTarget target,
		ref int aggressive, ref int defensive, ref int buff, ref int debuff)
	{
		switch (type)
		{
			// --- Offensive damage ---
			case CardActionType.Damage:
			case CardActionType.DamageAll:
			case CardActionType.DamageLostHP:
			case CardActionType.DamagePerStack:
				aggressive++;
				break;

			// --- Offensive debuffs applied to the opponent ---
			case CardActionType.Burn:
			case CardActionType.Poison:
			case CardActionType.Weaken:
				// When the enemy targets the player, it's a debuff
				if (target == ActionTarget.Enemy)
					debuff++;
				else
					debuff++; // Self-targeting debuff is unusual but still debuff-like
				break;

			case CardActionType.RemoveCard:
			case CardActionType.ExhaustCard:
			case CardActionType.DestroyCard:
			case CardActionType.SpendGold:
				debuff++;
				break;

			// --- Defensive / healing ---
			case CardActionType.Heal:
			case CardActionType.HealPerStack:
			case CardActionType.Guard:
			case CardActionType.Dodge:
				defensive++;
				break;

			// --- Buff (self-enhancing) ---
			case CardActionType.Empower:
			case CardActionType.Fury:
			case CardActionType.Energize:
			case CardActionType.DrawCard:
			case CardActionType.GiveEnergy:
			case CardActionType.GainGold:
				buff++;
				break;

			case CardActionType.None:
			default:
				break;
		}
	}

	/// <summary>
	/// Returns a player-friendly display string for the given intent.
	/// </summary>
	public static string GetIntentDisplayText(EnemyIntent intent)
	{
		switch (intent)
		{
			case EnemyIntent.Aggressive:          return "Aggressive";
			case EnemyIntent.Defensive:           return "Defensive";
			case EnemyIntent.AggressiveDefensive: return "Aggressive & Defensive";
			case EnemyIntent.Debuff:              return "Debuff";
			case EnemyIntent.Buff:                return "Buff";
			default:                              return "Unknown";
		}
	}

	/// <summary>
	/// Returns a hex colour code for the intent label so the player
	/// can quickly distinguish intent at a glance.
	/// </summary>
	public static Color GetIntentColor(EnemyIntent intent)
	{
		switch (intent)
		{
			case EnemyIntent.Aggressive:          return new Color(0.90f, 0.25f, 0.25f); // red
			case EnemyIntent.Defensive:           return new Color(0.30f, 0.70f, 0.95f); // blue
			case EnemyIntent.AggressiveDefensive: return new Color(0.85f, 0.55f, 0.20f); // orange
			case EnemyIntent.Debuff:              return new Color(0.70f, 0.30f, 0.85f); // purple
			case EnemyIntent.Buff:                return new Color(0.30f, 0.85f, 0.40f); // green
			default:                              return Color.gray;
		}
	}
}
