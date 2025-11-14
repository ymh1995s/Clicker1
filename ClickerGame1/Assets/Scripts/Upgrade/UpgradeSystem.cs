using System;

namespace ClickerGame
{
    // Central configuration for upgrade growth and costs.
    // IMPORTANT: This file (UpgradeConfig) is the single source of truth for upgrade costs,
    // growth parameters and cooldowns. Do NOT duplicate these values elsewhere or serialize
    // them into UI prefabs. UI and game logic should always read these values via
    // GameManager (which wraps UpgradeConfig) or directly from UpgradeConfig if needed.
    // This prevents "dual ownership" (2중 관리) where inspector/serialized values drift
    // apart from gameplay code.
    public static class UpgradeConfig
    {
        // Growth params (6 tiers: Tier1..Tier6)
        public static readonly double[] BaseGPCInc = new double[6] { 1.0, 5.0, 25.0, 120.0, 600.0, 3000.0 };
        public static readonly double[] GpcLevelGrowth = new double[6] { 0.02, 0.03, 0.04, 0.05, 0.06, 0.08 };

        public static readonly double[] BaseGPSInc = new double[6] { 1.0, 2.0, 10.0, 40.0, 200.0, 1000.0 };
        public static readonly double[] GpsLevelGrowth = new double[6] { 0.02, 0.03, 0.04, 0.05, 0.06, 0.08 };

        public const double BaseClickValue = 50.0;
        public const double ITEM_UNLOCK_BONUS = 1.15;

        // Item buy costs (one-time purchases to unlock upgrades) - 6 tiers
        public static readonly long[] ItemBuyCosts = new long[6] { 10_000, 50_000, 150_000, 500_000, 1_500_000, 2_000_000 };

        // Upgrade base costs and multipliers (6 tiers)
        public static readonly double[] UpgradeBaseCost = new double[6] { 100.0, 500.0, 2500.0, 12500.0, 60000.0, 300000.0 };
        public static readonly double[] UpgradeCostMultiplier = new double[6] { 1.15, 1.17, 1.20, 1.22, 1.25, 1.30 };

        // Base cooldowns and cooldown increases per purchase (6 tiers)
        public static readonly int[] BaseCooldownPerTier = new int[6] { 5, 10, 20, 40, 80, 160 };
        public static readonly int[] CooldownIncreasePerTier = new int[6] { 1, 2, 3, 5, 10, 20 };
    }
}
