using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI element representing a single active status effect icon with a stack count.
/// Attach to a prefab with an Image and a child TMP text.
/// </summary>
public class StatusEffectIcon : MonoBehaviour
{
	[SerializeField] private Image iconImage;
	[SerializeField] private TextMeshProUGUI stackText;

	public void Setup(Sprite icon, Color tint, int stacks)
	{
		iconImage.sprite = icon;
		iconImage.color = tint;
		stackText.text = stacks.ToString();
	}

	public void UpdateStacks(int stacks)
	{
		stackText.text = stacks.ToString();
	}
}
