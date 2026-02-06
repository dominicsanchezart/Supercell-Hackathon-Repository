using UnityEngine;

/// <summary>
/// Bootstrap for the Camp scene (LVL_Camp).
/// Reads ShopData for heal percent config, initializes CampView.
/// </summary>
public class CampSceneController : MonoBehaviour
{
	[SerializeField] CampView campView;
	[SerializeField] ShopData shopData;

	void Start()
	{
		if (RunManager.Instance == null || RunManager.Instance.State == null)
		{
			Debug.LogWarning("CampSceneController: No RunManager or RunState.");
			return;
		}

		float healPercent = 0.30f;
		if (shopData != null)
			healPercent = shopData.campHealPercent;

		campView.Initialize(healPercent);
	}
}
