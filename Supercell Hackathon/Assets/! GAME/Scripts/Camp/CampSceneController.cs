using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bootstrap for the Camp scene (LVL_Camp).
/// Reads CampData config, initializes CampView with OptionCards.
/// Has a standalone test mode for testing outside the full run flow.
///
/// Includes a fallback camera for standalone testing — automatically
/// destroyed when the scene loads additively (map camera already exists).
/// </summary>
public class CampSceneController : MonoBehaviour
{
	[SerializeField] CampView campView;
	[SerializeField] CampData campData;

	[Header("Standalone Test Mode")]
	[Tooltip("Enable to test the camp scene without RunManager.")]
	[SerializeField] bool testMode;
	[Tooltip("Current HP in test mode.")]
	[SerializeField] int testCurrentHP = 50;
	[Tooltip("Max HP in test mode.")]
	[SerializeField] int testMaxHP = 80;
	[Tooltip("Starter deck for testing upgrades (drag CardData assets here).")]
	[SerializeField] CardData[] testDeck;

	[Header("Fallback Camera")]
	[Tooltip("Camera used only for standalone testing. Destroyed when loaded additively.")]
	[SerializeField] Camera fallbackCamera;

	void Start()
	{
		HandleFallbackCamera();

		// If no RunManager exists and test mode is on, bootstrap a fake run
		if ((RunManager.Instance == null || RunManager.Instance.State == null) && testMode)
		{
			SetupTestRun();
		}

		if (RunManager.Instance == null || RunManager.Instance.State == null)
		{
			Debug.LogWarning("CampSceneController: No RunManager or RunState.");
			return;
		}

		campView.Initialize(campData);
	}

	void HandleFallbackCamera()
	{
		if (fallbackCamera == null) return;

		// If a RunManager exists, we loaded additively — the map camera is active
		// Destroy the fallback so we don't have two cameras rendering
		if (RunManager.Instance != null)
		{
			Destroy(fallbackCamera.gameObject);
			fallbackCamera = null;
		}
		// Otherwise we're in standalone mode — keep it
	}

	void SetupTestRun()
	{
		GameObject rmObj = new GameObject("RunManager (Test)");
		RunManager rm = rmObj.AddComponent<RunManager>();

		List<CardData> deck = new List<CardData>();
		if (testDeck != null)
		{
			for (int i = 0; i < testDeck.Length; i++)
			{
				if (testDeck[i] != null)
					deck.Add(testDeck[i]);
			}
		}

		int testSeed = UnityEngine.Random.Range(0, 100000);
		Debug.Log($"CampSceneController: Test seed = {testSeed}");
		rm.StartNewRun(testSeed, testMaxHP, deck, false);
		rm.State.currentHP = testCurrentHP;

		Debug.Log($"CampSceneController: Test mode — HP {testCurrentHP}/{testMaxHP}, {deck.Count} cards.");
	}
}
