using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the main menu screen (LVL_Main_Menu).
/// Play → starts a run (currently hardcodes Wrath patron).
/// Options → toggles an options panel.
/// Quit → exits the application.
/// </summary>
public class MainMenuController : MonoBehaviour
{
	[Header("Buttons")]
	[SerializeField] Button playButton;
	[SerializeField] Button optionsButton;
	[SerializeField] Button quitButton;

	[Header("Panels")]
	[SerializeField] GameObject optionsPanel;

	[Header("Run Configuration")]
	[Tooltip("MapConfig asset used by RunManager.")]
	[SerializeField] MapConfig mapConfig;
	[Tooltip("Starting HP for a new run.")]
	[SerializeField] int startingHP = 80;
	[Tooltip("Starting gold for a new run.")]
	[SerializeField] int startingGold = 0;
	[Tooltip("Starter deck cards (drag CardData assets here).")]
	[SerializeField] CardData[] starterDeck;
	[Tooltip("Patron faction. For now, hardcoded to Wrath.")]
	[SerializeField] CardFaction patron = CardFaction.Wrath;

	[Header("Scene")]
	[Tooltip("Name of the map scene to load.")]
	[SerializeField] string mapSceneName = "LVL_Map_Master";

	void Start()
	{
		if (optionsPanel != null)
			optionsPanel.SetActive(false);

		if (playButton != null)
			playButton.onClick.AddListener(OnPlay);

		if (optionsButton != null)
			optionsButton.onClick.AddListener(OnOptions);

		if (quitButton != null)
			quitButton.onClick.AddListener(OnQuit);
	}

	void OnPlay()
	{
		// Destroy any leftover RunManager from a previous run
		if (RunManager.Instance != null)
			Destroy(RunManager.Instance.gameObject);

		// Create a fresh RunManager
		GameObject rmObj = new GameObject("RunManager");
		RunManager rm = rmObj.AddComponent<RunManager>();
		rm.mapConfig = mapConfig;
		rm.mapSceneName = mapSceneName;

		// Build starter deck
		List<CardData> deck = new List<CardData>();
		if (starterDeck != null)
		{
			for (int i = 0; i < starterDeck.Length; i++)
			{
				if (starterDeck[i] != null)
					deck.Add(starterDeck[i]);
			}
		}

		// Start the run
		int seed = Random.Range(0, int.MaxValue);
		rm.StartNewRun(seed, startingHP, deck);

		// Set patron and starting gold
		rm.State.patronFaction = patron;
		rm.State.gold = startingGold;
	}

	void OnOptions()
	{
		if (optionsPanel != null)
			optionsPanel.SetActive(!optionsPanel.activeSelf);
	}

	void OnQuit()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
