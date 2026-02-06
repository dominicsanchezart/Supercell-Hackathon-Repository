using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CardView : MonoBehaviour
{
    public float smoothSpeed = 12f;

    [HideInInspector] public Hand owner;

    SpriteRenderer[] sprites;
	[SerializeField] GameObject mask;
	[SerializeField] private SpriteRenderer layer0;
	[SerializeField] private SpriteRenderer layer1;
	[SerializeField] private SpriteRenderer layer2;
	[SerializeField] private SpriteRenderer gradient;
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
		layer0.maskInteraction = enabled ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
		layer1.maskInteraction = enabled ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
		layer2.maskInteraction = enabled ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
		gradient.maskInteraction = enabled ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
		mask.SetActive(enabled);
	}

    // void OnMouseDown()
    // {
    //     owner.OnCardClicked(this);
    // }
}
