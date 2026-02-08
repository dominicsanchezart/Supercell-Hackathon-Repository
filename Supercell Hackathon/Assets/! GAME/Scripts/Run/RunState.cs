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

	[Header("Patron")]
	public PatronData patronData;
	public CardFaction patronFaction;

	[Header("Affinity")]
	public Dictionary<CardFaction, int> affinityPoints = new();

	[Header("Current Encounter")]
	public EnemyPreset currentEnemyPreset;

	[Header("Meta")]
	public int seed;
	public int shopVisitCount;
	public int cardRemoveCount;
}
