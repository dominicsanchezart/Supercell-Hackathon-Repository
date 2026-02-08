using UnityEngine;

/// <summary>
/// Static helper that manages patron affinity points for the current run.
/// Call OnCardPlayed() from Hand.UseCard() to track which factions the player favors.
///
/// Affinity tiers:
///   Cold (0–9 points): Patron is dismissive and terse.
///   Warm (10+ points): Patron is engaged and personality-rich.
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
	/// Called when the player plays a card. Awards 1 affinity point per matching faction.
	/// Dual-pact cards (both factions set) award 1 point to each.
	/// </summary>
	public static void OnCardPlayed(CardData card)
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null) return;
		var affinity = RunManager.Instance.State.affinityPoints;
		if (affinity == null) return;

		if (card.cardFaction1 != CardFaction.None && affinity.ContainsKey(card.cardFaction1))
		{
			affinity[card.cardFaction1]++;
			Debug.Log($"[Affinity] +1 {card.cardFaction1} (now {affinity[card.cardFaction1]})");
		}

		// Dual-pact: second faction gets a point too (only if different from first)
		if (card.cardFaction2 != CardFaction.None
			&& card.cardFaction2 != card.cardFaction1
			&& affinity.ContainsKey(card.cardFaction2))
		{
			affinity[card.cardFaction2]++;
			Debug.Log($"[Affinity] +1 {card.cardFaction2} (now {affinity[card.cardFaction2]})");
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
	/// Returns the affinity tier ("Cold" or "Warm") for a given faction.
	/// </summary>
	public static string GetAffinityTier(CardFaction faction)
	{
		return GetAffinity(faction) >= WARM_THRESHOLD ? "Warm" : "Cold";
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
	/// Logs all faction affinities to the console. Call from anywhere for debugging.
	/// Press F9 at runtime (handled by AffinityDebugHotkey MonoBehaviour) or call directly.
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
