using TMPro;
using UnityEngine;

public class Card : MonoBehaviour
{
    [field: SerializeField] public CardData cardData { get; private set; }
    [field: SerializeField] public CardBorderData cardBorderData { get; private set; }
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


	[Header("Text")]
	[SerializeField] private TextMeshProUGUI nameText;
	[SerializeField] private TextMeshProUGUI typeText;
	[SerializeField] private TextMeshProUGUI action1Text;



	public void SetCardData(CardData data)
	{
		cardData = data;
		SetupVisuals();
		SetupBorder();
		SetupText();
	}

	private void SetupBorder()
	{
		if (cardData.cardFaciton1 == CardFaction.None)
		{
			borderFull.gameObject.SetActive(true);
			borderTop.gameObject.SetActive(false);
			borderBottom.gameObject.SetActive(false);
			borderFull.sprite = cardBorderData.borderneautral;
			return;
		}

		if (cardData.cardFaciton1 == cardData.cardFaciton2)
		{
			borderFull.gameObject.SetActive(true);
			borderTop.gameObject.SetActive(false);
			borderBottom.gameObject.SetActive(false);

			borderFull.sprite = cardBorderData.GetFullBorder(cardData.cardFaciton1);
		}
		else
		{
			borderFull.gameObject.SetActive(false);
			borderTop.gameObject.SetActive(true);
			borderBottom.gameObject.SetActive(true);

			borderTop.sprite = cardBorderData.GetTopBorder(cardData.cardFaciton1);
			borderBottom.sprite = cardBorderData.GetBottomBorder(cardData.cardFaciton2);
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