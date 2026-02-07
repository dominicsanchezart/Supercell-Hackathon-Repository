using UnityEngine;

public enum StatusEffects
{
    Burn, // sets card on fire which decreases status per card effected (max of hand size), if that card is used, it will deal damage to the target
	Poison, // deals damage to the target at the end of their turn
	Weakened, // reduces the damage of the target's cards by the number of stacks for the rest of their turn
	Fury, // increases the damage of the target's cards by the number of stacks for the rest of their turn
	Energized, // adds energy to the target when used, but is removed at the end of the turn
	Dodge // increases the amount of guard recieved when using a guard card by the number of stacks for the rest of the turn
}