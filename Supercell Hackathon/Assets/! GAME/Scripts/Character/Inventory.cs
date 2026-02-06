using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<CardData> deck;
	public Deck testDeck;



	private void Awake()
	{
		AssignDeck(testDeck.cards);
	}

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
		deck = newDeck;
	}

	public void ClearDeck()
	{
		deck.Clear();
	}
}