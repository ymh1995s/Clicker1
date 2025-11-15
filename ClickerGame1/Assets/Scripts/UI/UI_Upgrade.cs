// 2025-11-01 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
// Summary:
// This is the UI_Upgrade class, implemented as a subclass of UI_Base. It manages the UI interactions
// for upgrading the Gold Per Click (GPC) value based on the selected GPCUpgradeType and its level.
// The script allows specifying the GPCUpgradeType in the Unity Editor and handles the logic for increasing
// the GPC value when the ClickButton is pressed.


public class UI_Upgrade : UI_Base
{
    [SerializeField] private Button _clickButton;

    [SerializeField] private EGPCUpgradeType _gpcUpgradeType;

    // Optional blur image placed under this UI for cooldown visuals. Should be of Image.Type.Filled
    [SerializeField] private Image _blurImage;

    // Duration for blur to fill (seconds) after clicking the upgrade
    [SerializeField] private float _rechargeDuration = 3f;

    // Expose the upgrade type so other scripts can find the matching UI_Upgrade
    public EGPCUpgradeType GPCUpgradeType => _gpcUpgradeType;

    private Coroutine _rechargeCoroutine;

    // Editor-visible mirror fields so designers can see runtime values in the Inspector
    [SerializeField] private int _editorGoldPerClick;
    [SerializeField] private int _editorGoldPerSecond;
    [SerializeField] private float _editorCooldown;
    // Show next upgrade cost as string so it is visible in the Unity Inspector during play
    [SerializeField] private string _editorNextUpgradeCost;

    // New: UI to show when player lacks enough gold for the next upgrade
    [SerializeField] private GameObject _needMoneyImage;
    [SerializeField] private TMP_Text _needMoneyText;

    // New: Text fields under ClickButton to show this-upgrade deltas
    [SerializeField] private TMP_Text _gpcStatText;
    [SerializeField] private TMP_Text _gpsStatText;

    // NOTE: UI should not re-store authoritative values like nextCost or cooldown in serialized fields.
    // Always read from GameManager/UpgradeConfig. This prevents 2중 관리 where Inspector values diverge
    // from gameplay code. The following fields are read-only mirrors for debugging only.

    protected override void Awake()
    {
        base.Awake();

        // Automatically connect serialized fields using FindChildGameObject
        if (_clickButton == null)
        {
            var go = FindChildGameObject("ClickButton");
            if (go != null)
                _clickButton = go.GetComponent<Button>();
        }

        // Try to find BlurImage under this object if not assigned
        if (_blurImage == null)
        {
            var blurGo = FindChildGameObject("BlurImage");
            if (blurGo != null)
                _blurImage = blurGo.GetComponent<Image>();
        }

        // Ensure blur image starts inactive in play
        if (_blurImage != null)
            _blurImage.gameObject.SetActive(false);

        // Try to find NeedMoneyImage and its Text child if not assigned
        if (_needMoneyImage == null)
        {
            var needGo = FindChildGameObject("NeedMoneyImage");
            if (needGo != null)
                _needMoneyImage = needGo;
        }

        if (_needMoneyText == null && _needMoneyImage != null)
        {
            // Prefer a TextMeshPro child named "Text", otherwise fallback to any TMP_Text in children
            var textTransform = _needMoneyImage.transform.Find("Text");
            if (textTransform != null)
                _needMoneyText = textTransform.GetComponent<TMP_Text>();
            if (_needMoneyText == null)
                _needMoneyText = _needMoneyImage.GetComponentInChildren<TMP_Text>(true);
        }

        // Try to find GPCStat / GPSStat under this UI (usually under ClickButton)
        if (_gpcStatText == null)
        {
            var gpcGo = FindChildGameObject("GPCStat");
            if (gpcGo != null)
                _gpcStatText = gpcGo.GetComponent<TMP_Text>();
        }
        if (_gpsStatText == null)
        {
            var gpsGo = FindChildGameObject("GPSStat");
            if (gpsGo != null)
                _gpsStatText = gpsGo.GetComponent<TMP_Text>();
        }

        if (_needMoneyImage != null)
            _needMoneyImage.SetActive(false);
    }

    protected override void Start()
    {
        base.Start();
        BindUIEvents();
        RefreshUI();

        // After start, try to restore cooldown visual if GameManager already has a remaining cooldown
        TryRestoreCooldownVisual();
    }

    void OnEnable()
    {
        // Subscribe to gold changes so NeedMoneyImage updates immediately when player gains/loses gold
        if (GameManager.Instance != null)
            GameManager.Instance.OnGoldChanged += RefreshUI;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldPerClickChanged += RefreshUI;
            GameManager.Instance.OnGoldPerSecondChanged += RefreshUI;
        }

        // Listen for save load completion so we can restore cooldown visuals if load happens after this UI enabled
        if (SaveManager.Instance != null)
            SaveManager.Instance.OnLoaded += OnSaveLoaded;

        // Listen for rebirth complete so we can immediately clear cooldown visuals
        if (GameManager.Instance != null)
            GameManager.Instance.OnRebirthSequenceComplete += OnRebirthSequenceComplete;

        // Attempt immediate restore (covers case where save already loaded earlier)
        TryRestoreCooldownVisual();
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGoldChanged -= RefreshUI;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnGoldPerClickChanged -= RefreshUI;
            GameManager.Instance.OnGoldPerSecondChanged -= RefreshUI;
        }

        if (SaveManager.Instance != null)
            SaveManager.Instance.OnLoaded -= OnSaveLoaded;

        if (GameManager.Instance != null)
            GameManager.Instance.OnRebirthSequenceComplete -= OnRebirthSequenceComplete;
    }

    protected override void BindUIEvents()
    {
        // Bind ClickButton to increase GPC
        if (_clickButton == null)
            return;

        _clickButton.onClick.RemoveAllListeners();
        _clickButton.onClick.AddListener(() => IncreaseGPC());
    }

    protected override void RefreshUI()
    {
        base.RefreshUI();
        if (_clickButton == null) return;

        // The upgrade button should only be interactable if the corresponding item has been purchased
        bool purchased = false;
        if (GameManager.Instance != null && GameManager.Instance.PurchasedGPCItems != null)
        {
            GameManager.Instance.PurchasedGPCItems.TryGetValue(_gpcUpgradeType, out purchased);
        }

        // If GameManager says tier is on cooldown, treat as not available
        bool tierAvailable = true;
        if (GameManager.Instance != null)
            tierAvailable = GameManager.Instance.IsTierAvailable(_gpcUpgradeType);

        _clickButton.interactable = purchased && tierAvailable;

        var colors = _clickButton.colors;
        colors.normalColor = (_clickButton.interactable) ? Color.white : Color.grey;
        _clickButton.colors = colors;

        // Update editor-visible mirrors so the values show in the Inspector during play
        if (GameManager.Instance != null)
        {
            _editorGoldPerClick = GameManager.Instance.GoldPerClick;
            _editorGoldPerSecond = GameManager.Instance.GoldPerSecond;
            _editorCooldown = GameManager.Instance.GetCooldownForTier(_gpcUpgradeType);

            // Optional: reflect GameManager cooldown into serialized recharge duration for clarity
            _rechargeDuration = _editorCooldown;

            // Update next upgrade cost string for editor visibility
            try
            {
                long cost = GameManager.Instance.GetUpgradeCost(_gpcUpgradeType);
                _editorNextUpgradeCost = cost.ToString();

                // Show NeedMoneyImage when player doesn't have enough gold for this upgrade
                bool hasEnough = GameManager.Instance.Gold >= cost;
                if (_needMoneyImage != null)
                    _needMoneyImage.SetActive(!hasEnough);

                if (!hasEnough && _needMoneyText != null)
                {
                    // If LocalizedText attached use its formatting behavior
                    var loc = _needMoneyText.GetComponent<LocalizedText>();
                    if (loc != null)
                    {
                        loc.Key = "NEED_GOLD";
                        loc.FormatArgs = new string[] { cost.ToString("N0") };
                        loc.Refresh();
                    }
                    else
                    {
                        string tmpl = LocalizationManager.Instance != null ? LocalizationManager.Instance.GetText("NEED_GOLD") : null;
                        if (string.IsNullOrEmpty(tmpl) || tmpl == "NEED_GOLD")
                        {
                            _needMoneyText.text = $"{cost:N0} 골드 필요";
                        }
                        else if (tmpl.Contains("{0}"))
                        {
                            _needMoneyText.text = string.Format(tmpl, cost.ToString("N0"));
                        }
                        else if (tmpl.Contains("###"))
                        {
                            _needMoneyText.text = tmpl.Replace("###", cost.ToString("N0"));
                        }
                        else
                        {
                            _needMoneyText.text = $"{cost:N0} {tmpl}";
                        }
                    }
                }

                // Update stat delta texts for this upgrade
                if (_gpcStatText != null)
                {
                    int deltaGpc = GameManager.Instance.GetProjectedGpcIncrease(_gpcUpgradeType);
                    _gpcStatText.text = $"+{deltaGpc:N0}";
                }
                if (_gpsStatText != null)
                {
                    int deltaGps = GameManager.Instance.GetProjectedGpsIncrease(_gpcUpgradeType);
                    _gpsStatText.text = $"+{deltaGps:N0}";
                }
            }
            catch (Exception)
            {
                _editorNextUpgradeCost = "N/A";
                if (_needMoneyImage != null)
                    _needMoneyImage.SetActive(false);

                if (_gpcStatText != null) _gpcStatText.text = "";
                if (_gpsStatText != null) _gpsStatText.text = "";
            }
        }
        else
        {
            if (_needMoneyImage != null)
                _needMoneyImage.SetActive(false);

            if (_gpcStatText != null) _gpcStatText.text = "";
            if (_gpsStatText != null) _gpsStatText.text = "";
        }
    }

    // Public helper to allow other scripts (like UI_ItemBuy) to enable the upgrade button immediately
    public void SetUpgradeInteractable(bool interactable)
    {
        // Do not allow enabling if recharge is running
        if (_rechargeCoroutine != null && interactable)
            return;

        if (_clickButton == null)
            return;

        _clickButton.interactable = interactable;
        var colors = _clickButton.colors;
        colors.normalColor = interactable ? Color.white : Color.grey;
        _clickButton.colors = colors;
    }

    private void IncreaseGPC()
    {
        if (GameManager.Instance == null) return;

        // Use the centralized GameManager method which handles cost checks, cooldown, gold deduction and stat recalculation
        bool upgraded = GameManager.Instance.AttemptUpgrade(_gpcUpgradeType);
        if (!upgraded)
        {
            // Upgrade failed (not enough gold or on cooldown)
            return;
        }

        // Start cooldown visual using GameManager's cooldown duration
        float cooldown = 3f;
        if (GameManager.Instance != null)
            cooldown = GameManager.Instance.GetCooldownForTier(_gpcUpgradeType);

        StartRechargeVisual(cooldown, 0f);

        RefreshUI();
    }

    // Updated StartRechargeVisual to allow starting from a non-zero fill to reflect saved progress
    private void StartRechargeVisual(float duration, float startFill = 0f)
    {
        if (_clickButton == null)
            return;

        // Ensure blur image exists and is of filled type
        if (_blurImage == null)
        {
            // No visual; just disable the button briefly
            _clickButton.interactable = false;
            RefreshUI();
            return;
        }

        // Stop existing coroutine if any
        if (_rechargeCoroutine != null)
            StopCoroutine(_rechargeCoroutine);

        // Disable the ClickButton's visual image while keeping the Button component to control interactability
        var btnImage = _clickButton.GetComponent<Image>();
        if (btnImage != null)
            btnImage.enabled = false;

        _blurImage.type = Image.Type.Filled;
        _blurImage.fillAmount = Mathf.Clamp01(startFill);
        _blurImage.gameObject.SetActive(true);

        // Make sure button is not interactable during recharge
        _clickButton.interactable = false;

        // store duration into editor mirror so inspector shows it
        _editorCooldown = duration;

        _rechargeCoroutine = StartCoroutine(RechargeCoroutine(duration, startFill));
        RefreshUI();
    }

    private IEnumerator RechargeCoroutine(float duration, float startFill = 0f)
    {
        float elapsed = 0f;
        float clampedStart = Mathf.Clamp01(startFill);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = clampedStart + (elapsed / Math.Max(0.0001f, duration)) * (1f - clampedStart);
            t = Mathf.Clamp01(t);
            if (_blurImage != null)
                _blurImage.fillAmount = t;
            yield return null;
        }

        // End of recharge
        if (_blurImage != null)
            _blurImage.gameObject.SetActive(false);

        var btnImage = _clickButton.GetComponent<Image>();
        if (btnImage != null)
            btnImage.enabled = true;

        _rechargeCoroutine = null;

        // Allow button to be interactable again only if the upgrade item has been purchased
        bool purchased = false;
        if (GameManager.Instance != null && GameManager.Instance.PurchasedGPCItems != null)
            GameManager.Instance.PurchasedGPCItems.TryGetValue(_gpcUpgradeType, out purchased);

        _clickButton.interactable = purchased;
        RefreshUI();
    }

    // Try to restore cooldown visual from GameManager state (called on Start, OnEnable and when SaveManager signals load)
    private void TryRestoreCooldownVisual()
    {
        if (GameManager.Instance == null) return;
        if (_rechargeCoroutine != null) return; // already running

        float remaining = 0f;
        try { remaining = GameManager.Instance.GetRemainingCooldown(_gpcUpgradeType); } catch { remaining = 0f; }
        if (remaining <= 0f)
        {
            // nothing to restore
            if (_blurImage != null)
                _blurImage.gameObject.SetActive(false);
            return;
        }

        // We have remaining time; compute how far along the cooldown already is relative to full duration
        float total = 0f;
        try { total = GameManager.Instance.GetCooldownForTier(_gpcUpgradeType); } catch { total = remaining; }
        float startFill = 0f;
        if (total > 0f)
            startFill = Mathf.Clamp01(1f - (remaining / total));

        StartRechargeVisual(remaining, startFill);
    }

    private void OnSaveLoaded()
    {
        // when save finishes loading, ensure UI matches cooldown state
        TryRestoreCooldownVisual();
        RefreshUI();
    }

    // Called when rebirth sequence completes. Clear any running recharge visuals so upgrade becomes immediately usable.
    private void OnRebirthSequenceComplete()
    {
        // Stop recharge coroutine and clear visuals
        if (_rechargeCoroutine != null)
        {
            try { StopCoroutine(_rechargeCoroutine); } catch { }
            _rechargeCoroutine = null;
        }

        if (_blurImage != null)
        {
            _blurImage.gameObject.SetActive(false);
            _blurImage.fillAmount = 0f;
        }

        var btnImage = _clickButton != null ? _clickButton.GetComponent<Image>() : null;
        if (btnImage != null)
            btnImage.enabled = true;

        // After rebirth, tiers should be available immediately; ensure button interactable follows purchase state
        bool purchased = false;
        if (GameManager.Instance != null && GameManager.Instance.PurchasedGPCItems != null)
            GameManager.Instance.PurchasedGPCItems.TryGetValue(_gpcUpgradeType, out purchased);

        if (_clickButton != null)
        {
            _clickButton.interactable = purchased;
            var colors = _clickButton.colors;
            colors.normalColor = (_clickButton.interactable) ? Color.white : Color.grey;
            _clickButton.colors = colors;
        }

        RefreshUI();
    }

    private string FormatLocalizedValue(string template, long value)
    {
        return string.Format(template, value.ToString("N0"));
    }
}

