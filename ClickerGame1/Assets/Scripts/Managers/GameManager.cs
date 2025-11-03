// 2025-11-01 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
// Summary:
// This is the GameManager class, implemented as a Singleton. It manages the core game state and data,
// including gold, upgrades, and their levels. It provides public getters and setters for all variables,
// supports querying all data in lists and dictionaries, and uses the observer pattern with event Actions
// to notify changes in the game state to related UI components.

public enum EGPCUpgradeType { Tier1, Tier2, Tier3, Tier4, Tier5, Tier6 }
public enum EGPSUpgradeType { Tier1, Tier2, Tier3, Tier4, Tier5, Tier6 }

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
    [SerializeField] private int _goldPerClick = 1;
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
        { EGPCUpgradeType.Tier1, 1 },
        { EGPCUpgradeType.Tier2, 1 },
        { EGPCUpgradeType.Tier3, 1 },
        { EGPCUpgradeType.Tier4, 1 },
        { EGPCUpgradeType.Tier5, 1 },
        { EGPCUpgradeType.Tier6, 1 }
    };
    public Dictionary<EGPCUpgradeType, int> GPCUpgrades => _gpcUpgrades;

    public List<KeyValuePair<EGPCUpgradeType, int>> GetAllGPCUpgrades()
    {
        return new List<KeyValuePair<EGPCUpgradeType, int>>(_gpcUpgrades);
    }

    // GPS Upgrades and their levels
    [SerializeField] private Dictionary<EGPSUpgradeType, int> _gpsUpgrades = new Dictionary<EGPSUpgradeType, int>
    {
        { EGPSUpgradeType.Tier1, 1 },
        { EGPSUpgradeType.Tier2, 1 },
        { EGPSUpgradeType.Tier3, 1 },
        { EGPSUpgradeType.Tier4, 1 },
        { EGPSUpgradeType.Tier5, 1 },
        { EGPSUpgradeType.Tier6, 1 }
    };
    public Dictionary<EGPSUpgradeType, int> GPSUpgrades => _gpsUpgrades;

    public List<KeyValuePair<EGPSUpgradeType, int>> GetAllGPSUpgrades()
    {
        return new List<KeyValuePair<EGPSUpgradeType, int>>(_gpsUpgrades);
    }

    // One-time purchased GPC items tracking
    [SerializeField] private Dictionary<EGPCUpgradeType, bool> _purchasedGPCItems = new Dictionary<EGPCUpgradeType, bool>
    {
        { EGPCUpgradeType.Tier1, false },
        { EGPCUpgradeType.Tier2, false },
        { EGPCUpgradeType.Tier3, false },
        { EGPCUpgradeType.Tier4, false },
        { EGPCUpgradeType.Tier5, false },
        { EGPCUpgradeType.Tier6, false }
    };
    public Dictionary<EGPCUpgradeType, bool> PurchasedGPCItems => _purchasedGPCItems;

    public List<KeyValuePair<EGPCUpgradeType, bool>> GetAllPurchasedGPCItems()
    {
        return new List<KeyValuePair<EGPCUpgradeType, bool>>(_purchasedGPCItems);
    }

    // --- New: Growth parameters from UpgradeSystem_Test ---
    // GPC tiers: BaseGain and GainRate (index order = Tier1..Tier6)
    private readonly double[] _gpcBaseGains = { 0.1, 1.0, 10.0, 50.0, 200.0, 1000.0 };
    private readonly double[] _gpcGainRates = { 1.10, 1.12, 1.15, 1.17, 1.18, 1.20 };

    // GPS tiers: BaseGain and GainRate (index order = Tier1..Tier6)
    private readonly double[] _gpsBaseGains = { 0.05, 0.5, 3.0, 15.0, 80.0, 400.0 };
    private readonly double[] _gpsGainRates = { 1.09, 1.11, 1.13, 1.15, 1.17, 1.18 };

    // Recalculate GoldPerClick from GPC upgrade levels using UpgradeSystem_Test formula
    public void RecalculateGoldPerClick()
    {
        double total = 1.0; // base click value

        var enumValues = Enum.GetValues(typeof(EGPCUpgradeType));
        for (int i = 0; i < enumValues.Length; i++)
        {
            var key = (EGPCUpgradeType)i;
            int storedLevel = 1;
            if (_gpcUpgrades.TryGetValue(key, out var val)) storedLevel = val;

            // Mapping: storedLevel == 1 => zero purchases (UpgradeTier.Level == 0)
            int purchases = Math.Max(0, storedLevel - 1);

            // Sum gains added on each purchase. In UpgradeSystem_Test, on each Upgrade() the tier's Level increments
            // and the game adds BaseGain * GainRate^(Level) where Level starts at 1 for first purchase.
            // We replicate that by summing k=1..purchases of BaseGain * GainRate^k
            double baseGain = (i < _gpcBaseGains.Length) ? _gpcBaseGains[i] : 0.0;
            double gainRate = (i < _gpcGainRates.Length) ? _gpcGainRates[i] : 1.0;

            for (int k = 1; k <= purchases; k++)
            {
                total += baseGain * Math.Pow(gainRate, k);
            }
        }

        // Use ceiling so small fractional increases are visible immediately
        GoldPerClick = (int)Math.Ceiling(total);
    }

    // Recalculate GoldPerSecond from GPS upgrade levels using UpgradeSystem_Test formula
    public void RecalculateGoldPerSecond()
    {
        double total = 0.0; // base GPS is 0

        var enumValues = Enum.GetValues(typeof(EGPSUpgradeType));
        for (int i = 0; i < enumValues.Length; i++)
        {
            var key = (EGPSUpgradeType)i;
            int storedLevel = 1;
            if (_gpsUpgrades.TryGetValue(key, out var val)) storedLevel = val;

            int purchases = Math.Max(0, storedLevel - 1);
            double baseGain = (i < _gpsBaseGains.Length) ? _gpsBaseGains[i] : 0.0;
            double gainRate = (i < _gpsGainRates.Length) ? _gpsGainRates[i] : 1.0;

            for (int k = 1; k <= purchases; k++)
            {
                total += baseGain * Math.Pow(gainRate, k);
            }
        }

        // Use ceiling for visibility of small gains
        GoldPerSecond = (int)Math.Ceiling(total);
    }

    // Virtual Unity event methods
    protected virtual void Awake()
    {
        // Ensure GPC starts at least at 1 at runtime (inspector could have 0)
        GoldPerClick = Math.Max(1, _goldPerClick);

        // Initialize other properties so any subscribers receive the initial values
        Gold = _gold;
        GoldPerSecond = _goldPerSecond;

        // Recalculate from any loaded upgrade levels
        RecalculateGoldPerClick();
        RecalculateGoldPerSecond();
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

