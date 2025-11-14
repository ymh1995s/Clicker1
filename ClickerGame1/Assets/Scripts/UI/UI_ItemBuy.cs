using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ItemBuy : UI_Base
{
    [SerializeField] private Button _clickButton;
    [SerializeField] private EGPCUpgradeType _gpcUpgradeType;
    // NOTE: _price should NOT be treated as authoritative. The authoritative item buy
    // costs are defined in UpgradeConfig (UpgradeSystem) and exposed via GameManager.GetItemBuyCost.
    // Do not set this value in the Inspector — it is kept here only for UI convenience and
    // will be overwritten at runtime from GameManager. This avoids "dual ownership" (2중 관리).
    private int _price; // default price is loaded from GameManager in Awake/RefreshUI

    // New: Need money UI under this buy button
    [SerializeField] private GameObject _needMoneyImage;
    [SerializeField] private TMP_Text _needMoneyText;

    protected override void Awake()
    {
        base.Awake();

        // Try to auto-assign the click button if not set in inspector
        if (_clickButton == null)
        {
            var go = FindChildGameObject("ClickButton");
            if (go != null)
                _clickButton = go.GetComponent<Button>();
        }

        if (_clickButton == null)
            Debug.LogWarning($"[UI_ItemBuy] ClickButton not found for {gameObject.name}. Button clicks won't work.", this);

        // Try to find NeedMoneyImage and its Text child if not assigned
        if (_needMoneyImage == null)
        {
            var needGo = FindChildGameObject("NeedMoneyImage");
            if (needGo != null)
                _needMoneyImage = needGo;
        }

        if (_needMoneyText == null && _needMoneyImage != null)
        {
            var textTransform = _needMoneyImage.transform.Find("Text");
            if (textTransform != null)
                _needMoneyText = textTransform.GetComponent<TMP_Text>();
            if (_needMoneyText == null)
                _needMoneyText = _needMoneyImage.GetComponentInChildren<TMP_Text>(true);
        }

        if (_needMoneyImage != null)
            _needMoneyImage.SetActive(false);

        // If GameManager exists, initialize price from its item cost table
        if (GameManager.Instance != null)
        {
            try
            {
                _price = GameManager.Instance.GetItemBuyCost(_gpcUpgradeType);
            }
            catch (Exception)
            {
                // keep default if any error
                _price = 0;
            }
        }
    }

    // Use MonoBehaviour OnEnable (do not override) to rebind when enabled
    protected void OnEnable()
    {
        // Re-bind in case the button was assigned later or re-enabled
        BindUIEvents();
        RefreshUI();

        // Subscribe to gold changes so NeedMoneyImage updates immediately when player gains/loses gold
        if (GameManager.Instance != null)
            GameManager.Instance.OnGoldChanged += RefreshUI;
    }

    void OnDisable()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGoldChanged -= RefreshUI;
    }

    protected override void Start()
    {
        base.Start();
        // Ensure binding and UI state
        BindUIEvents();
        RefreshUI();
    }

    protected override void BindUIEvents()
    {
        if (_clickButton == null)
            return;

        _clickButton.onClick.RemoveAllListeners();
        _clickButton.onClick.AddListener(() => TryBuyItem());
    }

    protected override void RefreshUI()
    {
        base.RefreshUI();
        if (_clickButton == null)
            return;

        bool purchased = false;
        if (GameManager.Instance != null && GameManager.Instance.PurchasedGPCItems != null)
            purchased = GameManager.Instance.PurchasedGPCItems[_gpcUpgradeType];

        // Ensure price reflects GameManager's configured price
        if (GameManager.Instance != null)
        {
            _price = GameManager.Instance.GetItemBuyCost(_gpcUpgradeType);
        }

        // Show NeedMoneyImage if player doesn't have enough gold for this item
        if (_needMoneyImage != null)
        {
            bool hasEnough = true;
            if (GameManager.Instance != null)
                hasEnough = GameManager.Instance.Gold >= _price;

            _needMoneyImage.SetActive(!hasEnough && !purchased);

            if (!hasEnough && _needMoneyText != null)
            {
                _needMoneyText.text = $"{_price:N0} 골드 필요";
            }
        }

        // Disable and gray out if already purchased
        _clickButton.interactable = !purchased;
        var colors = _clickButton.colors;
        colors.normalColor = purchased ? Color.grey : Color.white;
        _clickButton.colors = colors;
    }

    private void TryBuyItem()
    {
        // Safety: ensure GameManager and button exist
        if (_clickButton == null)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager instance not found.");
            return;
        }

        // Check already purchased
        if (GameManager.Instance.PurchasedGPCItems[_gpcUpgradeType])
            return;

        // Use authoritative price from GameManager
        int priceToUse = GameManager.Instance.GetItemBuyCost(_gpcUpgradeType);

        // Check if enough gold
        if (GameManager.Instance.Gold < priceToUse)
        {
            // Not enough gold - could play feedback here
            return;
        }

        // To avoid UI flicker where OnGoldChanged triggers subscribers before GPC/GPS
        // recalculation can occur, update purchase state and recalculate first, then
        // deduct gold so listeners receive the recalculated GPC/GPS together with the
        // updated gold value.

        // Mark purchased (one-time unlock)
        GameManager.Instance.PurchasedGPCItems[_gpcUpgradeType] = true;

        // Recalculate derived stats so purchase unlocks apply (GPC and GPS) BEFORE changing gold
        GameManager.Instance.RecalculateGoldPerClick();
        GameManager.Instance.RecalculateGoldPerSecond();

        // Now deduct gold (this will invoke OnGoldChanged after recalculations)
        GameManager.Instance.Gold -= priceToUse;

        // Instead of directly increasing GPC here, enable the corresponding UI_Upgrade button
        var upgrades = FindObjectsOfType<UI_Upgrade>();
        foreach (var up in upgrades)
        {
            if (up.GPCUpgradeType == _gpcUpgradeType)
            {
                up.SetUpgradeInteractable(true);
                break;
            }
        }

        RefreshUI();
    }
}
