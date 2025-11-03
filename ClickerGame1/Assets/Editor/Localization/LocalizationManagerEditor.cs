#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(LocalizationManager))]
public class LocalizationManagerEditor : Editor
{
    SerializedProperty _currentLanguageProp;
    LocalizationManager _manager;

    private void OnEnable()
    {
        _currentLanguageProp = serializedObject.FindProperty("_currentLanguage");
        _manager = (LocalizationManager)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Draw other serialized properties normally (exclude _currentLanguage which we handle with a dropdown)
        DrawPropertiesExcluding(serializedObject, "_currentLanguage");

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Language Selection", EditorStyles.boldLabel);

        // Get available languages from manager
        var options = _manager.GetAvailableLanguages();
        string[] labels = options.Select(o => string.IsNullOrEmpty(o.label) ? o.code : (o.label + " (" + o.code + ")")).ToArray();

        // Determine current index
        int currentIndex = 0;
        for (int i = 0; i < options.Count; i++)
        {
            if (string.Equals(options[i].code, _manager.CurrentLanguage, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(options[i].label, _manager.CurrentLanguage, StringComparison.OrdinalIgnoreCase))
            {
                currentIndex = i;
                break;
            }
        }

        if (labels.Length == 0)
        {
            EditorGUILayout.LabelField("No languages found in Resources/Localization");
        }
        else
        {
            int sel = EditorGUILayout.Popup("Language", currentIndex, labels);
            if (sel != currentIndex)
            {
                // Record undo and set serialized property
                Undo.RecordObject(_manager, "Change Localization Language");
                _currentLanguageProp.stringValue = options[sel].code;
                serializedObject.ApplyModifiedProperties();

                // Apply immediately
                _manager.SetLanguage(options[sel].code);
                EditorUtility.SetDirty(_manager);
            }

            if (GUILayout.Button("Reload Current Language"))
            {
                _manager.SetLanguage(_manager.CurrentLanguage);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
