using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Simple localization manager that loads CSV files from Resources/Localization/<lang>.csv
// CSV format: key, value  (supports quoted values and commas inside quotes)
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    // Current language code (file name without extension) e.g. "en", "ko"
    [SerializeField] private string _defaultLanguage = "en";
    public string CurrentLanguage { get; private set; }

    // Key -> localized string
    private Dictionary<string, string> _localized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public event Action OnLanguageChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Load default language on awake
        SetLanguage(_defaultLanguage);
    }

    // Set the active language by loading a CSV file from Resources/Localization/<lang>.csv
    public bool SetLanguage(string langCode)
    {
        if (string.IsNullOrEmpty(langCode)) return false;

        // Load TextAsset from Resources
        var path = Path.Combine("Localization", langCode);
        TextAsset ta = Resources.Load<TextAsset>(path);
        if (ta == null)
        {
            Debug.LogWarning($"LocalizationManager: Language file not found at Resources/{path}.csv");
            return false;
        }

        try
        {
            ParseCSV(ta.text);
            CurrentLanguage = langCode;
            OnLanguageChanged?.Invoke();
            Debug.Log($"LocalizationManager: Loaded language '{langCode}' ({_localized.Count} entries)");
            return true;
        }
        catch (Exception ex)
        {
            Debug.LogError($"LocalizationManager: Failed to parse localization CSV for '{langCode}': {ex}");
            return false;
        }
    }

    // Get localized text by key. Returns key itself if not found to make missing entries visible.
    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (_localized.TryGetValue(key, out var value)) return value;
        return key; // fallback
    }

    // Parse CSV into the dictionary. Supports headerless CSV: each line = key,value
    private void ParseCSV(string csvText)
    {
        _localized.Clear();
        using (var reader = new StringReader(csvText))
        {
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var fields = SplitCsvLine(line);
                if (fields.Length < 2) continue;
                var key = fields[0].Trim();
                var value = fields[1].Trim();
                if (string.IsNullOrEmpty(key)) continue;
                // If key already exists, overwrite with latest
                _localized[key] = value;
            }
        }
    }

    // Basic CSV line splitter supporting quoted values with commas and escaped quotes
    private string[] SplitCsvLine(string line)
    {
        var fields = new List<string>();
        bool inQuotes = false;
        var current = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '"')
            {
                // if next char is also quote, this is an escaped quote
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++; // skip next
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                fields.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        fields.Add(current.ToString());
        return fields.ToArray();
    }
}
