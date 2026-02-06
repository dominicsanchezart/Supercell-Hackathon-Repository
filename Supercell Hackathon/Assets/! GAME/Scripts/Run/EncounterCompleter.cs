using UnityEngine;

public class EncounterCompleter : MonoBehaviour
{
	// Called by UI button when encounter is done
	public void CompleteEncounter()
	{
		if (RunManager.Instance != null)
		{
			RunManager.Instance.OnEncounterComplete();
		}
		else
		{
			Debug.LogWarning("No RunManager found. Cannot return to map.");
		}
	}

	void Update()
	{
		// Debug shortcut: press Escape to return to map
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			CompleteEncounter();
		}
	}
}
