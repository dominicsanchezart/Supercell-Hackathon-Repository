using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Drop-in test harness for the patron dialogue system.
/// Place in ANY scene — it detects whether battle components exist and acts accordingly.
///
/// TWO MODES:
///   A) Standalone mode (empty scene / no Arena):
///      Creates a fake RunManager + RunState so dialogue can fire.
///   B) Battle scene mode (Arena exists):
///      Only ensures PatronDialogueManager is present, does NOT touch RunState.
///      If RunState has no patron, injects patron data without resetting anything else.
///
/// Usage:
///   1. Add this component to any GameObject
///   2. Assign the PatronDialogue prefab (the one with PatronDialogueManager + DialogueBox)
///   3. Optionally assign a PatronData asset
///   4. Hit Play
///   5. Right-click this component in the Inspector → pick a trigger
/// </summary>
public class PatronDialogueTest : MonoBehaviour
{
	[Header("Test Configuration")]
	[Tooltip("Which patron to test. If null, a minimal fake is created at runtime.")]
	[SerializeField] private PatronData testPatronData;

	[Tooltip("Patron faction to use if no PatronData is assigned.")]
	[SerializeField] private CardFaction testFaction = CardFaction.Wrath;

	[Tooltip("The PatronDialogue prefab (with PatronDialogueManager + DialogueBox). " +
		"If left empty, will try to find an existing one in the scene.")]
	[SerializeField] private GameObject patronDialoguePrefab;

	[Header("Fake Player State (standalone mode only)")]
	[SerializeField] private int fakeHP = 80;
	[SerializeField] private int fakeMaxHP = 80;

	private void Start()
	{
		// Delay setup by one frame so Arena and other components finish their Start() first
		StartCoroutine(DelayedSetup());
	}

	private IEnumerator DelayedSetup()
	{
		yield return null; // wait one frame

		bool hasArena = FindAnyObjectByType<Arena>() != null;

		if (hasArena)
		{
			Debug.Log("[DialogueTest] Battle scene detected — will not create fake RunState.");
			// Only ensure patron data exists on the run state (don't create a new RunState)
			EnsurePatronData();
		}
		else
		{
			Debug.Log("[DialogueTest] Standalone mode — creating fake RunState.");
			SetupFakeRunState();
		}

		SetupDialogueManager();
		Debug.Log("[DialogueTest] Ready. Right-click this component to fire triggers.");
	}

	/// <summary>
	/// Standalone mode: creates a fake RunManager + RunState from scratch.
	/// Only used when no Arena exists in the scene.
	/// </summary>
	private void SetupFakeRunState()
	{
		// If a real RunManager already exists with state, don't touch it
		if (RunManager.Instance != null && RunManager.Instance.State != null)
		{
			EnsurePatronData();
			Debug.Log("[DialogueTest] Using existing RunManager + RunState.");
			return;
		}

		// Create a temporary RunManager if none exists
		if (RunManager.Instance == null)
		{
			GameObject rmGO = new GameObject("[TEST] RunManager");
			rmGO.AddComponent<RunManager>();
		}

		// Start a fake run without loading a scene
		RunManager.Instance.StartNewRun(12345, fakeMaxHP, new List<CardData>(), loadScene: false);

		var state = RunManager.Instance.State;
		state.currentHP = fakeHP;
		state.maxHP = fakeMaxHP;

		InjectPatronData(state);

		Debug.Log($"[DialogueTest] Fake RunState created — Patron: {state.patronData?.patronName} ({state.patronFaction})");
	}

	/// <summary>
	/// Ensures the existing RunState has patron data set.
	/// Does NOT create or reset the RunState — safe to call in battle scenes.
	/// </summary>
	private void EnsurePatronData()
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null) return;

		var state = RunManager.Instance.State;

		// Only inject if patron is missing
		if (state.patronData == null)
		{
			InjectPatronData(state);
			Debug.Log($"[DialogueTest] Injected patron data: {state.patronData?.patronName} ({state.patronFaction})");
		}
	}

	/// <summary>
	/// Sets patron data on the given state, using the assigned PatronData or creating a fake.
	/// </summary>
	private void InjectPatronData(RunState state)
	{
		if (testPatronData != null)
		{
			state.patronData = testPatronData;
			state.patronFaction = testPatronData.faction;
		}
		else
		{
			var fakePatron = ScriptableObject.CreateInstance<PatronData>();
			fakePatron.patronName = testFaction switch
			{
				CardFaction.Wrath => "Cinder King",
				CardFaction.Pride => "Gilded Serpent",
				CardFaction.Ruin => "Stitch Prophet",
				_ => "Test Patron"
			};
			fakePatron.faction = testFaction;
			fakePatron.patronColor = testFaction switch
			{
				CardFaction.Wrath => new Color(0.9f, 0.25f, 0.15f),
				CardFaction.Pride => new Color(0.85f, 0.75f, 0.2f),
				CardFaction.Ruin => new Color(0.3f, 0.6f, 0.8f),
				_ => Color.white
			};
			state.patronData = fakePatron;
			state.patronFaction = testFaction;
		}
	}

	private void SetupDialogueManager()
	{
		// Already exists?
		if (PatronDialogueManager.Instance != null)
		{
			PatronDialogueManager.Instance.TryInitialize();
			Debug.Log("[DialogueTest] Using existing PatronDialogueManager singleton.");
			return;
		}

		// Try to instantiate the prefab
		if (patronDialoguePrefab != null)
		{
			Instantiate(patronDialoguePrefab);
			Debug.Log("[DialogueTest] Instantiated PatronDialogue prefab.");
			return;
		}

		// Try to find one already in the scene
		var existing = FindAnyObjectByType<PatronDialogueManager>();
		if (existing != null)
		{
			Debug.Log("[DialogueTest] Found existing PatronDialogueManager in scene.");
			return;
		}

		Debug.LogWarning("[DialogueTest] No PatronDialogueManager found! " +
			"Assign the PatronDialogue prefab to this component's Inspector slot.");
	}

	#region Context Menu Triggers — Right-click to fire

	[ContextMenu("1. Test Combat Start")]
	private void TestCombatStart()
	{
		if (!EnsureReady()) return;
		PatronDialogueManager.Instance.OnCombatStart("Test Enemy");
	}

	[ContextMenu("2. Test Big Damage Quip")]
	private void TestBigDamage()
	{
		if (!EnsureReady()) return;
		PatronDialogueManager.Instance.OnCombatStart("Setup"); // reset quip flag
		PatronDialogueManager.Instance.OnPlayerDealtDamage(20);
	}

	[ContextMenu("3. Test Low HP Quip")]
	private void TestLowHP()
	{
		if (!EnsureReady()) return;
		PatronDialogueManager.Instance.OnCombatStart("Setup"); // reset quip flag
		PatronDialogueManager.Instance.OnPlayerHPChanged(10, fakeMaxHP);
	}

	[ContextMenu("4. Test High Status Quip")]
	private void TestHighStatus()
	{
		if (!EnsureReady()) return;
		PatronDialogueManager.Instance.OnCombatStart("Setup"); // reset quip flag
		PatronDialogueManager.Instance.OnHighStatusStacks("Poison", 8);
	}

	[ContextMenu("5. Test Victory")]
	private void TestVictory()
	{
		if (!EnsureReady()) return;
		PatronDialogueManager.Instance.OnCombatEnd(true, 0.7f);
	}

	[ContextMenu("6. Test Close Call Victory")]
	private void TestCloseCallVictory()
	{
		if (!EnsureReady()) return;
		PatronDialogueManager.Instance.OnCombatEnd(true, 0.12f);
	}

	[ContextMenu("7. Test Reward Approval")]
	private void TestRewardApproval()
	{
		if (!EnsureReady()) return;
		var fakeCard = ScriptableObject.CreateInstance<CardData>();
		fakeCard.cardFaction1 = testPatronData != null ? testPatronData.faction : testFaction;
		PatronDialogueManager.Instance.OnRewardCardChosen(fakeCard);
	}

	[ContextMenu("8. Test Reward Disapproval")]
	private void TestRewardDisapproval()
	{
		if (!EnsureReady()) return;
		var fakeCard = ScriptableObject.CreateInstance<CardData>();
		fakeCard.cardFaction1 = testFaction == CardFaction.Wrath ? CardFaction.Pride : CardFaction.Wrath;
		PatronDialogueManager.Instance.OnRewardCardChosen(fakeCard);
	}

	[ContextMenu("9. Test Event Node")]
	private void TestEventNode()
	{
		if (!EnsureReady()) return;
		PatronDialogueManager.Instance.OnEventNodeEntered("Mysterious Shrine");
	}

	[ContextMenu("10. Test Boss Encounter")]
	private void TestBossEncounter()
	{
		if (!EnsureReady()) return;
		PatronDialogueManager.Instance.OnBossEncounterStart("The Hollow King");
	}

	[ContextMenu("--- Dismiss Current Line ---")]
	private void DismissLine()
	{
		if (!EnsureReady()) return;
		var box = FindAnyObjectByType<PatronDialogueBox>();
		if (box != null)
			box.Dismiss();
	}

	#endregion

	private bool EnsureReady()
	{
		if (!Application.isPlaying)
		{
			Debug.LogWarning("[DialogueTest] Must be in Play mode.");
			return false;
		}
		if (PatronDialogueManager.Instance == null)
		{
			Debug.LogWarning("[DialogueTest] No PatronDialogueManager found.");
			return false;
		}
		return true;
	}
}
