using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Drives the Patron Select screen (LVL_Patron_Select).
///
/// Layout matches the concept art:
///   Left side  — Large patron preview (portrait, silhouette, name, keywords, tinted background)
///   Right side — "Choose your Pact..." header, 3 selectable patron thumbnail slots, START button
///
/// Flow:
///   Main Menu → Play → loads this scene
///   Player clicks a patron slot → left preview updates
///   Player clicks START → run begins with chosen patron → map scene loads
///
/// Uses world-space SpriteRenderers + TextMeshPro (no Canvas), matching project conventions.
/// </summary>
public class PatronSelectController : MonoBehaviour
{
	[Header("Patron Data")]
	[Tooltip("All available patrons. Assign Wrath, Pride, Ruin ScriptableObject assets.")]
	[SerializeField] private PatronData[] patrons;

	[Header("Preview — Left Side")]
	[SerializeField] private SpriteRenderer patronPortrait;
	[SerializeField] private SpriteRenderer patronSilhouette;
	[SerializeField] private SpriteRenderer previewBackground;
	[SerializeField] private TextMeshPro patronNameText;
	[SerializeField] private TextMeshPro patronKeywordsText;

	[Header("Selection Slots — Right Side")]
	[Tooltip("Assign the 3 PatronSlot GameObjects in order.")]
	[SerializeField] private PatronSlot[] patronSlots;

	[Header("Start Button")]
	[SerializeField] private SpriteRenderer startButtonSprite;
	[SerializeField] private Collider2D startButtonCollider;
	[Tooltip("Color to tint the start button with the selected patron's color.")]
	[SerializeField] private bool tintStartButton = false;

	[Header("Scene")]
	[SerializeField] private string mapSceneName = "LVL_Map_Master";

	private int _selectedIndex = -1;

	void Start()
	{
		// Set up each slot with patron data
		for (int i = 0; i < patronSlots.Length && i < patrons.Length; i++)
		{
			patronSlots[i].Setup(patrons[i], i, OnSlotSelected);
		}

		// Auto-select the first patron
		if (patrons.Length > 0)
			SelectPatron(0);
	}

	void Update()
	{
		// Check for start button click via OnMouseDown on the collider
		if (Input.GetMouseButtonDown(0) && startButtonCollider != null)
		{
			Camera cam = Camera.main;
			if (cam != null)
			{
				Vector2 mouseWorld = cam.ScreenToWorldPoint(Input.mousePosition);
				if (startButtonCollider.OverlapPoint(mouseWorld))
					OnStartClicked();
			}
		}
	}

	/// <summary>
	/// Called when a patron slot is clicked.
	/// </summary>
	private void OnSlotSelected(int index)
	{
		SelectPatron(index);
	}

	/// <summary>
	/// Updates the left-side preview and highlights the selected slot.
	/// </summary>
	private void SelectPatron(int index)
	{
		if (index < 0 || index >= patrons.Length) return;
		if (index == _selectedIndex) return;

		_selectedIndex = index;
		PatronData patron = patrons[index];

		// Update preview
		if (patronPortrait != null)
			patronPortrait.sprite = patron.portrait;

		if (patronSilhouette != null)
		{
			// Silhouette uses same portrait but rendered dark behind
			patronSilhouette.sprite = patron.portrait;
			patronSilhouette.color = new Color(0f, 0f, 0f, 0.6f);
		}

		if (previewBackground != null)
		{
			Color bg = patron.patronColor;
			bg.a = previewBackground.color.a; // Preserve existing alpha
			previewBackground.color = bg;
		}

		if (patronNameText != null)
			patronNameText.text = patron.patronName.ToUpper();

		if (patronKeywordsText != null)
			patronKeywordsText.text = patron.selectKeywords;

		// Tint start button with patron color
		if (tintStartButton && startButtonSprite != null)
		{
			Color btn = patron.patronColor;
			btn.a = startButtonSprite.color.a;
			startButtonSprite.color = btn;
		}

		// Update slot selection highlights
		for (int i = 0; i < patronSlots.Length; i++)
			patronSlots[i].SetSelected(i == index);
	}

	/// <summary>
	/// Called when START is clicked. Begins the run with the selected patron.
	/// </summary>
	private void OnStartClicked()
	{
		if (_selectedIndex < 0 || _selectedIndex >= patrons.Length) return;

		PatronData patron = patrons[_selectedIndex];
		LaunchRunWithPatron(patron);
	}

	/// <summary>
	/// Builds the run state and loads the map scene.
	/// Mirrors the logic from MainMenuController.StartRunWithPatron().
	/// </summary>
	private void LaunchRunWithPatron(PatronData patron)
	{
		if (patron == null)
		{
			Debug.LogError("[PatronSelect] No patron selected!");
			return;
		}

		// Destroy any leftover RunManager from a previous run
		if (RunManager.Instance != null)
			Destroy(RunManager.Instance.gameObject);

		// Build starter deck
		List<CardData> deck = new List<CardData>();
		if (patron.starterDeck != null && patron.starterDeck.cards != null)
		{
			for (int i = 0; i < patron.starterDeck.cards.Count; i++)
			{
				if (patron.starterDeck.cards[i] != null)
					deck.Add(patron.starterDeck.cards[i]);
			}
		}

		int hp = patron.characterData != null ? patron.characterData.baseHealth : 80;
		int gold = patron.startingGold;
		int seed = Random.Range(0, int.MaxValue);

		// Capture for closure
		PatronData patronCapture = patron;
		List<CardData> deckCopy = new List<CardData>(deck);

		// Queue run setup — the map scene's RunManager will pick this up in Awake
		RunManager.PendingRunSetup = (rm) =>
		{
			rm.StartNewRun(seed, hp, deckCopy, false);
			rm.State.patronData = patronCapture;
			rm.State.patronFaction = patronCapture.faction;
			rm.State.gold = gold;
		};

		Debug.Log($"[PatronSelect] Starting run with {patron.patronName} ({patron.faction})");
		SceneManager.LoadScene(mapSceneName);
	}
}
