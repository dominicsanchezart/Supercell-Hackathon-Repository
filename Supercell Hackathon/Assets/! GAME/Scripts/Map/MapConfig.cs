using UnityEngine;

[CreateAssetMenu(fileName = "New Map Config", menuName = "Scriptable Objects/Map Config")]
public class MapConfig : ScriptableObject
{
	[Header("Grid Dimensions")]
	[Tooltip("Total floors (rows) including start and boss")]
	public int totalRows = 15;
	[Tooltip("Number of columns in the grid")]
	public int columns = 5;

	[Header("Path Generation")]
	[Tooltip("Number of independent paths traced through the grid (StS uses 6)")]
	public int pathCount = 6;

	[Header("Encounter Weights (for open floors)")]
	public float battleMinionWeight = 45f;
	public float eliteWeight = 8f;
	public float eventWeight = 22f;
	public float campWeight = 12f;
	public float shopWeight = 10f;
	public float treasureWeight = 5f;

	[Header("Floor Rules")]
	[Tooltip("Rows where encounter type is forced to Camp (e.g. mid-run rest)")]
	public int[] guaranteedCampRows = { 7 };
	[Tooltip("Rows where encounter type is forced to Treasure")]
	public int[] guaranteedTreasureRows = { 4 };
	[Tooltip("Number of rows before boss forced to Camp")]
	public int campRowsBeforeBoss = 1;
	[Tooltip("Minimum row before elites/camps/shops can appear")]
	public int minRowForAdvancedTypes = 4;

	[Header("Constraint Rules")]
	[Tooltip("Max consecutive BattleMinion encounters along a path")]
	public int maxConsecutiveBattles = 3;
	[Tooltip("Max shops per row")]
	public int maxShopsPerRow = 1;

	[Header("Enemy Presets")]
	[Tooltip("Pool of enemy presets for regular (minion) combat encounters. One is picked randomly per node.")]
	public EnemyPreset[] minionPresets;
	[Tooltip("Pool of enemy presets for boss encounters. One is picked randomly for the boss node.")]
	public EnemyPreset[] bossPresets;

	// --- Legacy compatibility (baseLanes used by MapView for centering) ---
	[HideInInspector] public int baseLanes => columns;
}
