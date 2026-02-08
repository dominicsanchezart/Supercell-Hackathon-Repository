using UnityEngine;
using TMPro;

/// <summary>
/// Controls the victory screen shown after defeating the final boss.
/// Displays the game logo, patron portrait + name, and handles
/// Main Menu / Quit buttons via BoxCollider2D click detection.
///
/// Setup:
///   1. Create a scene called LVL_Victory
///   2. Add a GameObject with this script
///   3. Assign the logo SpriteRenderer, patron portrait SpriteRenderer,
///      patron name TMP text, and the two BoxCollider2D buttons
///   4. Add LVL_Victory to Build Settings
/// </summary>
public class VictoryScreenController : MonoBehaviour
{
	[Header("Visuals")]
	[Tooltip("SpriteRenderer that displays the game logo.")]
	[SerializeField] private SpriteRenderer logoRenderer;
	[Tooltip("SpriteRenderer that displays the patron's portrait.")]
	[SerializeField] private SpriteRenderer patronPortraitRenderer;
	[Tooltip("Text that displays the patron's name.")]
	[SerializeField] private TextMeshPro patronNameText;
	[Tooltip("Text that displays 'VICTORY' or similar title.")]
	[SerializeField] private TextMeshPro titleText;

	[Header("Buttons (BoxCollider2D)")]
	[Tooltip("BoxCollider2D that acts as the Main Menu button.")]
	[SerializeField] private Collider2D mainMenuCollider;
	[Tooltip("BoxCollider2D that acts as the Quit button.")]
	[SerializeField] private Collider2D quitCollider;

	private Camera _cam;

	private void Start()
	{
		_cam = Camera.main;
		SetupPatronDisplay();
	}

	private void SetupPatronDisplay()
	{
		PatronData patron = null;
		if (RunManager.Instance != null && RunManager.Instance.State != null)
			patron = RunManager.Instance.State.patronData;

		if (patron != null)
		{
			// Use the regular portrait (not combat) for the victory screen
			if (patronPortraitRenderer != null)
			{
				patronPortraitRenderer.sprite = patron.portrait;
				patronPortraitRenderer.gameObject.SetActive(true);
			}

			if (patronNameText != null)
				patronNameText.text = "The " + patron.patronName + " thanks you";
		}
		else
		{
			if (patronPortraitRenderer != null)
				patronPortraitRenderer.gameObject.SetActive(false);

			if (patronNameText != null)
				patronNameText.text = "";
		}

		if (titleText != null)
			titleText.text = "VICTORY";
	}

	private void Update()
	{
		if (_cam == null || !Input.GetMouseButtonDown(0)) return;

		Vector2 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);

		if (mainMenuCollider != null && mainMenuCollider.OverlapPoint(mouseWorld))
		{
			OnMainMenu();
			return;
		}

		if (quitCollider != null && quitCollider.OverlapPoint(mouseWorld))
		{
			OnQuit();
			return;
		}
	}

	private void OnMainMenu()
	{
		if (RunManager.Instance != null)
		{
			RunManager.Instance.EndRun();
		}
		else
		{
			UnityEngine.SceneManagement.SceneManager.LoadScene(0);
		}
	}

	private void OnQuit()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
