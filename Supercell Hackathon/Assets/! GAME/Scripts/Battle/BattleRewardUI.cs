using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// Displays random card rewards after an enemy is defeated (or from a Treasure node).
/// Spawns Card.prefab instances directly (like CardViewer / ShopCardSlot)
/// and uses Physics2D raycasting for hover and click — no extra prefab needed.
///
/// Also awards gold with a configurable amount. Gold is displayed in the reward text.
///
/// Setup (in battle scene or treasure scene):
///   1. Create a child GameObject as the card root (where cards are parented)
///   2. Assign Card.prefab and a Deck as the reward pool
///   3. Optionally add a skip OptionCard and title text
/// </summary>
public class BattleRewardUI : MonoBehaviour
{
	[Header("UI Root")]
	[Tooltip("Parent object that holds all reward UI elements. Disabled by default.")]
	[SerializeField] private GameObject rewardPanel;

	[Header("Card Spawning")]
	[Tooltip("The real Card.prefab — spawned for each reward option.")]
	[SerializeField] private GameObject cardPrefab;
	[Tooltip("Parent transform for spawned cards.")]
	[SerializeField] private Transform cardRoot;

	[Header("Layout")]
	[SerializeField] private float cardSpacing = 2.8f;

	[Header("Hover")]
	[SerializeField] private float hoverScale = 1.25f;
	[SerializeField] private float hoverSmooth = 14f;
	[SerializeField] private int hoverSortingBoost = 500;

	[Header("Sorting")]
	[SerializeField] private string rewardSortingLayer = "Card";
	[SerializeField] private int baseSortingOrder = 100;

	[Header("Raycasting")]
	[SerializeField] private LayerMask cardLayer;

	[Header("Text")]
	[SerializeField] private TextMeshProUGUI titleText;
	[Tooltip("Optional separate text element for gold reward display. If null, gold is shown in the title.")]
	[SerializeField] private TextMeshProUGUI goldRewardText;

	[Header("Skip")]
	[Tooltip("Optional button to skip the reward and go straight to the map.")]
	[SerializeField] private OptionCard skipButton;

	[Header("Card Pool")]
	[Tooltip("Deck ScriptableObject containing all cards that can appear as rewards.")]
	[SerializeField] private Deck rewardPool;

	[Header("Reward Count")]
	[SerializeField] private int cardsToOffer = 3;

	[Header("Gold Reward")]
	[SerializeField] private int goldRewardMin = 10;
	[SerializeField] private int goldRewardMax = 25;

	// Runtime state
	private readonly List<GameObject> spawnedCards = new();
	private CardData[] offeredCards;
	private System.Action<CardData> onCardChosen;
	private System.Action onComplete;
	private bool isActive;

	// Hover state
	private Camera _cam;
	private Transform hoveredCard;
	private Vector3 hoveredOriginalScale;
	private int hoveredOriginalSorting;
	private CardView hoveredCardView;

	/// <summary>
	/// When true, all clicks are ignored (player already chose or skipped).
	/// </summary>
	public bool IsLocked { get; private set; }

	private void Awake()
	{
		_cam = Camera.main;

		if (rewardPanel != null)
			rewardPanel.SetActive(false);
	}

	private void Update()
	{
		if (!isActive) return;

		HandleHover();
		HandleClick();
	}

	/// <summary>
	/// Show the reward screen with random cards from the pool and a gold reward.
	/// </summary>
	public void ShowRewards(System.Action<CardData> onChosen, System.Action onDone)
	{
		int goldAmount = Random.Range(goldRewardMin, goldRewardMax + 1);
		ShowRewards(onChosen, onDone, goldAmount);
	}

	/// <summary>
	/// Show the reward screen with random cards and a specific gold amount.
	/// Use goldAmount = 0 to skip gold.
	/// </summary>
	public void ShowRewards(System.Action<CardData> onChosen, System.Action onDone, int goldAmount)
	{
		onCardChosen = onChosen;
		onComplete = onDone;
		IsLocked = false;
		isActive = true;

		if (_cam == null)
			_cam = Camera.main;

		if (rewardPanel != null)
			rewardPanel.SetActive(true);

		// Award gold immediately
		if (goldAmount > 0)
		{
			if (RunManager.Instance != null && RunManager.Instance.State != null)
			{
				RunManager.Instance.State.gold += goldAmount;
				Debug.Log($"[Rewards] Awarded {goldAmount} gold. Total: {RunManager.Instance.State.gold}");
			}
		}

		// Display title and gold
		if (titleText != null)
			titleText.text = "Choose a Card Reward";

		if (goldRewardText != null)
			goldRewardText.text = goldAmount > 0 ? $"+{goldAmount} Gold" : "";
		else if (titleText != null && goldAmount > 0)
			titleText.text = $"Choose a Card Reward\n<size=70%>+{goldAmount} Gold</size>";

		offeredCards = PickRandomCards(cardsToOffer);
		SpawnCards(offeredCards);

		if (skipButton != null)
		{
			skipButton.gameObject.SetActive(true);
			skipButton.Setup("Skip", "Take no card");
			skipButton.onClicked = _ => OnSkip();
		}
	}

	/// <summary>
	/// Standalone treasure reward — no card chosen callback needed.
	/// Awards gold and offers card rewards.
	/// </summary>
	public void ShowTreasureRewards(System.Action onDone, int goldAmount = -1)
	{
		if (goldAmount < 0)
			goldAmount = Random.Range(goldRewardMin, goldRewardMax + 1);

		ShowRewards(
			chosenCard =>
			{
				// Add card to run state deck
				if (RunManager.Instance != null && RunManager.Instance.State != null)
					RunManager.Instance.State.deck.Add(chosenCard);

				Debug.Log($"[Treasure] Added {chosenCard.cardName} to deck.");
			},
			onDone,
			goldAmount
		);
	}

	// ─── Card Spawning & Layout ───────────────────────────────────

	private void SpawnCards(CardData[] cards)
	{
		ClearCards();

		if (cardPrefab == null)
		{
			Debug.LogWarning("BattleRewardUI: cardPrefab is not assigned.");
			return;
		}

		Transform parent = cardRoot != null ? cardRoot : transform;

		for (int i = 0; i < cards.Length; i++)
		{
			GameObject cardObj = Instantiate(cardPrefab, parent);
			spawnedCards.Add(cardObj);

			// Set card data
			Card card = cardObj.GetComponent<Card>();
			if (card != null)
				card.SetCardData(cards[i]);

			// Place reward cards on the Card sorting layer above battle sprites
			CardView cardView = cardObj.GetComponent<CardView>();
			if (cardView != null)
			{
				cardView.SetSortingLayer(rewardSortingLayer);
				cardView.SetSortingOrder(baseSortingOrder + i);
				cardView.enabled = false; // disable Hand-based interaction
			}

			// Ensure the card has a collider for Physics2D raycasting
			Collider2D col = cardObj.GetComponent<Collider2D>();
			if (col != null)
				col.enabled = true;

			// Position: center the row around x=0
			float xOffset = (i - (cards.Length - 1) * 0.5f) * cardSpacing;
			cardObj.transform.localPosition = new Vector3(xOffset, 0f, 0f);
			cardObj.transform.localRotation = Quaternion.identity;
		}
	}

	// ─── Hover (same pattern as CardViewer) ───────────────────────

	private void HandleHover()
	{
		if (_cam == null || spawnedCards.Count == 0) return;

		Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
		mouseWorld.z = 0f;

		Collider2D hit = Physics2D.OverlapPoint(mouseWorld, cardLayer);

		if (hit != null)
		{
			Transform card = hit.transform;

			// Check if it belongs to one of our spawned cards
			bool isOurs = false;
			for (int i = 0; i < spawnedCards.Count; i++)
			{
				if (spawnedCards[i] == null) continue;
				if (spawnedCards[i].transform == card || card.IsChildOf(spawnedCards[i].transform))
				{
					card = spawnedCards[i].transform; // normalize to root card
					isOurs = true;
					break;
				}
			}

			if (isOurs && hoveredCard != card)
				BeginHover(card);
			else if (!isOurs)
				EndHover();
		}
		else
		{
			EndHover();
		}

		if (hoveredCard != null)
			UpdateHoveredCard();
	}

	private void BeginHover(Transform card)
	{
		EndHover();

		hoveredCard = card;
		hoveredOriginalScale = card.localScale;

		hoveredCardView = card.GetComponent<CardView>();
		if (hoveredCardView == null)
			hoveredCardView = card.GetComponentInParent<CardView>();

		if (hoveredCardView != null)
		{
			SpriteRenderer sr = card.GetComponentInChildren<SpriteRenderer>();
			hoveredOriginalSorting = sr != null ? sr.sortingOrder : 0;
			hoveredCardView.SetSortingOrder(hoveredOriginalSorting + hoverSortingBoost);
		}
	}

	private void UpdateHoveredCard()
	{
		if (hoveredCard == null) return;
		hoveredCard.localScale = Vector3.Lerp(
			hoveredCard.localScale,
			hoveredOriginalScale * hoverScale,
			Time.deltaTime * hoverSmooth
		);
	}

	private void EndHover()
	{
		if (hoveredCard == null) return;

		hoveredCard.localScale = hoveredOriginalScale;

		if (hoveredCardView != null)
			hoveredCardView.SetSortingOrder(hoveredOriginalSorting);

		hoveredCard = null;
		hoveredCardView = null;
	}

	// ─── Click ────────────────────────────────────────────────────

	private void HandleClick()
	{
		if (IsLocked) return;
		if (_cam == null || spawnedCards.Count == 0) return;
		if (!Input.GetMouseButtonDown(0)) return;

		Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
		mouseWorld.z = 0f;

		Collider2D hit = Physics2D.OverlapPoint(mouseWorld, cardLayer);
		if (hit == null) return;

		Transform clicked = hit.transform;
		for (int i = 0; i < spawnedCards.Count; i++)
		{
			if (spawnedCards[i] == null) continue;
			if (spawnedCards[i].transform == clicked ||
				clicked.IsChildOf(spawnedCards[i].transform))
			{
				OnCardClicked(i);
				return;
			}
		}
	}

	private void OnCardClicked(int index)
	{
		if (IsLocked) return;
		if (index < 0 || index >= offeredCards.Length) return;

		IsLocked = true;
		EndHover();

		CardData chosen = offeredCards[index];

		// Grey out non-chosen cards
		for (int i = 0; i < spawnedCards.Count; i++)
		{
			if (i == index) continue;
			GreyOutCard(spawnedCards[i]);
		}

		if (skipButton != null)
			skipButton.SetInteractable(false);

		Debug.Log($"Player chose reward card: {chosen.cardName}");
		onCardChosen?.Invoke(chosen);

		StartCoroutine(ProceedAfterDelay(0.6f));
	}

	private void OnSkip()
	{
		if (IsLocked) return;
		IsLocked = true;
		EndHover();

		for (int i = 0; i < spawnedCards.Count; i++)
			GreyOutCard(spawnedCards[i]);

		if (skipButton != null)
			skipButton.SetInteractable(false);

		Debug.Log("Player skipped card reward.");
		StartCoroutine(ProceedAfterDelay(0.3f));
	}

	private void GreyOutCard(GameObject cardObj)
	{
		if (cardObj == null) return;

		SpriteRenderer[] renderers = cardObj.GetComponentsInChildren<SpriteRenderer>();
		foreach (var sr in renderers)
		{
			Color c = sr.color;
			c.a = 0.35f;
			sr.color = c;
		}

		Collider2D col = cardObj.GetComponent<Collider2D>();
		if (col != null)
			col.enabled = false;
	}

	// ─── Helpers ──────────────────────────────────────────────────

	private System.Collections.IEnumerator ProceedAfterDelay(float delay)
	{
		yield return new WaitForSeconds(delay);
		Hide();
		onComplete?.Invoke();
	}

	private void Hide()
	{
		EndHover();
		ClearCards();
		isActive = false;

		if (rewardPanel != null)
			rewardPanel.SetActive(false);
	}

	private void ClearCards()
	{
		foreach (var card in spawnedCards)
		{
			if (card != null)
				Destroy(card);
		}
		spawnedCards.Clear();
	}

	private CardData[] PickRandomCards(int count)
	{
		if (rewardPool == null || rewardPool.cards == null || rewardPool.cards.Count == 0)
		{
			Debug.LogWarning("BattleRewardUI: rewardPool deck is empty. No cards to offer.");
			return new CardData[0];
		}

		List<CardData> pool = new(rewardPool.cards);
		List<CardData> picked = new();

		for (int i = 0; i < count && pool.Count > 0; i++)
		{
			int idx = Random.Range(0, pool.Count);
			picked.Add(pool[idx]);
			pool.RemoveAt(idx);
		}

		return picked.ToArray();
	}
}
