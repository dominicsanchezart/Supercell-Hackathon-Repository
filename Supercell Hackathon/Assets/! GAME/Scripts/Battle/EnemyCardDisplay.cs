using System.Collections;
using UnityEngine;

/// <summary>
/// Shows the enemy's played card in the center of the screen with a punch-scale animation,
/// holds briefly so the player can read it, then fades out and destroys it.
/// Attach to an empty GameObject in the battle scene and assign via the Arena inspector.
/// </summary>
public class EnemyCardDisplay : MonoBehaviour
{
    [Header("Prefab")]
    [Tooltip("The same card prefab used by Hand / BattleRewardUI.")]
    [SerializeField] private GameObject cardPrefab;

    [Header("Positioning")]
    [Tooltip("World-space position where the card appears (usually screen center).")]
    [SerializeField] private Vector3 displayPosition = new Vector3(0f, 0.5f, 0f);
    [Tooltip("Final resting scale of the card while it's on screen.")]
    [SerializeField] private float displayScale = 1.4f;

    [Header("Timing")]
    [SerializeField] private float punchInDuration = 0.2f;
    [SerializeField] private float holdDuration = 1.0f;
    [SerializeField] private float fadeOutDuration = 0.35f;

    [Header("Animation")]
    [Tooltip("How much bigger than displayScale the card overshoots during the punch-in.")]
    [SerializeField] private float overshootScale = 1.7f;
    [Tooltip("Sorting order boost so the card renders above everything.")]
    [SerializeField] private int sortingOrderBoost = 5000;

    /// <summary>
    /// Shows the given card, animates it, and returns a coroutine you can yield on.
    /// Usage:  yield return enemyCardDisplay.Show(cardData);
    /// </summary>
    public Coroutine Show(CardData data)
    {
        if (data == null || cardPrefab == null) return null;
        return StartCoroutine(ShowRoutine(data));
    }

    private IEnumerator ShowRoutine(CardData data)
    {
        // --- Spawn ---
        GameObject cardObj = Instantiate(cardPrefab, transform);
        cardObj.transform.position = displayPosition;
        cardObj.transform.localScale = Vector3.zero;

        // Set card data
        Card card = cardObj.GetComponent<Card>();
        if (card != null)
            card.SetCardData(data);

        // Push sorting order so it renders on top
        CardView view = cardObj.GetComponent<CardView>();
        if (view != null)
            view.SetSortingOrder(sortingOrderBoost);

        // Disable collider so the player can't interact with it
        Collider2D col = cardObj.GetComponentInChildren<Collider2D>();
        if (col != null) col.enabled = false;

        // --- Punch-in: 0 → overshoot → displayScale ---
        float half = punchInDuration * 0.5f;

        // Phase 1: scale up to overshoot
        yield return ScaleTo(cardObj.transform, Vector3.zero, Vector3.one * overshootScale, half);

        // Phase 2: settle down to display scale
        yield return ScaleTo(cardObj.transform, Vector3.one * overshootScale, Vector3.one * displayScale, half);

        // --- Hold ---
        yield return new WaitForSeconds(holdDuration);

        // --- Fade out (scale down + alpha) ---
        SpriteRenderer[] sprites = cardObj.GetComponentsInChildren<SpriteRenderer>();
        Canvas[] canvases = cardObj.GetComponentsInChildren<Canvas>();
        CanvasGroup[] groups = cardObj.GetComponentsInChildren<CanvasGroup>();
        TMPro.TextMeshProUGUI[] texts = cardObj.GetComponentsInChildren<TMPro.TextMeshProUGUI>();

        float elapsed = 0f;
        Vector3 startScale = cardObj.transform.localScale;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeOutDuration;
            float ease = t * t; // ease-in

            // Scale shrink
            cardObj.transform.localScale = Vector3.Lerp(startScale, Vector3.zero, ease);

            // Fade sprites
            float alpha = 1f - ease;
            foreach (var sr in sprites)
            {
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }

            // Fade TMP texts
            foreach (var tmp in texts)
            {
                Color c = tmp.color;
                c.a = alpha;
                tmp.color = c;
            }

            yield return null;
        }

        // --- Cleanup ---
        Destroy(cardObj);
    }

    private IEnumerator ScaleTo(Transform t, Vector3 from, Vector3 to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float p = Mathf.Clamp01(elapsed / duration);
            // Smooth-step for a snappy punch feel
            float ease = p * p * (3f - 2f * p);
            t.localScale = Vector3.LerpUnclamped(from, to, ease);
            yield return null;
        }
        t.localScale = to;
    }
}
