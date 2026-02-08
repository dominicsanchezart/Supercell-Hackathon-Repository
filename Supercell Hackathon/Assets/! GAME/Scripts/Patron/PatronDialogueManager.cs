using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates patron dialogue across the entire run.
/// Persists via DontDestroyOnLoad (singleton pattern, like RunManager).
///
/// Any scene can call PatronDialogueManager.Instance to trigger dialogue.
/// Combat triggers come from Arena; event/shop/camp scenes can call
/// OnEventNodeEntered(), OnBossEncounterStart(), etc.
///
/// Two layers:
///   1. Scripted templates (always available)
///   2. NeoCortex AI (optional, with timeout fallback to scripted)
///
/// Rules:
///   - Only ONE mid-combat quip per combat
///   - Lines don't repeat until entire pool is exhausted (across the run)
///   - Player never knows which layer (scripted/AI) responded
///
/// Setup:
///   Create a "PatronDialogue" prefab with:
///     - PatronDialogueManager (this script)
///     - NeoCortexProvider (child or same GO, optional)
///     - PatronDialogueBox (child GO with SpriteRenderer + TextMeshPro)
///   Place it in the first scene (map scene) or instantiate from RunManager.
/// </summary>
public class PatronDialogueManager : MonoBehaviour
{
	public static PatronDialogueManager Instance { get; private set; }

	[Header("References")]
	[SerializeField] private PatronDialogueBox dialogueBox;

	[Header("Dialogue Data")]
	[SerializeField] private PatronDialogueData wrathDialogue;
	[SerializeField] private PatronDialogueData prideDialogue;
	[SerializeField] private PatronDialogueData ruinDialogue;

	[Header("NeoCortex (Optional)")]
	[SerializeField] private NeoCortexProvider neoCortexProvider;

	// Active dialogue for the current patron
	private PatronDialogueData _activeDialogue;
	private CardFaction _patronFaction;
	private bool _initialized;

	// Prevent repeated lines
	private readonly HashSet<string> _usedLines = new();

	// Mid-combat quip tracking
	private bool _midCombatQuipFired;
	private int _turnDamageDealt;

	/// <summary>
	/// Returns true if the dialogue box is currently showing a line.
	/// </summary>
	public bool IsDialogueActive => dialogueBox != null && dialogueBox.IsShowing;

	/// <summary>
	/// Access the underlying dialogue box to subscribe to onDialogueFinished events.
	/// </summary>
	public PatronDialogueBox DialogueBox => dialogueBox;

	private void Awake()
	{
		if (Instance != null && Instance != this)
		{
			Destroy(gameObject);
			return;
		}
		Instance = this;
		DontDestroyOnLoad(gameObject);
	}

	private void Start()
	{
		TryInitialize();
	}

	/// <summary>
	/// Called when a new scene loads. Re-resolves the active patron dialogue
	/// in case we haven't initialized yet (e.g. patron was chosen after this GO was created).
	/// </summary>
	private void OnEnable()
	{
		UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
	}

	private void OnDisable()
	{
		UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
	{
		// Re-initialize if patron data changed (e.g. new run started)
		TryInitialize();

		// Dismiss any lingering dialogue from the previous scene
		if (dialogueBox != null)
			dialogueBox.Dismiss();
	}

	/// <summary>
	/// Resolves the active patron's dialogue data from the current RunState.
	/// Safe to call multiple times — will re-resolve if the patron faction changed.
	/// </summary>
	public void TryInitialize()
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null)
		{
			_activeDialogue = null;
			_initialized = false;
			return;
		}

		CardFaction faction = RunManager.Instance.State.patronFaction;

		// Skip if already initialized for this faction
		if (_initialized && faction == _patronFaction)
			return;

		_patronFaction = faction;
		_activeDialogue = _patronFaction switch
		{
			CardFaction.Wrath => wrathDialogue,
			CardFaction.Pride => prideDialogue,
			CardFaction.Ruin => ruinDialogue,
			_ => null
		};
		_initialized = true;

		Debug.Log($"[PatronDialogue] Initialized for {_patronFaction} patron.");
	}

	/// <summary>
	/// Resets all dialogue state for a brand new run.
	/// Called when the player starts a new run.
	/// </summary>
	public void ResetForNewRun()
	{
		_usedLines.Clear();
		_midCombatQuipFired = false;
		_turnDamageDealt = 0;
		_initialized = false;
		TryInitialize();
	}

	#region Public API — Called by Arena or any scene

	/// <summary>
	/// One line when a new fight begins. Always scripted.
	/// </summary>
	public void OnCombatStart(string enemyName)
	{
		TryInitialize();
		ResetForNewCombat();

		if (_activeDialogue == null) return;
		string line = PickUnusedLine(_activeDialogue.combatStartLines);
		if (!string.IsNullOrEmpty(line))
			ShowLine(line);
	}

	/// <summary>
	/// Called when player deals damage to the enemy. Accumulates per-turn.
	/// Fires a quip if total this turn >= 15.
	/// </summary>
	public void OnPlayerDealtDamage(int amount)
	{
		_turnDamageDealt += amount;

		if (_midCombatQuipFired) return;
		if (_turnDamageDealt < 15) return;

		_midCombatQuipFired = true;
		TryShowQuip(_activeDialogue?.bigDamageLines, DialogueTriggerType.MidCombatQuip,
			$"Player dealt {_turnDamageDealt} damage in a single turn.");
	}

	/// <summary>
	/// Called when player HP changes. Fires quip if below 25%.
	/// </summary>
	public void OnPlayerHPChanged(int currentHP, int maxHP)
	{
		if (_midCombatQuipFired) return;
		if (currentHP <= 0 || maxHP <= 0) return;
		if ((float)currentHP / maxHP >= 0.25f) return;

		_midCombatQuipFired = true;
		TryShowQuip(_activeDialogue?.lowHPLines, DialogueTriggerType.MidCombatQuip,
			$"Player dropped to {currentHP}/{maxHP} HP (critical).");
	}

	/// <summary>
	/// Called when a high status stack is detected on the enemy.
	/// </summary>
	public void OnHighStatusStacks(string statusName, int stacks)
	{
		if (_midCombatQuipFired) return;

		_midCombatQuipFired = true;
		TryShowQuip(_activeDialogue?.highStatusLines, DialogueTriggerType.MidCombatQuip,
			$"Enemy has {stacks} stacks of {statusName}.");
	}

	/// <summary>
	/// Called when combat ends. Shows victory or close-call line.
	/// </summary>
	public void OnCombatEnd(bool victory, float hpPercent)
	{
		if (!victory || _activeDialogue == null) return;

		string[] pool = hpPercent < 0.2f
			? _activeDialogue.closeCallVictoryLines
			: _activeDialogue.victoryLines;

		string line = PickUnusedLine(pool);
		if (!string.IsNullOrEmpty(line))
			ShowLine(line);
	}

	/// <summary>
	/// Called when the player picks a reward card. Approval if matches patron faction.
	/// </summary>
	public void OnRewardCardChosen(CardData card)
	{
		if (_activeDialogue == null) return;

		bool matchesFaction = card.cardFaction1 == _patronFaction || card.cardFaction2 == _patronFaction;
		string[] pool = matchesFaction ? _activeDialogue.approvalLines : _activeDialogue.disapprovalLines;

		string line = PickUnusedLine(pool);
		if (!string.IsNullOrEmpty(line))
			ShowLine(line);
	}

	/// <summary>
	/// Called at the start of a boss encounter. Uses NeoCortex with scripted fallback.
	/// </summary>
	public void OnBossEncounterStart(string bossName)
	{
		TryInitialize();
		TryShowQuip(_activeDialogue?.bossStartFallbackLines, DialogueTriggerType.BossEncounterStart,
			$"Boss encounter starting against {bossName}.");
	}

	/// <summary>
	/// Called when an event node is entered. Uses NeoCortex with scripted fallback.
	/// </summary>
	public void OnEventNodeEntered(string eventName)
	{
		TryInitialize();
		TryShowQuip(_activeDialogue?.eventFallbackLines, DialogueTriggerType.EventNodeEntered,
			$"Entered event: {eventName}.");
	}

	/// <summary>
	/// Resets per-turn tracking. Called at the start of each player turn.
	/// </summary>
	public void OnTurnStart()
	{
		_turnDamageDealt = 0;
	}

	#endregion

	#region Internal

	private void ResetForNewCombat()
	{
		_midCombatQuipFired = false;
		_turnDamageDealt = 0;
		// Don't clear _usedLines — they persist across the run to avoid repeats
	}

	/// <summary>
	/// Tries NeoCortex AI first (if available), falls back to scripted pool.
	/// </summary>
	private void TryShowQuip(string[] fallbackPool, DialogueTriggerType trigger, string description)
	{
		if (_activeDialogue == null) return;

		// Try NeoCortex AI if provider exists
		if (neoCortexProvider != null)
		{
			string context = neoCortexProvider.BuildContext(
				trigger,
				PatronAffinityTracker.GetActivePatronTier(),
				_patronFaction,
				description
			);

			neoCortexProvider.RequestLine(context,
				aiLine =>
				{
					// AI success — show the generated line
					ShowLine(aiLine);
				},
				() =>
				{
					// AI failed/timeout — fall back to scripted
					string line = PickUnusedLine(fallbackPool);
					if (!string.IsNullOrEmpty(line))
						ShowLine(line);
				}
			);
		}
		else
		{
			// No AI provider — just use scripted
			string line = PickUnusedLine(fallbackPool);
			if (!string.IsNullOrEmpty(line))
				ShowLine(line);
		}
	}

	/// <summary>
	/// Picks a random unused line from the pool. If all are used, resets the pool.
	/// </summary>
	private string PickUnusedLine(string[] pool)
	{
		if (pool == null || pool.Length == 0) return null;

		// Collect unused lines
		List<string> available = new();
		for (int i = 0; i < pool.Length; i++)
		{
			if (!string.IsNullOrEmpty(pool[i]) && !_usedLines.Contains(pool[i]))
				available.Add(pool[i]);
		}

		// If all used, reset for this pool
		if (available.Count == 0)
		{
			for (int i = 0; i < pool.Length; i++)
				_usedLines.Remove(pool[i]);

			for (int i = 0; i < pool.Length; i++)
			{
				if (!string.IsNullOrEmpty(pool[i]))
					available.Add(pool[i]);
			}
		}

		if (available.Count == 0) return null;

		string picked = available[Random.Range(0, available.Count)];
		_usedLines.Add(picked);
		return picked;
	}

	private void ShowLine(string text)
	{
		if (dialogueBox != null)
			dialogueBox.ShowLine(text);

		Debug.Log($"[Patron] {text}");
	}

	#endregion
}
