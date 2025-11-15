using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// UI_CharacterGacha: consume crystals to grant a star to a CharacterColloection frame.
// - Requires 100 crystals per gacha. Does not use gold.
public class UI_CharacterGacha : MonoBehaviour
{
    [SerializeField] private Button gachaButton;

    // NeedMoneyImage for crystals requirement
    [SerializeField] private GameObject _needMoneyImage;
    [SerializeField] private TMP_Text _needMoneyText;

    private Coroutine _crystalCheckCoroutine;
    private Coroutine _ensureInitCoroutine;

    private void Awake()
    {
        // Prefer a Button on the same GameObject (component might be attached directly to a Button)
        if (gachaButton == null)
        {
            gachaButton = GetComponent<Button>();
        }

        // Fallback: look for a child named "ClickButton" under this GameObject
        if (gachaButton == null)
        {
            var btnGo = transform.Find("ClickButton");
            if (btnGo != null) gachaButton = btnGo.GetComponent<Button>();
        }

        // Try to find NeedMoneyImage and its text if not assigned
        if (_needMoneyImage == null)
        {
            var needGo = transform.Find("NeedMoneyImage");
            if (needGo != null)
                _needMoneyImage = needGo.gameObject;
        }
        if (_needMoneyText == null && _needMoneyImage != null)
        {
            var txt = _needMoneyImage.transform.Find("Text");
            if (txt != null) _needMoneyText = txt.GetComponent<TMP_Text>();
            if (_needMoneyText == null && _needMoneyImage != null) _needMoneyText = _needMoneyImage.GetComponentInChildren<TMP_Text>(true);
        }

        // Do not force NeedMoneyImage state here; initialize UI in OnEnable using current GameManager state.
    }

    private void OnEnable()
    {
        BindButton();

        // Start coroutine that ensures GameManager/SaveManager are available before doing initial UI updates
        if (_ensureInitCoroutine != null) StopCoroutine(_ensureInitCoroutine);
        _ensureInitCoroutine = StartCoroutine(EnsureInitialized());

        // start monitoring crystal count
        if (_crystalCheckCoroutine != null) StopCoroutine(_crystalCheckCoroutine);
        _crystalCheckCoroutine = StartCoroutine(CrystalCheckLoop());
    }

    private void OnDisable()
    {
        UnbindButton();
        if (_crystalCheckCoroutine != null)
        {
            StopCoroutine(_crystalCheckCoroutine);
            _crystalCheckCoroutine = null;
        }
        if (_ensureInitCoroutine != null)
        {
            StopCoroutine(_ensureInitCoroutine);
            _ensureInitCoroutine = null;
        }

        // Unsubscribe from SaveManager.OnLoaded
        try { if (SaveManager.Instance != null) SaveManager.Instance.OnLoaded -= OnSaveLoaded; } catch { }
    }

    private IEnumerator EnsureInitialized()
    {
        // Wait until GameManager exists (it should be created in Awake). Timeout after few frames to avoid infinite loop.
        int ticks = 0;
        while (GameManager.Instance == null && ticks < 60)
        {
            ticks++;
            yield return null;
        }

        // Listen for save load to refresh characters if SaveManager loads later
        try { if (SaveManager.Instance != null) SaveManager.Instance.OnLoaded += OnSaveLoaded; } catch { }

        // Do an initial refresh now that managers likely exist
        RefreshNeedMoneyUI();
        UpdateGachaButtonState();

        _ensureInitCoroutine = null;
    }

    private void BindButton()
    {
        if (gachaButton != null)
        {
            gachaButton.onClick.RemoveListener(OnGachaButtonClicked);
            gachaButton.onClick.AddListener(OnGachaButtonClicked);
        }
    }

    private void UnbindButton()
    {
        if (gachaButton != null)
        {
            gachaButton.onClick.RemoveListener(OnGachaButtonClicked);
        }
    }

    // wrapper with correct signature for Button.onClick
    private void OnGachaButtonClicked()
    {
        bool ok = TryGacha();
        Debug.Log(ok ? "UI_CharacterGacha: Gacha purchase SUCCESS" : "UI_CharacterGacha: Gacha purchase FAILED");
    }

    private void OnSaveLoaded()
    {
        // Save data loaded - characters and crystals may have been applied; refresh UI
        RefreshNeedMoneyUI();
        UpdateGachaButtonState();
    }

    // Public API to attempt gacha. Returns true if a star was granted.
    public bool TryGacha()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("UI_CharacterGacha: GameManager not found.");
            Debug.Log("UI_CharacterGacha: Gacha failed - no GameManager instance.");
            return false;
        }

        // Require crystals for gacha
        const int CRYSTAL_COST = 100;
        if (GameManager.Instance.Crystal < CRYSTAL_COST)
        {
            Debug.Log($"UI_CharacterGacha: Not enough crystals. Need {CRYSTAL_COST}, have {GameManager.Instance.Crystal}.");
            UpdateGachaButtonState();
            return false;
        }

        // Find target frames
        var all = GetAllFrames();
        if (all == null || all.Length == 0)
        {
            Debug.LogWarning("UI_CharacterGacha: No CharacterColloection instances found in scene.");
            UpdateGachaButtonState();
            return false;
        }

        // Prefer those with stars < 5
        var eligible = all.Where(c => c != null && c.GetStars() < 5).ToArray();
        if (eligible.Length == 0)
        {
            Debug.Log("UI_CharacterGacha: All characters already max stars.");

            // Disable the gacha button since nothing left to grant
            if (gachaButton != null)
                gachaButton.interactable = false;

            RefreshNeedMoneyUI();
            UpdateGachaButtonState();
            return false;
        }

        // Pick random eligible and add a star
        var chosen = eligible[UnityEngine.Random.Range(0, eligible.Length)];
        string chosenId = chosen != null ? chosen.collectionId : "<unknown>";
        bool added = chosen.AddStar();
        if (!added)
        {
            // Shouldn't happen because we filtered <5, but just in case
            Debug.LogWarning($"UI_CharacterGacha: Failed to add star to chosen CharacterColloection '{chosenId}'.");
            UpdateGachaButtonState();
            return false;
        }

        // Success - consume crystals and log which character gained a star and its new star count
        GameManager.Instance.Crystal -= CRYSTAL_COST;

        int newStars = chosen.GetStars();
        Debug.Log($"UI_CharacterGacha: Gacha SUCCESS. Consumed {CRYSTAL_COST} crystals. Granted 1 star to '{chosenId}' (stars={newStars}).");

        // After successful gacha, refresh crystal-related UI in case Crystal changed elsewhere
        RefreshNeedMoneyUI();
        UpdateGachaButtonState();

        return true;
    }

    // Try to collect frames from common parents or global find
    private CharacterColloection[] GetAllFrames()
    {
        // Try to find by common singular names
        string[] candidates = new[] { "CharacterColloectionFrame", "CharacterCollectionFrame", "CharacterColloection", "CharacterCollection", "CharacterFrame", "Characters", "CharactersTab" };
        foreach (var name in candidates)
        {
            var go = GameObject.Find(name);
            if (go != null)
            {
                var arr = go.GetComponentsInChildren<CharacterColloection>(true);
                if (arr != null && arr.Length > 0) return arr;
            }
        }

        // Fallback to global FindObjectsOfType
        return UnityEngine.Object.FindObjectsOfType<CharacterColloection>(true);
    }

    private IEnumerator CrystalCheckLoop()
    {
        const float interval = 0.5f;
        while (true)
        {
            RefreshNeedMoneyUI();
            yield return new WaitForSeconds(interval);
        }
    }

    private void RefreshNeedMoneyUI()
    {
        if (_needMoneyImage == null || GameManager.Instance == null) return;
        bool hasEnoughCrystals = GameManager.Instance.Crystal >= 100;
        _needMoneyImage.SetActive(!hasEnoughCrystals);
        if (!hasEnoughCrystals && _needMoneyText != null)
        {
            // Prefer LocalizedText if present so formatting uses placeholders like ### or {0}
            var loc = _needMoneyText.GetComponent<LocalizedText>();
            const int CRYSTAL_COST = 100;
            if (loc != null)
            {
                loc.Key = "NEED_CRYSTAL";
                loc.FormatArgs = new string[] { CRYSTAL_COST.ToString("N0") };
                loc.Refresh();
            }
            else
            {
                // Use LocalizationManager template or fallback to numeric prefix
                if (LocalizationManager.Instance != null)
                {
                    var tmpl = LocalizationManager.Instance.GetText("NEED_CRYSTAL");
                    if (string.IsNullOrEmpty(tmpl) || tmpl == "NEED_CRYSTAL")
                        _needMoneyText.text = $"{CRYSTAL_COST:N0} 크리스탈 필요";
                    else if (tmpl.Contains("{0}"))
                        _needMoneyText.text = string.Format(tmpl, CRYSTAL_COST.ToString("N0"));
                    else if (tmpl.Contains("###"))
                        _needMoneyText.text = tmpl.Replace("###", CRYSTAL_COST.ToString("N0"));
                    else
                        _needMoneyText.text = $"{CRYSTAL_COST:N0} {tmpl}";
                }
                else
                {
                    _needMoneyText.text = $"{CRYSTAL_COST:N0} 크리스탈 필요";
                }
            }
        }

        // Update interactable state based on crystals and remaining characters
        UpdateGachaButtonState();
    }

    // Update button interactable state: requires crystals and at least one non-maxed character
    private void UpdateGachaButtonState()
    {
        if (gachaButton == null) return;
        if (GameManager.Instance == null)
        {
            gachaButton.interactable = false;
            return;
        }

        bool hasEnoughCrystals = GameManager.Instance.Crystal >= 100;

        var frames = GetAllFrames();
        bool anyEligible;

        // If frames could not be found yet (likely initialization order), do not assume "no eligible".
        // In that case, allow button based on crystals only and re-evaluate later when frames become available.
        if (frames == null || frames.Length == 0)
        {
            anyEligible = true; // optimistic - will be corrected after SaveManager.OnLoaded or subsequent checks
        }
        else
        {
            anyEligible = frames.Any(c => c != null && c.GetStars() < 5);
        }

        gachaButton.interactable = hasEnoughCrystals && anyEligible;
    }
}
