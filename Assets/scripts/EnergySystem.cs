using UnityEngine;

/// <summary>
/// 战略能源调度中心：规范航行跃迁、星球全息扫描、地表登陆与护盾充能的战略消耗与危机判定
/// </summary>
public static class EnergySystem
{
    public const int CostScanPlanet = 5;
    public const int CostShieldBoost = 10;
    public const int CostLanding = 10;
    public const int CostWarpReturn = 12;

    public const int CostRouteSafe = 8;
    public const int CostRouteStorm = 12;
    public const int CostRouteDeepSpace = 18;

    public static int CurrentEnergy => PlayerProfile.Instance != null ? PlayerProfile.Instance.currentEnergy : 80;
    public static int MaxEnergy => PlayerProfile.Instance != null ? PlayerProfile.Instance.maxEnergy : 100;
    public static int Credits => PlayerProfile.Instance != null ? PlayerProfile.Instance.credits : 100;

    public static bool CanAfford(int cost)
    {
        return CurrentEnergy >= cost;
    }

    public static bool TryConsume(int cost, string actionName = "")
    {
        if (PlayerProfile.Instance == null) return true;

        if (PlayerProfile.Instance.ConsumeEnergy(cost))
        {
            if (!string.IsNullOrEmpty(actionName) && WaveManager.Instance != null)
            {
                WaveManager.Instance.ShowBanner($"-{cost} ENERGY: {actionName}", 1.6f, new Color(0.3f, 0.8f, 1f));
            }
            return true;
        }
        else
        {
            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.ShowBanner("⚠ INSUFFICIENT ENERGY! ACTION DENIED ⚠", 2.0f, Color.red);
            }
            return false;
        }
    }

    public static void Replenish(int amount, string source = "")
    {
        if (PlayerProfile.Instance != null)
        {
            PlayerProfile.Instance.AddEnergy(amount);
            if (!string.IsNullOrEmpty(source) && WaveManager.Instance != null)
            {
                WaveManager.Instance.ShowBanner($"+{amount} ENERGY: {source}", 1.8f, Color.green);
            }
        }
    }

    public static void RewardCredits(int amount, string source = "")
    {
        if (PlayerProfile.Instance != null)
        {
            PlayerProfile.Instance.AddCredits(amount);
            if (!string.IsNullOrEmpty(source) && WaveManager.Instance != null)
            {
                WaveManager.Instance.ShowBanner($"+ CREDITS: {source}", 1.8f, Color.yellow);
            }
        }
    }
}
