using System.Collections;
using UnityEngine;

public class Arena : MonoBehaviour
{
	public Hand _character1;
	public Hand _character2;
	public bool _isPlayerTurn = true;
	private bool _battleOver = false;
	public bool IsViewingCards { get; private set; }

	[Header("Enemy AI")]
	[SerializeField] private float enemyActionDelay = 0.8f;

	[Header("Stage")]
	[SerializeField] private BattleStage battleStage;

	[Header("Card Viewer")]
	[SerializeField] private CardViewer cardViewer;



	private IEnumerator Start()
	{
		yield return null;

		if (battleStage != null)
			battleStage.Setup(_character1.characterInfo, _character2.characterInfo);

		if (cardViewer != null)
			cardViewer.onHideCards += () => IsViewingCards = false;

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
		yield return new WaitForSeconds(enemyActionDelay);

		bool playedCard = true;
		while (playedCard && !_battleOver)
		{
			playedCard = _character2.TryPlayRandomCard();
			if (playedCard)
			{
				if (CheckBattleOver()) yield break;
				yield return new WaitForSeconds(enemyActionDelay);
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
			return true;
		}

		if (!_character2.characterInfo.IsAlive())
		{
			_battleOver = true;
			Debug.Log("=== VICTORY! Enemy has been slain. ===");
			StopAllCoroutines();
			return true;
		}

		return false;
	}

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