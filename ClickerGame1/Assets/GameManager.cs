// 2025-11-01 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
// Summary:
// This is the GameManager class, implemented as a Singleton. It manages the core game state and data,
// including gold, upgrades, and their levels. It provides public getters and setters for all variables,
// supports querying all data in lists and dictionaries, and uses the observer pattern with event Actions
// to notify changes in the game state to related UI components.


public enum EGPCUpgradeType { A, B, C, D, E, F }
public enum EGPSUpgradeType { A, B, C, D, E, F }

public class GameManager : Singleton<GameManager>
{
    // Current gold amount
    [SerializeField] private int _gold;
    public int Gold
    {
        get => _gold;
        set
        {
            _gold = value;
            OnGoldChanged?.Invoke();
        }
    }
    public event Action OnGoldChanged;

    // Gold per click (GPC)
    [SerializeField] private int _goldPerClick;
    public int GoldPerClick
    {
        get => _goldPerClick;
        set
        {
            _goldPerClick = value;
            OnGoldPerClickChanged?.Invoke();
        }
    }
    public event Action OnGoldPerClickChanged;

    // Gold per second (GPS)
    [SerializeField] private int _goldPerSecond;
    public int GoldPerSecond
    {
        get => _goldPerSecond;
        set
        {
            _goldPerSecond = value;
            OnGoldPerSecondChanged?.Invoke();
        }
    }
    public event Action OnGoldPerSecondChanged;

    // GPC Upgrades and their levels
    [SerializeField] private Dictionary<EGPCUpgradeType, int> _gpcUpgrades = new Dictionary<EGPCUpgradeType, int>
    {
        { EGPCUpgradeType.A, 1 },
        { EGPCUpgradeType.B, 1 },
        { EGPCUpgradeType.C, 1 },
        { EGPCUpgradeType.D, 1 },
        { EGPCUpgradeType.E, 1 },
        { EGPCUpgradeType.F, 1 }
    };
    public Dictionary<EGPCUpgradeType, int> GPCUpgrades => _gpcUpgrades;

    public List<KeyValuePair<EGPCUpgradeType, int>> GetAllGPCUpgrades()
    {
        return new List<KeyValuePair<EGPCUpgradeType, int>>(_gpcUpgrades);
    }

    // GPS Upgrades and their levels
    [SerializeField] private Dictionary<EGPSUpgradeType, int> _gpsUpgrades = new Dictionary<EGPSUpgradeType, int>
    {
        { EGPSUpgradeType.A, 1 },
        { EGPSUpgradeType.B, 1 },
        { EGPSUpgradeType.C, 1 },
        { EGPSUpgradeType.D, 1 },
        { EGPSUpgradeType.E, 1 },
        { EGPSUpgradeType.F, 1 }
    };
    public Dictionary<EGPSUpgradeType, int> GPSUpgrades => _gpsUpgrades;

    public List<KeyValuePair<EGPSUpgradeType, int>> GetAllGPSUpgrades()
    {
        return new List<KeyValuePair<EGPSUpgradeType, int>>(_gpsUpgrades);
    }

    // Virtual Unity event methods
    protected virtual void Awake()
    {
        // Initialization logic
    }

    protected virtual void Start()
    {
        // Start logic
    }

    protected virtual void Update()
    {
        // Update logic
    }
}
