using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the main menu screen (LVL_Main_Menu).
/// Play → loads the patron select screen where the player picks their pact.
/// Quit → exits the application.
/// </summary>
public class MainMenuController : MonoBehaviour
{
	[Header("Buttons")]
	[SerializeField] Button playButton;
	[SerializeField] Button quitButton;

	[Header("Scene")]
	[Tooltip("Name of the patron select scene to load when Play is pressed.")]
	[SerializeField] string patronSelectSceneName = "LVL_Patron_Select";

	void Start()
	{
		if (playButton != null)
			playButton.onClick.AddListener(OnPlay);

		if (quitButton != null)
			quitButton.onClick.AddListener(OnQuit);
	}

	void OnPlay()
	{
		UnityEngine.SceneManagement.SceneManager.LoadScene(patronSelectSceneName);
	}

	void OnQuit()
	{
#if UNITY_EDITOR
		UnityEditor.EditorApplication.isPlaying = false;
#else
		Application.Quit();
#endif
	}
}
