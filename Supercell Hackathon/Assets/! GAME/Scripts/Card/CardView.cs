using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class CardView : MonoBehaviour
{
    [HideInInspector] public Hand owner;
    [HideInInspector] public bool isBurning;

    SpriteRenderer[] sprites;
	[SerializeField] GameObject mask;
	[SerializeField] private SpriteRenderer[] masedSprites;
	[SerializeField] private Canvas canvas;
	private SpriteMask spriteMask;

    int[] relativeOrders;
	int canvasSortingLayer = 20;

    private Color[] originalColors;
    private static readonly Color burnTint = new Color(1f, 0.45f, 0.3f, 1f);
    private const int MASK_LEEWAY = 10;



    void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>();

        relativeOrders = new int[sprites.Length];
        originalColors = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            relativeOrders[i] = sprites[i].sortingOrder;
            originalColors[i] = sprites[i].color;
        }

        if (mask != null)
        {
            spriteMask = mask.GetComponent<SpriteMask>();
            mask.SetActive(true);
        }

        // Default all masked sprites to visible inside mask
        foreach (var sprite in masedSprites)
        {
            sprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }
    }

    public void SetSortingOrder(int baseOrder)
    {
        int minOrder = int.MaxValue;
        int maxOrder = int.MinValue;

        for (int i = 0; i < sprites.Length; i++)
        {
            int order = baseOrder + relativeOrders[i];
            sprites[i].sortingOrder = order;
            if (order < minOrder) minOrder = order;
            if (order > maxOrder) maxOrder = order;
        }

		int canvasOrder = canvasSortingLayer + baseOrder;
		canvas.sortingOrder = canvasOrder;
		if (canvasOrder > maxOrder) maxOrder = canvasOrder;
		if (canvasOrder < minOrder) minOrder = canvasOrder;

        // Keep mask range tight around this card's actual sorting layers
        if (spriteMask != null)
        {
            spriteMask.backSortingOrder = minOrder - MASK_LEEWAY;
            spriteMask.frontSortingOrder = maxOrder + MASK_LEEWAY;
            spriteMask.isCustomRangeActive = true;
        }
    }

	public void SetMasked(bool enabled)
	{
		foreach (var sprite in masedSprites)
		{
			sprite.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
		}
	}

    public void SetBurning(bool burning)
    {
        isBurning = burning;
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i].color = burning ? burnTint : originalColors[i];
        }
    }
}