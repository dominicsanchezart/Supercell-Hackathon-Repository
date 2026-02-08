using UnityEngine;
using TMPro;

/// <summary>
/// Camp encounter UI controller.
/// Presents a single Rest option card (heal HP) and a Leave button.
///
/// OptionCards are world-space sprites with Collider2D click detection.
/// UI overlay (HP, feedback, Leave) lives on a Screen Space - Overlay Canvas.
/// </summary>
public class CampView : MonoBehaviour
{
	[Header("Option Cards")]
	[Tooltip("The Rest option card (placed in scene, wired in inspector).")]
	public OptionCard restOption;

	[Header("UI")]
	public TextMeshProUGUI hpDisplay;
	public TextMeshProUGUI feedbackText;
	public UnityEngine.UI.Button leaveButton;

	float healPercent;
	bool hasActed;

	// ─── Initialization ───────────────────────────────────────────

	public void Initialize(CampData campData)
	{
		healPercent = campData != null ? campData.healPercent : 0.30f;
		hasActed = false;

		// Calculate heal amount for display
		RunState state = RunManager.Instance?.State;
		int healAmount = 0;
		if (state != null)
			healAmount = Mathf.RoundToInt(state.maxHP * healPercent);

		if (restOption != null)
		{
			string restTitle = campData != null ? campData.restTitle : "Rest";
			string restDesc = campData != null ? campData.restDescription : "Heal {healAmount} HP.";
			restDesc = restDesc.Replace("{healAmount}", healAmount.ToString());

			restOption.Setup(
				restTitle,
				restDesc,
				campData != null ? campData.restIcon : null
			);
			restOption.onClicked += OnOptionClicked;
		}

		// Wire Leave button
		if (leaveButton != null)
			leaveButton.onClick.AddListener(OnLeaveClicked);

		if (feedbackText != null)
			feedbackText.text = "";

		RefreshHPDisplay();
	}

	// ─── Option Card Clicks ───────────────────────────────────────

	void OnOptionClicked(OptionCard card)
	{
		if (hasActed) return;

		if (card == restOption)
			OnRestChosen();
	}

	// ─── Rest ─────────────────────────────────────────────────────

	void OnRestChosen()
	{
		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		int healAmount = Mathf.RoundToInt(state.maxHP * healPercent);
		int oldHP = state.currentHP;
		state.currentHP = Mathf.Min(state.currentHP + healAmount, state.maxHP);
		int actualHeal = state.currentHP - oldHP;

		hasActed = true;

		if (restOption != null)
			restOption.SetInteractable(false);

		RefreshHPDisplay();

		if (feedbackText != null)
			feedbackText.text = $"Rested. Healed {actualHeal} HP.";
	}

	// ─── UI Helpers ───────────────────────────────────────────────

	void RefreshHPDisplay()
	{
		if (hpDisplay == null) return;

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		hpDisplay.text = $"HP: {state.currentHP} / {state.maxHP}";
	}

	void OnLeaveClicked()
	{
		if (RunManager.Instance != null)
			RunManager.Instance.OnEncounterComplete();
	}
}
