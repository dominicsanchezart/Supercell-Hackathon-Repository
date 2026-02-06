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

	void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
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

		SceneManager.LoadScene(sceneName);
	}

	public void OnEncounterComplete()
	{
		LoadMapScene();
	}

	void LoadMapScene()
	{
		SceneManager.LoadScene(mapSceneName);
	}
}
