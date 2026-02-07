using UnityEngine;

public enum CardActionType
{
	None,
    Damage,
	DamageAll,
	Heal,
	Guard,
	Empower,
	DrawCard,
	RemoveCard,
	ExhaustCard,
	SpendGold,
	Burn,
	Poison,
	Weaken,
	Fury,
	Energize,
	Dodge,
	DestroyCard,
	GiveEnergy,

	/// <summary>
	/// Deals damage equal to (maxHP - currentHP) + the card's base value.
	/// The base value acts as bonus flat damage on top of lost HP.
	/// </summary>
	DamageLostHP,

	/// <summary>
	/// Deals damage equal to (status effect stacks * base value).
	/// The status effect is chosen per-action slot on the CardData.
	/// </summary>
	DamagePerStack,
	GainGold,

	/// <summary>
	/// Heals equal to (status effect stacks * base value).
	/// The status effect is chosen per-action slot on the CardData.
	/// Consumes the stacks after use.
	/// </summary>
	HealPerStack
}