using UnityEngine;
using TMPro;

/// <summary>
/// Displays a Game Over screen when the player dies.
/// Shows defeat text and a button to return to the main menu (placeholder).
/// 
/// Setup: Attach to a GameObject in the battle scene.
/// The panel starts disabled and is shown via ShowDefeat().
/// </summary>
public class GameOverUI : MonoBehaviour
{
	[Header("UI Root")]
	[Tooltip("Parent object that holds all game-over UI elements. Disabled by default.")]
	[SerializeField] private GameObject gameOverPanel;

	[Header("Text")]
	[SerializeField] private TextMeshProUGUI titleText;
	[SerializeField] private TextMeshProUGUI subtitleText;

	[Header("Main Menu Button")]
	[SerializeField] private OptionCard mainMenuButton;

	private System.Action onMainMenuPressed;

	private void Awake()
	{
		if (gameOverPanel != null)
			gameOverPanel.SetActive(false);
	}

	/// <summary>
	/// Show the defeat screen.
	/// </summary>
	/// <param name="onMainMenu">Called when the player clicks the main menu button.</param>
	public void ShowDefeat(System.Action onMainMenu)
	{
		onMainMenuPressed = onMainMenu;

		if (gameOverPanel != null)
			gameOverPanel.SetActive(true);

		if (titleText != null)
			titleText.text = "DEFEAT";

		if (subtitleText != null)
			subtitleText.text = "Your journey ends here...";

		if (mainMenuButton != null)
		{
			mainMenuButton.gameObject.SetActive(true);
			mainMenuButton.Setup("Main Menu", "Return to main menu");
			mainMenuButton.onClicked = _ => OnMainMenuClicked();
		}
	}

	private void OnMainMenuClicked()
	{
		onMainMenuPressed?.Invoke();
	}

	public void Hide()
	{
		if (gameOverPanel != null)
			gameOverPanel.SetActive(false);
	}
}
