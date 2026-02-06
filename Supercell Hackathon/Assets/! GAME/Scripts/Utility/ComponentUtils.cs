using UnityEngine;

public static class ComponentUtils
{
    public static T GetComponentOrInChildren<T>(this GameObject obj) where T : Component
    {
        T component = obj.GetComponent<T>();
        if (component == null)
            component = obj.GetComponentInChildren<T>();
        return component;
    }

    public static T GetComponentOrInChildren<T>(this Component comp) where T : Component
    {
        T component = comp.GetComponent<T>();
        if (component == null)
            component = comp.GetComponentInChildren<T>();
        return component;
    }
}