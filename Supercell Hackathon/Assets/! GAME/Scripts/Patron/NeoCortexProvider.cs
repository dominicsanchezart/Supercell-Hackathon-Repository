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
	[Tooltip("Seconds to wait during live gameplay requests (mid-combat, shop, etc).")]
	[SerializeField] private float liveTimeout = 3f;
	[Tooltip("Seconds to wait during prefetch requests (scene loading, hidden from player).")]
	[SerializeField] private float prefetchTimeout = 15f;

	[Header("Agent References (one per patron)")]
	[SerializeField] private NeocortexSmartAgent wrathAgent;
	[SerializeField] private NeocortexSmartAgent prideAgent;
	[SerializeField] private NeocortexSmartAgent ruinAgent;

	private NeocortexSmartAgent _activeAgent;

	private Coroutine _timeoutRoutine;
	private System.Action<string> _onSuccess;
	private System.Action _onFailed;
	private bool _responseReceived;
	private float _requestStartTime;
	private float _currentTimeout;

	private void Start()
	{
		InitializeActiveAgent();
	}

	/// <summary>
	/// Resolves the active NeoCortex agent based on current patron faction.
	/// Can be called multiple times safely — will skip if already wired.
	/// </summary>
	public void InitializeActiveAgent()
	{
		if (_activeAgent != null) return; // Already wired

		if (RunManager.Instance == null || RunManager.Instance.State == null)
		{
			Debug.LogWarning("[NeoCortex] RunManager or State is null — cannot initialize agent yet.");
			return;
		}

		CardFaction faction = RunManager.Instance.State.patronFaction;
		_activeAgent = faction switch
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
			Debug.Log($"[NeoCortex] Active agent set for {faction}. Agent GO: {_activeAgent.gameObject.name}");
		}
		else
		{
			Debug.LogWarning($"[NeoCortex] No agent assigned for faction {faction}. Check Inspector references.");
			Debug.LogWarning($"[NeoCortex]   wrathAgent={wrathAgent}, prideAgent={prideAgent}, ruinAgent={ruinAgent}");
		}
	}

	/// <summary>
	/// Requests a dialogue line from the NeoCortex AI.
	/// isPrefetch = true uses the longer prefetch timeout (scene loading, hidden from player).
	/// isPrefetch = false uses the short live timeout (mid-gameplay, must be snappy).
	/// </summary>
	public void RequestLine(string contextMessage, System.Action<string> onSuccess, System.Action onFailed, bool isPrefetch = false)
	{
		// Retry init if agent wasn't ready on Start()
		if (_activeAgent == null)
			InitializeActiveAgent();

		if (_activeAgent == null)
		{
			Debug.LogWarning("[NeoCortex] No active agent — skipping AI request, using fallback.");
			onFailed?.Invoke();
			return;
		}

		_onSuccess = onSuccess;
		_onFailed = onFailed;
		_responseReceived = false;
		_currentTimeout = isPrefetch ? prefetchTimeout : liveTimeout;

		string mode = isPrefetch ? "PREFETCH" : "LIVE";
		Debug.Log($"[NeoCortex] {mode} request to agent ({_activeAgent.gameObject.name}), timeout={_currentTimeout}s...");
		_requestStartTime = Time.realtimeSinceStartup;

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
		// Clean old session
		if (_activeAgent != null)
		{
			_activeAgent.OnChatResponseReceived.RemoveListener(OnChatResponse);
			_activeAgent.OnRequestFailed.RemoveListener(OnRequestFailed);
			_activeAgent.CleanSessionID();
		}

		// Allow re-initialization for the new patron
		_activeAgent = null;
		_responseReceived = false;
	}

	[Header("Response Limits")]
	[Tooltip("Max number of sentences to keep from AI response.")]
	[SerializeField] private int maxSentences = 2;
	[Tooltip("Hard character cap. Response is truncated with '...' if exceeded.")]
	[SerializeField] private int maxCharacters = 120;

	private void OnChatResponse(ChatResponse response)
	{
		float elapsed = Time.realtimeSinceStartup - _requestStartTime;

		if (_responseReceived)
		{
			Debug.LogWarning($"[NeoCortex] Late response received after {elapsed:F2}s (already timed out). Message: \"{response.message}\"");
			return;
		}
		_responseReceived = true;

		if (_timeoutRoutine != null)
			StopCoroutine(_timeoutRoutine);

		string trimmed = TrimResponse(response.message);
		Debug.Log($"[NeoCortex] SUCCESS in {elapsed:F2}s — \"{trimmed}\"");
		_onSuccess?.Invoke(trimmed);
		ClearCallbacks();
	}

	/// <summary>
	/// Clamps AI response to maxSentences and maxCharacters.
	/// Strips quotes, narration markers, and excess whitespace.
	/// </summary>
	private string TrimResponse(string raw)
	{
		if (string.IsNullOrEmpty(raw)) return raw;

		// Strip wrapping quotes the AI sometimes adds
		string s = raw.Trim().Trim('"', '\u201C', '\u201D');

		// Strip narration markers like *action* at the start
		if (s.StartsWith("*"))
		{
			int endAsterisk = s.IndexOf('*', 1);
			if (endAsterisk > 0 && endAsterisk < s.Length - 1)
				s = s.Substring(endAsterisk + 1).TrimStart();
		}

		// Keep only maxSentences sentences
		int sentenceCount = 0;
		int cutIndex = -1;
		for (int i = 0; i < s.Length; i++)
		{
			char c = s[i];
			if (c == '.' || c == '!' || c == '?')
			{
				// Skip ellipsis (...)
				if (c == '.' && i + 1 < s.Length && s[i + 1] == '.')
					continue;

				sentenceCount++;
				if (sentenceCount >= maxSentences)
				{
					cutIndex = i + 1;
					break;
				}
			}
		}

		if (cutIndex > 0 && cutIndex < s.Length)
			s = s.Substring(0, cutIndex).TrimEnd();

		// Hard character cap
		if (s.Length > maxCharacters)
		{
			// Try to break at last space before the cap
			int lastSpace = s.LastIndexOf(' ', maxCharacters);
			if (lastSpace > maxCharacters / 2)
				s = s.Substring(0, lastSpace) + "...";
			else
				s = s.Substring(0, maxCharacters) + "...";
		}

		return s;
	}

	private void OnRequestFailed(string error)
	{
		float elapsed = Time.realtimeSinceStartup - _requestStartTime;

		if (_responseReceived) return;
		_responseReceived = true;

		if (_timeoutRoutine != null)
			StopCoroutine(_timeoutRoutine);

		Debug.LogError($"[NeoCortex] REQUEST FAILED after {elapsed:F2}s — Error: {error}");
		_onFailed?.Invoke();
		ClearCallbacks();
	}

	private IEnumerator TimeoutRoutine()
	{
		yield return new WaitForSeconds(_currentTimeout);

		if (!_responseReceived)
		{
			_responseReceived = true;
			float elapsed = Time.realtimeSinceStartup - _requestStartTime;
			Debug.LogWarning($"[NeoCortex] TIMEOUT after {elapsed:F2}s (limit={_currentTimeout}s) — falling back to scripted.");
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
