using UnityEngine;

/// <summary>
/// 跨场景玩家持久化档案单例：管理战略能源、信用点、飞船科技等级与历史探索战绩
/// </summary>
public class PlayerProfile : MonoBehaviour
{
    public static PlayerProfile Instance { get; private set; }

    [Header("Strategic Resources")]
    public int credits = 150;
    public int currentEnergy = 85;
    public int maxEnergy = 100;

    [Header("Ship Tech")]
    public int shipLevel = 1;
    public int weaponTechLevel = 1;
    public int totalMissionsCompleted = 0;

    private const string PrefKeyCredits = "Profile_Credits";
    private const string PrefKeyEnergy = "Profile_Energy";
    private const string PrefKeyMaxEnergy = "Profile_MaxEnergy";
    private const string PrefKeyShipLevel = "Profile_ShipLevel";
    private const string PrefKeyWeaponTech = "Profile_WeaponTech";
    private const string PrefKeyMissions = "Profile_Missions";

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        Load();
    }

    public void Save()
    {
        PlayerPrefs.SetInt(PrefKeyCredits, credits);
        PlayerPrefs.SetInt(PrefKeyEnergy, currentEnergy);
        PlayerPrefs.SetInt(PrefKeyMaxEnergy, maxEnergy);
        PlayerPrefs.SetInt(PrefKeyShipLevel, shipLevel);
        PlayerPrefs.SetInt(PrefKeyWeaponTech, weaponTechLevel);
        PlayerPrefs.SetInt(PrefKeyMissions, totalMissionsCompleted);
        PlayerPrefs.Save();
    }

    public void Load()
    {
        credits = PlayerPrefs.GetInt(PrefKeyCredits, 150);
        currentEnergy = PlayerPrefs.GetInt(PrefKeyEnergy, 85);
        maxEnergy = PlayerPrefs.GetInt(PrefKeyMaxEnergy, 100);
        shipLevel = PlayerPrefs.GetInt(PrefKeyShipLevel, 1);
        weaponTechLevel = PlayerPrefs.GetInt(PrefKeyWeaponTech, 1);
        totalMissionsCompleted = PlayerPrefs.GetInt(PrefKeyMissions, 0);

        // 保障初始最低行动能源
        if (currentEnergy < 25) currentEnergy = 25;
    }

    public bool ConsumeEnergy(int amount)
    {
        if (currentEnergy >= amount)
        {
            currentEnergy -= amount;
            Save();
            return true;
        }
        return false;
    }

    public void AddEnergy(int amount)
    {
        currentEnergy = Mathf.Clamp(currentEnergy + amount, 0, maxEnergy);
        Save();
    }

    public void AddCredits(int amount)
    {
        credits += amount;
        Save();
    }

    public void OnMissionComplete()
    {
        totalMissionsCompleted++;
        Save();
    }
}
