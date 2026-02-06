using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CardView : MonoBehaviour
{
    [HideInInspector] public Hand owner;

    SpriteRenderer[] sprites;
	[SerializeField] GameObject mask;
	[SerializeField] private SpriteRenderer[] masedSprites;
	[SerializeField] private Canvas canvas;

    int[] relativeOrders;
	int canvasSortingLayer = 20;



    void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>();

        relativeOrders = new int[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            relativeOrders[i] = sprites[i].sortingOrder;
        }
    }

    public void SetSortingOrder(int baseOrder)
    {
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i].sortingOrder = baseOrder + relativeOrders[i];
        }

		canvas.sortingOrder = canvasSortingLayer + baseOrder;
    }

	public void SetMasked(bool enabled)
	{
		foreach (var sprite in masedSprites)
		{
			sprite.maskInteraction = enabled ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
		}
		mask.SetActive(enabled);
	}
}