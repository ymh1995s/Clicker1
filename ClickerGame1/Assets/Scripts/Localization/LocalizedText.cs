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
        if (FormatArgs != null && FormatArgs.Length > 0)
        {
            try
            {
                localized = string.Format(localized, FormatArgs);
            }
            catch { }
        }

        if (_tmpText != null)
            _tmpText.text = localized;
        else if (_uiText != null)
            _uiText.text = localized;
    }
}
