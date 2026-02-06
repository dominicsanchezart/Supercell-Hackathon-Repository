using UnityEngine;

[CreateAssetMenu(fileName = "New Card Data", menuName = "Scriptable Objects/Card Data")]
public class CardData : ScriptableObject
{
	[Header("Card Info")]
    public string cardName;
	public CardType cardType;
	public CardFaction cardFaciton1;
	public CardFaction cardFaciton2;


	[Header("Card Visuals")]
	public Sprite layer0;
	public Sprite layer1;
	public Sprite layer2;
	public Sprite layer3;


	[Header("Card Actions")]
	[Range(0, 7)] public int baseEnergyCost;
	public CardActionType actionType1;
	public int action1Value;
	public CardActionType actionType2;
	public int action2Value;
	public CardActionType actionType3;
	public int action3Value;
	[TextArea] public string actionDescription;


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