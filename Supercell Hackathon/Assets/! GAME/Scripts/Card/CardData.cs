using UnityEngine;

[CreateAssetMenu(fileName = "New Card Data", menuName = "Scriptable Objects/Card Data")]
public class CardData : ScriptableObject
{
	[Header("Card Info")]
    public string cardName;
	public CardType cardType;
	public CardFaction cardFaction1;
	public CardFaction cardFaction2;


	[Header("Card Visuals")]
	public Sprite layer0;
	public Sprite layer1;
	public Sprite layer2;
	public Sprite layer3;


	[Header("Card Actions")]
	[Range(0, 3)] public int baseEnergyCost;
	[TextArea] public string actionDescription;


	[Header("Action 1: Mandatory")]
	public CardActionType actionType1;
	public ActionTarget actionTarget1;
	[Min(0)] public int action1Value;
	[Tooltip("Status effect used by DamagePerStack.")]
	public StatusEffectType action1StatusEffect;
	public ActionConditionData action1Condition;


	[Header("Action 2: Optional")]
	public CardActionType actionType2;
	public ActionTarget actionTarget2;
	[Min(0)] public int action2Value;
	[Tooltip("Status effect used by DamagePerStack.")]
	public StatusEffectType action2StatusEffect;
	public ActionConditionData action2Condition;


	[Header("Action 3: Optional")]
	public CardActionType actionType3;
	public ActionTarget actionTarget3;
	[Min(0)] public int action3Value;
	[Tooltip("Status effect used by DamagePerStack.")]
	public StatusEffectType action3StatusEffect;
	public ActionConditionData action3Condition;


	[Header("Upgrades")]
	public bool canUpgrade;
	public CardData upgradedCard;



	// will need to read from card modifier to replace actual values
	// probably move this logic to a card visuals class or something similar
	// <color=red>
	public string GetDescription()
	{
		if (string.IsNullOrEmpty(actionDescription))
			return "";

		// Replace placeholders with actual values
		string desc = actionDescription;
		desc = desc.Replace("{action1Value}", action1Value.ToString());
		desc = desc.Replace("{action2Value}", action2Value.ToString());
		desc = desc.Replace("{action3Value}", action3Value.ToString());

		return desc;
	}
}