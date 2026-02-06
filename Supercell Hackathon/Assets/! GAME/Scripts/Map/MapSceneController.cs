using UnityEngine;

public class MapSceneController : MonoBehaviour
{
	[SerializeField] MapView mapView;
	[Tooltip("Parent GameObject containing all map visuals. Disabled when an encounter scene loads.")]
	public GameObject mapVisuals;

	void Start()
	{
		if (RunManager.Instance == null)
		{
			Debug.LogWarning("RunManager not found. Generating test map.");
			GenerateTestMap();
			return;
		}

		if (RunManager.Instance.State == null || RunManager.Instance.State.mapData == null)
		{
			Debug.Log("No active run. Auto-starting a new run.");
			RunManager.Instance.StartNewRun(Random.Range(0, int.MaxValue), 80, new System.Collections.Generic.List<CardData>(), false);
		}

		mapView.Initialize(RunManager.Instance.State.mapData);
	}

	public void SetVisualsActive(bool active)
	{
		if (mapVisuals != null)
			mapVisuals.SetActive(active);
	}

	// Called by RunManager when returning from an encounter scene
	public void RefreshMap()
	{
		if (RunManager.Instance != null && RunManager.Instance.State != null)
		{
			mapView.RefreshAvailability();
			mapView.RebuildLines();
			mapView.PlayFadeIn();
		}
	}

	void GenerateTestMap()
	{
		MapConfig testConfig = ScriptableObject.CreateInstance<MapConfig>();
		testConfig.totalRows = 15;
		testConfig.columns = 5;
		testConfig.pathCount = 6;

		MapData testMap = MapGenerator.Generate(testConfig, Random.Range(0, int.MaxValue));
		mapView.Initialize(testMap);
	}
}
