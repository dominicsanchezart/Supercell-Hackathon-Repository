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



	private IEnumerator Start()
	{
		yield return null;

		if (battleStage != null)
			battleStage.Setup(_character1.characterInfo, _character2.characterInfo);

		if (cardViewer != null)
			cardViewer.onHideCards += () => IsViewingCards = false;

		// Enable battle UI at the start of battle
		if (battleUI != null)
			battleUI.SetActive(true);

		StartPlayerTurn();
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

		// Reset block at start of turn (like Slay the Spire)
		_character1.characterInfo.ResetBlock();

		// Process burn/poison damage at start of turn
		_character1.characterInfo.ProcessStartOfTurnEffects();
		if (CheckBattleOver()) return;

		_character1.StartTurn();

		Debug.Log($"=== PLAYER TURN === HP: {_character1.characterInfo.GetHealth()} | Energy: {_character1.characterInfo.GetEnergy()}");
	}

	public void EndPlayerTurn()
	{
		if (!_isPlayerTurn || _battleOver) return;

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
				break;

			case CardActionType.DamagePerStack:
				// value already includes (stacks * base) + modifiers (computed by CardModifiers)
				target.TakeDamage(value);
				break;

			case CardActionType.GainGold:
				target.GainGold(value);
				break;

			default:
				Debug.LogWarning("Unhandled card action type: " + type);
				break;
		}
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