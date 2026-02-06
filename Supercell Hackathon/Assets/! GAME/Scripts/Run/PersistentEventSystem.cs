using UnityEngine;
using UnityEngine.EventSystems;

public class PersistentEventSystem : MonoBehaviour
{
	void Awake()
	{
		// If there's already an EventSystem (from another scene), destroy this one
		EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
		if (systems.Length > 1)
		{
			for (int i = 0; i < systems.Length; i++)
			{
				if (systems[i].gameObject != gameObject)
				{
					Destroy(systems[i].gameObject);
				}
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
		// Destroy any EventSystem that isn't ours
		EventSystem[] systems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
		for (int i = 0; i < systems.Length; i++)
		{
			if (systems[i].gameObject != gameObject)
			{
				Destroy(systems[i].gameObject);
			}
		}
	}
}
