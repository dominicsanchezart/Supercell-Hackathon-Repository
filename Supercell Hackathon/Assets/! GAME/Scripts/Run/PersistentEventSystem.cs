using UnityEngine;
using UnityEngine.EventSystems;

public class PersistentEventSystem : MonoBehaviour
{
	void Awake()
	{
		// If there's already a persistent EventSystem, this is a duplicate — destroy self
		EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
		bool alreadyExists = false;
		for (int i = 0; i < systems.Length; i++)
		{
			if (systems[i].gameObject != gameObject)
			{
				// Check if the other one is already persistent (DontDestroyOnLoad)
				if (systems[i].gameObject.scene.name == "DontDestroyOnLoad")
				{
					alreadyExists = true;
					break;
				}
			}
		}

		if (alreadyExists)
		{
			// A persistent EventSystem already exists — destroy this duplicate
			Destroy(gameObject);
			return;
		}

		// We're the first — persist and clean up any non-persistent duplicates
		DontDestroyOnLoad(gameObject);
		for (int i = 0; i < systems.Length; i++)
		{
			if (systems[i] == null) continue;
			if (systems[i].gameObject == gameObject) continue;

			// If the other EventSystem is on a persistent object (e.g. RunManager),
			// only remove the component — don't destroy the whole GameObject
			if (systems[i].gameObject.scene.name == "DontDestroyOnLoad")
			{
				Destroy(systems[i]);
			}
			else
			{
				Destroy(systems[i].gameObject);
			}
		}
	}

	void OnEnable()
	{
		// When encounter scenes load additively they may bring their own EventSystem.
		// Clean up duplicates whenever a scene loads.
		UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
	}

	void OnDisable()
	{
		UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
	}

	void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
	{
		// Destroy duplicate EventSystems from newly loaded scenes.
		// Only destroy ones that are NOT in DontDestroyOnLoad, so we
		// never accidentally nuke the RunManager or other persistent objects.
		EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
		for (int i = 0; i < systems.Length; i++)
		{
			if (systems[i] == null) continue;
			if (systems[i].gameObject == gameObject) continue;

			// Never destroy persistent objects — just remove the extra EventSystem component
			if (systems[i].gameObject.scene.name == "DontDestroyOnLoad")
			{
				Destroy(systems[i]);
			}
			else
			{
				Destroy(systems[i].gameObject);
			}
		}
	}
}
