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
		ClearPrefetch();
		TryInitialize();

		// Reset NeoCortex session so each run gets fresh AI context
		if (neoCortexProvider != null)
			neoCortexProvider.CleanSessionOnNewRun();
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
		string desc =
			$"Your warlock enters combat against {enemyName}. " +
			"They draw their opening hand " +
			"and steel themselves for the fight.";
		TryShowQuip(
			_activeDialogue.combatStartLines,
			DialogueTriggerType.CombatStart, desc);
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
		string dmgDesc =
			$"Your warlock just unleashed a devastating blow, " +
			$"dealing {_turnDamageDealt} damage in one turn. " +
			"The enemy reels from the impact.";
		TryShowQuip(
			_activeDialogue?.bigDamageLines,
			DialogueTriggerType.MidCombatQuip, dmgDesc);
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
		string hpDesc =
			$"Your warlock has been beaten down to " +
			$"{currentHP}/{maxHP} HP. " +
			"They are bleeding and barely standing. " +
			"They might die here.";
		TryShowQuip(
			_activeDialogue?.lowHPLines,
			DialogueTriggerType.MidCombatQuip, hpDesc);
	}

	/// <summary>
	/// Called when a high status stack is detected on the enemy.
	/// </summary>
	public void OnHighStatusStacks(string statusName, int stacks)
	{
		if (_midCombatQuipFired) return;

		_midCombatQuipFired = true;
		string statusDesc =
			$"Your warlock's enemy writhes under {stacks} " +
			$"stacks of {statusName}. The effects compound " +
			"and the foe is falling apart.";
		TryShowQuip(
			_activeDialogue?.highStatusLines,
			DialogueTriggerType.MidCombatQuip, statusDesc);
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

		string desc;
		if (hpPercent < 0.2f)
			desc = "Your warlock barely survived. " +
				"They are torn open, bleeding, " +
				"clinging to life by a thread.";
		else if (hpPercent > 0.75f)
			desc = "Your warlock crushed the enemy " +
				"and still stands strong. " +
				"Barely a scratch on them.";
		else
			desc = "Your warlock won the fight. " +
				"They took some hits but the " +
				"enemy lies dead at their feet.";

		TryShowQuip(pool,
			DialogueTriggerType.CombatEnd, desc);
	}

	/// <summary>
	/// Called when the player picks a reward card. Approval if matches patron faction.
	/// </summary>
	public void OnRewardCardChosen(CardData card)
	{
		if (_activeDialogue == null) return;

		bool matchesFaction =
			card.cardFaction1 == _patronFaction ||
			card.cardFaction2 == _patronFaction;
		string[] pool = matchesFaction
			? _activeDialogue.approvalLines
			: _activeDialogue.disapprovalLines;

		string cardFaction = card.cardFaction1.ToString();
		string desc;
		if (matchesFaction)
			desc = $"Your warlock chose {card.cardName}, " +
				$"a {cardFaction} contract that feeds " +
				"your power. They honor your pact.";
		else
			desc = $"Your warlock chose {card.cardName}, " +
				$"a {cardFaction} contract. That power " +
				"belongs to a rival patron, not you.";

		TryShowQuip(pool,
			DialogueTriggerType.RewardCardChosen, desc);
	}

	/// <summary>
	/// Called at the start of a boss encounter. Uses NeoCortex with scripted fallback.
	/// </summary>
	public void OnBossEncounterStart(string bossName)
	{
		TryInitialize();
		string desc =
			$"Your warlock faces {bossName}, " +
			"the most dangerous foe they have " +
			"encountered. This could be the end.";
		TryShowQuip(
			_activeDialogue?.bossStartFallbackLines,
			DialogueTriggerType.BossEncounterStart, desc);
	}

	/// <summary>
	/// Called when an event node is entered. Uses NeoCortex with scripted fallback.
	/// </summary>
	public void OnEventNodeEntered(string eventName)
	{
		TryInitialize();
		string desc =
			$"Your warlock has stumbled into {eventName}. " +
			"Something lurks in the shadows here. " +
			"The outcome is uncertain.";
		TryShowQuip(
			_activeDialogue?.eventFallbackLines,
			DialogueTriggerType.EventNodeEntered, desc);
	}

	/// <summary>
	/// Called when the player plays a card from a rival faction in combat.
	/// Only fires once per combat to avoid spam.
	/// </summary>
	public void OnRivalCardPlayed(CardData card)
	{
		if (_midCombatQuipFired) return;
		if (_activeDialogue == null) return;

		bool isRival =
			card.cardFaction1 != CardFaction.None &&
			card.cardFaction1 != _patronFaction;

		if (!isRival) return;

		_midCombatQuipFired = true;
		string faction = card.cardFaction1.ToString();
		string desc =
			$"Your warlock just played {card.cardName}, " +
			$"a {faction} contract. That power belongs " +
			"to a rival patron. They spit on your pact.";
		TryShowQuip(
			_activeDialogue.rivalCardPlayedLines,
			DialogueTriggerType.RivalCardPlayed, desc);
	}

	/// <summary>
	/// Called when the player buys a card at the shop.
	/// Reacts based on whether the card matches the patron's faction.
	/// </summary>
	public void OnCardPurchased(CardData card)
	{
		if (_activeDialogue == null) return;

		bool matchesFaction =
			card.cardFaction1 == _patronFaction ||
			card.cardFaction2 == _patronFaction;
		string faction = card.cardFaction1.ToString();

		string desc;
		string[] pool;
		if (matchesFaction)
		{
			desc = $"Your warlock spent gold on {card.cardName}, " +
				$"a {faction} contract that strengthens " +
				"your hold over them. A wise investment.";
			pool = _activeDialogue.loyalCardPurchasedLines;
		}
		else
		{
			bool isRival =
				card.cardFaction1 != CardFaction.None;
			if (isRival)
				desc = $"Your warlock wasted gold on {card.cardName}, " +
					$"a {faction} contract. They spent your " +
					"tribute buying power from a rival.";
			else
				desc = $"Your warlock bought {card.cardName}, " +
					"a neutral contract. Not your power, " +
					"but not a rival's either.";
			pool = _activeDialogue.rivalCardPurchasedLines;
		}

		TryShowQuip(pool,
			DialogueTriggerType.CardPurchased, desc);
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
	/// Shows an AI line if one was prefetched, otherwise tries AI live with scripted fallback.
	/// Prefetch cache is consumed on use (one-shot).
	/// </summary>
	private void TryShowQuip(string[] fallbackPool, DialogueTriggerType trigger, string description)
	{
		if (_activeDialogue == null) return;

		// Check if we have a prefetched AI line ready for this trigger
		if (_prefetchedLine != null && _prefetchedTrigger == trigger)
		{
			string cached = _prefetchedLine;
			ClearPrefetch();
			Debug.Log($"[PatronDialogue] Using prefetched AI line for {trigger}");
			ShowLine(cached);
			return;
		}

		// No prefetch — try AI with scripted fallback
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
					if (!string.IsNullOrEmpty(aiLine))
						ShowLine(aiLine);
				},
				() =>
				{
					// AI failed — fall back to scripted
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

	#endregion

	#region Prefetch System

	private string _prefetchedLine;
	private DialogueTriggerType _prefetchedTrigger;

	/// <summary>
	/// Call this BEFORE the trigger fires (e.g. when player clicks a map node)
	/// to give the AI a head start. The response is cached and consumed by the
	/// next TryShowQuip call for the matching trigger type.
	/// </summary>
	public void PrefetchLine(DialogueTriggerType trigger, string description)
	{
		if (neoCortexProvider == null) return;
		TryInitialize();
		if (_activeDialogue == null) return;

		// Clear any stale prefetch
		ClearPrefetch();

		string context = neoCortexProvider.BuildContext(
			trigger,
			PatronAffinityTracker.GetActivePatronTier(),
			_patronFaction,
			description
		);

		Debug.Log($"[PatronDialogue] Prefetching AI line for {trigger}...");

		neoCortexProvider.RequestLine(context,
			aiLine =>
			{
				_prefetchedLine = aiLine;
				_prefetchedTrigger = trigger;
				Debug.Log($"[PatronDialogue] Prefetch READY for {trigger}: \"{aiLine}\"");
			},
			() =>
			{
				Debug.Log($"[PatronDialogue] Prefetch failed for {trigger} — will use scripted.");
			},
			isPrefetch: true
		);
	}

	private void ClearPrefetch()
	{
		_prefetchedLine = null;
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
