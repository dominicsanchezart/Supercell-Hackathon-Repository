using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the main menu screen (LVL_Main_Menu).
/// Play → starts a run using the selected PatronData.
/// Quit → exits the application.
///
/// For now, Play hardcodes the default patron (index 0 / Wrath).
/// Later, a patron select screen will let the player choose.
/// </summary>
public class MainMenuController : MonoBehaviour
{
	[Header("Buttons")]
	[SerializeField] Button playButton;
	[SerializeField] Button quitButton;

	[Header("Patron Configuration")]
	[Tooltip("Default patron used when Play is pressed. Will be replaced by patron select screen later.")]
	[SerializeField] PatronData defaultPatron;

	[Header("Scene")]
	[Tooltip("Name of the map scene to load.")]
	[SerializeField] string mapSceneName = "LVL_Map_Master";

	void Start()
	{
		if (playButton != null)
			playButton.onClick.AddListener(OnPlay);

		if (quitButton != null)
			quitButton.onClick.AddListener(OnQuit);
	}

	void OnPlay()
	{
		StartRunWithPatron(defaultPatron);
	}

	/// <summary>
	/// Starts a run with the given patron. Called by OnPlay for now,
	/// and later by the patron select screen.
	/// </summary>
	public void StartRunWithPatron(PatronData patron)
	{
		if (patron == null)
		{
			Debug.LogError("MainMenuController: No PatronData assigned!");
			return;
		}

		// Destroy any leftover RunManager from a previous run
		if (RunManager.Instance != null)
			Destroy(RunManager.Instance.gameObject);

		// Build starter deck from PatronData's Deck asset
		List<CardData> deck = new List<CardData>();
		if (patron.starterDeck != null && patron.starterDeck.cards != null)
		{
			for (int i = 0; i < patron.starterDeck.cards.Count; i++)
			{
				if (patron.starterDeck.cards[i] != null)
					deck.Add(patron.starterDeck.cards[i]);
			}
		}

		// Pull stats from PatronData's CharacterData
		int hp = patron.characterData != null ? patron.characterData.baseHealth : 80;
		int gold = patron.startingGold;
		int seed = Random.Range(0, int.MaxValue);

		// Capture for closure
		PatronData patronCapture = patron;
		List<CardData> deckCopy = new List<CardData>(deck);

		// Queue run setup — the map scene's RunManager will pick this up in Awake
		RunManager.PendingRunSetup = (rm) =>
		{
			rm.StartNewRun(seed, hp, deckCopy, false);
			rm.State.patronData = patronCapture;
			rm.State.patronFaction = patronCapture.faction;
			rm.State.gold = gold;
		};

		// Load the map scene (its RunManager will handle the rest)
		UnityEngine.SceneManagement.SceneManager.LoadScene(mapSceneName);
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
