using TMPro;
using UnityEngine;

/// <summary>
/// Shows contextual info about the currently selected/hovered card.
/// Attach to a UI panel with child TextMeshProUGUI fields and wire up a Hand reference.
/// </summary>
public class CardInfoPopup : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private Hand hand;
	[SerializeField] private GameObject popupRoot;

	[Header("Text Fields")]
	[SerializeField] private TextMeshProUGUI cardNameText;
	[SerializeField] private TextMeshProUGUI cardTypeText;
	[SerializeField] private TextMeshProUGUI energyCostText;
	[SerializeField] private TextMeshProUGUI descriptionText;
	[SerializeField] private TextMeshProUGUI action1Text;
	[SerializeField] private TextMeshProUGUI action2Text;
	[SerializeField] private TextMeshProUGUI action3Text;
	[SerializeField] private TextMeshProUGUI statusEffectsText;

	private CardData lastShownCard;

	private void Update()
	{
		CardData selected = GetSelectedCardData();

		if (selected == null)
		{
			Hide();
			return;
		}

		if (selected != lastShownCard)
			Show(selected);
	}

	private CardData GetSelectedCardData()
	{
		if (hand == null) return null;
		if (hand.cardsInHand.Count == 0) return null;

		// Use reflection-free access: selectedIndex is private, so we read from the public list
		// The hand exposes cardsInHand and the card views are children of handRoot
		int index = GetSelectedIndex();
		if (index < 0 || index >= hand.cardsInHand.Count) return null;

		return hand.cardsInHand[index];
	}

	/// <summary>
	/// Reads the selected index from the Hand's card views.
	/// Override or adjust if you expose selectedIndex publicly.
	/// </summary>
	private int GetSelectedIndex()
	{
		// Find which card view is currently scaled up (selected)
		if (hand.handRoot == null) return -1;

		int best = -1;
		float bestY = float.MinValue;

		for (int i = 0; i < hand.handRoot.childCount; i++)
		{
			Transform child = hand.handRoot.GetChild(i);
			// The selected card has the highest local Y position (lifted)
			if (child.localPosition.y > bestY)
			{
				bestY = child.localPosition.y;
				best = i;
			}
		}

		return best;
	}

	private void Show(CardData data)
	{
		lastShownCard = data;
		popupRoot.SetActive(true);

		// Card name
		if (cardNameText != null)
			cardNameText.text = data.cardName;

		// Card type
		if (cardTypeText != null)
			cardTypeText.text = GetCardTypeText(data.cardType);

		// Energy cost
		if (energyCostText != null)
			energyCostText.text = $"{data.baseEnergyCost} Energy";

		// Description
		if (descriptionText != null)
			descriptionText.text = data.GetDescription();

		// Action breakdowns
		if (action1Text != null)
			action1Text.text = FormatAction(data.actionType1, data.action1Value, data.actionTarget1);

		if (action2Text != null)
		{
			bool hasAction2 = data.actionType2 != CardActionType.None;
			action2Text.gameObject.SetActive(hasAction2);
			if (hasAction2)
				action2Text.text = FormatAction(data.actionType2, data.action2Value, data.actionTarget2);
		}

		if (action3Text != null)
		{
			bool hasAction3 = data.actionType3 != CardActionType.None;
			action3Text.gameObject.SetActive(hasAction3);
			if (hasAction3)
				action3Text.text = FormatAction(data.actionType3, data.action3Value, data.actionTarget3);
		}

		// Status effects summary
		if (statusEffectsText != null)
		{
			string effects = GetStatusEffectsText(data);
			statusEffectsText.gameObject.SetActive(!string.IsNullOrEmpty(effects));
			statusEffectsText.text = effects;
		}
	}

	private void Hide()
	{
		if (popupRoot != null)
			popupRoot.SetActive(false);

		lastShownCard = null;
	}

	#region Text Formatting

	private static string GetCardTypeText(CardType type)
	{
		return type switch
		{
			CardType.Attack  => "<color=#FF6666>Attack</color>",
			CardType.Defense => "<color=#6699FF>Defense</color>",
			CardType.Item    => "<color=#FFCC44>Item</color>",
			CardType.Curse   => "<color=#CC44FF>Curse</color>",
			CardType.Spell   => "<color=#44DDFF>Spell</color>",
			_                => type.ToString()
		};
	}

	private static string FormatAction(CardActionType type, int value, ActionTarget target)
	{
		string targetLabel = target switch
		{
			ActionTarget.Self  => "self",
			ActionTarget.Enemy => "enemy",
			ActionTarget.All   => "all",
			_                  => ""
		};

		return type switch
		{
			CardActionType.Damage      => $"Deal <color=#FF6666>{value}</color> damage to {targetLabel}",
			CardActionType.DamageAll   => $"Deal <color=#FF6666>{value}</color> damage to {targetLabel}",
			CardActionType.Heal        => $"Heal <color=#66FF66>{value}</color> HP to {targetLabel}",
			CardActionType.Guard       => $"Gain <color=#6699FF>{value}</color> Block",
			CardActionType.Empower     => $"Gain <color=#FFCC44>{value}</color> Empower",
			CardActionType.DrawCard    => $"Draw <color=#FFFFFF>{value}</color> card(s)",
			CardActionType.RemoveCard  => $"Remove a random card from {targetLabel}'s hand",
			CardActionType.ExhaustCard => $"Exhaust a card from {targetLabel}'s hand",
			CardActionType.SpendGold   => $"Spend <color=#FFD700>{value}</color> Gold",
			CardActionType.Burn        => $"Apply <color=#FF4400>{value}</color> Burn to {targetLabel}",
			CardActionType.Poison      => $"Apply <color=#88FF00>{value}</color> Poison to {targetLabel}",
			CardActionType.Weaken      => $"Apply <color=#AA88FF>{value}</color> Weaken to {targetLabel}",
			CardActionType.None        => "",
			_                          => $"{type} ({value})"
		};
	}

	/// <summary>
	/// Returns a combined string of any status-effect-related actions on the card.
	/// </summary>
	private static string GetStatusEffectsText(CardData data)
	{
		string result = "";

		void Append(CardActionType type, int value)
		{
			string line = type switch
			{
				CardActionType.Burn   => $"<color=#FF4400>Burn</color>: Ignites random cards in hand. Each burned card played deals damage to you.",
				CardActionType.Poison => $"<color=#88FF00>Poison</color>: Deals {value} damage at the start of the target's turn.",
				CardActionType.Weaken => $"<color=#AA88FF>Weaken</color>: Reduces the target's damage output.",
				_                     => null
			};
			if (line != null)
				result += (result.Length > 0 ? "\n" : "") + line;
		}

		Append(data.actionType1, data.action1Value);
		Append(data.actionType2, data.action2Value);
		Append(data.actionType3, data.action3Value);

		return result;
	}

	#endregion
}
