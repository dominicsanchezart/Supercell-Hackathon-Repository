using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Camp encounter UI controller.
/// Presents two OptionCard choices: Rest (heal HP) or Upgrade (improve a card).
/// Upgrade selection reuses the CardViewer overlay (same pattern as shop card removal).
///
/// OptionCards are world-space sprites with Collider2D click detection.
/// UI overlay (HP, feedback, Leave) lives on a Screen Space - Overlay Canvas.
/// </summary>
public class CampView : MonoBehaviour
{
	[Header("Option Cards")]
	[Tooltip("The Rest option card (placed in scene, wired in inspector).")]
	public OptionCard restOption;
	[Tooltip("The Upgrade option card (placed in scene, wired in inspector).")]
	public OptionCard upgradeOption;

	[Header("UI")]
	public TextMeshProUGUI hpDisplay;
	public TextMeshProUGUI feedbackText;
	public Button leaveButton;

	// Card viewer (found at runtime from Main Camera child)
	private CardViewer cardViewer;

	float healPercent;
	bool hasActed;
	bool isUpgradeOpen;

	/// <summary>
	/// Maps CardViewer card index → actual deck index.
	/// Needed because CardViewer only shows upgradeable cards (a filtered subset).
	/// </summary>
	List<int> upgradeableDeckIndices = new();

	// ─── Initialization ───────────────────────────────────────────

	public void Initialize(CampData campData)
	{
		// Find the CardViewer on the Main Camera
		cardViewer = CardViewer.Instance;

		healPercent = campData != null ? campData.healPercent : 0.30f;
		hasActed = false;
		isUpgradeOpen = false;

		// Skin the option cards using CampData text (same placeholder pattern as CardData)
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

		if (upgradeOption != null)
		{
			string upgradeTitle = campData != null ? campData.upgradeTitle : "Upgrade";
			string upgradeDesc = campData != null ? campData.upgradeDescription : "Upgrade one card\nin your deck.";

			upgradeOption.Setup(
				upgradeTitle,
				upgradeDesc,
				campData != null ? campData.upgradeIcon : null
			);
			upgradeOption.onClicked += OnOptionClicked;
		}

		// Wire CardViewer callbacks
		if (cardViewer != null)
		{
			cardViewer.onCardSelected += OnUpgradeCardSelected;
			cardViewer.onHideCards += OnUpgradeViewerClosed;
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
		if (isUpgradeOpen) return;

		if (card == restOption)
			OnRestChosen();
		else if (card == upgradeOption)
			OnUpgradeChosen();
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
		DisableOptions();
		RefreshHPDisplay();

		if (feedbackText != null)
			feedbackText.text = $"Rested. Healed {actualHeal} HP.";
	}

	// ─── Upgrade ──────────────────────────────────────────────────

	void OnUpgradeChosen()
	{
		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		// Build list of upgradeable cards + index mapping
		upgradeableDeckIndices.Clear();
		List<CardData> upgradeableCards = new();

		for (int i = 0; i < state.deck.Count; i++)
		{
			CardData card = state.deck[i];
			if (card.canUpgrade && card.upgradedCard != null)
			{
				upgradeableDeckIndices.Add(i);
				upgradeableCards.Add(card);
			}
		}

		if (upgradeableCards.Count == 0)
		{
			if (feedbackText != null)
				feedbackText.text = "No cards can be upgraded.";
			return;
		}

		// Open CardViewer with only upgradeable cards
		ShowUpgradeViewer(upgradeableCards.ToArray());
	}

	void ShowUpgradeViewer(CardData[] cards)
	{
		if (cardViewer == null) return;

		isUpgradeOpen = true;

		// Disable option card interaction while viewer is open
		if (restOption != null) restOption.SetInteractable(false);
		if (upgradeOption != null) upgradeOption.SetInteractable(false);

		// CardViewer manages its own backdrop and raycaster toggling
		cardViewer.DisplayCards(cards);
	}

	/// <summary>
	/// Called when a card is clicked inside the CardViewer.
	/// cardIndex is the index in the displayed (filtered) array.
	/// </summary>
	void OnUpgradeCardSelected(int cardIndex)
	{
		if (!isUpgradeOpen) return;
		if (cardIndex < 0 || cardIndex >= upgradeableDeckIndices.Count) return;

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		int deckIndex = upgradeableDeckIndices[cardIndex];
		if (deckIndex < 0 || deckIndex >= state.deck.Count) return;

		CardData card = state.deck[deckIndex];
		if (!card.canUpgrade || card.upgradedCard == null) return;

		// Upgrade the card
		string oldName = card.cardName;
		state.deck[deckIndex] = card.upgradedCard;

		hasActed = true;

		// Close viewer
		CloseUpgradeViewer();

		// Keep options disabled (already acted)
		DisableOptions();

		if (feedbackText != null)
			feedbackText.text = $"Upgraded {oldName}!";
	}

	/// <summary>
	/// Called when the CardViewer is closed (ESC or HideCards).
	/// If the player hasn't acted yet, restore option interactability.
	/// </summary>
	void OnUpgradeViewerClosed()
	{
		if (!isUpgradeOpen) return;

		CloseUpgradeViewer();

		// If they didn't pick a card, let them choose again
		if (!hasActed)
		{
			if (restOption != null) restOption.SetInteractable(true);
			if (upgradeOption != null) upgradeOption.SetInteractable(true);
		}
	}

	void CloseUpgradeViewer()
	{
		isUpgradeOpen = false;
		// Backdrop and raycaster are managed by CardViewer itself
	}

	// ─── UI Helpers ───────────────────────────────────────────────

	void DisableOptions()
	{
		if (restOption != null) restOption.SetInteractable(false);
		if (upgradeOption != null) upgradeOption.SetInteractable(false);
	}

	void RefreshHPDisplay()
	{
		if (hpDisplay == null) return;

		RunState state = RunManager.Instance?.State;
		if (state == null) return;

		hpDisplay.text = $"HP: {state.currentHP} / {state.maxHP}";
	}

	void OnLeaveClicked()
	{
		if (isUpgradeOpen) return;

		if (RunManager.Instance != null)
			RunManager.Instance.OnEncounterComplete();
	}

	private void OnDestroy()
	{
		// Clear callbacks so the persistent CardViewer doesn't hold stale delegates
		if (cardViewer != null)
			cardViewer.ClearCallbacks();
	}
}
