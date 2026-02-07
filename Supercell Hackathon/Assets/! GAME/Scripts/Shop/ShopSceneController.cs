using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bootstrap for the Shop scene (LVL_Shop).
/// Reads RunState, generates shop inventory, and wires up the ShopView.
/// Has a standalone test mode for testing outside the full run flow.
///
/// Includes a fallback camera for standalone testing — automatically
/// destroyed when the scene loads additively (map camera already exists).
/// </summary>
public class ShopSceneController : MonoBehaviour
{
	[SerializeField] ShopView shopView;
	[SerializeField] ShopData shopData;

	[Header("Standalone Test Mode")]
	[Tooltip("Enable to test the shop scene without RunManager.")]
	[SerializeField] bool testMode;
	[Tooltip("Gold given in test mode.")]
	[SerializeField] int testGold = 300;
	[Tooltip("Starter deck cards for test mode (drag CardData assets here).")]
	[SerializeField] CardData[] testDeck;
	[Tooltip("Patron faction for test mode.")]
	[SerializeField] CardFaction testFaction = CardFaction.Wrath;

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
			Debug.LogWarning("ShopSceneController: No RunManager or RunState. Cannot open shop.");
			return;
		}

		RunState state = RunManager.Instance.State;

		// Increment shop visit count for unique seed
		state.shopVisitCount++;

		// Generate shop inventory
		List<ShopItem> items = ShopInventory.Generate(shopData, state);

		// Initialize shop UI
		shopView.Initialize(items);
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
		// Create a temporary RunManager if none exists
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
		Debug.Log($"ShopSceneController: Test seed = {testSeed}");
		rm.StartNewRun(testSeed, 80, deck, false);
		rm.State.gold = testGold;
		rm.State.patronFaction = testFaction;

		Debug.Log($"ShopSceneController: Test mode — created RunManager with {testGold}g, {deck.Count} cards, {testFaction} faction.");
	}
}
