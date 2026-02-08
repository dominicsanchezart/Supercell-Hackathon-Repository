using System.Collections;
using UnityEngine;

public class Arena : MonoBehaviour
{
	public Hand _character1;
	public Hand _character2;
	public bool _isPlayerTurn = true;
	private bool _battleOver = false;
	public bool IsViewingCards { get; private set; }

	[Header("Stage")]
	[SerializeField] private BattleStage battleStage;

	[Header("Card Viewer")]
	[SerializeField] private CardViewer cardViewer;

	[Header("Battle Result UI")]
	[SerializeField] private BattleRewardUI battleRewardUI;
	[SerializeField] private GameOverUI gameOverUI;

	[Header("Battle HUD")]
	[Tooltip("Root GameObject for all in-battle UI (HUDs, buttons, etc). Disabled when battle ends.")]
	[SerializeField] private GameObject battleUI;

	[Header("Victory Delay")]
	[SerializeField] private float victoryDelay = 1.0f;
	[SerializeField] private float defeatDelay = 1.5f;

	// Patron dialogue (found at runtime from persistent singleton)
	private PatronDialogueManager patronDialogue;

	// Patron passive tracking
	private PatronPassive _activePassive = PatronPassive.None;
	private bool _tookDamageThisTurn;       // Pride: tracks if player took damage this turn
	private bool _perfectFormActive;         // Pride: grants bonus at start of next turn
	private bool _emergencyProtocolTriggered; // Ruin: one-time trigger per combat

	private IEnumerator Start()
	{
		yield return null;

		// Find the persistent patron dialogue singleton
		patronDialogue = PatronDialogueManager.Instance;

		// Apply enemy preset from RunState (if available)
		ApplyEnemyPreset();

		// Sync run HP into battle (player may not be at max HP)
		if (RunManager.Instance != null && RunManager.Instance.State != null)
		{
			_character1.characterInfo.SetHealth(RunManager.Instance.State.currentHP);

			// Initialize patron passive from run state
			if (RunManager.Instance.State.patronData != null)
				_activePassive = RunManager.Instance.State.patronData.passive;
		}

		if (battleStage != null)
			battleStage.Setup(_character1.characterInfo, _character2.characterInfo);

		if (cardViewer != null)
			cardViewer.onHideCards += () => IsViewingCards = false;

		// Enable battle UI at the start of battle
		if (battleUI != null)
			battleUI.SetActive(true);

		// Fire combat start dialogue
		if (patronDialogue != null)
		{
			string enemyName = _character2.characterInfo._data != null
				? _character2.characterInfo._data.name : "Enemy";
			patronDialogue.OnCombatStart(enemyName);
		}

		StartPlayerTurn();
	}

	/// <summary>
	/// Applies the enemy preset from RunState to the enemy's CharacterInfo and Inventory.
	/// Falls back to whatever is baked into the scene if no preset is set.
	/// </summary>
	private void ApplyEnemyPreset()
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null) return;

		EnemyPreset preset = RunManager.Instance.State.currentEnemyPreset;
		if (preset == null) return;

		// Swap enemy CharacterData (stats, sprites, etc.)
		if (preset.characterData != null)
		{
			_character2.characterInfo._data = preset.characterData;
			_character2.characterInfo.SetupCharacter(); // Re-init HP/energy from new data
		}

		// Swap enemy deck
		if (preset.deck != null && preset.deck.cards != null)
		{
			_character2.characterInfo.GetInventory().AssignDeck(preset.deck.cards);
		}

		Debug.Log($"[Arena] Applied enemy preset: {preset.enemyName} " +
			$"(HP: {preset.characterData?.baseHealth}, Deck: {preset.deck?.cards?.Count ?? 0} cards)");
	}

	private void Update()
	{
		if (_battleOver || !_isPlayerTurn || IsViewingCards) return;

		if (Input.GetKeyDown(KeyCode.E))
		{
			EndPlayerTurn();
		}
	}

	public void StartPlayerTurn()
	{
		if (_battleOver) return;

		_isPlayerTurn = true;
		_tookDamageThisTurn = false;

		// Reset block at start of turn (like Slay the Spire)
		_character1.characterInfo.ResetBlock();

		// Process burn/poison damage at start of turn
		_character1.characterInfo.ProcessStartOfTurnEffects();
		if (CheckBattleOver()) return;

		// Patron passive: Perfect Form (Pride) — bonus from last turn's clean play
		if (_activePassive == PatronPassive.PerfectForm && _perfectFormActive)
		{
			_character1.characterInfo.GainEnergy(1);
			_character1.characterInfo.Heal(2);
			_perfectFormActive = false;
			Debug.Log("[Passive] Perfect Form: +1 Energy, Heal 2 (no damage taken last turn)");
		}

		// Reset dialogue per-turn tracking
		if (patronDialogue != null)
			patronDialogue.OnTurnStart();

		_character1.StartTurn();

		Debug.Log($"=== PLAYER TURN === HP: {_character1.characterInfo.GetHealth()} | Energy: {_character1.characterInfo.GetEnergy()}");
	}

	public void EndPlayerTurn()
	{
		if (!_isPlayerTurn || _battleOver) return;

		// Patron passive: Perfect Form (Pride) — if no damage taken, set bonus for next turn
		if (_activePassive == PatronPassive.PerfectForm)
		{
			_perfectFormActive = !_tookDamageThisTurn;
			if (_perfectFormActive)
				Debug.Log("[Passive] Perfect Form: No damage taken — bonus queued for next turn.");
		}

		// Decay weaken, reset empower at end of turn
		_character1.characterInfo.ProcessEndOfTurnEffects();
		_character1.EndTurn();

		StartEnemyTurn();
	}

	public void StartEnemyTurn()
	{
		if (_battleOver) return;

		_isPlayerTurn = false;

		_character2.characterInfo.ResetBlock();
		_character2.characterInfo.ProcessStartOfTurnEffects();
		if (CheckBattleOver()) return;

		_character2.StartTurn();

		Debug.Log($"=== ENEMY TURN === HP: {_character2.characterInfo.GetHealth()} | Energy: {_character2.characterInfo.GetEnergy()}");

		StartCoroutine(RunEnemyAI());
	}

	private IEnumerator RunEnemyAI()
	{
		float delay = _character2.characterInfo._data.attackDelay;
		yield return new WaitForSeconds(delay);

		bool playedCard = true;
		while (playedCard && !_battleOver)
		{
			playedCard = _character2.TryPlayRandomCard();
			if (playedCard)
			{
				if (CheckBattleOver()) yield break;
				yield return new WaitForSeconds(delay);
			}
		}

		if (!_battleOver)
			EndEnemyTurn();
	}

	[ContextMenu("End Enemy Turn")]
	public void EndEnemyTurn()
	{
		if (_battleOver) return;

		_character2.characterInfo.ProcessEndOfTurnEffects();
		_character2.EndTurn();

		StartPlayerTurn();
	}

	/// <summary>
	/// Instantly kills the enemy. For testing combat flow across the map.
	/// Right-click Arena in the Inspector → "Defeat Enemy (Test)".
	/// </summary>
	[ContextMenu("Defeat Enemy (Test)")]
	public void DefeatEnemyTest()
	{
		if (_battleOver) return;
		_character2.characterInfo.TakeDamage(99999);
		CheckBattleOver();
	}

	/// <summary>
	/// Resolves a card action using the ActionTarget to determine who gets affected.
	/// Self = caster, Enemy = opponent, All = both.
	/// </summary>
	public void ResolveAction(CardActionType type, int value, ActionTarget target, bool casterIsPlayer)
	{
		CharacterInfo caster = casterIsPlayer ? _character1.characterInfo : _character2.characterInfo;
		CharacterInfo opponent = casterIsPlayer ? _character2.characterInfo : _character1.characterInfo;
		Hand casterHand = casterIsPlayer ? _character1 : _character2;
		Hand opponentHand = casterIsPlayer ? _character2 : _character1;

		switch (target)
		{
			case ActionTarget.Self:
				ApplyAction(type, value, caster, casterHand, caster);
				break;
			case ActionTarget.Enemy:
				ApplyAction(type, value, opponent, opponentHand, caster);
				break;
			case ActionTarget.All:
				ApplyAction(type, value, caster, casterHand, caster);
				ApplyAction(type, value, opponent, opponentHand, caster);
				break;
		}
	}

	/// <summary>
	/// Returns the opponent CharacterInfo for the given side.
	/// </summary>
	public CharacterInfo GetOpponent(bool forPlayer)
	{
		return forPlayer ? _character2.characterInfo : _character1.characterInfo;
	}

	private void ApplyAction(CardActionType type, int value, CharacterInfo target, Hand targetHand, CharacterInfo source)
	{
		// Values arrive pre-modified by CardModifiers (empower, weaken, dodge, etc.)
		switch (type)
		{
			case CardActionType.None:
				break;

			case CardActionType.Damage:
			case CardActionType.DamageAll:
				target.TakeDamage(value);
				if (target == _character1.characterInfo)
				{
					_tookDamageThisTurn = true;
					ApplyDamagePassives();
				}
				// Player dealing damage to enemy — notify dialogue
				if (source == _character1.characterInfo && target == _character2.characterInfo)
					NotifyPlayerDealtDamage(value);
				break;

			case CardActionType.Heal:
				target.Heal(value);
				break;

			case CardActionType.Guard:
				target.GainBlock(value);
				break;

			case CardActionType.Empower:
				target.Empower(value);
				break;

			case CardActionType.DrawCard:
				for (int i = 0; i < value; i++)
					targetHand.DrawCardFromDeck();
				break;

			case CardActionType.RemoveCard:
				// for (int i = 0; i < value; i++)
					targetHand.RemoveRandomCard();
				break;

			case CardActionType.ExhaustCard:
				targetHand.ExhaustCard();
				break;

			case CardActionType.SpendGold:
				target.TrySpendGold(value);
				break;

			case CardActionType.Burn:
				target.ApplyBurn(value);
				break;

			case CardActionType.Poison:
				target.ApplyPoison(value);
				break;

			case CardActionType.Weaken:
				target.ApplyWeaken(value);
				break;

			case CardActionType.Fury:
				target.ApplyFury(value);
				break;

			case CardActionType.Energize:
				target.ApplyEnergized(value);
				break;

			case CardActionType.Dodge:
				target.ApplyDodge(value);
				break;

			case CardActionType.DestroyCard:
				targetHand.DestroyCard();
				break;

			case CardActionType.GiveEnergy:
				target.GainEnergy(value);
				break;

			case CardActionType.DamageLostHP:
				// value already includes lostHP + base + modifiers (computed by CardModifiers)
				target.TakeDamage(value);
				if (target == _character1.characterInfo)
				{
					_tookDamageThisTurn = true;
					ApplyDamagePassives();
				}
				if (source == _character1.characterInfo && target == _character2.characterInfo)
					NotifyPlayerDealtDamage(value);
				break;

			case CardActionType.DamagePerStack:
				// value already includes (stacks * base) + modifiers (computed by CardModifiers)
				target.TakeDamage(value);
				if (target == _character1.characterInfo)
				{
					_tookDamageThisTurn = true;
					ApplyDamagePassives();
				}
				if (source == _character1.characterInfo && target == _character2.characterInfo)
					NotifyPlayerDealtDamage(value);
				break;

			case CardActionType.GainGold:
				target.GainGold(value);
				break;

			case CardActionType.HealPerStack:
				// value already includes (stacks * base) (computed by CardModifiers)
				target.Heal(value);
				break;

			default:
				Debug.LogWarning("Unhandled card action type: " + type);
				break;
		}
	}

	/// <summary>
	/// Called whenever the player takes card damage. Applies relevant patron passives
	/// and notifies the dialogue manager.
	/// </summary>
	private void ApplyDamagePassives()
	{
		// Bleed Out (Wrath): gain 2 Fury when taking card damage
		if (_activePassive == PatronPassive.BleedOut)
		{
			_character1.characterInfo.ApplyFury(2);
			Debug.Log("[Passive] Bleed Out: +2 Fury from taking card damage.");
		}

		// Emergency Protocol (Ruin): first time dropping below 50% HP, gain defensive surge
		if (_activePassive == PatronPassive.EmergencyProtocol && !_emergencyProtocolTriggered)
		{
			int currentHP = _character1.characterInfo.GetHealth();
			int maxHP = _character1.characterInfo._data.baseHealth;
			if (currentHP > 0 && currentHP < maxHP / 2)
			{
				_emergencyProtocolTriggered = true;
				_character1.characterInfo.GainBlock(5);
				_character1.characterInfo.ApplyDodge(2);
				_character1.DrawCardFromDeck();
				_character1.DrawCardFromDeck();
				Debug.Log("[Passive] Emergency Protocol: +5 Guard, +2 Dodge, Draw 2 (dropped below 50% HP)");
			}
		}

		// Notify dialogue manager: player took damage (low HP quip check)
		if (patronDialogue != null)
		{
			patronDialogue.OnPlayerHPChanged(
				_character1.characterInfo.GetHealth(),
				_character1.characterInfo._data.baseHealth);
		}
	}

	/// <summary>
	/// Called after player deals damage to the enemy. Notifies dialogue for quip triggers.
	/// </summary>
	private void NotifyPlayerDealtDamage(int value)
	{
		if (patronDialogue != null)
		{
			patronDialogue.OnPlayerDealtDamage(value);
			CheckEnemyStatusForDialogue();
		}
	}

	private void CheckEnemyStatusForDialogue()
	{
		if (patronDialogue == null) return;
		CharacterInfo enemy = _character2.characterInfo;

		if (enemy.GetPoison() >= 5)
			patronDialogue.OnHighStatusStacks("Poison", enemy.GetPoison());
		else if (enemy.GetBurn() >= 5)
			patronDialogue.OnHighStatusStacks("Burn", enemy.GetBurn());
		else if (enemy.GetWeaken() >= 5)
			patronDialogue.OnHighStatusStacks("Weaken", enemy.GetWeaken());
	}

	public bool CheckBattleOver()
	{
		if (_battleOver) return true;

		if (!_character1.characterInfo.IsAlive())
		{
			_battleOver = true;
			Debug.Log("=== DEFEAT! Player has been slain. ===");
			StopAllCoroutines();
			StartCoroutine(HandleDefeat());
			return true;
		}

		if (!_character2.characterInfo.IsAlive())
		{
			_battleOver = true;
			Debug.Log("=== VICTORY! Enemy has been slain. ===");
			StopAllCoroutines();
			StartCoroutine(HandleVictory());
			return true;
		}

		return false;
	}

	#region Battle Result Handling

	private IEnumerator HandleVictory()
	{
		// Brief pause so the player sees the killing blow land
		yield return new WaitForSeconds(victoryDelay);

		// Hide the player's hand
		_character1.ClearHand();

		// Disable battle UI
		if (battleUI != null)
			battleUI.SetActive(false);

		// Slide enemies off screen
		if (battleStage != null)
			yield return battleStage.SlideOut();

		// Sync HP back to the run state
		SyncPlayerStateToRun();

		// Fire combat end dialogue
		if (patronDialogue != null)
		{
			float hpPercent = _character1.characterInfo._data.baseHealth > 0
				? (float)_character1.characterInfo.GetHealth() / _character1.characterInfo._data.baseHealth
				: 1f;
			patronDialogue.OnCombatEnd(true, hpPercent);
		}

		// Show card reward UI
		if (battleRewardUI != null)
		{
			battleRewardUI.ShowRewards(
				chosenCard =>
				{
					// Add chosen card to the player's inventory and run state
					_character1.characterInfo.GetInventory().AddCardToDeck(chosenCard);

					if (RunManager.Instance != null && RunManager.Instance.State != null)
						RunManager.Instance.State.deck.Add(chosenCard);

					Debug.Log($"Added {chosenCard.cardName} to deck.");

					// Fire reward card chosen dialogue
					if (patronDialogue != null)
						patronDialogue.OnRewardCardChosen(chosenCard);
				},
				() =>
				{
					// After choosing (or skipping), return to the map
					ReturnToMap();
				}
			);
		}
		else
		{
			// No reward UI assigned — go straight back to map
			Debug.LogWarning("Arena: No BattleRewardUI assigned. Returning to map immediately.");
			ReturnToMap();
		}
	}

	private IEnumerator HandleDefeat()
	{
		yield return new WaitForSeconds(defeatDelay);

		// Hide the player's hand
		_character1.ClearHand();

		// Disable battle UI
		if (battleUI != null)
			battleUI.SetActive(false);

		// Show game over UI
		if (gameOverUI != null)
		{
			gameOverUI.ShowDefeat(() =>
			{
				// Placeholder: return to main menu
				ReturnToMainMenu();
			});
		}
		else
		{
			Debug.LogWarning("Arena: No GameOverUI assigned. Returning to main menu immediately.");
			ReturnToMainMenu();
		}
	}

	private void SyncPlayerStateToRun()
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null) return;

		RunManager.Instance.State.currentHP = _character1.characterInfo.GetHealth();
	}

	private void ReturnToMap()
	{
		// Disable CardViewer before scene unload to prevent null ref errors
		if (cardViewer != null)
			cardViewer.enabled = false;

		if (RunManager.Instance != null)
		{
			RunManager.Instance.OnEncounterComplete();
		}
		else
		{
			Debug.LogWarning("Arena: No RunManager found. Cannot return to map.");
		}
	}

	private void ReturnToMainMenu()
	{
		// Disable CardViewer before scene unload to prevent null ref errors
		if (cardViewer != null)
			cardViewer.enabled = false;

		if (RunManager.Instance != null)
		{
			RunManager.Instance.EndRun();
		}
		else
		{
			// Fallback: just load a scene by name
			Debug.Log("No RunManager — loading main menu scene directly (placeholder).");
			UnityEngine.SceneManagement.SceneManager.LoadScene(0);
		}
	}

	#endregion

	#region Card Viewer Buttons

	public void ShowDeck()
	{
		if (cardViewer == null) return;
		IsViewingCards = true;
		cardViewer.DisplayCards(_character1.characterInfo.GetInventory().deck.ToArray());
	}

	public void ShowDiscardPile()
	{
		if (cardViewer == null) return;
		IsViewingCards = true;
		cardViewer.DisplayCards(_character1.discardPile.ToArray());
	}

	public void ShowExhaustPile()
	{
		if (cardViewer == null) return;
		IsViewingCards = true;
		cardViewer.DisplayCards(_character1.exhaustPile.ToArray());
	}

	public void HideCardViewer()
	{
		if (cardViewer == null) return;
		cardViewer.HideCards();
		// IsViewingCards is reset by the onHideCards callback
	}

	#endregion
}