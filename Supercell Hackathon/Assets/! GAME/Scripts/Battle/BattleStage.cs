using System.Collections;
using UnityEngine;

/// <summary>
/// Places player and NPC/enemy sprites on the left and right sides of the screen
/// and slides them in from off-screen when the battle starts.
/// Attach to any GameObject in the scene (e.g. the Arena).
/// </summary>
public class BattleStage : MonoBehaviour
{
	[Header("Character References")]
	[SerializeField] private CharacterInfo leftCharacter;
	[SerializeField] private CharacterInfo rightCharacter;

	[Header("Sprite Holders")]
	[Tooltip("Assign existing SpriteRenderers, or leave empty to auto-create them.")]
	[SerializeField] private SpriteRenderer leftSpriteRenderer;
	[SerializeField] private SpriteRenderer rightSpriteRenderer;

	[Header("Patron Portrait")]
	[Tooltip("SpriteRenderer for the patron portrait, slides in behind the player.")]
	[SerializeField] private SpriteRenderer patronSpriteRenderer;
	[Tooltip("Offset from the player sprite position (local space).")]
	[SerializeField] private Vector3 patronOffset = new Vector3(-1.5f, 1.5f, 0f);
	[Tooltip("Scale of the patron portrait.")]
	[SerializeField] private float patronScale = 2f;

	[Header("Battle Background")]
	[Tooltip("SpriteRenderer used to display per-enemy battle backgrounds.")]
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

	[Header("Scale")]
	[SerializeField] private int sortingOrder = -10;

	[Header("Damage Shake")]
	[SerializeField] private float shakeDuration = 0.3f;
	[SerializeField] private float shakeMagnitude = 0.15f;

	private Vector3 _leftTarget;
	private Vector3 _rightTarget;
	private Vector3 _patronTarget;



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
	/// Call this to set up the stage with two characters and slide them in.
	/// Can be called again to swap in a different NPC/enemy.
	/// </summary>
	public void Setup(CharacterInfo left, CharacterInfo right)
	{
		// Unsubscribe from previous characters
		Unsubscribe();

		leftCharacter = left;
		rightCharacter = right;

		// Subscribe to events
		Subscribe();

		Initialize();
	}

	/// <summary>
	/// Initializes from serialized references if Setup() isn't called manually.
	/// </summary>
	private IEnumerator Start()
	{
		yield return null; // wait one frame for camera to be ready

		if (leftCharacter != null && rightCharacter != null)
			Initialize();
	}

	private void Initialize()
	{
		Camera cam = Camera.main;
		if (cam == null)
		{
			Debug.LogWarning("BattleStage: No main camera found.");
			return;
		}

		// Calculate world positions from viewport
		float camZ = Mathf.Abs(cam.transform.position.z);
		Vector3 leftViewport = new Vector3(horizontalOffset, verticalPosition, camZ);
		Vector3 rightViewport = new Vector3(1f - horizontalOffset, verticalPosition, camZ);

		_leftTarget = cam.ViewportToWorldPoint(leftViewport);
		_leftTarget.z = 0f;
		_rightTarget = cam.ViewportToWorldPoint(rightViewport);
		_rightTarget.z = 0f;

		// Set sprites
		SetupSprite(leftSpriteRenderer, leftCharacter._data, false);
		SetupSprite(rightSpriteRenderer, rightCharacter._data, true);

		// Set up patron portrait from RunState
		SetupPatronPortrait();

		// Update battle background from the right (enemy) character if available
		UpdateBackground(rightCharacter._data);

		// Start off-screen and slide in
		StopAllCoroutines();
		StartCoroutine(SlideIn());
	}

	private void SetupSprite(SpriteRenderer sr, CharacterData data, bool isRightSide)
	{
		if (data.characterSprite != null)
			sr.sprite = data.characterSprite;

		sr.sortingOrder = sortingOrder;

		// Flip the right-side character so they face left (toward the player)
		bool shouldFlip = isRightSide;
		if (data.flipSpriteForRight)
			shouldFlip = !shouldFlip;

		sr.flipX = shouldFlip;
	}

	private void UpdateBackground(CharacterData enemyData)
	{
		if (backgroundRenderer == null) return;

		if (enemyData.battleBackground != null)
		{
			backgroundRenderer.sprite = enemyData.battleBackground;
			backgroundRenderer.gameObject.SetActive(true);
		}
	}

	private void SetupPatronPortrait()
	{
		if (patronSpriteRenderer == null) return;

		// Get patron data from run state
		PatronData patron = null;
		if (RunManager.Instance != null && RunManager.Instance.State != null)
			patron = RunManager.Instance.State.patronData;

		if (patron == null || patron.portrait == null)
		{
			patronSpriteRenderer.gameObject.SetActive(false);
			return;
		}

		patronSpriteRenderer.sprite = patron.portrait;
		patronSpriteRenderer.sortingOrder = sortingOrder - 1; // behind player sprite
		patronSpriteRenderer.transform.localScale = Vector3.one * patronScale;

		// Position relative to player
		_patronTarget = _leftTarget + patronOffset;
	}

	private IEnumerator SlideIn()
	{
		Vector3 leftStart = _leftTarget + Vector3.left * slideDistance;
		Vector3 rightStart = _rightTarget + Vector3.right * slideDistance;
		Vector3 patronStart = _patronTarget + Vector3.left * slideDistance;

		leftSpriteRenderer.transform.position = leftStart;
		rightSpriteRenderer.transform.position = rightStart;

		leftSpriteRenderer.gameObject.SetActive(true);
		rightSpriteRenderer.gameObject.SetActive(true);

		// Only show patron portrait if it has a sprite assigned
		bool showPatron = patronSpriteRenderer != null && patronSpriteRenderer.sprite != null;
		if (showPatron)
		{
			patronSpriteRenderer.transform.position = patronStart;
			patronSpriteRenderer.gameObject.SetActive(true);
		}

		float elapsed = 0f;
		while (elapsed < slideDuration)
		{
			elapsed += Time.deltaTime;
			float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));

			leftSpriteRenderer.transform.position = Vector3.Lerp(leftStart, _leftTarget, t);
			rightSpriteRenderer.transform.position = Vector3.Lerp(rightStart, _rightTarget, t);

			if (showPatron)
				patronSpriteRenderer.transform.position = Vector3.Lerp(patronStart, _patronTarget, t);

			yield return null;
		}

		leftSpriteRenderer.transform.position = _leftTarget;
		rightSpriteRenderer.transform.position = _rightTarget;
		if (showPatron)
			patronSpriteRenderer.transform.position = _patronTarget;
	}

	/// <summary>
	/// Slides both characters out. Useful for scene transitions.
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

		Vector3 leftStart = leftSpriteRenderer.transform.position;
		Vector3 rightStart = rightSpriteRenderer.transform.position;
		Vector3 patronStart = patronSpriteRenderer != null ? patronSpriteRenderer.transform.position : Vector3.zero;
		bool hasPatron = patronSpriteRenderer != null && patronSpriteRenderer.gameObject.activeSelf;

		float elapsed = 0f;
		while (elapsed < slideDuration)
		{
			elapsed += Time.deltaTime;
			float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));

			leftSpriteRenderer.transform.position = Vector3.Lerp(leftStart, leftEnd, t);
			rightSpriteRenderer.transform.position = Vector3.Lerp(rightStart, rightEnd, t);

			if (hasPatron)
				patronSpriteRenderer.transform.position = Vector3.Lerp(patronStart, patronEnd, t);

			yield return null;
		}

		leftSpriteRenderer.gameObject.SetActive(false);
		rightSpriteRenderer.gameObject.SetActive(false);
		if (hasPatron)
			patronSpriteRenderer.gameObject.SetActive(false);
	}

	/// <summary>
	/// Swap in a new right-side character (e.g. new enemy encounter) with a slide transition.
	/// </summary>
	public Coroutine SwapRightCharacter(CharacterInfo newRight)
	{
		return StartCoroutine(SwapRightRoutine(newRight));
	}

	private IEnumerator SwapRightRoutine(CharacterInfo newRight)
	{
		// Slide old one out
		Vector3 rightEnd = _rightTarget + Vector3.right * slideDistance;
		float elapsed = 0f;
		while (elapsed < slideDuration)
		{
			elapsed += Time.deltaTime;
			float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
			rightSpriteRenderer.transform.position = Vector3.Lerp(_rightTarget, rightEnd, t);
			yield return null;
		}

		// Swap data
		rightCharacter = newRight;
		SetupSprite(rightSpriteRenderer, rightCharacter._data, true);

		// Slide new one in
		elapsed = 0f;
		while (elapsed < slideDuration)
		{
			elapsed += Time.deltaTime;
			float t = slideCurve.Evaluate(Mathf.Clamp01(elapsed / slideDuration));
			rightSpriteRenderer.transform.position = Vector3.Lerp(rightEnd, _rightTarget, t);
			yield return null;
		}

		rightSpriteRenderer.transform.position = _rightTarget;
	}

	private SpriteRenderer CreateSpriteHolder(string objectName)
	{
		GameObject go = new GameObject(objectName);
		go.transform.SetParent(transform);
		go.SetActive(false);
		return go.AddComponent<SpriteRenderer>();
	}

	#region Damage Shake

	private void ShakeLeft()
	{
		ShowDamageSprite(leftSpriteRenderer, leftCharacter);
		StartCoroutine(Shake(leftSpriteRenderer.transform, _leftTarget));
	}

	private void ShakeRight()
	{
		ShowDamageSprite(rightSpriteRenderer, rightCharacter);
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

	private void OnDestroy()
	{
		Unsubscribe();
	}

	#endregion

	#region Sprite State Swapping

	private void OnLeftCardPlayed(CardType type) => ShowActionSprite(leftSpriteRenderer, leftCharacter, type);
	private void OnRightCardPlayed(CardType type) => ShowActionSprite(rightSpriteRenderer, rightCharacter, type);

	private void ShowActionSprite(SpriteRenderer sr, CharacterInfo info, CardType type)
	{
		if (info == null || info._data == null) return;

		Sprite actionSprite = type == CardType.Attack
			? info._data.activeActionSprite
			: info._data.passiveActionSprite;

		if (actionSprite != null)
			StartCoroutine(FlashSprite(sr, info._data, actionSprite));
	}

	private void ShowDamageSprite(SpriteRenderer sr, CharacterInfo info)
	{
		if (info == null || info._data == null) return;

		Sprite dmgSprite = info._data.damageTakenSprite;
		if (dmgSprite != null)
			StartCoroutine(FlashSprite(sr, info._data, dmgSprite));
	}

	/// <summary>
	/// Temporarily swaps to a sprite, then returns to the idle/default sprite.
	/// </summary>
	private IEnumerator FlashSprite(SpriteRenderer sr, CharacterData data, Sprite tempSprite)
	{
		sr.sprite = tempSprite;
		yield return new WaitForSeconds(data.poseDuration);
		// Return to idle (fall back to characterSprite if no idle sprite)
		sr.sprite = data.idleSprite != null ? data.idleSprite : data.characterSprite;
	}

	#endregion

	#region Subscribe / Unsubscribe

	private void Subscribe()
	{
		if (leftCharacter != null)
		{
			leftCharacter.OnDamageTaken += ShakeLeft;
			leftCharacter.OnCardPlayed += OnLeftCardPlayed;
		}
		if (rightCharacter != null)
		{
			rightCharacter.OnDamageTaken += ShakeRight;
			rightCharacter.OnCardPlayed += OnRightCardPlayed;
		}
	}

	private void Unsubscribe()
	{
		if (leftCharacter != null)
		{
			leftCharacter.OnDamageTaken -= ShakeLeft;
			leftCharacter.OnCardPlayed -= OnLeftCardPlayed;
		}
		if (rightCharacter != null)
		{
			rightCharacter.OnDamageTaken -= ShakeRight;
			rightCharacter.OnCardPlayed -= OnRightCardPlayed;
		}
	}

	#endregion
}
