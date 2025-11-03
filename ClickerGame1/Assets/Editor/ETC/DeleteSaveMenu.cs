#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

// Editor utility to delete the game's persistent save file from the Tools menu
public static class DeleteSaveMenu
{
    private const string SaveFileName = "save_game.json";

    [MenuItem("Tools/Save/Delete Save File...")]
    public static void DeleteSaveFileMenu()
    {
        if (!EditorUtility.DisplayDialog("Delete Save File", "Are you sure you want to delete the save file? This cannot be undone.", "Delete", "Cancel"))
            return;

        string path = Path.Combine(Application.persistentDataPath, SaveFileName);
        bool deleted = false;
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
                deleted = true;
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.DisplayDialog("Delete Save File", "Failed to delete save file:\n" + ex.Message, "OK");
            Debug.LogError("DeleteSaveMenu: Failed to delete save file: " + ex);
            return;
        }

        // Try to reset in-memory GameManager/save state if present
        try
        {
            // If SaveManager exists in the scene, call its DeleteSaveFile to keep behavior consistent
            var saveManager = Object.FindObjectOfType<SaveManager>(true);
            if (saveManager != null)
            {
                saveManager.DeleteSaveFile();
            }
            else
            {
                // Otherwise attempt to reset GameManager via reflection if it exposes an initialization method
                var gm = Object.FindObjectOfType<GameManager>(true);
                if (gm != null)
                {
                    var gmType = gm.GetType();
                    var initMethod = gmType.GetMethod("InitializeGameData", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                    if (initMethod != null)
                        initMethod.Invoke(gm, null);
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("DeleteSaveMenu: Failed to reset in-memory data: " + e);
        }

        if (deleted)
            EditorUtility.DisplayDialog("Delete Save File", "Save file deleted:\n" + path, "OK");
        else
            EditorUtility.DisplayDialog("Delete Save File", "No save file found at:\n" + path, "OK");

        // Refresh the editor
        AssetDatabase.Refresh();
    }
}
#endif