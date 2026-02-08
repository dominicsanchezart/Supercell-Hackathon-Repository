using UnityEngine;

/// <summary>
/// Static helper that manages patron affinity points for the current run.
/// Call OnCardPlayed() from Hand.UseCard() to track which factions the player favors.
///
/// Affinity tiers:
///   Hostile (below 0): Patron is aggressive, threatening, questions the pact.
///   Cold (0–9 points): Patron is dismissive and terse.
///   Warm (10+ points): Patron is engaged and personality-rich.
///
/// Playing a card from the patron's faction: +1 to patron affinity.
/// Playing a card from a rival faction: -(energy cost) to patron affinity.
/// Playing a factionless (None) card: no change.
///
/// Affinity resets at the start of each new run (initialized in RunManager.StartNewRun).
/// </summary>
public static class PatronAffinityTracker
{
	/// <summary>
	/// Points threshold to transition from Cold to Warm tier.
	/// </summary>
	public const int WARM_THRESHOLD = 10;

	/// <summary>
	/// Below this threshold the patron becomes Hostile.
	/// </summary>
	public const int HOSTILE_THRESHOLD = 0;

	/// <summary>
	/// Called when the player plays a card.
	/// Matching faction: +1 affinity.
	/// Rival faction: -(energy cost) affinity. Expensive betrayals sting more.
	/// Neutral cards: no effect.
	/// </summary>
	public static void OnCardPlayed(CardData card)
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null) return;
		var state = RunManager.Instance.State;
		var affinity = state.affinityPoints;
		if (affinity == null) return;

		CardFaction patronFaction = state.patronFaction;
		int cost = Mathf.Max(1, card.baseEnergyCost);

		// Process first faction (skip factionless cards)
		if (card.cardFaction1 != CardFaction.None)
		{
			if (card.cardFaction1 == patronFaction)
			{
				if (affinity.ContainsKey(patronFaction))
				{
					affinity[patronFaction]++;
					Debug.Log($"[Affinity] +1 {patronFaction} (now {affinity[patronFaction]})");
				}
			}
			else
			{
				// Rival faction — subtract points equal to energy cost
				if (affinity.ContainsKey(patronFaction))
				{
					affinity[patronFaction] -= cost;
					Debug.Log($"[Affinity] -{cost} {patronFaction} from rival {card.cardFaction1} play (now {affinity[patronFaction]})");
				}
			}
		}

		// Process second faction (dual-pact cards)
		if (card.cardFaction2 != CardFaction.None
			&& card.cardFaction2 != card.cardFaction1)
		{
			if (card.cardFaction2 == patronFaction)
			{
				if (affinity.ContainsKey(patronFaction))
				{
					affinity[patronFaction]++;
					Debug.Log($"[Affinity] +1 {patronFaction} from dual-pact (now {affinity[patronFaction]})");
				}
			}
			// Don't double-punish for dual-pact rival cards
		}
	}

	/// <summary>
	/// Returns the current affinity points for a given faction.
	/// </summary>
	public static int GetAffinity(CardFaction faction)
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null) return 0;
		var affinity = RunManager.Instance.State.affinityPoints;
		if (affinity != null && affinity.TryGetValue(faction, out int points))
			return points;
		return 0;
	}

	/// <summary>
	/// Returns the affinity tier for a given faction.
	/// "Hostile" (below 0), "Cold" (0-9), "Warm" (10+)
	/// </summary>
	public static string GetAffinityTier(CardFaction faction)
	{
		int points = GetAffinity(faction);
		if (points < HOSTILE_THRESHOLD) return "Hostile";
		if (points >= WARM_THRESHOLD) return "Warm";
		return "Cold";
	}

	/// <summary>
	/// Returns the affinity tier for the player's active patron.
	/// </summary>
	public static string GetActivePatronTier()
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null) return "Cold";
		return GetAffinityTier(RunManager.Instance.State.patronFaction);
	}

	/// <summary>
	/// Logs all faction affinities to the console.
	/// </summary>
	public static void LogAffinityStatus()
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null)
		{
			Debug.Log("[Affinity] No active run.");
			return;
		}

		var state = RunManager.Instance.State;
		string patronName = state.patronData != null ? state.patronData.name : "None";
		CardFaction patronFaction = state.patronFaction;

		string log = $"[Affinity] Patron: {patronName} ({patronFaction})\n";
		log += $"  Active Patron Tier: {GetActivePatronTier()}\n";

		if (state.affinityPoints != null)
		{
			foreach (var kvp in state.affinityPoints)
			{
				string tier = GetAffinityTier(kvp.Key);
				string marker = kvp.Key == patronFaction ? " <-- PATRON" : "";
				log += $"  {kvp.Key}: {kvp.Value} pts ({tier}){marker}\n";
			}
		}

		Debug.Log(log);
	}
}
