using UnityEngine;

[CreateAssetMenu(fileName = "New Map Config", menuName = "Scriptable Objects/Map Config")]
public class MapConfig : ScriptableObject
{
	[Header("Map Dimensions")]
	public int totalRows = 15;
	public int baseLanes = 3;

	[Header("Encounter Weights")]
	public float battleMinionWeight = 45f;
	public float eventWeight = 22f;
	public float campWeight = 15f;
	public float shopWeight = 12f;

	[Header("Structure Rules")]
	[Range(0f, 0.5f)] public float laneSplitChance = 0.15f;
	[Range(0f, 0.5f)] public float laneConvergeChance = 0.10f;
	[Range(0f, 1f)] public float adjacentConnectionChance = 0.30f;
	[Range(0f, 0.5f)] public float nodeSkipChance = 0.15f;

	[Header("Guaranteed Rows")]
	[Tooltip("Number of rows before boss that are forced to Camp")]
	public int campRowsBeforeBoss = 1;
}
