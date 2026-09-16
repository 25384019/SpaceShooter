using UnityEngine;

/// <summary>
/// 母舰星图航线调度中心：提供安全巡航、富矿暴雨与旗舰深空三类战略航线抉择
/// </summary>
public class StarMapController : MonoBehaviour
{
    public static StarMapController Instance { get; private set; }

    public enum SectorRoute
    {
        SafeSector,
        StormBelt,
        DeepSpaceBoss
    }

    public static SectorRoute SelectedRoute = SectorRoute.SafeSector;
    public bool isMapOpen = false;

    private Texture2D panelTex;
    private GUIStyle titleStyle;
    private GUIStyle routeBtnStyle;
    private GUIStyle descStyle;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupStyles();
    }

    private void SetupStyles()
    {
        panelTex = new Texture2D(1, 1);
        panelTex.SetPixel(0, 0, new Color(0.04f, 0.06f, 0.12f, 0.94f));
        panelTex.Apply();

        titleStyle = new GUIStyle();
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.fontSize = 22;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = new Color(0.2f, 0.9f, 1f);

        routeBtnStyle = new GUIStyle();
        routeBtnStyle.alignment = TextAnchor.MiddleCenter;
        routeBtnStyle.fontSize = 15;
        routeBtnStyle.fontStyle = FontStyle.Bold;
        routeBtnStyle.normal.textColor = Color.white;

        descStyle = new GUIStyle();
        descStyle.alignment = TextAnchor.MiddleLeft;
        descStyle.fontSize = 12;
        descStyle.normal.textColor = new Color(0.85f, 0.9f, 0.95f);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (panelTex != null) Destroy(panelTex);
    }

    public void OpenStarMap()
    {
        isMapOpen = true;
    }

    public void CloseStarMap()
    {
        isMapOpen = false;
    }

    public void LaunchExpedition(SectorRoute route, int cost)
    {
        if (!EnergySystem.TryConsume(cost, "SECTOR JUMP"))
        {
            return;
        }

        SelectedRoute = route;
        isMapOpen = false;

        // 根据路线对 WaveManager 进行参数注入
        if (WaveManager.Instance != null)
        {
            switch (route)
            {
                case SectorRoute.SafeSector:
                    WaveManager.Instance.bossSpawnScore = 25;
                    WaveManager.Instance.wave2TriggerScore = 12;
                    WaveManager.Instance.ShowBanner("✦ ENGAGING ROUTE: SAFE SECTOR ✦", 2.5f, Color.green);
                    break;
                case SectorRoute.StormBelt:
                    WaveManager.Instance.bossSpawnScore = 22;
                    WaveManager.Instance.wave2TriggerScore = 5; // 早触发暴雨
                    WaveManager.Instance.ShowBanner("✦ ENGAGING ROUTE: METEOR STORM BELT ✦", 2.5f, Color.yellow);
                    break;
                case SectorRoute.DeepSpaceBoss:
                    WaveManager.Instance.bossSpawnScore = 10; // 快速切入决战
                    WaveManager.Instance.wave2TriggerScore = 999;
                    WaveManager.Instance.ShowBanner("✦ ENGAGING ROUTE: DEEP SPACE BOSS HUNT ✦", 2.5f, Color.red);
                    break;
            }
        }
    }

    void OnGUI()
    {
        if (!isMapOpen) return;

        float w = Mathf.Min(Screen.width * 0.88f, 560f);
        float h = 330f;
        float x = (Screen.width - w) * 0.5f;
        float y = (Screen.height - h) * 0.5f;

        Rect rect = new Rect(x, y, w, h);
        if (panelTex != null) GUI.DrawTexture(rect, panelTex);

        GUILayout.BeginArea(new Rect(x + 18, y + 16, w - 36, h - 32));

        GUILayout.Label("✦ STAR MAP NAVIGATION / 航线战略部署 ✦", titleStyle);
        GUILayout.Space(8);
        GUILayout.Label($"Mothership Energy: {EnergySystem.CurrentEnergy}/{EnergySystem.MaxEnergy}⚡  |  Credits: ", descStyle);
        GUILayout.Space(14);

        // 航线 1
        if (GUILayout.Button($"[ ROUTE A: SAFE PATROL SECTOR (-{EnergySystem.CostRouteSafe}⚡) ]", GUILayout.Height(36)))
        {
            LaunchExpedition(SectorRoute.SafeSector, EnergySystem.CostRouteSafe);
        }
        GUILayout.Label("   Low asteroid density. Reliable transit. Standard credit yield.", descStyle);
        GUILayout.Space(10);

        // 航线 2
        if (GUILayout.Button($"[ ROUTE B: ASTEROID STORM BELT (-{EnergySystem.CostRouteStorm}⚡) ]", GUILayout.Height(36)))
        {
            LaunchExpedition(SectorRoute.StormBelt, EnergySystem.CostRouteStorm);
        }
        GUILayout.Label("   High density storm wave. Rich shield pickups. Higher bounty bonus.", descStyle);
        GUILayout.Space(10);

        // 航线 3
        if (GUILayout.Button($"[ ROUTE C: DEEP SPACE FLAGSHIP HUNT (-{EnergySystem.CostRouteDeepSpace}⚡) ]", GUILayout.Height(36)))
        {
            LaunchExpedition(SectorRoute.DeepSpaceBoss, EnergySystem.CostRouteDeepSpace);
        }
        GUILayout.Label("   Extreme threat. Fast Flagship Boss encounter. Massive reward: +.", descStyle);

        GUILayout.EndArea();
    }
}
