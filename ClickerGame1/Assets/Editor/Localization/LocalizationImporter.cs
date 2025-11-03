#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

// Editor utility to import CSV files into Assets/Resources/Localization
public static class LocalizationImporter
{
    private const string ResourcesFolder = "Assets/Resources/Localization";

    [MenuItem("Tools/Localization/Import CSV Folder...")]
    public static void ImportCsvFolder()
    {
        string folder = EditorUtility.OpenFolderPanel("Select folder containing CSV files", "", "");
        if (string.IsNullOrEmpty(folder)) return;

        if (!Directory.Exists(ResourcesFolder))
            Directory.CreateDirectory(ResourcesFolder);

        var files = Directory.GetFiles(folder, "*.csv");
        int count = 0;
        foreach (var f in files)
        {
            try
            {
                var dest = Path.Combine(ResourcesFolder, Path.GetFileName(f));
                File.Copy(f, dest, true);
                count++;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"LocalizationImporter: Failed to copy {f}: {ex}");
            }
        }

        AssetDatabase.Refresh();
        Debug.Log($"LocalizationImporter: Imported {count} CSV files to {ResourcesFolder}");
    }
}
#endif
