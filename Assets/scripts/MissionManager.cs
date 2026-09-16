using UnityEngine;

/// <summary>
/// 任务契约管理器：调度星系赏金目标、实时跟踪进度、触发完成通报与战略结算发放
/// </summary>
public class MissionManager : MonoBehaviour
{
    public static MissionManager Instance { get; private set; }

    public MissionConfig activeMission;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (activeMission == null || activeMission.isCompleted)
        {
            activeMission = MissionConfig.CreateRandom();
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void OnRockDestroyed()
    {
        if (activeMission != null && !activeMission.isCompleted && activeMission.type == MissionConfig.MissionType.AsteroidMining)
        {
            activeMission.currentCount++;
            CheckCompletion();
        }
    }

    public void OnBossKilled()
    {
        if (activeMission != null && !activeMission.isCompleted && activeMission.type == MissionConfig.MissionType.FlagshipBounty)
        {
            activeMission.currentCount++;
            CheckCompletion();
        }
    }

    public void OnShieldCollected()
    {
        if (activeMission != null && !activeMission.isCompleted && activeMission.type == MissionConfig.MissionType.ShieldRecovery)
        {
            activeMission.currentCount++;
            CheckCompletion();
        }
    }

    private void CheckCompletion()
    {
        if (activeMission.currentCount >= activeMission.targetCount)
        {
            activeMission.isCompleted = true;

            // 发放任务战略收益
            EnergySystem.Replenish(activeMission.rewardEnergy, "MISSION COMPLETED");
            EnergySystem.RewardCredits(activeMission.rewardCredits, "CONTRACT REWARD");

            if (PlayerProfile.Instance != null)
            {
                PlayerProfile.Instance.OnMissionComplete();
            }

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.ShowBanner($"★ CONTRACT CLEARED: +{activeMission.rewardEnergy}⚡ + ★", 3.0f, Color.yellow);
            }
        }
    }
}
