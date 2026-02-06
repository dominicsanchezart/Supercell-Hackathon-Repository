using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Bootstrap for the Shop scene (LVL_Shop).
/// Reads RunState, generates shop inventory, and wires up the ShopView.
/// </summary>
public class ShopSceneController : MonoBehaviour
{
	[SerializeField] ShopView shopView;
	[SerializeField] ShopData shopData;

	void Start()
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null)
		{
			Debug.LogWarning("ShopSceneController: No RunManager or RunState. Cannot open shop.");
			return;
		}

		RunState state = RunManager.Instance.State;

		// Increment shop visit count for unique seed
		state.shopVisitCount++;

		// Generate shop inventory
		List<ShopItem> items = ShopInventory.Generate(shopData, state);

		// Initialize shop UI
		shopView.Initialize(items);
	}
}
