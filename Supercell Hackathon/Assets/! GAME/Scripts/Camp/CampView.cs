using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Camp UI controller. Two options: Rest (heal) or Upgrade a Card.
/// </summary>
public class CampView : MonoBehaviour
{
	[Header("Main Buttons")]
	public Button restButton;
	public Button upgradeButton;
	public Button leaveButton;

	[Header("Rest")]
	public TextMeshProUGUI restDescription;

	[Header("Upgrade Card Selection")]
	public GameObject upgradePanel;
	public Transform upgradeCardContainer;
	public GameObject upgradeCardPrefab;
	public Button cancelUpgradeButton;

	[Header("Status")]
	public TextMeshProUGUI hpDisplay;
	public TextMeshProUGUI feedbackText;

	float healPercent;
	bool hasActed;

	public void Initialize(float campHealPercent)
	{
		healPercent = campHealPercent;
		hasActed = false;

		if (restButton != null)
			restButton.onClick.AddListener(OnRestClicked);

		if (upgradeButton != null)
			upgradeButton.onClick.AddListener(OnUpgradeClicked);

		if (leaveButton != null)
			leaveButton.onClick.AddListener(OnLeaveClicked);

		if (cancelUpgradeButton != null)
			cancelUpgradeButton.onClick.AddListener(HideUpgradePanel);

		if (upgradePanel != null)
			upgradePanel.SetActive(false);

		if (feedbackText != null)
			feedbackText.text = "";

		RefreshHPDisplay();
		UpdateRestDescription();
	}

	void UpdateRestDescription()
	{
		if (restDescription == null) return;

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		int healAmount = Mathf.RoundToInt(state.maxHP * healPercent);
		restDescription.text = $"Rest at the campfire.\nHeal {healAmount} HP ({Mathf.RoundToInt(healPercent * 100)}% of max).";
	}

	void RefreshHPDisplay()
	{
		if (hpDisplay == null) return;

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		hpDisplay.text = $"HP: {state.currentHP} / {state.maxHP}";
	}

	// --- REST ---

	void OnRestClicked()
	{
		if (hasActed) return;

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		int healAmount = Mathf.RoundToInt(state.maxHP * healPercent);
		int oldHP = state.currentHP;
		state.currentHP = Mathf.Min(state.currentHP + healAmount, state.maxHP);
		int actualHeal = state.currentHP - oldHP;

		hasActed = true;
		DisableChoiceButtons();
		RefreshHPDisplay();

		if (feedbackText != null)
			feedbackText.text = $"Rested. Healed {actualHeal} HP.";
	}

	// --- UPGRADE ---

	void OnUpgradeClicked()
	{
		if (hasActed) return;

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		// Check if any cards can be upgraded
		bool hasUpgradeable = false;
		for (int i = 0; i < state.deck.Count; i++)
		{
			if (state.deck[i].canUpgrade && state.deck[i].upgradedCard != null)
			{
				hasUpgradeable = true;
				break;
			}
		}

		if (!hasUpgradeable)
		{
			if (feedbackText != null)
				feedbackText.text = "No cards can be upgraded.";
			return;
		}

		ShowUpgradePanel();
	}

	void ShowUpgradePanel()
	{
		if (upgradePanel == null) return;
		upgradePanel.SetActive(true);

		// Clear existing
		if (upgradeCardContainer != null)
		{
			foreach (Transform child in upgradeCardContainer)
				Destroy(child.gameObject);
		}

		RunState state = RunManager.Instance?.State;
		if (state == null || upgradeCardPrefab == null) return;

		// Show only upgradeable cards
		for (int i = 0; i < state.deck.Count; i++)
		{
			CardData card = state.deck[i];
			if (!card.canUpgrade || card.upgradedCard == null) continue;

			int index = i;
			GameObject obj = Instantiate(upgradeCardPrefab, upgradeCardContainer);

			TextMeshProUGUI nameText = obj.GetComponentInChildren<TextMeshProUGUI>();
			if (nameText != null)
				nameText.text = card.cardName;

			Button btn = obj.GetComponent<Button>();
			if (btn != null)
				btn.onClick.AddListener(() => OnUpgradeCardSelected(index));
		}
	}

	void HideUpgradePanel()
	{
		if (upgradePanel != null)
			upgradePanel.SetActive(false);
	}

	void OnUpgradeCardSelected(int deckIndex)
	{
		RunState state = RunManager.Instance?.State;
		if (state == null) return;
		if (deckIndex < 0 || deckIndex >= state.deck.Count) return;

		CardData card = state.deck[deckIndex];
		if (!card.canUpgrade || card.upgradedCard == null) return;

		// Replace the card with its upgraded version
		string oldName = card.cardName;
		state.deck[deckIndex] = card.upgradedCard;

		hasActed = true;
		DisableChoiceButtons();
		HideUpgradePanel();

		if (feedbackText != null)
			feedbackText.text = $"Upgraded {oldName}!";
	}

	void DisableChoiceButtons()
	{
		if (restButton != null)
			restButton.interactable = false;

		if (upgradeButton != null)
			upgradeButton.interactable = false;
	}

	void OnLeaveClicked()
	{
		if (RunManager.Instance != null)
			RunManager.Instance.OnEncounterComplete();
	}
}
