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

    private void IncreaseGPC()
    {
        int upgradeLevel = GameManager.Instance.GPCUpgrades[_gpcUpgradeType];
        int increaseAmount = 0;

        switch (_gpcUpgradeType)
        {
            case EGPCUpgradeType.A:
                increaseAmount = upgradeLevel * 10;
                break;
            case EGPCUpgradeType.B:
                increaseAmount = upgradeLevel * 20;
                break;
            case EGPCUpgradeType.C:
                increaseAmount = upgradeLevel * 30;
                break;
            case EGPCUpgradeType.D:
                increaseAmount = upgradeLevel * 40;
                break;
            case EGPCUpgradeType.E:
                increaseAmount = upgradeLevel * 50;
                break;
        }

        GameManager.Instance.GoldPerClick += increaseAmount;
        GameManager.Instance.GPCUpgrades[_gpcUpgradeType]++;
        RefreshUI();
    }
}
