// 2025-11-01 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
// Summary:
// This is the UI_Upgrade class, implemented as a subclass of UI_Base. It manages the UI interactions
// for upgrading the Gold Per Click (GPC) value based on the selected GPCUpgradeType and its level.
// The script allows specifying the GPCUpgradeType in the Unity Editor and handles the logic for increasing
// the GPC value when the ClickButton is pressed.


public class UI_Upgrade : UI_Base
{
    [SerializeField] private Button _clickButton;

    [SerializeField] private EGPCUpgradeType _gpcUpgradeType;

    // Expose the upgrade type so other scripts can find the matching UI_Upgrade
    public EGPCUpgradeType GPCUpgradeType => _gpcUpgradeType;

    protected override void Awake()
    {
        base.Awake();

        // Automatically connect serialized fields using FindChildGameObject
        _clickButton = FindChildGameObject("ClickButton").GetComponent<Button>();
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
        _clickButton.interactable = purchased;

        var colors = _clickButton.colors;
        colors.normalColor = purchased ? Color.white : Color.grey;
        _clickButton.colors = colors;
    }

    // Public helper to allow other scripts (like UI_ItemBuy) to enable the upgrade button immediately
    public void SetUpgradeInteractable(bool interactable)
    {
        _clickButton.interactable = interactable;
        var colors = _clickButton.colors;
        colors.normalColor = interactable ? Color.white : Color.grey;
        _clickButton.colors = colors;
    }

    private void IncreaseGPC()
    {
        if (GameManager.Instance == null) return;

        // Ensure the GPCUpgrades dictionary has an entry for this tier
        if (!GameManager.Instance.GPCUpgrades.ContainsKey(_gpcUpgradeType))
        {
            GameManager.Instance.GPCUpgrades[_gpcUpgradeType] = 1; // default stored level
        }

        int upgradeLevel = GameManager.Instance.GPCUpgrades[_gpcUpgradeType];

        // Increment stored level (represents number of purchases + 1 initial)
        GameManager.Instance.GPCUpgrades[_gpcUpgradeType] = upgradeLevel + 1;

        // Remember previous displayed GPC to enforce minimum visual change
        int previousGpc = GameManager.Instance.GoldPerClick;

        // Recalculate total GoldPerClick using UpgradeSystem_Test formula
        GameManager.Instance.RecalculateGoldPerClick();

        // Enforce minimum visual increase of 1 if recalculation didn't change integer value
        if (GameManager.Instance.GoldPerClick <= previousGpc)
        {
            GameManager.Instance.GoldPerClick = previousGpc + 1;
        }

        RefreshUI();
    }
}

