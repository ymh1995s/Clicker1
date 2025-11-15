using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Simple localization manager that loads CSV files from Resources/Localization
// Supports two formats:
// 1) Per-language CSV: Resources/Localization/en.csv with lines: key,value
// 2) Combined CSV template: Resources/Localization/localization_template.csv (or any CSV) with header: Key,Korean,English,Japanese,Chinese
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance { get; private set; }

    // Current language code editable in the Inspector (e.g. "en", "ko")
    [SerializeField]
    private string _currentLanguage = "en";
    public string CurrentLanguage => _currentLanguage;

    // Key -> localized string
    private Dictionary<string, string> _localized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    public event Action OnLanguageChanged;

    // Map short language codes to header column names used in combined CSV
    private static readonly Dictionary<string, string> LangCodeToHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "en", "English" },
        { "ko", "Korean" },
        { "ja", "Japanese" },
        { "jp", "Japanese" },
        { "zh", "Chinese" },
        { "cn", "Chinese" }
    };

    // Reverse mapping from header to code for convenience
    private static readonly Dictionary<string, string> HeaderToLangCode = CreateHeaderToLangCode();

    private static Dictionary<string, string> CreateHeaderToLangCode()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in LangCodeToHeader)
        {
            if (!dict.ContainsKey(kv.Value)) dict[kv.Value] = kv.Key;
        }
        return dict;
    }

    // Simple container describing a language option for UI
    public struct LanguageOption
    {
        public string code; // language code (e.g. en, ko, ja, zh)
        public string label; // display label (English, Korean... or custom header)

        public LanguageOption(string code, string label)
        {
            this.code = code;
            this.label = label;
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Determine language to load: prefer SaveManager saved value if available, otherwise use serialized default
        try
        {
            string langToLoad = _currentLanguage;
            if (SaveManager.Instance != null)
            {
                var saved = SaveManager.Instance.GetSavedLanguage();
                if (!string.IsNullOrEmpty(saved)) langToLoad = saved;
            }

            if (string.IsNullOrWhiteSpace(langToLoad)) langToLoad = "en";
            _currentLanguage = langToLoad;
            SetLanguage(_currentLanguage);
        }
        catch
        {
            // fallback to serialized value
            if (string.IsNullOrWhiteSpace(_currentLanguage)) _currentLanguage = "en";
            try { SetLanguage(_currentLanguage); } catch { }
        }
    }

#if UNITY_EDITOR
    // Called when a value is changed in the Inspector in edit mode.
    private void OnValidate()
    {
        // Avoid attempting to load during domain reloads when asset database isn't ready
        try
        {
            if (!Application.isPlaying && !string.IsNullOrWhiteSpace(_currentLanguage))
            {
                SetLanguage(_currentLanguage);
            }
        }
        catch { }
    }
#endif

    // Expose available languages by scanning Resources/Localization. This is used by runtime UI to build toggles.
    public List<LanguageOption> GetAvailableLanguages()
    {
        var list = new List<LanguageOption>();

        // Load all TextAssets under Resources/Localization
        var assets = Resources.LoadAll<TextAsset>("Localization");
        if (assets != null && assets.Length > 0)
        {
            // First, collect per-language files (named like en, ko, etc.)
            foreach (var a in assets)
            {
                var name = a.name; // filename without extension
                // skip common template names for now
                if (string.Equals(name, "localization_template", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "localization", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(name, "strings", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // treat as per-language file
                string label = name;
                if (LangCodeToHeader.TryGetValue(name, out var mapped)) label = mapped;
                list.Add(new LanguageOption(name, label));
            }

            // Next, if a combined template exists, parse its header and include any headers not already added
            TextAsset template = null;
            foreach (var tname in new[] { "localization_template", "localization", "strings" })
            {
                foreach (var a in assets)
                {
                    if (string.Equals(a.name, tname, StringComparison.OrdinalIgnoreCase))
                    {
                        template = a;
                        break;
                    }
                }
                if (template != null) break;
            }

            if (template != null)
            {
                // parse header
                using (var reader = new StringReader(template.text))
                {
                    var headerLine = reader.ReadLine();
                    if (!string.IsNullOrWhiteSpace(headerLine))
                    {
                        var headers = SplitCsvLine(headerLine);
                        for (int i = 1; i < headers.Length; i++) // skip first 'Key'
                        {
                            var hdr = headers[i].Trim();
                            if (string.IsNullOrEmpty(hdr)) continue;
                            // if not already present by per-lang file
                            bool exists = list.Exists(x => string.Equals(x.label, hdr, StringComparison.OrdinalIgnoreCase) || string.Equals(x.code, hdr, StringComparison.OrdinalIgnoreCase));
                            if (!exists)
                            {
                                // try to map header to code
                                string code = HeaderToLangCode.TryGetValue(hdr, out var c) ? c : hdr;
                                list.Add(new LanguageOption(code, hdr));
                            }
                        }
                    }
                }
            }
        }

        // As a fallback, if nothing found, add default English
        if (list.Count == 0)
        {
            list.Add(new LanguageOption("en", "English"));
        }

        return list;
    }

    // Set the active language by trying per-language CSV first, then combined template
    public bool SetLanguage(string langCode)
    {
        if (string.IsNullOrEmpty(langCode)) return false;

        // Normalize code
        string code = langCode.Trim();

        // 1) Try per-language file: Resources/Localization/<code>.csv
        TextAsset ta = Resources.Load<TextAsset>($"Localization/{code}");
        if (ta != null)
        {
            try
            {
                // Try parse as simple key,value CSV
                ParseCSV(ta.text, null);
                _currentLanguage = code;
                OnLanguageChanged?.Invoke();
                Debug.Log($"LocalizationManager: Loaded per-language file '{code}' ({_localized.Count} entries)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LocalizationManager: Failed to parse per-language CSV '{code}': {ex}. Will attempt combined template.");
            }
        }

        // 2) Try combined template. Look for a template file. We check a few common names.
        string[] templateNames = new[] { "localization_template", "localization", "strings" };
        foreach (var name in templateNames)
        {
            ta = Resources.Load<TextAsset>($"Localization/{name}");
            if (ta == null) continue;

            // Resolve header name from code
            string headerName = ResolveHeaderName(code);
            try
            {
                ParseCSV(ta.text, headerName);
                _currentLanguage = code;
                OnLanguageChanged?.Invoke();
                Debug.Log($"LocalizationManager: Loaded combined template '{name}' for language '{code}' ({_localized.Count} entries)");
                return true;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"LocalizationManager: Failed to parse combined CSV '{name}': {ex}");
            }
        }

        Debug.LogWarning($"LocalizationManager: Language file not found for '{langCode}'");
        return false;
    }

    // Get localized text by key. Returns key itself if not found to make missing entries visible.
    public string GetText(string key)
    {
        if (string.IsNullOrEmpty(key)) return string.Empty;
        if (_localized.TryGetValue(key, out var value)) return value;
        return key; // fallback
    }

    // Parse CSV into the dictionary.
    // If languageHeader is null -> expect simple two-column CSV (key,value).
    // If languageHeader is provided -> expect header row and multiple language columns; will pick the column matching languageHeader.
    private void ParseCSV(string csvText, string languageHeader)
    {
        _localized.Clear();
        using (var reader = new StringReader(csvText))
        {
            string headerLine = reader.ReadLine();
            if (headerLine == null) return;

            string[] headerFields = SplitCsvLine(headerLine);

            int langIndex = -1;
            if (!string.IsNullOrWhiteSpace(languageHeader) && headerFields != null && headerFields.Length > 1)
            {
                for (int i = 0; i < headerFields.Length; i++)
                {
                    if (string.Equals(headerFields[i].Trim(), languageHeader.Trim(), StringComparison.OrdinalIgnoreCase))
                    {
                        langIndex = i;
                        break;
                    }
                }

                // If languageHeader not found in header, treat file as simple two-column CSV and process header as data
                if (langIndex == -1)
                {
                    // fallthrough to simple parsing: process header as a data line
                    ProcessCsvLine(headerLine, 1);
                }
            }
            else if (string.IsNullOrWhiteSpace(languageHeader))
            {
                // If expecting simple CSV, process the header as data as well
                ProcessCsvLine(headerLine, 1);
            }

            // Read rest of lines
            string line;
            while ((line = reader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (langIndex >= 0)
                {
                    // Multi-column: pick key and the language-specific column
                    var fields = SplitCsvLine(line);
                    if (fields.Length <= langIndex) continue;
                    var key = fields.Length > 0 ? fields[0].Trim() : string.Empty;
                    var value = fields[langIndex].Trim();
                    if (string.IsNullOrEmpty(key)) continue;
                    _localized[key] = value;
                }
                else
                {
                    // Simple two-column: key,value
                    ProcessCsvLine(line, 1);
                }
            }
        }
    }

    // Helper to parse a simple CSV line where valueIndex indicates which column is the value (usually 1)
    private void ProcessCsvLine(string line, int valueIndex)
    {
        var fields = SplitCsvLine(line);
        if (fields.Length <= valueIndex) return;
        var key = fields[0].Trim();
        var value = fields[valueIndex].Trim();
        if (string.IsNullOrEmpty(key)) return;
        _localized[key] = value;
    }

    // Resolve language header name from code or return code if not mapped
    private string ResolveHeaderName(string code)
    {
        if (string.IsNullOrEmpty(code)) return code;
        if (LangCodeToHeader.TryGetValue(code, out var header)) return header;
        // If the code already looks like a header name, return it
        return code;
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
