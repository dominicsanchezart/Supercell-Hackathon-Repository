using UnityEngine;

/// <summary>
/// Tiny helper added at runtime to cards in the removal panel.
/// Routes OnMouseDown clicks back to ShopView.OnRemoveCardSelected().
/// </summary>
public class RemovalCardClick : MonoBehaviour
{
	ShopView shopView;
	int deckIndex;

	public void Initialize(ShopView view, int index)
	{
		shopView = view;
		deckIndex = index;
	}

	void OnMouseDown()
	{
		if (shopView != null)
			shopView.OnRemoveCardSelected(deckIndex);
	}
}
