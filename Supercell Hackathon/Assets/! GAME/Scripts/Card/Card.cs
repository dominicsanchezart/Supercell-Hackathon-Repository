using UnityEngine;

public class Card : MonoBehaviour
{
    [field: SerializeField] public CardData cardData { get; private set; }
	[SerializeField] private GameObject mask;



	public void EnableMask()
	{
		mask.SetActive(true);
	}

	public void DisableMask()
	{
		mask.SetActive(false);
	}
}