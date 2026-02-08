using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Shop Data", menuName = "Scriptable Objects/Shop Data")]
public class ShopData : ScriptableObject
{
	[Header("Card Pools (Deck SOs — drag Deck assets here)")]
	[Tooltip("Patron-specific card pools")]
	public Deck wrathDeck;
	public Deck prideDeck;
	public Deck ruinDeck;
	[Tooltip("Neutral cards available to all patrons")]
	public Deck neutralDeck;
	[Tooltip("Item cards")]
	public Deck itemDeck;

	[Header("Shop Layout")]
	[Tooltip("Number of patron-specific cards offered")]
	public int patronCardCount = 6;
	[Tooltip("Number of neutral cards offered")]
	public int neutralCardCount = 0;
	[Tooltip("Number of item cards offered")]
	public int itemCardCount = 2;

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

	/// <summary>
	/// Returns the card list for the given patron faction from the corresponding Deck SO.
	/// </summary>
	public CardData[] GetPatronCards(CardFaction faction)
	{
		Deck deck = faction switch
		{
			CardFaction.Wrath => wrathDeck,
			CardFaction.Pride => prideDeck,
			CardFaction.Ruin => ruinDeck,
			_ => neutralDeck
		};

		return GetCardsFromDeck(deck);
	}

	/// <summary>
	/// Returns the neutral card pool.
	/// </summary>
	public CardData[] GetNeutralCards()
	{
		return GetCardsFromDeck(neutralDeck);
	}

	/// <summary>
	/// Returns the item card pool.
	/// </summary>
	public CardData[] GetItemCards()
	{
		return GetCardsFromDeck(itemDeck);
	}

	private CardData[] GetCardsFromDeck(Deck deck)
	{
		if (deck == null || deck.cards == null || deck.cards.Count == 0)
			return new CardData[0];

		return deck.cards.ToArray();
	}
}
