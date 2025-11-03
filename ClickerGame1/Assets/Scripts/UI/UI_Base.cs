// 2025-11-01 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Summary:
// This is a base class for UI components in Unity. It provides common functionality for UI management,
// including finding child components and game objects, showing and closing popups, and binding UI events.
// It uses TextMeshPro for text rendering and ensures all references are serialized for debugging in the Unity Editor.

public abstract class UI_Base : MonoBehaviour
{
    // Finds a child component of type T by name, considering inactive objects.
    protected T FindChildComponent<T>(string name) where T : Component
    {
        T[] components = GetComponentsInChildren<T>(true);
        foreach (var component in components)
        {
            if (component.gameObject.name == name)
                return component;
        }
        return null;
    }

    // Finds a child GameObject by name, considering inactive objects.
    protected GameObject FindChildGameObject(string name)
    {
        Transform[] transforms = GetComponentsInChildren<Transform>(true);
        foreach (var transform in transforms)
        {
            if (transform.gameObject.name == name)
                return transform.gameObject;
        }
        return null;
    }

    // Shows and activates a popup UI of type T.
    protected T ShowPopup<T>() where T : UI_Base
    {
        T popup = FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (popup != null)
        {
            popup.gameObject.SetActive(true);
        }
        return popup;
    }

    // Closes and deactivates a popup UI of type T.
    protected T ClosePopup<T>() where T : UI_Base
    {
        T popup = FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (popup != null)
        {
            popup.gameObject.SetActive(false);
        }
        return popup;
    }

    // Virtual method to bind UI events.
    protected virtual void BindUIEvents()
    {

    }

    // Virtual method to refresh UI elements.
    protected virtual void RefreshUI()
    {
        // Override this method to implement UI refresh logic.
    }

    // Virtual Unity event methods.
    protected virtual void Awake() { }
    protected virtual void Start()
    {
        BindUIEvents();
        RefreshUI();
    }
    protected virtual void Update() { }

    // Method to handle button click events.
    protected virtual void OnButtonClick(Button button)
    {
        // Override this method to handle button click events.
    }
}
