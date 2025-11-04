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
    // Default GPC set to 10 to match UpgradeSystem_Test initial assumptions
    [SerializeField] private int _goldPerClick = 10;
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

    // Item buy costs (matches UpgradeSystem_Test itemBuyCost for tiers 1..6)
    private readonly int[] _itemBuyCosts = { 1000, 5000, 25000, 100000, 500000, 2000000 };
    public int GetItemBuyCost(EGPCUpgradeType tier)
    {
        int idx = (int)tier; // Tier1 -> 0
        if (idx < 0 || idx >= _itemBuyCosts.Length) return 0;
        return _itemBuyCosts[idx];
    }

    // --- Growth parameters taken from UpgradeSystem_Test (mapped to 0-based arrays) ---
    // base incremental GPC added per purchase for each tier
    private readonly double[] _baseGPCInc = { 1.0, 5.0, 25.0, 120.0, 600.0, 3000.0 };
    // linear growth factor per additional level used in DeltaGPC = base * (1 + L * growth)
    private readonly double[] _gpcLevelGrowth = { 0.02, 0.03, 0.04, 0.05, 0.06, 0.08 };

    // base incremental GPS added per purchase
    private readonly double[] _baseGPSInc = { 1.0, 2.0, 10.0, 40.0, 200.0, 1000.0 };
    private readonly double[] _gpsLevelGrowth = { 0.02, 0.03, 0.04, 0.05, 0.06, 0.08 };

    // base click value to preserve initial GPC (UpgradeSystem_Test uses 10 as starting GPC)
    private const double _baseClickValue = 10.0;

    // Item unlock multiplier when purchased via UI_ItemBuy
    private const double ITEM_UNLOCK_BONUS = 1.15;

    // --- Cooldown per tier (based on UpgradeSystem_Test)
    private readonly int[] _baseCooldownPerTier = { 5, 10, 20, 40, 80, 160 };
    private readonly int[] _cooldownIncreasePerTier = { 1, 2, 3, 5, 10, 20 };

    // Next available time per GPC tier (seconds, Time.time)
    private readonly Dictionary<EGPCUpgradeType, double> _tierNextAvailableTime = new Dictionary<EGPCUpgradeType, double>();

    // Ensure dictionary has entries
    private void EnsureTierCooldownsInitialized()
    {
        foreach (EGPCUpgradeType t in Enum.GetValues(typeof(EGPCUpgradeType)))
        {
            if (!_tierNextAvailableTime.ContainsKey(t))
                _tierNextAvailableTime[t] = 0.0;
        }
    }

    // Returns cooldown duration in seconds for given tier using stored level
    public int GetCooldownForTier(EGPCUpgradeType tier)
    {
        EnsureTierCooldownsInitialized();
        int idx = (int)tier; // Tier1 -> 0
        int storedLevel = 1;
        if (_gpcUpgrades.TryGetValue(tier, out var lv)) storedLevel = lv;
        int purchases = Math.Max(0, storedLevel - 1);
        int baseCd = (idx >= 0 && idx < _baseCooldownPerTier.Length) ? _baseCooldownPerTier[idx] : 0;
        int inc = (idx >= 0 && idx < _cooldownIncreasePerTier.Length) ? _cooldownIncreasePerTier[idx] : 0;
        return baseCd + inc * purchases;
    }

    // Returns whether tier is currently available (not on cooldown)
    public bool IsTierAvailable(EGPCUpgradeType tier)
    {
        EnsureTierCooldownsInitialized();
        if (!_tierNextAvailableTime.TryGetValue(tier, out var t)) return true;
        return Time.time >= t;
    }

    // Use cooldown for tier now: sets next available time to Time.time + cooldown
    public void UseTierCooldown(EGPCUpgradeType tier)
    {
        EnsureTierCooldownsInitialized();
        int cd = GetCooldownForTier(tier);
        _tierNextAvailableTime[tier] = Time.time + cd;
    }

    // Optional: get remaining cooldown seconds (0 if available)
    public float GetRemainingCooldown(EGPCUpgradeType tier)
    {
        EnsureTierCooldownsInitialized();
        if (!_tierNextAvailableTime.TryGetValue(tier, out var t)) return 0f;
        double rem = t - Time.time;
        return (float)Math.Max(0.0, rem);
    }

    // Recalculate GoldPerClick using linear-per-level increment formula from UpgradeSystem_Test
    public void RecalculateGoldPerClick()
    {
        double total = _baseClickValue; // start with base click value (e.g. 10)

        var enumValues = Enum.GetValues(typeof(EGPCUpgradeType));
        for (int i = 0; i < enumValues.Length; i++)
        {
            var key = (EGPCUpgradeType)i;
            int storedLevel = 1;
            if (_gpcUpgrades.TryGetValue(key, out var val)) storedLevel = val;

            int purchases = Math.Max(0, storedLevel - 1);

            double baseInc = (i < _baseGPCInc.Length) ? _baseGPCInc[i] : 0.0;
            double growth = (i < _gpcLevelGrowth.Length) ? _gpcLevelGrowth[i] : 0.0;

            // apply unlock multiplier if the one-time item was bought for this tier
            double unlockMult = 1.0;
            if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(key, out var bought) && bought)
                unlockMult = ITEM_UNLOCK_BONUS;

            // Sum over previous purchases: for k = 0..purchases-1, add base * (1 + k * growth) * unlockMult;
            for (int k = 0; k < purchases; k++)
            {
                total += baseInc * (1.0 + k * growth) * unlockMult;
            }
        }

        GoldPerClick = (int)Math.Ceiling(total);
    }

    // Recalculate GoldPerSecond using the same linear-per-level formula
    public void RecalculateGoldPerSecond()
    {
        double total = 0.0;

        // Use GPC upgrade purchases to determine GPS increases so that GPC upgrades
        // also contribute to GoldPerSecond (matches UpgradeSystem_Test behavior)
        var enumValues = Enum.GetValues(typeof(EGPCUpgradeType));
        for (int i = 0; i < enumValues.Length; i++)
        {
            var key = (EGPCUpgradeType)i;
            int storedLevel = 1;
            if (_gpcUpgrades.TryGetValue(key, out var val)) storedLevel = val;

            int purchases = Math.Max(0, storedLevel - 1);

            double baseInc = (i < _baseGPSInc.Length) ? _baseGPSInc[i] : 0.0;
            double growth = (i < _gpsLevelGrowth.Length) ? _gpsLevelGrowth[i] : 0.0;

            double unlockMult = 1.0;
            if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(key, out var bought) && bought)
                unlockMult = ITEM_UNLOCK_BONUS;

            for (int k = 0; k < purchases; k++)
            {
                total += baseInc * (1.0 + k * growth) * unlockMult;
            }
        }

        GoldPerSecond = (int)Math.Ceiling(total);
    }

    // Upgrade cost data (matches UpgradeSystem_Test baseCost and costMultiplier, 0-based for Tier1..Tier6)
    private readonly double[] _upgradeBaseCost = { 100.0, 500.0, 2500.0, 12500.0, 60000.0, 300000.0 };
    private readonly double[] _upgradeCostMultiplier = { 1.15, 1.17, 1.20, 1.22, 1.25, 1.30 };

    // Get current stored level for tier (storedLevel mapping: 1 => zero purchases)
    public int GetStoredGpcLevel(EGPCUpgradeType tier)
    {
        if (_gpcUpgrades.TryGetValue(tier, out var val)) return val;
        return 1;
    }

    // Get cost for next upgrade of given tier
    public long GetUpgradeCost(EGPCUpgradeType tier)
    {
        int idx = (int)tier; // Tier1 -> 0
        int storedLevel = GetStoredGpcLevel(tier);
        int purchases = Math.Max(0, storedLevel - 1); // purchases already made
        double baseCost = (idx >= 0 && idx < _upgradeBaseCost.Length) ? _upgradeBaseCost[idx] : 0.0;
        double mult = (idx >= 0 && idx < _upgradeCostMultiplier.Length) ? _upgradeCostMultiplier[idx] : 1.0;
        double cost = baseCost * Math.Pow(mult, purchases);
        return (long)Math.Floor(cost);
    }

    // Attempt to perform an upgrade: check cooldown, cost, deduct gold, increment stored level, recalc stats, set cooldown
    public bool AttemptUpgrade(EGPCUpgradeType tier)
    {
        // check availability
        if (!IsTierAvailable(tier)) return false;
        long cost = GetUpgradeCost(tier);
        if (Gold < cost) return false;

        // Deduct gold
        Gold -= (int)cost;

        // Increment stored level (storedLevel starts at 1)
        if (!_gpcUpgrades.ContainsKey(tier)) _gpcUpgrades[tier] = 1;
        _gpcUpgrades[tier] = _gpcUpgrades[tier] + 1;

        // Recalculate GPC/GPS
        RecalculateGoldPerClick();
        RecalculateGoldPerSecond();

        // Apply gameplay cooldown
        UseTierCooldown(tier);

        return true;
    }

    // Public helpers to compute the increase (delta) for the next purchase of a given tier
    public long GetNextGpcIncrease(EGPCUpgradeType tier)
    {
        int idx = (int)tier;
        int storedLevel = GetStoredGpcLevel(tier);
        int purchases = Math.Max(0, storedLevel - 1);

        double baseInc = (idx >= 0 && idx < _baseGPCInc.Length) ? _baseGPCInc[idx] : 0.0;
        double growth = (idx >= 0 && idx < _gpcLevelGrowth.Length) ? _gpcLevelGrowth[idx] : 0.0;

        double unlockMult = 1.0;
        if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(tier, out var bought) && bought)
            unlockMult = ITEM_UNLOCK_BONUS;

        double delta = baseInc * (1.0 + purchases * growth) * unlockMult;
        return (long)Math.Ceiling(delta);
    }

    public long GetNextGpsIncrease(EGPCUpgradeType tier)
    {
        int idx = (int)tier;
        int storedLevel = GetStoredGpcLevel(tier);
        int purchases = Math.Max(0, storedLevel - 1);

        double baseInc = (idx >= 0 && idx < _baseGPSInc.Length) ? _baseGPSInc[idx] : 0.0;
        double growth = (idx >= 0 && idx < _gpsLevelGrowth.Length) ? _gpsLevelGrowth[idx] : 0.0;

        double unlockMult = 1.0;
        if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(tier, out var bought) && bought)
            unlockMult = ITEM_UNLOCK_BONUS;

        double delta = baseInc * (1.0 + purchases * growth) * unlockMult;
        return (long)Math.Ceiling(delta);
    }

    // Projected increase in integer GoldPerClick when purchasing the next upgrade for given tier.
    // This accounts for the way GoldPerClick is computed (ceil of total), returning the actual
    // integer delta that will be observed after RecalculateGoldPerClick() runs.
    public int GetProjectedGpcIncrease(EGPCUpgradeType tier)
    {
        // compute exact total before
        double totalBefore = _baseClickValue;
        var enumValues = Enum.GetValues(typeof(EGPCUpgradeType));
        for (int i = 0; i < enumValues.Length; i++)
        {
            var key = (EGPCUpgradeType)i;
            int storedLevel = 1;
            if (_gpcUpgrades.TryGetValue(key, out var val)) storedLevel = val;

            int purchases = Math.Max(0, storedLevel - 1);

            double baseInc = (i < _baseGPCInc.Length) ? _baseGPCInc[i] : 0.0;
            double growth = (i < _gpcLevelGrowth.Length) ? _gpcLevelGrowth[i] : 0.0;

            double unlockMult = 1.0;
            if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(key, out var bought) && bought)
                unlockMult = ITEM_UNLOCK_BONUS;

            for (int k = 0; k < purchases; k++)
            {
                totalBefore += baseInc * (1.0 + k * growth) * unlockMult;
            }
        }

        // compute the delta for the specific tier (next purchase)
        int idxTier = (int)tier;
        int stored = GetStoredGpcLevel(tier);
        int purchasesTier = Math.Max(0, stored - 1);
        double baseIncTier = (idxTier >= 0 && idxTier < _baseGPCInc.Length) ? _baseGPCInc[idxTier] : 0.0;
        double growthTier = (idxTier >= 0 && idxTier < _gpcLevelGrowth.Length) ? _gpcLevelGrowth[idxTier] : 0.0;
        double unlockMultTier = 1.0;
        if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(tier, out var boughtTier) && boughtTier)
            unlockMultTier = ITEM_UNLOCK_BONUS;

        double deltaExact = baseIncTier * (1.0 + purchasesTier * growthTier) * unlockMultTier;

        double totalAfter = totalBefore + deltaExact;

        int beforeInt = (int)Math.Ceiling(totalBefore);
        int afterInt = (int)Math.Ceiling(totalAfter);
        return Math.Max(0, afterInt - beforeInt);
    }

    public int GetProjectedGpsIncrease(EGPCUpgradeType tier)
    {
        // compute exact total before for GPS
        double totalBefore = 0.0;
        var enumValues = Enum.GetValues(typeof(EGPCUpgradeType));
        for (int i = 0; i < enumValues.Length; i++)
        {
            var key = (EGPCUpgradeType)i;
            int storedLevel = 1;
            if (_gpcUpgrades.TryGetValue(key, out var val)) storedLevel = val;

            int purchases = Math.Max(0, storedLevel - 1);

            double baseInc = (i < _baseGPSInc.Length) ? _baseGPSInc[i] : 0.0;
            double growth = (i < _gpsLevelGrowth.Length) ? _gpsLevelGrowth[i] : 0.0;

            double unlockMult = 1.0;
            if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(key, out var bought) && bought)
                unlockMult = ITEM_UNLOCK_BONUS;

            for (int k = 0; k < purchases; k++)
            {
                totalBefore += baseInc * (1.0 + k * growth) * unlockMult;
            }
        }

        int idxTier = (int)tier;
        int stored = GetStoredGpcLevel(tier);
        int purchasesTier = Math.Max(0, stored - 1);
        double baseIncTier = (idxTier >= 0 && idxTier < _baseGPSInc.Length) ? _baseGPSInc[idxTier] : 0.0;
        double growthTier = (idxTier >= 0 && idxTier < _gpsLevelGrowth.Length) ? _gpsLevelGrowth[idxTier] : 0.0;
        double unlockMultTier = 1.0;
        if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(tier, out var boughtTier) && boughtTier)
            unlockMultTier = ITEM_UNLOCK_BONUS;

        double deltaExact = baseIncTier * (1.0 + purchasesTier * growthTier) * unlockMultTier;

        double totalAfter = totalBefore + deltaExact;

        int beforeInt = (int)Math.Ceiling(totalBefore);
        int afterInt = (int)Math.Ceiling(totalAfter);
        return Math.Max(0, afterInt - beforeInt);
    }

    // Virtual Unity event methods
    protected virtual void Awake()
    {
        // Recalculate from any loaded upgrade levels to ensure GoldPerClick/GPS start from formula
        RecalculateGoldPerClick();
        RecalculateGoldPerSecond();

        // Ensure GPC starts at least at base click runtime (fallback)
        GoldPerClick = Math.Max((int)_baseClickValue, GoldPerClick);

        // Initialize other properties so any subscribers receive the initial values
        Gold = _gold;
        // GoldPerSecond already set by Recalculate
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

