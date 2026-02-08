using TMPro;
using UnityEngine;

/// <summary>
/// Displays the enemy's predicted intent for their next turn as a text label.
/// Attach to a UI GameObject with a TextMeshProUGUI component (typically
/// placed above the enemy's HUD).
/// </summary>
[RequireComponent(typeof(TextMeshProUGUI))]
public class EnemyIntentDisplay : MonoBehaviour
{
	[Header("References")]
	[Tooltip("The enemy Hand to analyse. If left null, Arena will assign it at runtime.")]
	[SerializeField] private Hand enemyHand;

	[Header("Display Settings")]
	[Tooltip("Prefix shown before the intent label.")]
	[SerializeField] private string prefix = "Intent: ";
	[Tooltip("If true, the intent text colour changes to match the category.")]
	[SerializeField] private bool colorizeText = true;

	private TextMeshProUGUI _text;
	private EnemyIntent _currentIntent = EnemyIntent.Unknown;

	private void Awake()
	{
		_text = GetComponent<TextMeshProUGUI>();
		Hide();
	}

	/// <summary>
	/// Assign the enemy hand reference at runtime (called by Arena).
	/// </summary>
	public void Bind(Hand hand)
	{
		enemyHand = hand;
	}

	/// <summary>
	/// Evaluates the enemy's upcoming hand and refreshes the display.
	/// Call this at the start of the player's turn.
	/// </summary>
	public void Refresh()
	{
		if (enemyHand == null)
		{
			Hide();
			return;
		}

		_currentIntent = EnemyIntentResolver.Resolve(enemyHand);
		string label = EnemyIntentResolver.GetIntentDisplayText(_currentIntent);

		_text.text = prefix + label;

		if (colorizeText)
			_text.color = EnemyIntentResolver.GetIntentColor(_currentIntent);

		gameObject.SetActive(true);
	}

	/// <summary>
	/// Hides the intent display (e.g. during the enemy's turn).
	/// </summary>
	public void Hide()
	{
		if (_text != null)
			_text.text = "";

		gameObject.SetActive(false);
	}

	/// <summary>
	/// Returns the current predicted intent.
	/// </summary>
	public EnemyIntent CurrentIntent => _currentIntent;
}
