using System.Collections;
using UnityEngine;

/// <summary>
/// Reusable character display for non-combat encounter scenes (shop, camp, event, treasure).
/// Places player and NPC sprites on the left/right sides of the screen with slide-in/out animation.
/// Supports sprite reactions (action poses, damage poses) and screen shake — same patterns as BattleStage
/// but without any combat logic (health, damage events, card-played callbacks).
///
/// Usage:
///   1. Add to any encounter scene
///   2. Assign npcData (CharacterData SO for the NPC — shopkeeper, etc.)
///   3. Call Setup() from the scene controller — player data is read from RunState automatically
///   4. Call SlideOut() when leaving the scene
///   5. Optionally call PlayLeftReaction / PlayRightReaction / ShakeLeft / ShakeRight for visual feedback
/// </summary>
public class EncounterStage : MonoBehaviour
{
	public enum ReactionType
	{
		Action,   // activeActionSprite (e.g. attack pose, sell animation)
		Passive,  // passiveActionSprite (e.g. non-attack card pose)
		Damage    // damageTakenSprite (e.g. taking a hit, spending gold)
	}

	[Header("NPC Character")]
	[Tooltip("CharacterData for the right-side NPC (shopkeeper, camp companion, etc.). Player data is read from RunState.")]
	[SerializeField] private CharacterData npcData;

	[Header("Sprite Holders")]
	[Tooltip("Assign existing SpriteRenderers, or leave empty to auto-create them.")]
	[SerializeField] private SpriteRenderer leftSpriteRenderer;
	[SerializeField] private SpriteRenderer rightSpriteRenderer;

	[Header("Patron Portrait")]
	[SerializeField] private SpriteRenderer patronSpriteRenderer;
	[SerializeField] private Vector3 patronOffset = new Vector3(-1.5f, 1.5f, 0f);
	[SerializeField] private float patronScale = 2f;

	[Header("Background")]
	[Tooltip("Optional background sprite renderer for the encounter scene.")]
	[SerializeField] private SpriteRenderer backgroundRenderer;

	[Header("Positioning")]
	[Tooltip("How far from center (in viewport %). 0.25 = quarter of the screen.")]
	[SerializeField] private float horizontalOffset = 0.25f;
	[Tooltip("Vertical position in viewport (0 = bottom, 0.5 = center).")]
	[SerializeField] private float verticalPosition = 0.35f;

	[Header("Slide Animation")]
	[SerializeField] private float slideDistance = 8f;
	[SerializeField] private float slideDuration = 0.6f;
	[SerializeField] private AnimationCurve slideCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
	[SerializeField] private float patronSlideDelay = 0.3f;

	[Header("Sorting")]
	[SerializeField] private string characterSortingLayer = "Character";
	[SerializeField] private int sortingOrder = 0;

	[Header("Shake")]
	[SerializeField] private float shakeDuration = 0.3f;
	[SerializeField] private float shakeMagnitude = 0.15f;

	// Cached targets for slide animation
	private Vector3 _leftTarget;
	private Vector3 _rightTarget;
	private Vector3 _patronTarget;

	// Cached CharacterData for sprite reactions
	private CharacterData _leftData;
	private CharacterData _rightData;

	private void Awake()
	{
		if (leftSpriteRenderer == null)
			leftSpriteRenderer = CreateSpriteHolder("Player Sprite");

		if (rightSpriteRenderer == null)
			rightSpriteRenderer = CreateSpriteHolder("NPC Sprite");

		if (patronSpriteRenderer == null)
			patronSpriteRenderer = CreateSpriteHolder("Patron Portrait");
	}

	/// <summary>
	/// Sets up the stage using the serialized npcData and player data from RunState.
	/// Waits one frame for the camera to be ready (scene may have just loaded additively).
	/// </summary>
	public void Setup()
	{
		StartCoroutine(SetupDelayed(null, npcData));
	}

	/// <summary>
	/// Sets up the stage with explicit NPC data. Use this overload when
	/// the NPC is determined dynamically (e.g. random event character).
	/// </summary>
	public void Setup(CharacterData overrideNPC)
	{
		StartCoroutine(SetupDelayed(null, overrideNPC));
	}

	/// <summary>
	/// Full setup with both character data references.
	/// </summary>
	public void Setup(CharacterData leftCharacterData, CharacterData rightCharacterData)
	{
		StartCoroutine(SetupDelayed(leftCharacterData, rightCharacterData));
	}

	/// <summary>
	/// Waits one frame so the DontDestroyOnLoad camera is ready after additive scene load,
	/// then resolves player data from RunState and initializes sprites + slide-in.
	/// </summary>
	private IEnumerator SetupDelayed(CharacterData leftOverride, CharacterData rightCharacterData)
	{
		// Wait one frame for camera to be ready (same pattern as BattleStage.Start)
		yield return null;

		// Resolve player CharacterData from RunState if not explicitly provided
		CharacterData leftCharacterData = leftOverride;
		if (leftCharacterData == null && RunManager.Instance != null && RunManager.Instance.State != null)
		{
			PatronData patron = RunManager.Instance.State.patronData;
			if (patron != null)
				leftCharacterData = patron.characterData;
		}

		_leftData = leftCharacterData;
		_rightData = rightCharacterData;

		Camera cam = Camera.main;
		if (cam == null)
		{
			Debug.LogWarning("EncounterStage: No main camera found.");
			yield break;
		}

		// Calculate world positions from viewport (same math as BattleStage)
		float camZ = Mathf.Abs(cam.transform.position.z);
		Vector3 leftViewport = new Vector3(horizontalOffset, verticalPosition, camZ);
		Vector3 rightViewport = new Vector3(1f - horizontalOffset, verticalPosition, camZ);

		_leftTarget = cam.ViewportToWorldPoint(leftViewport);
		_leftTarget.z = 0f;
		_rightTarget = cam.ViewportToWorldPoint(rightViewport);
		_rightTarget.z = 0f;

		// Set sprites
		if (_leftData != null)
		{
			SetupSprite(leftSpriteRenderer, _leftData, false);
			_leftTarget += _leftData.spriteOffset;
		}

		if (_rightData != null)
		{
			SetupSprite(rightSpriteRenderer, _rightData, true);
			_rightTarget += _rightData.spriteOffset;
		}

		// Set up patron portrait from RunState
		SetupPatronPortrait();

		// Update background from NPC data if available
		if (_rightData != null)
			UpdateBackground(_rightData);

		// Slide in
		yield return StartCoroutine(SlideIn());
	}

	private void SetupSprite(SpriteRenderer sr, CharacterData data, bool isRightSide)
	{
		if (data.characterSprite != null)
			sr.sprite = data.characterSprite;

		sr.sortingLayerName = characterSortingLayer;
		sr.sortingOrder = sortingOrder;

		float scale = Mathf.Approximately(data.spriteScale, 0f) ? 1f : data.spriteScale;
		sr.transform.localScale = Vector3.one * scale;

		bool shouldFlip = isRightSide;
		if (data.flipSpriteForRight)
			shouldFlip = !shouldFlip;

		sr.flipX = shouldFlip;
	}

	private void UpdateBackground(CharacterData data)
	{
		if (backgroundRenderer == null) return;

		if (data.battleBackground != null)
		{
			backgroundRenderer.sprite = data.battleBackground;
			backgroundRenderer.gameObject.SetActive(true);
		}
	}

	private void SetupPatronPortrait()
	{
		if (patronSpriteRenderer == null) return;

		PatronData patron = null;
		if (RunManager.Instance != null && RunManager.Instance.State != null)
			patron = RunManager.Instance.State.patronData;

		if (patron == null || patron.portrait == null)
		{
			patronSpriteRenderer.gameObject.SetActive(false);
			return;
		}

		patronSpriteRenderer.sprite = patron.portrait;
		patronSpriteRenderer.sortingLayerName = characterSortingLayer;
		patronSpriteRenderer.sortingOrder = sortingOrder - 1;
		patronSpriteRenderer.transform.localScale = Vector3.one * patronScale;

		_patronTarget = _leftTarget + patronOffset;
	}

	#region Slide Animation

	private IEnumerator SlideIn()
	{
		Vector3 leftStart = _leftTarget + Vector3.left * slideDistance;
		Vector3 rightStart = _rightTarget + Vector3.right * slideDistance;

		bool showLeft = _leftData != null;
		bool showRight = _rightData != null;

		if (showLeft)
		{
			leftSpriteRenderer.transform.position = leftStart;
			leftSpriteRenderer.gameObject.SetActive(true);
		}

		if (showRight)
		{
			rightSpriteRenderer.transform.position = rightStart;
			rightSpriteRenderer.gameObject.SetActive(true);
		}

		bool showPatron = patronSpriteRenderer != null && patronSpriteRenderer.sprite != null;

		// Slide player + NPC in
		float elapsed = 0f;
		while (elapsed < slideDuration)
		{
			elapsed += Time.deltaTime;
			float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));

			if (showLeft)
				leftSpriteRenderer.transform.position = Vector3.Lerp(leftStart, _leftTarget, t);
			if (showRight)
				rightSpriteRenderer.transform.position = Vector3.Lerp(rightStart, _rightTarget, t);

			yield return null;
		}

		if (showLeft) leftSpriteRenderer.transform.position = _leftTarget;
		if (showRight) rightSpriteRenderer.transform.position = _rightTarget;

		// Patron slides in after the player
		if (showPatron)
		{
			yield return new WaitForSeconds(patronSlideDelay);

			Vector3 patronStart = _patronTarget + Vector3.left * slideDistance;
			patronSpriteRenderer.transform.position = patronStart;
			patronSpriteRenderer.gameObject.SetActive(true);

			elapsed = 0f;
			while (elapsed < slideDuration)
			{
				elapsed += Time.deltaTime;
				float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
				patronSpriteRenderer.transform.position = Vector3.Lerp(patronStart, _patronTarget, t);
				yield return null;
			}

			patronSpriteRenderer.transform.position = _patronTarget;
		}
	}

	/// <summary>
	/// Slides all characters off screen. Yield on the returned Coroutine to wait for completion.
	/// </summary>
	public Coroutine SlideOut()
	{
		return StartCoroutine(SlideOutRoutine());
	}

	private IEnumerator SlideOutRoutine()
	{
		Vector3 leftEnd = _leftTarget + Vector3.left * slideDistance;
		Vector3 rightEnd = _rightTarget + Vector3.right * slideDistance;
		Vector3 patronEnd = _patronTarget + Vector3.left * slideDistance;

		Vector3 leftStart = leftSpriteRenderer != null ? leftSpriteRenderer.transform.position : Vector3.zero;
		Vector3 rightStart = rightSpriteRenderer != null ? rightSpriteRenderer.transform.position : Vector3.zero;
		Vector3 patronStart = patronSpriteRenderer != null ? patronSpriteRenderer.transform.position : Vector3.zero;

		bool hasLeft = leftSpriteRenderer != null && leftSpriteRenderer.gameObject.activeSelf;
		bool hasRight = rightSpriteRenderer != null && rightSpriteRenderer.gameObject.activeSelf;
		bool hasPatron = patronSpriteRenderer != null && patronSpriteRenderer.gameObject.activeSelf;

		float elapsed = 0f;
		while (elapsed < slideDuration)
		{
			elapsed += Time.deltaTime;
			float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));

			if (hasLeft)
				leftSpriteRenderer.transform.position = Vector3.Lerp(leftStart, leftEnd, t);
			if (hasRight)
				rightSpriteRenderer.transform.position = Vector3.Lerp(rightStart, rightEnd, t);
			if (hasPatron)
				patronSpriteRenderer.transform.position = Vector3.Lerp(patronStart, patronEnd, t);

			yield return null;
		}

		if (hasLeft) leftSpriteRenderer.gameObject.SetActive(false);
		if (hasRight) rightSpriteRenderer.gameObject.SetActive(false);
		if (hasPatron) patronSpriteRenderer.gameObject.SetActive(false);
	}

	#endregion

	#region Reactions (Sprite Poses)

	/// <summary>
	/// Plays a temporary sprite reaction on the left (player) character.
	/// </summary>
	public void PlayLeftReaction(ReactionType reaction)
	{
		if (_leftData == null || leftSpriteRenderer == null) return;
		Sprite pose = GetReactionSprite(_leftData, reaction);
		if (pose != null)
			StartCoroutine(FlashSprite(leftSpriteRenderer, _leftData, pose));
	}

	/// <summary>
	/// Plays a temporary sprite reaction on the right (NPC) character.
	/// </summary>
	public void PlayRightReaction(ReactionType reaction)
	{
		if (_rightData == null || rightSpriteRenderer == null) return;
		Sprite pose = GetReactionSprite(_rightData, reaction);
		if (pose != null)
			StartCoroutine(FlashSprite(rightSpriteRenderer, _rightData, pose));
	}

	private Sprite GetReactionSprite(CharacterData data, ReactionType reaction)
	{
		return reaction switch
		{
			ReactionType.Action => data.activeActionSprite,
			ReactionType.Passive => data.passiveActionSprite,
			ReactionType.Damage => data.damageTakenSprite,
			_ => null
		};
	}

	private IEnumerator FlashSprite(SpriteRenderer sr, CharacterData data, Sprite tempSprite)
	{
		sr.sprite = tempSprite;
		yield return new WaitForSeconds(data.poseDuration);
		sr.sprite = data.idleSprite != null ? data.idleSprite : data.characterSprite;
	}

	#endregion

	#region Shake

	/// <summary>
	/// Shakes the left (player) character sprite.
	/// </summary>
	public void ShakeLeft()
	{
		if (leftSpriteRenderer == null) return;
		StartCoroutine(Shake(leftSpriteRenderer.transform, _leftTarget));
	}

	/// <summary>
	/// Shakes the right (NPC) character sprite.
	/// </summary>
	public void ShakeRight()
	{
		if (rightSpriteRenderer == null) return;
		StartCoroutine(Shake(rightSpriteRenderer.transform, _rightTarget));
	}

	private IEnumerator Shake(Transform target, Vector3 restPosition)
	{
		float elapsed = 0f;
		while (elapsed < shakeDuration)
		{
			float strength = shakeMagnitude * (1f - elapsed / shakeDuration);
			Vector3 offset = new Vector3(
				Random.Range(-strength, strength),
				Random.Range(-strength, strength),
				0f);
			target.position = restPosition + offset;
			elapsed += Time.deltaTime;
			yield return null;
		}
		target.position = restPosition;
	}

	#endregion

	#region Helpers

	private SpriteRenderer CreateSpriteHolder(string objectName)
	{
		GameObject go = new GameObject(objectName);
		go.transform.SetParent(transform);
		go.SetActive(false);
		return go.AddComponent<SpriteRenderer>();
	}

	#endregion
}
