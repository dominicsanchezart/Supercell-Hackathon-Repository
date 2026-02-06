using System.Collections;
using UnityEngine;

public class Arena : MonoBehaviour
{
    public Hand _character1;
	public Hand _character2;
	public bool _isPlayerTurn = true;



	private IEnumerator Start()
	{
		yield return null;
		StartPlayerTurn();
	}

	public void StartPlayerTurn()
	{
		// start player turn, enable card selection, etc.
		_isPlayerTurn = true;
		_character1.StartTurn();
	}

	public void EndPlayerTurn()
	{
		if (!_isPlayerTurn) return;
		// end player turn, start enemy turn
		// enemy plays cards, applies effects, then ends turn
		// back to player turn
		_character1.EndTurn();
		StartEnemyTurn();
	}

	public void StartEnemyTurn()
	{
		// start enemy turn, enable card selection, etc.
		_isPlayerTurn = false;
		_character2.StartTurn();
	}

	[ContextMenu("End Enemy Turn")]
	public void EndEnemyTurn()
	{
		// end enemy turn, start player turn
		_character2.EndTurn();
		StartPlayerTurn();
	}

	public void ResolveAction(CardActionType type, int value, bool isPlayer)
	{
		CharacterInfo target = isPlayer ? _character1.characterInfo : _character2.characterInfo;
		Hand targetHand = isPlayer ? _character1 : _character2;
		switch (type)
		{
			case CardActionType.None:
				break;

			case CardActionType.Damage:
				target.TakeDamage(value);
				break;

			case CardActionType.DamageAll:
				// target.TakeDamage(value);
				break;

			case CardActionType.Heal:
				target.Heal(value);
				break;

			case CardActionType.Guard:
				target.GainBlock(target, value);
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
				// target.SpendGold(value);
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

			default:
				Debug.LogWarning("Unhandled card action type: " + type);
				break;
		}
	}
}