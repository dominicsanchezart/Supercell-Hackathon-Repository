using System.Collections;
using UnityEngine;
using Neocortex;
using Neocortex.Data;

/// <summary>
/// Thin wrapper around the NeoCortex Unity SDK.
/// Isolates the third-party dependency so the rest of the patron dialogue system
/// never directly calls the SDK. If the SDK is unavailable, this component can
/// be left unassigned and the system gracefully falls back to scripted lines.
///
/// Setup:
///   1. Install NeoCortex SDK via Package Manager (git URL)
///   2. Configure API key in Tools > Neocortex Settings
///   3. Create 3 agents on neocortex.link (one per patron personality)
///   4. Add 3 GameObjects with NeocortexSmartAgent components, assign their Project IDs
///   5. Wire them into this component's Inspector fields
/// </summary>
public class NeoCortexProvider : MonoBehaviour
{
	[Header("Timeout")]
	[Tooltip("Seconds to wait for AI response before falling back to scripted.")]
	[SerializeField] private float aiTimeout = 2.5f;

	[Header("Agent References (one per patron)")]
	[SerializeField] private NeocortexSmartAgent wrathAgent;
	[SerializeField] private NeocortexSmartAgent prideAgent;
	[SerializeField] private NeocortexSmartAgent ruinAgent;

	private NeocortexSmartAgent _activeAgent;

	private Coroutine _timeoutRoutine;
	private System.Action<string> _onSuccess;
	private System.Action _onFailed;
	private bool _responseReceived;

	private void Start()
	{
		if (RunManager.Instance != null && RunManager.Instance.State != null)
		{
			_activeAgent = RunManager.Instance.State.patronFaction switch
			{
				CardFaction.Wrath => wrathAgent,
				CardFaction.Pride => prideAgent,
				CardFaction.Ruin  => ruinAgent,
				_ => null
			};

			if (_activeAgent != null)
			{
				_activeAgent.OnChatResponseReceived.AddListener(OnChatResponse);
				_activeAgent.OnRequestFailed.AddListener(OnRequestFailed);
			}
		}
	}

	/// <summary>
	/// Requests a dialogue line from the NeoCortex AI.
	/// If the response doesn't arrive within the timeout, onFailed is invoked.
	/// </summary>
	public void RequestLine(string contextMessage, System.Action<string> onSuccess, System.Action onFailed)
	{
		if (_activeAgent == null)
		{
			onFailed?.Invoke();
			return;
		}

		_onSuccess = onSuccess;
		_onFailed = onFailed;
		_responseReceived = false;

		_activeAgent.TextToText(contextMessage);
		_timeoutRoutine = StartCoroutine(TimeoutRoutine());
	}

	/// <summary>
	/// Builds a rich context string to send to the NeoCortex AI agent.
	/// Includes patron identity, enemy info, player state, and event details.
	/// </summary>
	public string BuildContext(DialogueTriggerType trigger,
		string affinityTier, CardFaction patronFaction,
		string description)
	{
		string patronName = "";
		string pactTitle = "";
		string passiveDesc = "";
		int playerHP = 0;
		int playerMaxHP = 0;
		string enemyName = "";
		int enemyDifficulty = 0;
		int gold = 0;
		int deckSize = 0;

		if (RunManager.Instance != null
			&& RunManager.Instance.State != null)
		{
			var state = RunManager.Instance.State;

			if (state.patronData != null)
			{
				patronName = state.patronData.patronName;
				pactTitle = state.patronData.pactTitle;
				passiveDesc = state.patronData.passiveDescription;
			}

			playerHP = state.currentHP;
			playerMaxHP = state.maxHP;
			gold = state.gold;
			deckSize = state.deck != null ? state.deck.Count : 0;

			if (state.currentEnemyPreset != null)
			{
				enemyName = state.currentEnemyPreset.enemyName;
				enemyDifficulty = state.currentEnemyPreset.difficultyTier;
			}
		}

		float hpPercent = playerMaxHP > 0
			? (float)playerHP / playerMaxHP * 100f : 100f;
		string hpStatus = hpPercent > 75 ? "healthy"
			: hpPercent > 40 ? "wounded"
			: hpPercent > 15 ? "badly hurt" : "near death";

		string ctx = $"You are {patronName}, the {pactTitle}. ";
		ctx += "You are speaking directly to your warlock, ";
		ctx += "the imp who made a pact with you. ";
		ctx += $"Your warlock's devotion to you is {affinityTier}. ";
		ctx += $"They are {hpStatus} ({playerHP}/{playerMaxHP} HP), ";
		ctx += $"carrying {gold} gold and {deckSize} contracts. ";

		if (!string.IsNullOrEmpty(enemyName))
		{
			string threat = enemyDifficulty <= 1 ? "a weak foe"
				: enemyDifficulty <= 3 ? "a worthy opponent"
				: "a dangerous adversary";
			ctx += $"They face {enemyName}, {threat}. ";
		}

		ctx += $"What just happened: {description} ";
		ctx += "Address your warlock directly using you/your. ";
		ctx += "1-2 sentences only. No quotes. No narration.";

		return ctx;
	}

	/// <summary>
	/// Resets the NeoCortex session ID so each new run starts with fresh AI context.
	/// </summary>
	public void CleanSessionOnNewRun()
	{
		if (_activeAgent != null)
			_activeAgent.CleanSessionID();
	}

	private void OnChatResponse(ChatResponse response)
	{
		if (_responseReceived) return;
		_responseReceived = true;

		if (_timeoutRoutine != null)
			StopCoroutine(_timeoutRoutine);

		_onSuccess?.Invoke(response.message);
		ClearCallbacks();
	}

	private void OnRequestFailed(string error)
	{
		if (_responseReceived) return;
		_responseReceived = true;

		if (_timeoutRoutine != null)
			StopCoroutine(_timeoutRoutine);

		Debug.LogWarning($"[NeoCortex] Request failed: {error}");
		_onFailed?.Invoke();
		ClearCallbacks();
	}

	private IEnumerator TimeoutRoutine()
	{
		yield return new WaitForSeconds(aiTimeout);

		if (!_responseReceived)
		{
			_responseReceived = true;
			Debug.LogWarning($"[NeoCortex] Timeout after {aiTimeout}s — falling back to scripted.");
			_onFailed?.Invoke();
			ClearCallbacks();
		}
	}

	private void ClearCallbacks()
	{
		_onSuccess = null;
		_onFailed = null;
	}

	private void OnDestroy()
	{
		if (_activeAgent != null)
		{
			_activeAgent.OnChatResponseReceived.RemoveListener(OnChatResponse);
			_activeAgent.OnRequestFailed.RemoveListener(OnRequestFailed);
		}
	}
}
