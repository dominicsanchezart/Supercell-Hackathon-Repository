using UnityEngine;

public class MapSceneController : MonoBehaviour
{
	[SerializeField] MapView mapView;

	void Start()
	{
		if (RunManager.Instance == null)
		{
			// No RunManager at all: generate a throwaway test map
			Debug.LogWarning("RunManager not found. Generating test map.");
			GenerateTestMap();
			return;
		}

		// RunManager exists but no run started yet: auto-start one without reloading
		if (RunManager.Instance.State == null || RunManager.Instance.State.mapData == null)
		{
			Debug.Log("No active run. Auto-starting a new run.");
			RunManager.Instance.StartNewRun(Random.Range(0, int.MaxValue), 80, new System.Collections.Generic.List<CardData>(), false);
		}

		mapView.Initialize(RunManager.Instance.State.mapData);
	}

	void GenerateTestMap()
	{
		// For testing the map scene standalone without RunManager
		MapConfig testConfig = ScriptableObject.CreateInstance<MapConfig>();
		testConfig.totalRows = 15;
		testConfig.baseLanes = 3;

		MapData testMap = MapGenerator.Generate(testConfig, Random.Range(0, int.MaxValue));
		mapView.Initialize(testMap);
	}
}
