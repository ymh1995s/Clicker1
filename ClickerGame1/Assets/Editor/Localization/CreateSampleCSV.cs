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
        string header = "Key,Korean,English";
        string[] samples = new string[]
        {
            // Use ### placeholder so UI formatting replaces it with formatted numbers
            "NEED_GOLD,### 골드 필요,### Need Gold",
            "NEED_CRYSTAL,### 크리스탈 필요,### Need Crystal",
            "MORE_RECORDS,더 많은 기록 남기기,Keep More Records",
            "MORE_KNOWLEDGE,더 많은 지식 쌓기,Gain More Knowledge",
            "MORE_FIREPLACE_TIME,더 많은 불멍 하기,Enjoy More Fireplace Time",
            "MORE_MEALS,더 많은 식사 하기,Have More Meals",
            "MORE_WORKS,더 많은 작업 하기,Do More Work",
            "RECORD_JOURNAL,기록을 남기는 일지,Record Journal",
            "KNOWLEDGE_BOOKSHELF,지식을 쌓는 책장,Bookshelf of Knowledge",
            "RELAXING_FIREPLACE,불멍이 가능한 벽난로,Relaxing Fireplace",
            "SATISFYING_MEAL,허기를 채우는 식사,Satisfying Meal",
            "WORKBENCH,재료는 제작하는 작업대,Workbench for Crafting",
            "MYSTERIOUS_ALCHEMY,신비를 담은 연금술,Mysterious Alchemy",
            // Added key: Character Gacha
            "CHARACTER_GACHA,캐릭터 뽑기,Character Gacha",
            // Added keys: Language, Sound, Exit Game
            "UI.LANGUAGE,언어,Language",
            "UI.SOUND,사운드,Sound",
            "UI.EXIT,게임 종료,Exit Game"
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
