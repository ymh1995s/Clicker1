using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// UI_Language: attach this to the parent Language GameObject which contains child buttons
// e.g. KoreanBtn and EnglishBtn. This component will disable (make non-interactable)
// the currently-selected language button so it acts like a toggle.
public class UI_Language : MonoBehaviour
{
    [Header("Assign child buttons or leave empty to auto-find by name")]
    [SerializeField] private Button koreanButton;
    [SerializeField] private Button englishButton;

    private Coroutine _waitForSaveCoroutine;

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

        // Start a short wait to ensure SaveManager (if present) is available and apply saved state
        if (_waitForSaveCoroutine != null) StopCoroutine(_waitForSaveCoroutine);
        _waitForSaveCoroutine = StartCoroutine(WaitForSaveThenApply());
    }

    private void OnDisable()
    {
        UnbindButtons();
        try { if (SaveManager.Instance != null) SaveManager.Instance.OnLoaded -= OnSaveLoaded; } catch { }
        if (_waitForSaveCoroutine != null) { StopCoroutine(_waitForSaveCoroutine); _waitForSaveCoroutine = null; }
    }

    private IEnumerator WaitForSaveThenApply()
    {
        // Wait one frame first to allow other Awakes to run
        yield return null;

        // If SaveManager not yet present, wait until it becomes available (bounded)
        float timeout = 3f;
        float elapsed = 0f;
        while (SaveManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        // Subscribe to future save loads
        try
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.OnLoaded -= OnSaveLoaded;
                SaveManager.Instance.OnLoaded += OnSaveLoaded;
            }
        }
        catch { }

        // Apply saved language state now (prefer SaveManager value if available)
        ApplySelectedState();

        _waitForSaveCoroutine = null;
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
        }

        // Persist immediately
        try { if (SaveManager.Instance != null) SaveManager.Instance.SetSavedLanguage("ko"); } catch { }

        ApplySelectedState();
    }

    private void OnEnglishClicked()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.SetLanguage("en");
        }

        // Persist immediately
        try { if (SaveManager.Instance != null) SaveManager.Instance.SetSavedLanguage("en"); } catch { }

        ApplySelectedState();
    }

    private void OnSaveLoaded()
    {
        ApplySelectedState();
    }

    // Public API: force UI to refresh from saved values
    public void RefreshFromSave()
    {
        ApplySelectedState();
    }

    private void ApplySelectedState()
    {
        // Determine current language code, prefer SaveManager saved value at startup
        string lang = null;
        try
        {
            if (SaveManager.Instance != null)
            {
                var saved = SaveManager.Instance.GetSavedLanguage();
                if (!string.IsNullOrEmpty(saved)) lang = saved;
            }
        }
        catch { lang = null; }

        if (string.IsNullOrEmpty(lang))
        {
            lang = LocalizationManager.Instance != null ? LocalizationManager.Instance.CurrentLanguage : "en";
        }

        bool koreanSelected = string.Equals(lang, "ko", System.StringComparison.OrdinalIgnoreCase);
        bool englishSelected = string.Equals(lang, "en", System.StringComparison.OrdinalIgnoreCase);

        if (koreanButton != null) koreanButton.interactable = !koreanSelected;
        if (englishButton != null) englishButton.interactable = !englishSelected;
    }
}
