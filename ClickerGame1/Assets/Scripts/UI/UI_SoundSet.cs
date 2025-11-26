using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// Simple UI component to toggle global sound On/Off using two child buttons named ONBtn and OFFBtn.
// This UI now controls AudioManager volumes (BGM and SFX) instead of changing AudioListener.volume
// so pressing option buttons will no longer cause the global listener volume to be modified unexpectedly.
public class UI_SoundSet : MonoBehaviour
{
    [Header("Assign child buttons or leave empty to auto-find by name")]
    [SerializeField] private Button onButton;
    [SerializeField] private Button offButton;

    private const string PREF_KEY = "MasterSoundOn";

    private Coroutine _waitForSaveCoroutine;

    private void Awake()
    {
        // Auto-find children by name if not assigned
        if (onButton == null)
        {
            var t = transform.Find("ONBtn") ?? transform.Find("OnBtn") ?? transform.Find("ON") ?? transform.Find("On");
            if (t != null) onButton = t.GetComponent<Button>();
        }
        if (offButton == null)
        {
            var t = transform.Find("OFFBtn") ?? transform.Find("OffBtn") ?? transform.Find("OFF") ?? transform.Find("Off");
            if (t != null) offButton = t.GetComponent<Button>();
        }
    }

    private void OnEnable()
    {
        BindButtons();

        if (_waitForSaveCoroutine != null) StopCoroutine(_waitForSaveCoroutine);
        _waitForSaveCoroutine = StartCoroutine(WaitForSaveThenApply());
    }

    private void OnDisable()
    {
        UnbindButtons();
        if (_waitForSaveCoroutine != null) { StopCoroutine(_waitForSaveCoroutine); _waitForSaveCoroutine = null; }
        try { if (SaveManager.Instance != null) SaveManager.Instance.OnLoaded -= OnSaveLoaded; } catch { }
    }

    private IEnumerator WaitForSaveThenApply()
    {
        // Wait one frame to let other Awake/Start run
        yield return null;

        float timeout = 3f;
        float elapsed = 0f;
        // Wait until SaveManager exists and reports loaded, or timeout
        while ((SaveManager.Instance == null || !SaveManager.Instance.IsLoaded) && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (SaveManager.Instance != null)
        {
            // Subscribe to OnLoaded so future loads notify this UI
            SaveManager.Instance.OnLoaded -= OnSaveLoaded;
            SaveManager.Instance.OnLoaded += OnSaveLoaded;
        }

        // Read saved preference: prefer SaveManager saved value if present and loaded, otherwise fall back to PlayerPrefs
        bool on = true;
        if (SaveManager.Instance != null && SaveManager.Instance.IsLoaded)
        {
            on = SaveManager.Instance.GetSavedSoundOn();
        }
        else
        {
            on = PlayerPrefs.GetInt(PREF_KEY, 1) == 1;
        }

        // Do NOT force-apply audio settings when options UI is simply shown. Only update button visuals.
        UpdateButtonVisuals(on);

        _waitForSaveCoroutine = null;
    }

    private void OnSaveLoaded()
    {
        try
        {
            if (SaveManager.Instance != null && SaveManager.Instance.IsLoaded)
            {
                var on = SaveManager.Instance.GetSavedSoundOn();
                // Only update UI visuals; do not force change audio immediately
                UpdateButtonVisuals(on);
            }
        }
        catch { }
    }

    private void BindButtons()
    {
        if (onButton != null)
        {
            onButton.onClick.RemoveListener(OnOnClicked);
            onButton.onClick.AddListener(OnOnClicked);
        }
        if (offButton != null)
        {
            offButton.onClick.RemoveListener(OnOffClicked);
            offButton.onClick.AddListener(OnOffClicked);
        }
    }

    private void UnbindButtons()
    {
        if (onButton != null)
            onButton.onClick.RemoveListener(OnOnClicked);
        if (offButton != null)
            offButton.onClick.RemoveListener(OnOffClicked);
    }

    private void OnOnClicked()
    {
        // User explicitly toggled sound ON - apply audio settings
        ApplySoundSetting(true);
        PlayerPrefs.SetInt(PREF_KEY, 1);
        PlayerPrefs.Save();
        if (SaveManager.Instance != null) SaveManager.Instance.SetSavedSoundOn(true);
    }

    private void OnOffClicked()
    {
        // User explicitly toggled sound OFF - apply audio settings
        ApplySoundSetting(false);
        PlayerPrefs.SetInt(PREF_KEY, 0);
        PlayerPrefs.Save();
        if (SaveManager.Instance != null) SaveManager.Instance.SetSavedSoundOn(false);
    }

    private void ApplySoundSetting(bool enabled)
    {
        // Use AudioManager to control volumes so we don't touch AudioListener.volume which may be used elsewhere.
        if (AudioManager.Instance != null)
        {
            if (enabled)
            {
                // Set to fixed user-requested levels: BGM 0.3, SFX 0.2
                AudioManager.Instance.SetBgmVolume(0.3f);
                AudioManager.Instance.SetSfxVolume(0.2f);
                AudioManager.Instance.SetMasterEnabled(true);
            }
            else
            {
                AudioManager.Instance.SetMasterEnabled(false);
            }
        }
        else
        {
            // Fallback: set AudioListener volume if AudioManager not present
            AudioListener.volume = enabled ? 0.3f : 0f;
        }

        // Update button visuals to reflect current state
        UpdateButtonVisuals(enabled);
    }

    private void UpdateButtonVisuals(bool enabled)
    {
        if (onButton != null) onButton.interactable = !enabled; // disable the active action
        if (offButton != null) offButton.interactable = enabled;
    }

    // Called by SaveManager after load to force immediate sync at startup
    public void RefreshFromSave()
    {
        bool on = true;
        try
        {
            if (SaveManager.Instance != null && SaveManager.Instance.IsLoaded)
                on = SaveManager.Instance.GetSavedSoundOn();
            else
                on = PlayerPrefs.GetInt(PREF_KEY, 1) == 1;
        }
        catch { on = PlayerPrefs.GetInt(PREF_KEY, 1) == 1; }

        // Only update visuals; do not force audio change on refresh unless user toggles
        UpdateButtonVisuals(on);
    }
}
