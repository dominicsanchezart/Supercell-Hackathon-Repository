using System.Collections;
using UnityEngine;

/// <summary>
/// Spawns a temporary sprite that moves from one position to another over a set duration,
/// then destroys itself. Used for card play visual effects (slash, shield, spell glow, etc.).
/// </summary>
public class CardPlayEffect : MonoBehaviour
{
	/// <summary>
	/// Creates and animates a card play effect sprite.
	/// </summary>
	/// <param name="sprite">The sprite to display.</param>
	/// <param name="start">World position to start at.</param>
	/// <param name="end">World position to move toward.</param>
	/// <param name="duration">How long the movement takes (matches pose duration).</param>
	/// <param name="flipX">Whether to flip the sprite horizontally (for right-to-left effects).</param>
	/// <param name="scale">Scale multiplier for the effect sprite.</param>
	/// <param name="sortingLayer">Sorting layer name.</param>
	/// <param name="sortingOrder">Sorting order (should be above characters).</param>
	public static CardPlayEffect Spawn(
		Sprite sprite,
		Vector3 start,
		Vector3 end,
		float duration,
		bool flipX = false,
		float scale = 1f,
		string sortingLayer = "Character",
		int sortingOrder = 50)
	{
		GameObject go = new GameObject("CardPlayEffect");
		SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
		sr.sprite = sprite;
		sr.sortingLayerName = sortingLayer;
		sr.sortingOrder = sortingOrder;
		sr.flipX = flipX;

		go.transform.position = start;
		go.transform.localScale = Vector3.one * scale;

		CardPlayEffect effect = go.AddComponent<CardPlayEffect>();
		effect.StartCoroutine(effect.MoveAndDestroy(start, end, duration));
		return effect;
	}

	private IEnumerator MoveAndDestroy(Vector3 start, Vector3 end, float duration)
	{
		float elapsed = 0f;
		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			// Ease-out for a snappy feel
			float eased = 1f - (1f - t) * (1f - t);
			transform.position = Vector3.Lerp(start, end, eased);
			yield return null;
		}

		transform.position = end;
		Destroy(gameObject);
	}
}
