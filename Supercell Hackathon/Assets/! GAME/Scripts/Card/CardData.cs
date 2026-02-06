using UnityEngine;

[CreateAssetMenu(fileName = "New Card Data", menuName = "Scriptable Objects/Card Data")]
public class CardData : ScriptableObject
{
	[Header("Card Info")]
    public string cardName;
	public CardType cardType;
	[TextArea] public string cardDescription;
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


	[Header("Upgrades")]
	public bool canUpgrade;
	public CardData upgradedCard;
}