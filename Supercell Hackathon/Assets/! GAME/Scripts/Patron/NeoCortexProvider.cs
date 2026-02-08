using System.Collections;
using UnityEngine;

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

	// NOTE: The NeocortexSmartAgent references are commented out until the SDK is installed.
	// Once installed, uncomment these and the SDK calls below.
	//
	// [Header("Agent References (one per patron)")]
	// [SerializeField] private NeocortexSmartAgent wrathAgent;
	// [SerializeField] private NeocortexSmartAgent prideAgent;
	// [SerializeField] private NeocortexSmartAgent ruinAgent;
	//
	// private NeocortexSmartAgent _activeAgent;

	private Coroutine _timeoutRoutine;
	private System.Action<string> _onSuccess;
	private System.Action _onFailed;
	private bool _responseReceived;

	private void Start()
	{
		// Once SDK is installed, uncomment:
		// if (RunManager.Instance != null && RunManager.Instance.State != null)
		// {
		//     _activeAgent = RunManager.Instance.State.patronFaction switch
		//     {
		//         CardFaction.Wrath => wrathAgent,
		//         CardFaction.Pride => prideAgent,
		//         CardFaction.Ruin  => ruinAgent,
		//         _ => null
		//     };
		//
		//     if (_activeAgent != null)
		//     {
		//         _activeAgent.OnChatResponseReceived += OnChatResponse;
		//         _activeAgent.OnRequestFailed += OnRequestFailed;
		//     }
		// }
	}

	/// <summary>
	/// Requests a dialogue line from the NeoCortex AI.
	/// If the response doesn't arrive within the timeout, onFailed is invoked.
	/// </summary>
	public void RequestLine(string contextMessage, System.Action<string> onSuccess, System.Action onFailed)
	{
		// Until SDK is installed, immediately fall back to scripted
		// Remove this early return once SDK agents are wired up
		onFailed?.Invoke();
		return;

		// --- Uncomment once SDK is installed ---
		// if (_activeAgent == null)
		// {
		//     onFailed?.Invoke();
		//     return;
		// }
		//
		// _onSuccess = onSuccess;
		// _onFailed = onFailed;
		// _responseReceived = false;
		//
		// _activeAgent.TextToText(contextMessage);
		// _timeoutRoutine = StartCoroutine(TimeoutRoutine());
	}

	/// <summary>
	/// Builds a context string to send to the NeoCortex AI agent.
	/// </summary>
	public string BuildContext(DialogueTriggerType trigger, string affinityTier,
		CardFaction patronFaction, string description)
	{
		string patronName = "";
		int playerHP = 0;
		int playerMaxHP = 0;

		if (RunManager.Instance != null && RunManager.Instance.State != null)
		{
			var state = RunManager.Instance.State;
			if (state.patronData != null)
				patronName = state.patronData.patronName;
			playerHP = state.currentHP;
			playerMaxHP = state.maxHP;
		}

		return $"Event: {trigger} | Patron: {patronName} ({patronFaction}) | " +
			   $"Affinity: {affinityTier} | Player HP: {playerHP}/{playerMaxHP} | " +
			   $"What happened: {description}";
	}

	// --- SDK Callbacks (uncomment once installed) ---

	// private void OnChatResponse(ChatResponse response)
	// {
	//     if (_responseReceived) return;
	//     _responseReceived = true;
	//
	//     if (_timeoutRoutine != null)
	//         StopCoroutine(_timeoutRoutine);
	//
	//     _onSuccess?.Invoke(response.message);
	//     ClearCallbacks();
	// }

	// private void OnRequestFailed(string error)
	// {
	//     if (_responseReceived) return;
	//     _responseReceived = true;
	//
	//     if (_timeoutRoutine != null)
	//         StopCoroutine(_timeoutRoutine);
	//
	//     Debug.LogWarning($"[NeoCortex] Request failed: {error}");
	//     _onFailed?.Invoke();
	//     ClearCallbacks();
	// }

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
		// Unsubscribe from SDK events
		// if (_activeAgent != null)
		// {
		//     _activeAgent.OnChatResponseReceived -= OnChatResponse;
		//     _activeAgent.OnRequestFailed -= OnRequestFailed;
		// }
	}
}
