/// <summary>
/// Runtime data for a single shop slot. Not a MonoBehaviour — just data.
/// </summary>
[System.Serializable]
public class ShopItem
{
	public enum SlotType { PatronCard, NeutralCard, ItemCard, CardRemoval }

	public SlotType slotType;
	public CardData card;       // null for CardRemoval
	public int price;
	public bool isOnSale;
	public bool isSold;

	public int GetDisplayPrice()
	{
		return isOnSale ? UnityEngine.Mathf.RoundToInt(price * 0.5f) : price;
	}
}
