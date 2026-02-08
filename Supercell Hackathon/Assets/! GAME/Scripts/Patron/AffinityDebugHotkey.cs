using UnityEngine;

/// <summary>
/// Attach to any persistent GameObject (e.g. RunManager) to enable F9 debug hotkey
/// that logs all patron affinity data to the console.
/// </summary>
public class AffinityDebugHotkey : MonoBehaviour
{
	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.F9))
		{
			PatronAffinityTracker.LogAffinityStatus();
		}
	}
}
