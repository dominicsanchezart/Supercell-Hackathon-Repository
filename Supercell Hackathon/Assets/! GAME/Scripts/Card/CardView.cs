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

    int[] relativeOrders;
	int canvasSortingLayer = 20;

    private Color[] originalColors;
    private static readonly Color burnTint = new Color(1f, 0.45f, 0.3f, 1f);



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

    public void SetBurning(bool burning)
    {
        isBurning = burning;
        for (int i = 0; i < sprites.Length; i++)
        {
            sprites[i].color = burning ? burnTint : originalColors[i];
        }
    }
}