using UnityEngine;

[CreateAssetMenu(fileName = "New Global Card Data", menuName = "Scriptable Objects/Global Card Data")]
public class GlobalCardData : ScriptableObject
{
	[Header("Wrath Borders")]
    public Sprite WrathBorderFull;
    public Sprite WrathBorderTop;
    public Sprite WrathBorderBottom;

	[Header("Pride Borders")]
    public Sprite PrideBorderFull;
    public Sprite PrideBorderTop;
    public Sprite PrideBorderBottom;

	[Header("Ruin Borders")]
    public Sprite RuinBorderFull;
    public Sprite RuinBorderTop;
    public Sprite RuinBorderBottom;
}