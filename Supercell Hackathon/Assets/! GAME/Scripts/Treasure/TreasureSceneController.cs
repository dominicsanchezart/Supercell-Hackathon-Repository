using UnityEngine;

/// <summary>
/// Simple controller for the Treasure encounter node.
/// Reuses BattleRewardUI to show gold + card rewards without a combat encounter.
///
/// Setup:
///   1. Create a scene (LVL_Treasure) with a BattleRewardUI (same prefab as battle scene)
///   2. Attach this script to a root GameObject
///   3. Assign the BattleRewardUI reference
///   4. Configure gold amounts (defaults: 15-35)
///
/// Flow: Scene loads → rewards shown immediately → player picks card (or skips) → return to map.
/// </summary>
public class TreasureSceneController : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private BattleRewardUI rewardUI;

	[Header("Gold")]
	[SerializeField] private int goldMin = 15;
	[SerializeField] private int goldMax = 35;

	private void Start()
	{
		if (rewardUI == null)
		{
			Debug.LogError("[Treasure] No BattleRewardUI assigned. Returning to map.");
			ReturnToMap();
			return;
		}

		int goldAmount = Random.Range(goldMin, goldMax + 1);
		rewardUI.ShowTreasureRewards(ReturnToMap, goldAmount);
	}

	private void ReturnToMap()
	{
		if (RunManager.Instance != null)
			RunManager.Instance.OnEncounterComplete();
		else
			Debug.LogWarning("[Treasure] No RunManager found. Cannot return to map.");
	}
}
