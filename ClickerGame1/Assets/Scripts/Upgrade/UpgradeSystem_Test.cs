using System;
using UnityEngine;
// TODO 11-10 이 스크립트는 캐릭터 특성까지 고려하여 완전히 재편성 되어야함.

// UpgradeSystem_Test: simulation helper for upgrade formulas and simple auto-buy strategy.
// Converted from top-level script into a MonoBehaviour so it compiles in Unity.
public class UpgradeSystem_Test : MonoBehaviour
{
    // === 상수 ===
    private const long TARGET_GOLD = 10_000_000L;
    private const int PLAY_TIME_SEC = 1800; // 30 min
    private const double CLICK_PER_SEC = 4.0; // 가정: 초당 4 클릭

    // 티어 수
    private const int TIERS = 6;

    // 하드코드 배열(1-based 사용 편의상 index 0 더미)
    private readonly long[] baseCost = { 0, 100, 500, 2500, 12500, 60000, 300000 };
    private readonly double[] costMultiplier = { 0, 1.15, 1.17, 1.20, 1.22, 1.25, 1.30 };
    private readonly long[] baseGPCInc = { 0, 1, 5, 25, 120, 600, 3000 };
    private readonly long[] baseGPSInc = { 0, 1, 2, 10, 40, 200, 1000 };
    private readonly double[] growthFactorTier = { 0, 0.02, 0.03, 0.04, 0.05, 0.06, 0.08 };
    private readonly double[] unlockMultiplier = { 0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0 }; // UI_ItemBuy로 1.15로 바뀔 수 있음
    private readonly int[] baseCooldown = { 0, 5, 10, 20, 40, 80, 160 };
    private readonly int[] cooldownIncrease = { 0, 1, 2, 3, 5, 10, 20 };

    // UI_ItemBuy 비용 (tier 1..6 매칭)
    private readonly long[] itemBuyCost = { 1000, 5000, 25000, 100000, 500000, 2000000 };
    private const double ITEM_UNLOCK_BONUS = 1.15; // 구매 시 해당 티어 효과에 곱해지는 보너스

    // === 게임 상태 ===
    private long gold = 0;
    private double accumulatedTime = 0.0;

    private long GPC = 10; // 클릭당 골드 (초기값)
    private long GPS = 0;  // 초당 자동 골드 (초기값)

    // 티어별 레벨, 마지막 구매 시각(쿨다운 체크), 언락(아이템 구매)
    private int[] tierLevel = new int[TIERS + 1]; // 0..6
    private double[] tierCooldownEnd = new double[TIERS + 1]; // 게임 시간으로 비교
    private bool[] tierUnlocked = new bool[TIERS + 1]; // UI_ItemBuy를 통해 언락
    private bool[] itemBought = new bool[TIERS + 1]; // 1회 구매(아이템)

    private void Awake()
    {
        // Initialize arrays
        for (int i = 0; i <= TIERS; i++)
        {
            tierLevel[i] = 0;
            tierCooldownEnd[i] = 0.0;
            tierUnlocked[i] = false;
            itemBought[i] = false;
        }
    }

    // === 헬퍼 함수 ===
    private long Cost(int t, int L)
    {
        double c = baseCost[t] * Math.Pow(costMultiplier[t], L);
        return (long)Math.Floor(c);
    }
    private long DeltaGPC(int t, int L)
    {
        double d = baseGPCInc[t] * (1.0 + L * growthFactorTier[t]);
        // apply unlock multiplier from array (note: array is readonly reference but contents mutable)
        d *= unlockMultiplier[t];
        return (long)Math.Floor(d);
    }
    private long DeltaGPS(int t, int L)
    {
        double d = baseGPSInc[t] * (1.0 + L * growthFactorTier[t]);
        d *= unlockMultiplier[t];
        long val = (long)Math.Floor(d);
        return Math.Max(1, val); // 최소 1 보장
    }
    private int CooldownNext(int t, int L)
    {
        return baseCooldown[t] + cooldownIncrease[t] * L;
    }

    // === 아이템 구매(언락) ===
    public bool BuyItemUnlock(int tierIndex, double gameTime)
    {
        if (tierIndex < 1 || tierIndex > TIERS) return false;
        if (itemBought[tierIndex]) return false;
        long cost = itemBuyCost[tierIndex - 1]; // 매칭: itemBuyCost[0] -> tier 1
        if (gold < cost) return false;
        gold -= cost;
        itemBought[tierIndex] = true;
        tierUnlocked[tierIndex] = true;
        // unlockMultiplier is readonly reference, but we can update its element by copying to a mutable array
        // However unlockMultiplier was declared readonly; to change, create a small mutable copy array field instead.
        // For simplicity, adjust behavior by marking tierUnlocked and using ITEM_UNLOCK_BONUS in calculations if itemBought.
        return true;
    }

    // === 업그레이드 시도 ===
    public bool TryUpgradeTier(int t, double gameTime)
    {
        if (t < 1 || t > TIERS) return false;
        if (!tierUnlocked[t]) return false; // 언락 필요
        int L = tierLevel[t];
        if (gameTime < tierCooldownEnd[t]) return false; // 쿨타임 중
        long cost = Cost(t, L);
        if (gold < cost) return false;

        // 구매 실행
        gold -= cost;
        // if itemBought, apply ITEM_UNLOCK_BONUS when computing delta
        double multiplier = itemBought[t] ? ITEM_UNLOCK_BONUS : 1.0;
        long dGPC = (long)Math.Floor(baseGPCInc[t] * (1.0 + L * growthFactorTier[t]) * multiplier);
        long dGPS = (long)Math.Floor(baseGPSInc[t] * (1.0 + L * growthFactorTier[t]) * multiplier);
        dGPS = Math.Max(1, dGPS);

        GPC += dGPC;
        GPS += dGPS;

        // 레벨 증가, 쿨타임 설정
        tierLevel[t] = L + 1;
        int cd = CooldownNext(t, L);
        tierCooldownEnd[t] = gameTime + cd;
        return true;
    }

    // === 간단 자동 구매 전략(가장 싸고 가능한 것부터) ===
    private void AutoBuyStrategy(double gameTime)
    {
        // 1) 먼저 언락 가능한 아이템 체크
        for (int t = 1; t <= TIERS; t++)
        {
            if (!tierUnlocked[t] && !itemBought[t])
            {
                long cost = itemBuyCost[t - 1];
                if (gold >= cost) { BuyItemUnlock(t, gameTime); return; }
            }
        }
        // 2) 가능한 업그레이드 중 최저 비용 우선
        long bestCost = long.MaxValue;
        int bestTier = -1;
        for (int t = 1; t <= TIERS; t++)
        {
            if (!tierUnlocked[t]) continue;
            int L = tierLevel[t];
            if (gameTime < tierCooldownEnd[t]) continue;
            long c = Cost(t, L);
            if (c <= gold && c < bestCost)
            {
                bestCost = c; bestTier = t;
            }
        }
        if (bestTier != -1) TryUpgradeTier(bestTier, gameTime);
    }

    // === 게임 루프(초 단위 시뮬레이트 예시) ===
    public void Simulate()
    {
        double gameTime = 0.0;
        double dt = 1.0; // 1초 단위로 업데이트 (원하면 더 세분화 가능)
        for (int sec = 0; sec < PLAY_TIME_SEC; sec++)
        {
            // 1) 클릭 수입 (플레이어가 매초 CLICK_PER_SEC 만큼 클릭한다고 가정)
            double clickIncomeThisSec = GPC * CLICK_PER_SEC;
            // 2) 자동 수입
            double autoIncomeThisSec = GPS;
            // 합산(정수화)
            long income = (long)Math.Floor(clickIncomeThisSec + autoIncomeThisSec);
            gold += income;

            // 3) 자동 구매(간단 전략)
            AutoBuyStrategy(gameTime);

            // 4) 시간 증가
            gameTime += dt;
        }
        // 시뮬 끝: 결과 출력
        Debug.Log($"시뮬레이션 종료 시간 {PLAY_TIME_SEC}s, 골드 = {gold}");
    }
}