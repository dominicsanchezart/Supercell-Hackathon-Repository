using UnityEngine;

/// <summary>
/// Generic singleton base class for MonoBehaviours.
/// Persists across scene loads and destroys duplicates automatically.
/// 
/// Usage: public class MyManager : Singleton&lt;MyManager&gt; { }
/// Access: MyManager.Instance
/// </summary>
public class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this as T;
        DontDestroyOnLoad(gameObject);
    }

    protected virtual void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}