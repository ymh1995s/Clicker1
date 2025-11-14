// 2025-11-01 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ClickerGame; // use UpgradeConfig from UpgradeSystem
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
    // Default GPC set to 50 to match UpgradeSystem requested initial GPC
    [SerializeField] private int _goldPerClick = 50;
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

    // New: Crystal and CPM (crystals per minute)
    [SerializeField] private int _crystal = 0;
    public int Crystal
    {
        get => _crystal;
        set
        {
            _crystal = value;
            OnCrystalChanged?.Invoke();
        }
    }
    public event Action OnCrystalChanged;

    [SerializeField] private int _cpm = 0; // crystals per minute
    public int CPM
    {
        get => _cpm;
        set
        {
            _cpm = value;
            // update UI if required
        }
    }

    // Character-based modifiers storage
    private Dictionary<string, int> _characterStars = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

    // Aggregated character effect fields
    private int _startGoldFromChars = 0;
    private double _characterGpcBonusPercent = 0.0; // e.g. 0.10 for +10%
    private double _characterGpsBonusPercent = 0.0;
    private int _clearCrystalReward = 0;

    // Public accessors for character-derived values
    public int StartGoldFromCharacters => _startGoldFromChars;
    public double CharacterGpcBonusPercent => _characterGpcBonusPercent;
    public double CharacterGpsBonusPercent => _characterGpsBonusPercent;
    public int ClearCrystalReward => _clearCrystalReward;

    // Call this when a character's star count changes
    public void UpdateCharacterStars(string id, int stars)
    {
        if (string.IsNullOrEmpty(id)) return;
        _characterStars[id] = Mathf.Clamp(stars, 0, 5);
        RecalculateCharacterEffects();
    }

    private void RecalculateCharacterEffects()
    {
        // reset
        _startGoldFromChars = 0;
        _characterGpcBonusPercent = 0.0;
        _characterGpsBonusPercent = 0.0;
        CPM = 0;
        _clearCrystalReward = 0;

        foreach (var kv in _characterStars)
        {
            string id = kv.Key.ToUpperInvariant();
            int s = Mathf.Clamp(kv.Value, 0, 5);
            switch (id)
            {
                case "A":
                case "B":
                    _startGoldFromChars = Math.Max(_startGoldFromChars, GetStartGoldForStars(s));
                    break;
                case "C":
                case "D":
                    _characterGpcBonusPercent += GetGpcPercentForStars(s);
                    break;
                case "E":
                case "F":
                    _characterGpsBonusPercent += GetGpsPercentForStars(s);
                    break;
                case "G":
                case "H":
                    CPM += GetCpmForStars(s);
                    break;
                case "I":
                case "J":
                    _clearCrystalReward += GetClearCrystalForStars(s);
                    break;
            }
        }

        // Recalculate gold stats with character bonuses applied
        RecalculateGoldPerClick();
        RecalculateGoldPerSecond();

        // Optionally log
        Debug.Log($"Character effects applied: StartGold={_startGoldFromChars}, GPC%={_characterGpcBonusPercent:P}, GPS%={_characterGpsBonusPercent:P}, CPM={CPM}, ClearCrystal={_clearCrystalReward}");
    }

    private int GetStartGoldForStars(int s)
    {
        switch (s)
        {
            case 1: return 1000;
            case 2: return 1200;
            case 3: return 1500;
            case 4: return 1700;
            case 5: return 2000;
            default: return 0;
        }
    }

    private double GetGpcPercentForStars(int s)
    {
        switch (s)
        {
            case 1: return 0.10;
            case 2: return 0.12;
            case 3: return 0.15;
            case 4: return 0.17;
            case 5: return 0.20;
            default: return 0.0;
        }
    }

    private double GetGpsPercentForStars(int s)
    {
        // same mapping as GPC
        return GetGpcPercentForStars(s);
    }

    private int GetCpmForStars(int s)
    {
        switch (s)
        {
            case 1: return 5;
            case 2: return 6;
            case 3: return 8;
            case 4: return 9;
            case 5: return 10;
            default: return 0;
        }
    }

    private int GetClearCrystalForStars(int s)
    {
        switch (s)
        {
            case 1: return 100;
            case 2: return 120;
            case 3: return 150;
            case 4: return 170;
            case 5: return 200;
            default: return 0;
        }
    }

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

    // Item buy costs are defined in UpgradeConfig. Provide a thin wrapper so UI calls remain unchanged.
    public int GetItemBuyCost(EGPCUpgradeType tier)
    {
        int idx = (int)tier; // Tier1 -> 0
        if (idx < 0 || idx >= UpgradeConfig.ItemBuyCosts.Length) return 0;
        return (int)UpgradeConfig.ItemBuyCosts[idx];
    }

    // Growth parameters are sourced from UpgradeConfig (UpgradeSystem)

    // Use UpgradeConfig for cooldowns
    private int[] _baseCooldownPerTier => UpgradeConfig.BaseCooldownPerTier;
    private int[] _cooldownIncreasePerTier => UpgradeConfig.CooldownIncreasePerTier;

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
        int baseCd = (idx >= 0 && idx < UpgradeConfig.BaseCooldownPerTier.Length) ? UpgradeConfig.BaseCooldownPerTier[idx] : 0;
        int inc = (idx >= 0 && idx < UpgradeConfig.CooldownIncreasePerTier.Length) ? UpgradeConfig.CooldownIncreasePerTier[idx] : 0;
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
        double total = UpgradeConfig.BaseClickValue; // start with base click value (from UpgradeConfig)

        var enumValues = Enum.GetValues(typeof(EGPCUpgradeType));
        for (int i = 0; i < enumValues.Length; i++)
        {
            var key = (EGPCUpgradeType)i;
            int storedLevel = 1;
            if (_gpcUpgrades.TryGetValue(key, out var val)) storedLevel = val;

            int purchases = Math.Max(0, storedLevel - 1);

            double baseInc = (i < UpgradeConfig.BaseGPCInc.Length) ? UpgradeConfig.BaseGPCInc[i] : 0.0;
            double growth = (i < UpgradeConfig.GpcLevelGrowth.Length) ? UpgradeConfig.GpcLevelGrowth[i] : 0.0;

            // apply unlock multiplier if the one-time item was bought for this tier
            double unlockMult = 1.0;
            if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(key, out var bought) && bought)
                unlockMult = UpgradeConfig.ITEM_UNLOCK_BONUS;

            // Sum over previous purchases: for k = 0..purchases-1, add base * (1 + k * growth) * unlockMult;
            for (int k = 0; k < purchases; k++)
            {
                total += baseInc * (1.0 + k * growth) * unlockMult;
            }
        }

        // Apply character GPC percent bonus
        total = total * (1.0 + _characterGpcBonusPercent);

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

            double baseInc = (i < UpgradeConfig.BaseGPSInc.Length) ? UpgradeConfig.BaseGPSInc[i] : 0.0;
            double growth = (i < UpgradeConfig.GpsLevelGrowth.Length) ? UpgradeConfig.GpsLevelGrowth[i] : 0.0;

            double unlockMult = 1.0;
            if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(key, out var bought) && bought)
                unlockMult = UpgradeConfig.ITEM_UNLOCK_BONUS;

            for (int k = 0; k < purchases; k++)
            {
                total += baseInc * (1.0 + k * growth) * unlockMult;
            }
        }

        // Apply character GPS percent bonus
        total = total * (1.0 + _characterGpsBonusPercent);

        GoldPerSecond = (int)Math.Ceiling(total);
    }

    // Get current stored level for tier (storedLevel mapping: 1 => zero purchases)
    public int GetStoredGpcLevel(EGPCUpgradeType tier)
    {
        if (_gpcUpgrades.TryGetValue(tier, out var val)) return val;
        return 1;
    }

    // Upgrade cost data (matches UpgradeSystem_Test baseCost and costMultiplier, 0-based for Tier1..Tier6)
    // Get cost for next upgrade of given tier
    public long GetUpgradeCost(EGPCUpgradeType tier)
    {
        int idx = (int)tier; // Tier1 -> 0
        int storedLevel = GetStoredGpcLevel(tier);
        int purchases = Math.Max(0, storedLevel - 1); // purchases already made
        double baseCost = (idx >= 0 && idx < UpgradeConfig.UpgradeBaseCost.Length) ? UpgradeConfig.UpgradeBaseCost[idx] : 0.0;
        double mult = (idx >= 0 && idx < UpgradeConfig.UpgradeCostMultiplier.Length) ? UpgradeConfig.UpgradeCostMultiplier[idx] : 1.0;
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

        double baseInc = (idx >= 0 && idx < UpgradeConfig.BaseGPCInc.Length) ? UpgradeConfig.BaseGPCInc[idx] : 0.0;
        double growth = (idx >= 0 && idx < UpgradeConfig.GpcLevelGrowth.Length) ? UpgradeConfig.GpcLevelGrowth[idx] : 0.0;

        double unlockMult = 1.0;
        if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(tier, out var bought) && bought)
            unlockMult = UpgradeConfig.ITEM_UNLOCK_BONUS;

        double delta = baseInc * (1.0 + purchases * growth) * unlockMult;
        return (long)Math.Ceiling(delta);
    }

    public long GetNextGpsIncrease(EGPCUpgradeType tier)
    {
        int idx = (int)tier;
        int storedLevel = GetStoredGpcLevel(tier);
        int purchases = Math.Max(0, storedLevel - 1);

        double baseInc = (idx >= 0 && idx < UpgradeConfig.BaseGPSInc.Length) ? UpgradeConfig.BaseGPSInc[idx] : 0.0;
        double growth = (idx >= 0 && idx < UpgradeConfig.GpsLevelGrowth.Length) ? UpgradeConfig.GpsLevelGrowth[idx] : 0.0;

        double unlockMult = 1.0;
        if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(tier, out var bought) && bought)
            unlockMult = UpgradeConfig.ITEM_UNLOCK_BONUS;

        double delta = baseInc * (1.0 + purchases * growth) * unlockMult;
        return (long)Math.Ceiling(delta);
    }

    // Projected increase in integer GoldPerClick when purchasing the next upgrade for given tier.
    // This accounts for the way GoldPerClick is computed (ceil of total), returning the actual
    // integer delta that will be observed after RecalculateGoldPerClick() runs.
    public int GetProjectedGpcIncrease(EGPCUpgradeType tier)
    {
        // compute exact total before
        double totalBefore = UpgradeConfig.BaseClickValue;
        var enumValues = Enum.GetValues(typeof(EGPCUpgradeType));
        for (int i = 0; i < enumValues.Length; i++)
        {
            var key = (EGPCUpgradeType)i;
            int storedLevel = 1;
            if (_gpcUpgrades.TryGetValue(key, out var val)) storedLevel = val;

            int purchases = Math.Max(0, storedLevel - 1);

            double baseInc = (i < UpgradeConfig.BaseGPCInc.Length) ? UpgradeConfig.BaseGPCInc[i] : 0.0;
            double growth = (i < UpgradeConfig.GpcLevelGrowth.Length) ? UpgradeConfig.GpcLevelGrowth[i] : 0.0;

            double unlockMult = 1.0;
            if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(key, out var bought) && bought)
                unlockMult = UpgradeConfig.ITEM_UNLOCK_BONUS;

            for (int k = 0; k < purchases; k++)
            {
                totalBefore += baseInc * (1.0 + k * growth) * unlockMult;
            }
        }

        // compute the delta for the specific tier (next purchase)
        int idxTier = (int)tier;
        int stored = GetStoredGpcLevel(tier);
        int purchasesTier = Math.Max(0, stored - 1);
        double baseIncTier = (idxTier >= 0 && idxTier < UpgradeConfig.BaseGPCInc.Length) ? UpgradeConfig.BaseGPCInc[idxTier] : 0.0;
        double growthTier = (idxTier >= 0 && idxTier < UpgradeConfig.GpcLevelGrowth.Length) ? UpgradeConfig.GpcLevelGrowth[idxTier] : 0.0;
        double unlockMultTier = 1.0;
        if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(tier, out var boughtTier) && boughtTier)
            unlockMultTier = UpgradeConfig.ITEM_UNLOCK_BONUS;

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

            double baseInc = (i < UpgradeConfig.BaseGPSInc.Length) ? UpgradeConfig.BaseGPSInc[i] : 0.0;
            double growth = (i < UpgradeConfig.GpsLevelGrowth.Length) ? UpgradeConfig.GpsLevelGrowth[i] : 0.0;

            double unlockMult = 1.0;
            if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(key, out var bought) && bought)
                unlockMult = UpgradeConfig.ITEM_UNLOCK_BONUS;

            for (int k = 0; k < purchases; k++)
            {
                totalBefore += baseInc * (1.0 + k * growth) * unlockMult;
            }
        }

        int idxTier = (int)tier;
        int stored = GetStoredGpcLevel(tier);
        int purchasesTier = Math.Max(0, stored - 1);
        double baseIncTier = (idxTier >= 0 && idxTier < UpgradeConfig.BaseGPSInc.Length) ? UpgradeConfig.BaseGPSInc[idxTier] : 0.0;
        double growthTier = (idxTier >= 0 && idxTier < UpgradeConfig.GpsLevelGrowth.Length) ? UpgradeConfig.GpsLevelGrowth[idxTier] : 0.0;
        double unlockMultTier = 1.0;
        if (_purchasedGPCItems != null && _purchasedGPCItems.TryGetValue(tier, out var boughtTier) && boughtTier)
            unlockMultTier = UpgradeConfig.ITEM_UNLOCK_BONUS;

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
        GoldPerClick = Math.Max((int)UpgradeConfig.BaseClickValue, GoldPerClick);

        // Initialize other properties so any subscribers receive the initial values
        Gold = _gold;
        // GoldPerSecond already set by Recalculate

        // Ensure we save on application quitting (covers normal quits)
        try
        {
            Application.quitting += HandleApplicationQuitting;
        }
        catch { }
    }

    protected virtual void Start()
    {
        // Start logic
    }

    protected virtual void Update()
    {
        // Update logic

        // Handle Android back button (maps to KeyCode.Escape) to quit the application.
        // This ensures pressing the hardware/software Back button on Android will exit the game.
        // Keep this logic in GameManager so there's a single place to control app lifecycle behavior.
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Save current game state before quitting. SaveManager.Save writes synchronously to disk.
            try
            {
                SaveManager.Instance?.Save();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"GameManager: Save before quit failed - {ex}");
            }

            Debug.Log("Android back button pressed - quitting application.");
            Application.Quit();
        }
        #else
        // In editor or other platforms, do nothing here (prevents accidental quits during development)
        #endif
    }

    private void HandleApplicationQuitting()
    {
        try
        {
            SaveManager.Instance?.Save();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"GameManager: HandleApplicationQuitting save failed - {ex}");
        }
    }

    protected virtual void OnDisable()
    {
        try
        {
            Application.quitting -= HandleApplicationQuitting;
        }
        catch { }
    }

    protected virtual void OnDestroy()
    {
        // nothing extra here — quitting is handled by Application.quitting
    }
}

