using System;

[Serializable]
public class MissionConfig
{
    public enum MissionType
    {
        AsteroidMining,   // 击破特定数量陨石
        FlagshipBounty,   // 歼灭敌军母舰旗舰
        ShieldRecovery    // 打捞能量护盾核心
    }

    public string title;
    public string description;
    public MissionType type;
    public int targetCount;
    public int currentCount;
    public int rewardEnergy;
    public int rewardCredits;
    public bool isCompleted;

    public static MissionConfig CreateRandom()
    {
        int roll = UnityEngine.Random.Range(0, 3);
        MissionConfig m = new MissionConfig();

        switch (roll)
        {
            case 0:
                m.type = MissionType.AsteroidMining;
                m.title = "ASTEROID SWEEP";
                m.description = "Clear floating asteroids from navigation lane";
                m.targetCount = 12;
                m.rewardEnergy = 20;
                m.rewardCredits = 60;
                break;

            case 1:
                m.type = MissionType.FlagshipBounty;
                m.title = "FLAGSHIP BOUNTY";
                m.description = "Eliminate approaching enemy dreadnought";
                m.targetCount = 1;
                m.rewardEnergy = 35;
                m.rewardCredits = 120;
                break;

            case 2:
            default:
                m.type = MissionType.ShieldRecovery;
                m.title = "CORE SALVAGE";
                m.description = "Recover energy shield modules from deep space";
                m.targetCount = 2;
                m.rewardEnergy = 25;
                m.rewardCredits = 50;
                break;
        }

        m.currentCount = 0;
        m.isCompleted = false;
        return m;
    }

    public string GetProgressString()
    {
        if (isCompleted) return "★ MISSION COMPLETED ★";
        return $"[{currentCount}/{targetCount}] {title}";
    }
}
