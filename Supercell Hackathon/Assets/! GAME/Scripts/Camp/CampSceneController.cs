using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bootstrap for the Camp scene (LVL_Camp).
/// Reads ShopData for heal percent config, initializes CampView.
/// Has a standalone test mode for testing outside the full run flow.
/// </summary>
public class CampSceneController : MonoBehaviour
{
	[SerializeField] CampView campView;
	[SerializeField] ShopData shopData;

	[Header("Standalone Test Mode")]
	[Tooltip("Enable to test the camp scene without RunManager.")]
	[SerializeField] bool testMode;
	[Tooltip("Current HP in test mode.")]
	[SerializeField] int testCurrentHP = 50;
	[Tooltip("Max HP in test mode.")]
	[SerializeField] int testMaxHP = 80;
	[Tooltip("Starter deck for testing upgrades (drag CardData assets here).")]
	[SerializeField] CardData[] testDeck;

	void Start()
	{
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

		float healPercent = 0.30f;
		if (shopData != null)
			healPercent = shopData.campHealPercent;

		campView.Initialize(healPercent);
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

		rm.StartNewRun(42, testMaxHP, deck, false);
		rm.State.currentHP = testCurrentHP;

		Debug.Log($"CampSceneController: Test mode — HP {testCurrentHP}/{testMaxHP}, {deck.Count} cards.");
	}
}
