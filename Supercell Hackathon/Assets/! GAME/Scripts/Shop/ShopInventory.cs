using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates the shop's stock for a given run state.
/// Deterministic based on seed + shop visit count.
/// </summary>
public static class ShopInventory
{
	public static List<ShopItem> Generate(ShopData shopData, RunState runState)
	{
		// Unique seed per shop visit so each shop is different
		int shopSeed = runState.seed + (runState.shopVisitCount * 7919);
		System.Random rng = new System.Random(shopSeed);

		List<ShopItem> items = new();

		// 1. Patron-specific cards (only shop-eligible)
		CardData[] patronPool = FilterShopEligible(shopData.GetPatronCards(runState.patronFaction));
		if (patronPool.Length > 0)
		{
			CardData[] picked = PickRandom(rng, patronPool, shopData.patronCardCount);
			for (int i = 0; i < picked.Length; i++)
			{
				items.Add(new ShopItem
				{
					slotType = ShopItem.SlotType.PatronCard,
					card = picked[i],
					price = RollCardPrice(rng, shopData, picked[i]),
					isOnSale = false,
					isSold = false
				});
			}
		}

		// 2. Neutral cards (only shop-eligible)
		CardData[] neutralPool = FilterShopEligible(shopData.neutralCards);
		if (neutralPool != null && neutralPool.Length > 0)
		{
			CardData[] picked = PickRandom(rng, neutralPool, shopData.neutralCardCount);
			for (int i = 0; i < picked.Length; i++)
			{
				items.Add(new ShopItem
				{
					slotType = ShopItem.SlotType.NeutralCard,
					card = picked[i],
					price = RollCardPrice(rng, shopData, picked[i]),
					isOnSale = false,
					isSold = false
				});
			}
		}

		// 3. Item cards (only shop-eligible)
		CardData[] itemPool = FilterShopEligible(shopData.itemCards);
		if (itemPool != null && itemPool.Length > 0)
		{
			CardData[] picked = PickRandom(rng, itemPool, shopData.itemCardCount);
			for (int i = 0; i < picked.Length; i++)
			{
				items.Add(new ShopItem
				{
					slotType = ShopItem.SlotType.ItemCard,
					card = picked[i],
					price = shopData.RollPrice(rng, shopData.itemMinPrice, shopData.itemMaxPrice),
					isOnSale = false,
					isSold = false
				});
			}
		}

		// 4. Card removal
		items.Add(new ShopItem
		{
			slotType = ShopItem.SlotType.CardRemoval,
			card = null,
			price = shopData.GetCardRemovePrice(runState.cardRemoveCount),
			isOnSale = false,
			isSold = false
		});

		// 5. Mark one random card as on sale
		List<int> cardIndices = new();
		for (int i = 0; i < items.Count; i++)
		{
			if (items[i].card != null)
				cardIndices.Add(i);
		}

		if (cardIndices.Count > 0 && rng.NextDouble() < shopData.saleChance)
		{
			int saleIndex = cardIndices[rng.Next(cardIndices.Count)];
			items[saleIndex].isOnSale = true;
		}

		return items;
	}

	static int RollCardPrice(System.Random rng, ShopData data, CardData card)
	{
		// Price based on energy cost as a proxy for rarity
		// 0-2 energy = common, 3-4 = uncommon, 5+ = rare
		if (card.baseEnergyCost >= 5)
			return data.RollPrice(rng, data.rareMinPrice, data.rareMaxPrice);
		else if (card.baseEnergyCost >= 3)
			return data.RollPrice(rng, data.uncommonMinPrice, data.uncommonMaxPrice);
		else
			return data.RollPrice(rng, data.commonMinPrice, data.commonMaxPrice);
	}

	/// <summary>
	/// Filters out cards that are not shop-eligible (e.g. enemy-only cards).
	/// </summary>
	static CardData[] FilterShopEligible(CardData[] pool)
	{
		if (pool == null || pool.Length == 0) return pool ?? new CardData[0];

		List<CardData> eligible = new();
		for (int i = 0; i < pool.Length; i++)
		{
			if (pool[i] != null && pool[i].shopEligible)
				eligible.Add(pool[i]);
		}
		return eligible.ToArray();
	}

	static CardData[] PickRandom(System.Random rng, CardData[] pool, int count)
	{
		if (pool.Length <= count)
			return (CardData[])pool.Clone();

		// Fisher-Yates on a copy, take first N
		List<CardData> shuffled = new List<CardData>(pool);
		for (int i = shuffled.Count - 1; i > 0; i--)
		{
			int j = rng.Next(i + 1);
			(shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
		}

		CardData[] result = new CardData[count];
		for (int i = 0; i < count; i++)
			result[i] = shuffled[i];
		return result;
	}
}
