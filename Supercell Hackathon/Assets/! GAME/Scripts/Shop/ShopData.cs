using UnityEngine;

[CreateAssetMenu(fileName = "New Shop Data", menuName = "Scriptable Objects/Shop Data")]
public class ShopData : ScriptableObject
{
	[Header("Card Pools (assign all available cards here)")]
	[Tooltip("All patron-specific cards that can appear in shops")]
	public CardData[] wrathCards;
	public CardData[] prideCards;
	public CardData[] ruinCards;
	[Tooltip("Neutral cards available to all patrons")]
	public CardData[] neutralCards;
	[Tooltip("Item cards")]
	public CardData[] itemCards;

	[Header("Shop Layout")]
	[Tooltip("Number of patron-specific cards offered")]
	public int patronCardCount = 3;
	[Tooltip("Number of neutral cards offered")]
	public int neutralCardCount = 1;
	[Tooltip("Number of item cards offered")]
	public int itemCardCount = 1;

	[Header("Pricing")]
	public int commonMinPrice = 35;
	public int commonMaxPrice = 45;
	public int uncommonMinPrice = 60;
	public int uncommonMaxPrice = 80;
	public int rareMinPrice = 115;
	public int rareMaxPrice = 145;
	public int itemMinPrice = 45;
	public int itemMaxPrice = 65;

	[Header("Card Removal")]
	public int cardRemoveBasePrice = 50;
	public int cardRemovePriceIncrease = 25;

	[Header("Sale")]
	[Range(0f, 1f)] public float saleChance = 0.5f;
	[Range(0f, 1f)] public float saleDiscount = 0.5f;

	[Header("Camp Settings")]
	[Range(0f, 1f)] public float campHealPercent = 0.30f;

	public int GetCardRemovePrice(int timesUsed)
	{
		return cardRemoveBasePrice + (cardRemovePriceIncrease * timesUsed);
	}

	public int RollPrice(System.Random rng, int min, int max)
	{
		return min + rng.Next(max - min + 1);
	}

	public CardData[] GetPatronCards(CardFaction faction)
	{
		return faction switch
		{
			CardFaction.Wrath => wrathCards ?? new CardData[0],
			CardFaction.Pride => prideCards ?? new CardData[0],
			CardFaction.Ruin => ruinCards ?? new CardData[0],
			_ => neutralCards ?? new CardData[0]
		};
	}
}
