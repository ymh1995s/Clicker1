using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Attach to a GameObject with a TextMeshProUGUI or Text component to show localized text
[RequireComponent(typeof(RectTransform))]
public class LocalizedText : MonoBehaviour
{
    [Tooltip("Localization key to look up in CSV files")]
    public string Key;

    [Tooltip("Optional formatting args, e.g. {0} will be replaced by arg0")]
    public string[] FormatArgs;

    private TMP_Text _tmpText;
    private Text _uiText;

    private void Awake()
    {
        _tmpText = GetComponent<TMP_Text>();
        _uiText = GetComponent<Text>();
    }

    private void Start()
    {
        Refresh();
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += Refresh;
        }
    }

    private void OnDestroy()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= Refresh;
    }

    public void Refresh()
    {
        if (string.IsNullOrEmpty(Key)) return;
        if (LocalizationManager.Instance == null) return;
        string localized = LocalizationManager.Instance.GetText(Key);

        // If format args provided, support several placeholder styles:
        // - Standard .NET format: {0}, {1}, ... -> use string.Format
        // - Triple-hash placeholder: ### -> replace sequentially with args
        // - C-style: %d or %s -> replace sequentially
        if (FormatArgs != null && FormatArgs.Length > 0)
        {
            bool formatted = false;
            // Try .NET format first if contains '{'
            if (localized.Contains("{"))
            {
                try
                {
                    localized = string.Format(localized, FormatArgs);
                    formatted = true;
                }
                catch { formatted = false; }
            }

            if (!formatted && localized.Contains("###"))
            {
                string temp = localized;
                for (int i = 0; i < FormatArgs.Length; i++)
                {
                    if (temp.Contains("###"))
                        temp = temp.ReplaceFirst("###", FormatArgs[i]);
                }
                localized = temp;
                formatted = true;
            }

            if (!formatted && (localized.Contains("%d") || localized.Contains("%s")))
            {
                string temp = localized;
                for (int i = 0; i < FormatArgs.Length; i++)
                {
                    if (temp.Contains("%d")) temp = temp.ReplaceFirst("%d", FormatArgs[i]);
                    else if (temp.Contains("%s")) temp = temp.ReplaceFirst("%s", FormatArgs[i]);
                }
                localized = temp;
                formatted = true;
            }

            if (!formatted)
            {
                // fallback: prefix the first arg
                localized = $"{FormatArgs[0]} {localized}";
            }
        }

        if (_tmpText != null)
            _tmpText.text = localized;
        else if (_uiText != null)
            _uiText.text = localized;
    }
}

// Helper extension to replace only the first occurrence
public static class StringExtensions
{
    public static string ReplaceFirst(this string text, string search, string replace)
    {
        int pos = text.IndexOf(search);
        if (pos < 0) return text;
        return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
    }
}
