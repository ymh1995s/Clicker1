using UnityEngine;
using UnityEngine.UI;

// LanguageUI: attach this to the parent "Language" GameObject which contains child buttons
// named e.g. "KoreanBtn" and "EnglishBtn" (or assign the Button references in Inspector).
// Clicking buttons will call LocalizationManager.SetLanguage("ko") / SetLanguage("en").
public class LanguageUI : MonoBehaviour
{
    [Header("Assign child buttons or leave empty to auto-find by name")]
    [SerializeField] private Button koreanButton;
    [SerializeField] private Button englishButton;

    private void Awake()
    {
        // Auto-find children if not assigned
        if (koreanButton == null)
        {
            var kb = transform.Find("KoreanBtn") ?? transform.Find("KoreanButton") ?? transform.Find("Korean");
            if (kb != null) koreanButton = kb.GetComponent<Button>();
        }

        if (englishButton == null)
        {
            var eb = transform.Find("EnglishBtn") ?? transform.Find("EnglishButton") ?? transform.Find("English");
            if (eb != null) englishButton = eb.GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();
    }

    private void BindButtons()
    {
        if (koreanButton != null)
        {
            koreanButton.onClick.RemoveListener(OnKoreanClicked);
            koreanButton.onClick.AddListener(OnKoreanClicked);
        }

        if (englishButton != null)
        {
            englishButton.onClick.RemoveListener(OnEnglishClicked);
            englishButton.onClick.AddListener(OnEnglishClicked);
        }
    }

    private void UnbindButtons()
    {
        if (koreanButton != null)
            koreanButton.onClick.RemoveListener(OnKoreanClicked);
        if (englishButton != null)
            englishButton.onClick.RemoveListener(OnEnglishClicked);
    }

    private void OnKoreanClicked()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage("ko");
            Debug.Log("LanguageUI: Set language to Korean (ko)");
        }
        else
        {
            Debug.LogWarning("LanguageUI: LocalizationManager instance not found.");
        }
    }

    private void OnEnglishClicked()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage("en");
            Debug.Log("LanguageUI: Set language to English (en)");
        }
        else
        {
            Debug.LogWarning("LanguageUI: LocalizationManager instance not found.");
        }
    }
}
