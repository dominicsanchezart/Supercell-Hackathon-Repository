using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RunState
{
	[Header("Resources")]
	public int gold;
	public int currentHP;
	public int maxHP;

	[Header("Deck")]
	public List<CardData> deck = new();

	[Header("Map")]
	public MapData mapData;

	[Header("Meta")]
	public int seed;
}
