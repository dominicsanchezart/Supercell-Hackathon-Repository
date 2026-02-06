using UnityEngine;

[CreateAssetMenu(fileName = "CardBorderData", menuName = "Scriptable Objects/CardBorderData", order = 1)]
public class CardBorderData : ScriptableObject
{
	public Sprite borderneautral;
	public Sprite borderWrath;
	public Sprite borderWrathTop;
	public Sprite borderWrathBottom;
	public Sprite borderPride;
	public Sprite borderPrideTop;
	public Sprite borderPrideBottom;
	public Sprite borderRuin;
	public Sprite borderRuinTop;
	public Sprite borderRuinBottom;



	public Sprite GetFullBorder(CardFaction faction)
	{
		switch (faction)
		{
			case CardFaction.Wrath:
				return borderWrath;
			case CardFaction.Pride:
				return borderPride;
			case CardFaction.Ruin:
				return borderRuin;
			default:
				return null;
		}
	}

	public Sprite GetTopBorder(CardFaction faction)
	{
		switch (faction)
		{
			case CardFaction.Wrath:
				return borderWrathTop;
			case CardFaction.Pride:
				return borderPrideTop;
			case CardFaction.Ruin:
				return borderRuinTop;
			default:
				return null;
		}
	}

	public Sprite GetBottomBorder(CardFaction faction)
	{
		switch (faction)
		{
			case CardFaction.Wrath:
				return borderWrathBottom;
			case CardFaction.Pride:
				return borderPrideBottom;
			case CardFaction.Ruin:
				return borderRuinBottom;
			default:
				return null;
		}
	}
}