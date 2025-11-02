using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UI_ItemBuy : UI_Base
{
    [SerializeField] private Button _clickButton;
    [SerializeField] private EGPCUpgradeType _gpcUpgradeType;
    [SerializeField] private int _price = 100; // default price

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
    }

    // Use MonoBehaviour OnEnable (do not override) to rebind when enabled
    protected void OnEnable()
    {
        // Re-bind in case the button was assigned later or re-enabled
        BindUIEvents();
        RefreshUI();
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

        // Check if enough gold
        if (GameManager.Instance.Gold < _price)
        {
            // Not enough gold - could play feedback here
            return;
        }

        // Deduct gold and mark purchased
        GameManager.Instance.Gold -= _price;
        GameManager.Instance.PurchasedGPCItems[_gpcUpgradeType] = true;

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
