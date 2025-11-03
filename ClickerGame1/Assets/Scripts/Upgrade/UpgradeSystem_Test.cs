// ======================================
// Clicker Upgrade System (1-hour cycle version)
// ======================================
using System;
using System.Collections.Generic;

namespace UpgradeSystemSample
{
    public class ClickerGame
    {
        public double Gold = 0;
        public double GPC = 1;   // Gold Per Click (초기 1골드)
        public double GPS = 0;   // Gold Per Second (초기 0골드)
        public double PlayTime = 0; // 분 단위 플레이 시간

        // GPC/GPS 업그레이드 티어별 정보
        private List<UpgradeTier> GpcTiers;
        private List<UpgradeTier> GpsTiers;

        public ClickerGame()
        {
            // 티어 구성: 기본비용, 증가율, 기본효과, 성장률
            GpcTiers = new List<UpgradeTier>()
        {
            new UpgradeTier("GPC T1", 10, 1.15, 0.1, 1.10),
            new UpgradeTier("GPC T2", 1_000, 1.18, 1, 1.12),
            new UpgradeTier("GPC T3", 100_000, 1.22, 10, 1.15),
            new UpgradeTier("GPC T4", 1_000_000, 1.25, 50, 1.17),
            new UpgradeTier("GPC T5", 10_000_000, 1.28, 200, 1.18),
            new UpgradeTier("GPC T6", 100_000_000, 1.30, 1_000, 1.20)
        };

            GpsTiers = new List<UpgradeTier>()
        {
            new UpgradeTier("GPS T1", 50, 1.14, 0.05, 1.09),
            new UpgradeTier("GPS T2", 5_000, 1.17, 0.5, 1.11),
            new UpgradeTier("GPS T3", 500_000, 1.20, 3, 1.13),
            new UpgradeTier("GPS T4", 2_000_000, 1.23, 15, 1.15),
            new UpgradeTier("GPS T5", 20_000_000, 1.26, 80, 1.17),
            new UpgradeTier("GPS T6", 100_000_000, 1.30, 400, 1.18)
        };
        }

        public void Update(double deltaTime)
        {
            // 초당 골드 증가
            Gold += GPS * deltaTime;

            // 1분 = 60초
            PlayTime += deltaTime / 60.0;
        }

        public void Click()
        {
            Gold += GPC;
        }

        public void TryUpgrade(bool isClickUpgrade)
        {
            var tiers = isClickUpgrade ? GpcTiers : GpsTiers;

            foreach (var tier in tiers)
            {
                double cost = tier.GetCurrentCost();
                if (Gold >= cost)
                {
                    Gold -= cost;
                    tier.Upgrade();
                    if (isClickUpgrade)
                        GPC += tier.GetCurrentGain();
                    else
                        GPS += tier.GetCurrentGain();
                    break;
                }
            }
        }

        public void Simulate(double totalSeconds)
        {
            double time = 0;
            double upgradeTimer = 0;
            double upgradeInterval = 180; // 초단위, 초반 3분에 1회 업그레이드

            while (time < totalSeconds)
            {
                Update(1.0);
                time += 1.0;
                upgradeTimer += 1.0;

                // 업그레이드 주기 증가 (점점 느려짐)
                if (upgradeTimer >= upgradeInterval)
                {
                    upgradeTimer = 0;
                    // 클릭/자동 골드 업그레이드 번갈아 수행
                    bool upgradeClick = (time % 600 < 300);
                    TryUpgrade(upgradeClick);

                    // 플레이 시간이 길어질수록 업그레이드 주기 증가
                    upgradeInterval *= 1.10; // 10%씩 길어짐
                    upgradeInterval = Math.Min(upgradeInterval, 900); // 최대 15분 간격 제한
                }
            }
        }
    }

    public class UpgradeTier
    {
        public string Name;
        public double BaseCost;
        public double CostRate;
        public double BaseGain;
        public double GainRate;
        public int Level = 0;

        public UpgradeTier(string name, double baseCost, double costRate, double baseGain, double gainRate)
        {
            Name = name;
            BaseCost = baseCost;
            CostRate = costRate;
            BaseGain = baseGain;
            GainRate = gainRate;
        }

        public double GetCurrentCost()
        {
            return BaseCost * Math.Pow(CostRate, Level);
        }

        public double GetCurrentGain()
        {
            return BaseGain * Math.Pow(GainRate, Level);
        }

        public void Upgrade()
        {
            Level++;
        }
    }

}