using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public List<CardData> deck;
	public Deck testDeck;



	private void Awake()
	{
		// Use the run's deck if available (from PatronData starter deck + any cards added during the run)
		if (RunManager.Instance != null && RunManager.Instance.State != null
			&& RunManager.Instance.State.deck.Count > 0)
		{
			AssignDeck(RunManager.Instance.State.deck);
		}
		else if (testDeck != null && testDeck.cards != null)
		{
			AssignDeck(testDeck.cards);
		}
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