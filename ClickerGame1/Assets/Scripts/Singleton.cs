// 2025-11-01 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
// Summary:
// This is a generic Singleton base class that provides a thread-safe implementation of the Singleton pattern.
// It ensures that only one instance of the derived class exists and provides a mechanism to find and activate
// the instance if it is inactive in the scene. This class does not use DontDestroyOnLoad.

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;

    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<T>(FindObjectsInactive.Include);
                if (_instance != null && !_instance.gameObject.activeSelf)
                {
                    _instance.gameObject.SetActive(true);
                }
            }
            return _instance;
        }
    }
}
