using System.Collections;
using UnityEngine;

/// <summary>
/// 星球遭遇与全息扫描控制器：承载深空航行到达后的未知星球发现、全息能量扫描与登陆/返航战略决策
/// </summary>
public class PlanetEncounter : MonoBehaviour
{
    public static PlanetEncounter Instance { get; private set; }

    public PlanetConfig currentPlanet;
    private bool isEncounterActive = false;
    private GameObject planetVisualObj;
    private SpriteRenderer planetRenderer;

    // --- OnGUI 样式与贴图缓存 ---
    private Texture2D panelBgTex;
    private GUIStyle headerStyle;
    private GUIStyle infoStyle;
    private GUIStyle buttonStyle;
    private GUIStyle warningStyle;

    public bool IsEncounterActive => isEncounterActive;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupGuiStyles();
    }

    private void SetupGuiStyles()
    {
        panelBgTex = new Texture2D(1, 1);
        panelBgTex.SetPixel(0, 0, new Color(0.06f, 0.08f, 0.14f, 0.92f));
        panelBgTex.Apply();

        headerStyle = new GUIStyle();
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.fontSize = 20;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.normal.textColor = new Color(0.2f, 0.85f, 1f);

        infoStyle = new GUIStyle();
        infoStyle.alignment = TextAnchor.MiddleCenter;
        infoStyle.fontSize = 14;
        infoStyle.normal.textColor = new Color(0.9f, 0.95f, 1f);

        warningStyle = new GUIStyle();
        warningStyle.alignment = TextAnchor.MiddleCenter;
        warningStyle.fontSize = 13;
        warningStyle.normal.textColor = new Color(1f, 0.75f, 0.2f);
        warningStyle.fontStyle = FontStyle.Italic;

        buttonStyle = new GUIStyle();
        buttonStyle.alignment = TextAnchor.MiddleCenter;
        buttonStyle.fontSize = 15;
        buttonStyle.fontStyle = FontStyle.Bold;
        buttonStyle.normal.textColor = Color.yellow;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (panelBgTex != null) Destroy(panelBgTex);
    }

    public void TriggerEncounter()
    {
        if (isEncounterActive) return;
        isEncounterActive = true;

        currentPlanet = PlanetConfig.GenerateRandomPlanet();
        CreatePlanetVisual();

        // 停止背景高速滚动
        BackgroundScroller scroller = FindObjectOfType<BackgroundScroller>();
        if (scroller != null)
        {
            scroller.SetSpeed(0.2f);
        }

        CameraShake.Shake(0.25f, 0.2f);
    }

    private void CreatePlanetVisual()
    {
        if (planetVisualObj != null) Destroy(planetVisualObj);

        planetVisualObj = new GameObject("PlanetVisual");
        planetVisualObj.transform.position = new Vector3(0, 1.2f, 0);
        planetVisualObj.transform.localScale = Vector3.one * 3.6f;

        planetRenderer = planetVisualObj.AddComponent<SpriteRenderer>();
        planetRenderer.sprite = GameResources.RockSprite;
        planetRenderer.sortingOrder = 2;

        // 根据气候渲染行星基色
        switch (currentPlanet.atmosphere)
        {
            case PlanetConfig.PlanetAtmosphere.FrozenTundra:
                planetRenderer.color = new Color(0.4f, 0.8f, 1f, 0.9f);
                break;
            case PlanetConfig.PlanetAtmosphere.AncientRuins:
                planetRenderer.color = new Color(0.7f, 0.5f, 0.9f, 0.9f);
                break;
            case PlanetConfig.PlanetAtmosphere.MoltenDesert:
                planetRenderer.color = new Color(1f, 0.45f, 0.2f, 0.9f);
                break;
            case PlanetConfig.PlanetAtmosphere.ToxicSwamp:
                planetRenderer.color = new Color(0.3f, 0.9f, 0.4f, 0.9f);
                break;
        }
    }

    void Update()
    {
        if (planetVisualObj != null)
        {
            // 行星缓慢自转
            planetVisualObj.transform.Rotate(0, 0, 4f * Time.deltaTime);
        }
    }

    private void PerformScan()
    {
        if (EnergySystem.TryConsume(EnergySystem.CostScanPlanet, "PLANETARY SCAN"))
        {
            currentPlanet.isScanned = true;
            CameraShake.Shake(0.12f, 0.15f);
        }
    }

    private void PrepareLanding()
    {
        if (EnergySystem.TryConsume(EnergySystem.CostLanding, "ATMOSPHERE ENTRY"))
        {
            // 获得勘探发现奖励
            int reward = currentPlanet.resourceLevel * 25;
            EnergySystem.RewardCredits(reward, "PLANETARY SURVEY DATA");

            if (WaveManager.Instance != null)
            {
                WaveManager.Instance.ShowBanner($"★ LANDING PREPARED: + SURVEY REWARD ★", 2.5f, Color.green);
            }

            StartCoroutine(FinishEncounterRoutine(true));
        }
    }

    private void ReturnToBase()
    {
        if (EnergySystem.TryConsume(EnergySystem.CostWarpReturn, "WARP JUMP RETURN"))
        {
            StartCoroutine(FinishEncounterRoutine(false));
        }
    }

    private IEnumerator FinishEncounterRoutine(bool hasLanded)
    {
        yield return new WaitForSeconds(1.5f);
        isEncounterActive = false;
        if (planetVisualObj != null) Destroy(planetVisualObj);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.TriggerVictory();
        }
    }

    void OnGUI()
    {
        if (!isEncounterActive) return;

        float panelW = Mathf.Min(Screen.width * 0.85f, 520f);
        float panelH = 260f;
        float panelX = (Screen.width - panelW) * 0.5f;
        float panelY = Screen.height - panelH - 40f;

        Rect panelRect = new Rect(panelX, panelY, panelW, panelH);
        if (panelBgTex != null) GUI.DrawTexture(panelRect, panelBgTex);

        GUILayout.BeginArea(new Rect(panelX + 15, panelY + 12, panelW - 30, panelH - 24));

        if (!currentPlanet.isScanned)
        {
            GUILayout.Label("✦ UNKNOWN CELESTIAL BODY DETECTED ✦", headerStyle);
            GUILayout.Space(10);
            GUILayout.Label("Deep space sensor detects an uncharted planet ahead.", infoStyle);
            GUILayout.Label("Atmosphere: [ ??? ]   Hazard: [ ??? ]   Resources: [ ??? ]", warningStyle);
            GUILayout.Space(25);

            if (GUILayout.Button($"[ SCAN PLANET (-{EnergySystem.CostScanPlanet}⚡ ENERGY) ]", GUILayout.Height(42)))
            {
                PerformScan();
            }
        }
        else
        {
            GUILayout.Label($"✦ PLANET {currentPlanet.planetName.ToUpper()} ✦", headerStyle);
            GUILayout.Space(6);
            string starsH = new string('★', currentPlanet.hazardLevel) + new string('☆', 5 - currentPlanet.hazardLevel);
            string starsR = new string('★', currentPlanet.resourceLevel) + new string('☆', 5 - currentPlanet.resourceLevel);

            GUILayout.Label($"Climate: {currentPlanet.atmosphere}   |   Hazard: {starsH}   |   Richness: {starsR}", infoStyle);
            GUILayout.Label($"Ground Contract: {currentPlanet.groundMissionPreview}", warningStyle);
            GUILayout.Space(16);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button($"[ PREPARE LANDING (-{EnergySystem.CostLanding}⚡) ]", GUILayout.Height(40)))
            {
                PrepareLanding();
            }
            if (GUILayout.Button($"[ WARP RETURN BASE (-{EnergySystem.CostWarpReturn}⚡) ]", GUILayout.Height(40)))
            {
                ReturnToBase();
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.EndArea();
    }
}
