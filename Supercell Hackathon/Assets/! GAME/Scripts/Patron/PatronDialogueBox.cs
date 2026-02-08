using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays patron dialogue lines with a typewriter effect.
/// Persists across scenes as a child of the PatronDialogueManager (DontDestroyOnLoad).
///
/// Automatically repositions itself relative to the main camera each frame,
/// so it stays anchored to the bottom-left of the viewport regardless of scene.
///
/// Features:
///   - Typewriter text reveal (configurable speed)
///   - Auto-dismiss after a configurable delay
///   - Click-to-dismiss (only after typewriter finishes to avoid card click conflicts)
///   - Fades in/out smoothly
///   - Camera-relative positioning (survives scene transitions)
///
/// Setup:
///   1. Create a child GameObject under PatronDialogueManager
///   2. Add this component
///   3. Add child SpriteRenderer for background (9-sliced speech box)
///   4. Add child TextMeshPro for dialogue text (world-space)
///   5. Sorting order should be high (e.g. 900+) to render above game sprites
/// </summary>
public class PatronDialogueBox : MonoBehaviour
{
	[Header("References")]
	[SerializeField] private SpriteRenderer boxBackground;
	[Tooltip("9-sliced border frame sprite. Tinted with the patron's color at runtime.")]
	[SerializeField] private SpriteRenderer boxBorderFrame;
	[SerializeField] private TextMeshPro dialogueText;

	[Header("Typewriter")]
	[SerializeField] private float charDelay = 0.03f;

	[Header("Font Sizing")]
	[Tooltip("Font size for short text (1 sentence).")]
	[SerializeField] private float fontSizeShort = 4f;
	[Tooltip("Font size for medium text (2 sentences).")]
	[SerializeField] private float fontSizeMedium = 3f;
	[Tooltip("Font size for long text (3+ sentences).")]
	[SerializeField] private float fontSizeLong = 2f;

	[Header("Timing")]
	[SerializeField] private float autoDismissTime = 3.5f;
	[SerializeField] private float fadeSpeed = 5f;

	[Header("Colors")]
	[Tooltip("If true, tints the text outline with the patron's color.")]
	[SerializeField] private bool usePatronColor = true;
	[Tooltip("Background box color (default black for readability).")]
	[SerializeField] private Color backgroundColor = new Color(0f, 0f, 0f, 0.9f);
	[Tooltip("Thickness of the text outline (0-1).")]
	[SerializeField] private float textOutlineWidth = 0.3f;

	[Header("Screen Positioning")]
	[Tooltip("Viewport position (0-1). X=0 left, Y=0 bottom.")]
	[SerializeField] private Vector2 viewportAnchor = new Vector2(0.25f, 0.22f);
	[Tooltip("Sorting layer for the dialogue box. 'Shop' sits above Card layer and below Deck View BG.")]
	[SerializeField] private string dialogueSortingLayer = "Shop";
	[SerializeField] private int sortingOrder = 0;

	// State
	private Coroutine _activeRoutine;
	private bool _isShowing;
	private bool _typewriterDone;
	private bool _patronColorApplied;
	private float _dismissCooldown; // Prevents accidental dismiss from card clicks

	/// <summary>
	/// Returns true if the dialogue box is currently showing (including typewriter + auto-dismiss).
	/// </summary>
	public bool IsShowing => _isShowing;

	/// <summary>
	/// Fired when a dialogue line finishes (either dismissed or auto-faded out).
	/// Subscribe to know when it's safe to proceed after dialogue.
	/// </summary>
	public System.Action onDialogueFinished;

	private void Awake()
	{
		// Start hidden
		SetAlpha(0f);
		if (dialogueText != null)
			dialogueText.text = "";

		// Set sorting layer and order
		if (boxBackground != null)
		{
			boxBackground.sortingLayerName = dialogueSortingLayer;
			boxBackground.sortingOrder = sortingOrder;
			// Black background for readability
			boxBackground.color = new Color(backgroundColor.r, backgroundColor.g, backgroundColor.b, 0f);
		}
		if (boxBorderFrame != null)
		{
			boxBorderFrame.sortingLayerName = dialogueSortingLayer;
			boxBorderFrame.sortingOrder = sortingOrder + 1;
			// Start hidden, patron color applied lazily
			boxBorderFrame.color = new Color(1f, 1f, 1f, 0f);
		}
		if (dialogueText != null)
		{
			dialogueText.sortingOrder = sortingOrder + 2;
			var textRenderer = dialogueText.GetComponent<MeshRenderer>();
			if (textRenderer != null)
				textRenderer.sortingLayerName = dialogueSortingLayer;

			// White text, outline set to black initially (patron color applied lazily)
			dialogueText.color = new Color(1f, 1f, 1f, 0f);
			dialogueText.outlineColor = new Color32(0, 0, 0, 255);
			dialogueText.outlineWidth = textOutlineWidth;
		}
	}

	/// <summary>
	/// Applies patron visuals: swaps the border frame sprite and tints it + text outline.
	/// Background stays black, text stays white.
	/// </summary>
	private void ApplyPatronColor()
	{
		if (_patronColorApplied) return;
		if (!usePatronColor) return;
		if (RunManager.Instance == null || RunManager.Instance.State?.patronData == null) return;

		PatronData patron = RunManager.Instance.State.patronData;
		Color pc = patron.patronColor;

		// Swap border frame sprite per patron
		if (boxBorderFrame != null && patron.dialogueFrameSprite != null)
			boxBorderFrame.sprite = patron.dialogueFrameSprite;

		// Tint border frame with patron color
		if (boxBorderFrame != null)
			boxBorderFrame.color = new Color(pc.r, pc.g, pc.b, boxBorderFrame.color.a);

		// Tint text outline with patron color
		if (dialogueText != null)
			dialogueText.outlineColor = new Color32(
				(byte)(pc.r * 255),
				(byte)(pc.g * 255),
				(byte)(pc.b * 255),
				255);

		_patronColorApplied = true;
	}

	/// <summary>
	/// Shows a dialogue line with typewriter effect. Cancels any active line.
	/// </summary>
	public void ShowLine(string text)
	{
		if (string.IsNullOrEmpty(text)) return;

		ApplyPatronColor();
		SnapToCamera();

		if (_activeRoutine != null)
			StopCoroutine(_activeRoutine);

		_activeRoutine = StartCoroutine(DialogueRoutine(text));
	}

	/// <summary>
	/// Immediately hides the dialogue box.
	/// </summary>
	public void Dismiss()
	{
		if (!_isShowing) return;

		if (_activeRoutine != null)
		{
			StopCoroutine(_activeRoutine);
			_activeRoutine = null;
		}

		_isShowing = false;
		_typewriterDone = false;
		StartCoroutine(DismissFadeOut());
	}

	private IEnumerator DismissFadeOut()
	{
		yield return FadeOut();
		onDialogueFinished?.Invoke();
	}

	/// <summary>
	/// Resets patron color so it can be re-applied for a new run/patron.
	/// </summary>
	public void ResetPatronColor()
	{
		_patronColorApplied = false;
	}

	private IEnumerator DialogueRoutine(string text)
	{
		_isShowing = true;
		_typewriterDone = false;
		_dismissCooldown = 0.5f; // Prevent accidental dismiss from clicks that triggered the dialogue

		// Scale font size based on text length
		if (dialogueText != null)
			dialogueText.fontSize = GetFontSizeForText(text);

		// Fade in
		yield return FadeIn();

		// Typewriter
		dialogueText.text = "";
		for (int i = 0; i < text.Length; i++)
		{
			dialogueText.text = text.Substring(0, i + 1);
			yield return new WaitForSeconds(charDelay);
		}

		_typewriterDone = true;

		// Wait for auto-dismiss
		yield return new WaitForSeconds(autoDismissTime);

		// Fade out
		_isShowing = false;
		_typewriterDone = false;
		yield return FadeOut();

		_activeRoutine = null;
		onDialogueFinished?.Invoke();
	}

	private IEnumerator FadeIn()
	{
		float alpha = 0f;
		while (alpha < 1f)
		{
			alpha = Mathf.MoveTowards(alpha, 1f, Time.deltaTime * fadeSpeed);
			SetAlpha(alpha);
			yield return null;
		}
		SetAlpha(1f);
	}

	private IEnumerator FadeOut()
	{
		float alpha = GetAlpha();
		while (alpha > 0f)
		{
			alpha = Mathf.MoveTowards(alpha, 0f, Time.deltaTime * fadeSpeed);
			SetAlpha(alpha);
			yield return null;
		}
		SetAlpha(0f);
		if (dialogueText != null)
			dialogueText.text = "";
	}

	private void SetAlpha(float a)
	{
		if (boxBackground != null)
		{
			// Background uses its own configured alpha (e.g. 0.9) scaled by fade
			Color c = boxBackground.color;
			c.a = backgroundColor.a * a;
			boxBackground.color = c;
		}

		if (boxBorderFrame != null)
		{
			Color c = boxBorderFrame.color;
			c.a = a;
			boxBorderFrame.color = c;
		}

		if (dialogueText != null)
		{
			Color c = dialogueText.color;
			c.a = a;
			dialogueText.color = c;
		}
	}

	private float GetAlpha()
	{
		if (boxBackground != null)
			return boxBackground.color.a > 0 ? boxBackground.color.a / backgroundColor.a : 0f;
		if (dialogueText != null)
			return dialogueText.color.a;
		return 0f;
	}

	/// <summary>
	/// Positions the dialogue box relative to the main camera's viewport.
	/// Called when showing a line so it's always anchored correctly regardless of scene.
	/// </summary>
	private void SnapToCamera()
	{
		Camera cam = Camera.main;
		if (cam == null) return;

		float camZ = Mathf.Abs(cam.transform.position.z);
		Vector3 worldPos = cam.ViewportToWorldPoint(new Vector3(viewportAnchor.x, viewportAnchor.y, camZ));
		worldPos.z = 0f;
		transform.position = worldPos;
	}

	private void LateUpdate()
	{
		// Keep anchored to camera while visible (handles camera movement during battle)
		if (_isShowing)
			SnapToCamera();

		// Tick down dismiss cooldown so card clicks right before/during dialogue don't insta-dismiss
		if (_dismissCooldown > 0f)
			_dismissCooldown -= Time.deltaTime;

		// Click-to-dismiss only after typewriter is done AND cooldown has elapsed.
		// Uses a box overlap check so only clicks ON the dialogue box dismiss it,
		// preventing card drag/selection clicks from accidentally closing the dialogue.
		if (_isShowing && _typewriterDone && _dismissCooldown <= 0f && Input.GetMouseButtonDown(0))
		{
			if (IsClickOnDialogueBox())
				Dismiss();
		}
	}

	/// <summary>
	/// Checks if the current mouse click is over the dialogue box background.
	/// Falls back to always-dismiss if no background renderer is assigned.
	/// </summary>
	private bool IsClickOnDialogueBox()
	{
		if (boxBackground == null) return true; // No background — fallback to dismiss anywhere

		Camera cam = Camera.main;
		if (cam == null) return true;

		Vector3 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
		mouseWorld.z = boxBackground.transform.position.z;

		return boxBackground.bounds.Contains(mouseWorld);
	}

	/// <summary>
	/// Returns a font size based on sentence count.
	/// Short lines get bigger text to fill the box.
	/// </summary>
	private float GetFontSizeForText(string text)
	{
		int sentences = CountSentences(text);
		if (sentences <= 1) return fontSizeShort;
		if (sentences <= 2) return fontSizeMedium;
		return fontSizeLong;
	}

	private int CountSentences(string text)
	{
		if (string.IsNullOrEmpty(text)) return 0;

		int count = 0;
		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (c == '.' || c == '!' || c == '?')
			{
				// Skip ellipsis
				if (c == '.' && i + 1 < text.Length && text[i + 1] == '.')
					continue;
				count++;
			}
		}

		// If no punctuation found, treat it as one sentence
		return Mathf.Max(1, count);
	}
}
