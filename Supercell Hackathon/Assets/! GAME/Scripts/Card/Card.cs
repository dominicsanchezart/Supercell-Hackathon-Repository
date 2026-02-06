using TMPro;
using UnityEngine;

public class Card : MonoBehaviour
{
    [field: SerializeField] public CardData cardData { get; private set; }
	[SerializeField] private GameObject mask;


	[Header("Visuals")]
	[SerializeField] private SpriteRenderer layer0;
	[SerializeField] private SpriteRenderer layer1;
	[SerializeField] private SpriteRenderer layer2;
	[SerializeField] private SpriteRenderer layer3;


	[Header("Border")]
	[SerializeField] private SpriteRenderer borderFull;
	[SerializeField] private SpriteRenderer borderTop;
	[SerializeField] private SpriteRenderer borderBottom;
	[SerializeField] private Sprite borderWrath;
	[SerializeField] private Sprite borderWrathTop;
	[SerializeField] private Sprite borderWrathBottom;
	[SerializeField] private Sprite borderPride;
	[SerializeField] private Sprite borderPrideTop;
	[SerializeField] private Sprite borderPrideBottom;
	[SerializeField] private Sprite borderRuin;
	[SerializeField] private Sprite borderRuinTop;
	[SerializeField] private Sprite borderRuinBottom;


	[Header("Text")]
	[SerializeField] private TextMeshProUGUI nameText;
	[SerializeField] private TextMeshProUGUI typeText;
	[SerializeField] private TextMeshProUGUI action1Text;



	public void EnableMask()
	{
		mask.SetActive(true);
	}

	public void DisableMask()
	{
		mask.SetActive(false);
	}

	private void Start()
	{
		SetupVisuals();
		SetupBorder();
		SetupText();
	}

	private void SetupBorder()
	{
		// if borders are the same then use single border, otherwise split
		if (cardData.cardFaciton1 == cardData.cardFaciton2)
		{
			borderFull.gameObject.SetActive(true);
			borderTop.gameObject.SetActive(false);
			borderBottom.gameObject.SetActive(false);


			switch (cardData.cardFaciton1)
			{
				case CardFaction.Wrath:
					borderFull.sprite = borderWrath;
					break;
				case CardFaction.Pride:
					borderFull.sprite = borderPride;
					break;
				case CardFaction.Ruin:
					borderFull.sprite = borderRuin;
					break;
			}
		}
		else
		{
			borderFull.gameObject.SetActive(false);
			borderTop.gameObject.SetActive(true);
			borderBottom.gameObject.SetActive(true);

			switch (cardData.cardFaciton1)
			{
				case CardFaction.Wrath:
					borderTop.sprite = borderWrathTop;
					break;
				case CardFaction.Pride:
					borderTop.sprite = borderPrideTop;
					break;
				case CardFaction.Ruin:
					borderTop.sprite = borderRuinTop;
					break;
			}

			switch (cardData.cardFaciton2)
			{
				case CardFaction.Wrath:
					borderBottom.sprite = borderWrathBottom;
					break;
				case CardFaction.Pride:
					borderBottom.sprite = borderPrideBottom;
					break;
				case CardFaction.Ruin:
					borderBottom.sprite = borderRuinBottom;
					break;
			}
		}
	}

	private void SetupVisuals()
	{
		layer0.sprite = cardData.layer0;
		layer1.sprite = cardData.layer1;
		layer2.sprite = cardData.layer2;
		layer3.sprite = cardData.layer3;
	}

	private void SetupText()
	{
		nameText.text = cardData.cardName;
		typeText.text = cardData.cardType.ToString();
		action1Text.text = cardData.GetDescription();
	}
}