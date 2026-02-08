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
	[SerializeField] private TextMeshPro dialogueText;

	[Header("Typewriter")]
	[SerializeField] private float charDelay = 0.03f;

	[Header("Timing")]
	[SerializeField] private float autoDismissTime = 3.5f;
	[SerializeField] private float fadeSpeed = 5f;

	[Header("Patron Color")]
	[Tooltip("If true, tints the box background with the patron's color.")]
	[SerializeField] private bool usePatronColor = true;

	[Header("Screen Positioning")]
	[Tooltip("Viewport position (0-1). X=0 left, Y=0 bottom.")]
	[SerializeField] private Vector2 viewportAnchor = new Vector2(0.25f, 0.22f);
	[Tooltip("Sorting order for the box (should be high to render above game content).")]
	[SerializeField] private int sortingOrder = 950;

	// State
	private Coroutine _activeRoutine;
	private bool _isShowing;
	private bool _typewriterDone;
	private bool _patronColorApplied;
	private float _dismissCooldown; // Prevents accidental dismiss from card clicks

	private void Awake()
	{
		// Start hidden
		SetAlpha(0f);
		if (dialogueText != null)
			dialogueText.text = "";

		// Set sorting order
		if (boxBackground != null)
			boxBackground.sortingOrder = sortingOrder;
		if (dialogueText != null)
			dialogueText.sortingOrder = sortingOrder + 1;
	}

	/// <summary>
	/// Applies patron color tint. Called lazily since RunState may not exist at Awake.
	/// </summary>
	private void ApplyPatronColor()
	{
		if (_patronColorApplied) return;
		if (!usePatronColor || boxBackground == null) return;
		if (RunManager.Instance == null || RunManager.Instance.State?.patronData == null) return;

		Color pc = RunManager.Instance.State.patronData.patronColor;
		boxBackground.color = new Color(pc.r, pc.g, pc.b, boxBackground.color.a);
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
		StartCoroutine(FadeOut());
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
			Color c = boxBackground.color;
			c.a = a;
			boxBackground.color = c;
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
			return boxBackground.color.a;
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
}
