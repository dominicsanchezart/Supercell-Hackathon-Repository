using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the map-to-encounter transition:
/// 1. Selected node plays a "pop" scale animation
/// 2. Screen fades to black via a full-screen UI Image overlay
/// 3. Callback fires once fade is complete
///
/// Setup: Attach to a Canvas with a full-screen Image child (black, starts transparent).
/// Wire the fadeImage reference in the Inspector.
/// </summary>
public class MapTransition : MonoBehaviour
{
	[Header("References")]
	[Tooltip("Full-screen UI Image used for fade. Should be black with alpha=0.")]
	public Image fadeImage;

	[Header("Node Animation")]
	[Tooltip("How large the node pops before fading")]
	public float popScale = 1.4f;
	[Tooltip("Duration of the pop animation")]
	public float popDuration = 0.25f;

	[Header("Fade")]
	[Tooltip("Duration of the fade to black")]
	public float fadeDuration = 0.4f;
	[Tooltip("Delay between pop finishing and fade starting")]
	public float delayBeforeFade = 0.1f;

	bool isTransitioning;

	void Awake()
	{
		// Ensure fade image starts fully transparent and non-blocking
		if (fadeImage != null)
		{
			fadeImage.color = new Color(0f, 0f, 0f, 0f);
			fadeImage.raycastTarget = false;
		}
	}

	/// <summary>
	/// Plays the full transition sequence. Call this instead of immediately loading the scene.
	/// </summary>
	public void PlayTransition(Transform selectedNode, Action onComplete)
	{
		if (isTransitioning) return;
		isTransitioning = true;
		StartCoroutine(TransitionSequence(selectedNode, onComplete));
	}

	/// <summary>
	/// Fades from black back to clear. Call when returning to the map.
	/// </summary>
	public void FadeIn(float duration = -1f)
	{
		if (duration < 0f) duration = fadeDuration;
		StartCoroutine(FadeAlpha(1f, 0f, duration, () =>
		{
			if (fadeImage != null)
				fadeImage.raycastTarget = false;
		}));
	}

	IEnumerator TransitionSequence(Transform selectedNode, Action onComplete)
	{
		// 1. Pop animation on the selected node
		if (selectedNode != null)
			yield return PopNode(selectedNode);

		// 2. Brief pause
		yield return new WaitForSeconds(delayBeforeFade);

		// 3. Fade to black
		if (fadeImage != null)
			fadeImage.raycastTarget = true; // block input during fade

		yield return FadeAlpha(0f, 1f, fadeDuration, null);

		isTransitioning = false;

		// 4. Fire callback (load the scene)
		onComplete?.Invoke();
	}

	IEnumerator PopNode(Transform node)
	{
		Vector3 originalScale = node.localScale;
		Vector3 targetScale = originalScale * popScale;
		float elapsed = 0f;

		// Ease out (scale up with deceleration)
		while (elapsed < popDuration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / popDuration;
			// Ease out cubic: 1 - (1-t)^3
			float ease = 1f - (1f - t) * (1f - t) * (1f - t);
			node.localScale = Vector3.Lerp(originalScale, targetScale, ease);
			yield return null;
		}

		node.localScale = targetScale;
	}

	IEnumerator FadeAlpha(float from, float to, float duration, Action onDone)
	{
		if (fadeImage == null)
		{
			onDone?.Invoke();
			yield break;
		}

		float elapsed = 0f;
		Color c = fadeImage.color;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = Mathf.Clamp01(elapsed / duration);
			// Ease in-out for smooth feel
			float ease = t * t * (3f - 2f * t);
			c.a = Mathf.Lerp(from, to, ease);
			fadeImage.color = c;
			yield return null;
		}

		c.a = to;
		fadeImage.color = c;
		onDone?.Invoke();
	}

	public bool IsTransitioning => isTransitioning;
}
