#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

// Editor utility to create a sample localization CSV template directly into Assets/Resources/Localization
public static class LocalizationSampleCreator
{
    [MenuItem("Tools/Localization/Create Sample CSV Template")]
    public static void CreateSampleCsv()
    {
        string resourcesFolder = "Assets/Resources/Localization";
        if (!Directory.Exists(resourcesFolder))
            Directory.CreateDirectory(resourcesFolder);

        string fileName = "localization_template.csv";
        string path = Path.Combine(resourcesFolder, fileName);

        // Header and sample rows. Values are UTF-8 with BOM to avoid encoding issues in some editors.
        string header = "Key,Korean,English,Japanese,Chinese";
        string[] samples = new string[]
        {
            "GAME_TITLE,Å¬¸®Ä¿ °ÔÀÓ,Clicker Game,«¯«ê«Ã«¯«²?«à,ïÃ?Û¯öÇêý?",
            "UI.Gold,°ñµå,Gold,«´?«ë«É,ÑÑ?",
            "UI.GoldFormat,{0} °ñµå,{0} Gold,{0} «´?«ë«É,{0} ÑÑ?",
            "UI.GPC,Å¬¸¯´ç È¹µæ,Gold per Click,«¯«ê«Ã«¯ª¢ª¿ªêªÎüòÔð,Øßó­ïÃ??ÔðîÜÑÑ?",
            "UI.GPS,ÃÊ´ç È¹µæ,Gold per Second,õ©ª´ªÈªÎüòÔð,Øßõ©?ÔðîÜÑÑ?",
            "UI.BestGold,ÃÖ°í °ñµå,Best Gold,õÌÍÔ«´?«ë«É,õÌÍÔÑÑ?",
            "UI.Level,·¹º§,Level,«ì«Ù«ë,Ôõ?",
            "UI.Clicks,Å¬¸¯ ¼ö,Clicks,«¯«ê«Ã«¯?,ïÃ?ó­?",
            "BUTTON.Buy,±¸¸Å,Buy,ÏÅìý,??",
            "TOOLTIP.UpgradeLevel,¾÷±×·¹ÀÌµå ·¹º§,Upgrade Level,«¢«Ã«×«°«ì?«É«ì«Ù«ë,ã®?Ôõ?"
        };

        try
        {
            // Combine header and sample rows
            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var sw = new StreamWriter(fs, new System.Text.UTF8Encoding(true)))
            {
                sw.WriteLine(header);
                foreach (var s in samples) sw.WriteLine(s);
            }

            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Sample CSV Created", "Created localization template at:\n" + path, "OK");
            var ta = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
            if (ta != null) Selection.activeObject = ta;
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"LocalizationSampleCreator: Failed to create sample CSV: {ex}");
            EditorUtility.DisplayDialog("Error", "Failed to create sample CSV:\n" + ex.Message, "OK");
        }
    }
}
#endif
