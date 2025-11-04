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
    }

    protected override void Start()
    {
        base.Start();
        BindUIEvents();
        RefreshUI();
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
        // The upgrade button should only be interactable if the corresponding item has been purchased
        bool purchased = false;
        if (GameManager.Instance != null && GameManager.Instance.PurchasedGPCItems != null)
        {
            GameManager.Instance.PurchasedGPCItems.TryGetValue(_gpcUpgradeType, out purchased);
        }

        // If a recharge is in progress, button must remain non-interactable
        bool rechargeInProgress = _rechargeCoroutine != null;

        _clickButton.interactable = purchased && !rechargeInProgress;

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
            }
            catch (Exception)
            {
                _editorNextUpgradeCost = "N/A";
            }
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

        StartRechargeVisual(cooldown);

        RefreshUI();
    }

    private void StartRechargeVisual(float duration)
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
        _blurImage.fillAmount = 0f;
        _blurImage.gameObject.SetActive(true);

        // Make sure button is not interactable during recharge
        _clickButton.interactable = false;

        // store duration into editor mirror so inspector shows it
        _editorCooldown = duration;

        _rechargeCoroutine = StartCoroutine(RechargeCoroutine(duration));
        RefreshUI();
    }

    private IEnumerator RechargeCoroutine(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Math.Max(0.0001f, duration));
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
}

