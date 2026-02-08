using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<CardData> deck;



	public void AddCardToDeck(CardData card)
	{
		deck.Add(card);
	}

	public void RemoveCardFromDeck(CardData card)
	{
		deck.Remove(card);
	}

	public void AssignDeck(List<CardData> newDeck)
	{
		deck = new List<CardData>(newDeck);
		ShuffleDeck();
	}

	/// <summary>
	/// Fisher-Yates shuffle — randomizes the deck in place without touching the source list.
	/// </summary>
	public void ShuffleDeck()
	{
		for (int i = deck.Count - 1; i > 0; i--)
		{
			int j = Random.Range(0, i + 1);
			(deck[i], deck[j]) = (deck[j], deck[i]);
		}
	}

	public void ClearDeck()
	{
		deck.Clear();
	}
}