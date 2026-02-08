using UnityEngine;

[CreateAssetMenu(fileName = "New Map Icon Set", menuName = "Scriptable Objects/Map Icon Set")]
public class MapIconSet : ScriptableObject
{
	[Header("Encounter Icons")]
	public Sprite battleMinionIcon;
	public Sprite battleBossIcon;
	public Sprite shopIcon;
	public Sprite campIcon;
	public Sprite eventIcon;
	public Sprite treasureIcon;

	[Header("Node Background")]
	public Sprite nodeBackground;

	public Sprite GetIcon(EncounterType type)
	{
		return type switch
		{
			EncounterType.BattleMinion => battleMinionIcon,
			EncounterType.BattleBoss => battleBossIcon,
			EncounterType.Shop => shopIcon,
			EncounterType.Camp => campIcon,
			EncounterType.Event => eventIcon,
			EncounterType.Treasure => treasureIcon,
			_ => null
		};
	}
}
