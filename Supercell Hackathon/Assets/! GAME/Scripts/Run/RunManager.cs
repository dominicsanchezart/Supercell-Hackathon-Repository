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
	public string mainMenuSceneName = "LVL_Main_Menu";
	public string mapSceneName = "LVL_Map_Master";
	public string battleSceneName = "LVL_Battle";
	public string shopSceneName = "LVL_Shop";
	public string campSceneName = "LVL_Camp";
	public string eventSceneName = "LVL_Event";
	public string treasureSceneName = "LVL_Treasure";

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
			gold = 0,
			affinityPoints = new Dictionary<CardFaction, int>
			{
				{ CardFaction.Wrath, 0 },
				{ CardFaction.Pride, 0 },
				{ CardFaction.Ruin, 0 }
			}
		};

		if (mapConfig != null)
			State.mapData = MapGenerator.Generate(mapConfig, seed);

		// Reset patron dialogue state for the new run
		if (PatronDialogueManager.Instance != null)
			PatronDialogueManager.Instance.ResetForNewRun();

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

		// Pick an enemy preset for combat encounters
		if (node.encounterType == EncounterType.BattleMinion || node.encounterType == EncounterType.BattleBoss)
		{
			State.currentEnemyPreset = PickEnemyPreset(node.encounterType);
		}
		else
		{
			State.currentEnemyPreset = null;
		}

		// Prefetch patron AI dialogue while the scene loads
		PrefetchPatronDialogue(node);

		string sceneName = node.encounterType switch
		{
			EncounterType.BattleMinion => battleSceneName,
			EncounterType.BattleBoss => battleSceneName,
			EncounterType.Shop => shopSceneName,
			EncounterType.Camp => campSceneName,
			EncounterType.Event => eventSceneName,
			EncounterType.Treasure => treasureSceneName,
			_ => mapSceneName
		};

		StartCoroutine(LoadEncounterAdditive(sceneName));
	}

	/// <summary>
	/// Picks a random enemy preset from the appropriate pool in MapConfig.
	/// Returns null if no presets are configured.
	/// </summary>
	private EnemyPreset PickEnemyPreset(EncounterType type)
	{
		if (mapConfig == null) return null;

		EnemyPreset[] pool = type == EncounterType.BattleBoss
			? mapConfig.bossPresets
			: mapConfig.minionPresets;

		if (pool == null || pool.Length == 0) return null;

		return pool[Random.Range(0, pool.Length)];
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

	/// <summary>
	/// Fires an AI dialogue request BEFORE the encounter scene loads.
	/// By the time the scene finishes loading and OnCombatStart/OnEventNodeEntered fires,
	/// the AI response is already cached and displays instantly.
	/// </summary>
	void PrefetchPatronDialogue(MapNodeData node)
	{
		if (PatronDialogueManager.Instance == null) return;

		string enemyName = State.currentEnemyPreset != null
			? State.currentEnemyPreset.enemyName : "an unknown foe";

		switch (node.encounterType)
		{
			case EncounterType.BattleMinion:
				PatronDialogueManager.Instance.PrefetchLine(
					DialogueTriggerType.CombatStart,
					$"Your warlock enters combat against {enemyName}. " +
					"They draw their opening hand and steel themselves for the fight.");
				break;

			case EncounterType.BattleBoss:
				PatronDialogueManager.Instance.PrefetchLine(
					DialogueTriggerType.BossEncounterStart,
					$"Your warlock faces {enemyName}, " +
					"the most dangerous foe they have encountered. This could be the end.");
				break;

			case EncounterType.Event:
				PatronDialogueManager.Instance.PrefetchLine(
					DialogueTriggerType.EventNodeEntered,
					"Your warlock has stumbled into a strange encounter. " +
					"Something lurks in the shadows here. The outcome is uncertain.");
				break;

			case EncounterType.Treasure:
				PatronDialogueManager.Instance.PrefetchLine(
					DialogueTriggerType.EventNodeEntered,
					"Your warlock has discovered a hidden treasure cache. " +
					"Gold glimmers and mysterious cards await.");
				break;
		}
	}

	void LoadMapScene()
	{
		SceneManager.LoadScene(mapSceneName);
	}

	/// <summary>
	/// Ends the current run (e.g. player died). Resets state and returns to main menu.
	/// </summary>
	public void EndRun()
	{
		Debug.Log("Run ended. Returning to main menu.");
		State = null;

		SceneManager.LoadScene(mainMenuSceneName);
		Destroy(gameObject);
	}
}
