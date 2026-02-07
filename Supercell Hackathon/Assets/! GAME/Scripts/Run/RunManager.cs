using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RunManager : MonoBehaviour
{
	public static RunManager Instance { get; private set; }

	[Header("Configuration")]
	public MapConfig mapConfig;

	[Header("Scene Names")]
	public string mapSceneName = "LVL_Map_Master";
	public string battleSceneName = "LVL_Battle";
	public string shopSceneName = "LVL_Shop";
	public string campSceneName = "LVL_Camp";
	public string eventSceneName = "LVL_Event";

	public RunState State { get; private set; }

	// Track which encounter scene is currently loaded additively
	string loadedEncounterScene;

	/// <summary>
	/// Pending run configuration set by MainMenuController before loading the map scene.
	/// The map scene's RunManager picks this up in Awake and starts the run.
	/// </summary>
	public static System.Action<RunManager> PendingRunSetup;

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);

		// If the main menu queued a run config, apply it now
		if (PendingRunSetup != null)
		{
			PendingRunSetup.Invoke(this);
			PendingRunSetup = null;
		}
	}

	public void StartNewRun(int seed, int startingHP, List<CardData> starterDeck, bool loadScene = true)
	{
		State = new RunState
		{
			seed = seed,
			currentHP = startingHP,
			maxHP = startingHP,
			deck = new List<CardData>(starterDeck),
			gold = 0
		};

		if (mapConfig != null)
			State.mapData = MapGenerator.Generate(mapConfig, seed);

		if (loadScene)
			LoadMapScene();
	}

	public void StartNewRun()
	{
		int seed = Random.Range(0, int.MaxValue);
		StartNewRun(seed, 80, new List<CardData>());
	}

	public void OnEncounterSelected(MapNodeData node)
	{
		State.mapData.currentNodeId = node.nodeId;
		node.isCompleted = true;
		State.mapData.UpdateAvailability();

		string sceneName = node.encounterType switch
		{
			EncounterType.BattleMinion => battleSceneName,
			EncounterType.BattleBoss => battleSceneName,
			EncounterType.Shop => shopSceneName,
			EncounterType.Camp => campSceneName,
			EncounterType.Event => eventSceneName,
			_ => mapSceneName
		};

		StartCoroutine(LoadEncounterAdditive(sceneName));
	}

	public void OnEncounterComplete()
	{
		StartCoroutine(UnloadEncounterScene());
	}

	MapSceneController FindMapController()
	{
		return Object.FindAnyObjectByType<MapSceneController>();
	}

	void SetMapInteractable(bool value)
	{
		MapView mapView = Object.FindAnyObjectByType<MapView>();
		if (mapView != null)
			mapView.interactable = value;
	}

	void ResetMapCameraZoom()
	{
		MapView mapView = Object.FindAnyObjectByType<MapView>();
		if (mapView != null && mapView.mapCamera != null)
		{
			mapView.zoomLevel = 5f;
			mapView.mapCamera.orthographicSize = 5f;
		}
	}

	IEnumerator LoadEncounterAdditive(string sceneName)
	{
		// Disable map interaction and hide map visuals
		SetMapInteractable(false);
		ResetMapCameraZoom();
		MapSceneController mapController = FindMapController();
		if (mapController != null)
			mapController.SetVisualsActive(false);

		// Load encounter scene on top of map
		AsyncOperation load = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
		yield return load;

		loadedEncounterScene = sceneName;

		// Set the encounter scene as the active scene
		Scene encounterScene = SceneManager.GetSceneByName(sceneName);
		if (encounterScene.IsValid())
			SceneManager.SetActiveScene(encounterScene);
	}

	IEnumerator UnloadEncounterScene()
	{
		if (!string.IsNullOrEmpty(loadedEncounterScene))
		{
			Scene scene = SceneManager.GetSceneByName(loadedEncounterScene);
			if (scene.IsValid() && scene.isLoaded)
			{
				AsyncOperation unload = SceneManager.UnloadSceneAsync(loadedEncounterScene);
				yield return unload;
			}
			loadedEncounterScene = null;
		}

		// Re-activate the map scene
		Scene mapScene = SceneManager.GetSceneByName(mapSceneName);
		if (mapScene.IsValid())
			SceneManager.SetActiveScene(mapScene);

		// Show map visuals and re-enable interaction
		MapSceneController mapController = FindMapController();
		if (mapController != null)
		{
			mapController.SetVisualsActive(true);
			mapController.RefreshMap();
		}

		SetMapInteractable(true);
	}

	void LoadMapScene()
	{
		SceneManager.LoadScene(mapSceneName);
	}

	/// <summary>
	/// Ends the current run (e.g. player died). Placeholder: returns to main menu.
	/// </summary>
	public void EndRun()
	{
		Debug.Log("Run ended. Returning to main menu (placeholder).");
		State = null;

		// TODO: Replace with actual main menu scene name once it exists.
		// For now, reload the map scene as a placeholder.
		SceneManager.LoadScene(mapSceneName);
	}
}
