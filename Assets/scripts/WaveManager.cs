using System.Collections;
using UnityEngine;

/// <summary>
/// 关卡波次管理器：统筹 Wave 1 陨石带 -> Wave 2 随机事件暴雨 -> Wave 3 Boss 旗舰大决战与通关流转
/// </summary>
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    public enum WaveStage
    {
        Wave1_AsteroidField,
        Wave2_MeteorStorm,
        Wave3_BossBattle,
        Cleared
    }

    [Header("Wave State")]
    public WaveStage currentStage = WaveStage.Wave1_AsteroidField;
    public int bossSpawnScore = 25;
    public int wave2TriggerScore = 10;

    private bool wave2Triggered = false;
    private bool bossSpawned = false;

    // --- OnGUI Banner 提示 ---
    private string bannerText = "";
    private float bannerTimer = 0f;
    private Color bannerColor = Color.yellow;
    private GUIStyle bannerStyle;
    private Rect bannerRect;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupBannerStyle();
    }

    private void SetupBannerStyle()
    {
        bannerStyle = new GUIStyle();
        bannerStyle.alignment = TextAnchor.MiddleCenter;
        bannerStyle.fontSize = 24;
        bannerStyle.fontStyle = FontStyle.Bold;
    }

    void Start()
    {
        ShowBanner("✦ WAVE 1: ASTEROID SECTOR ✦", 2.8f, Color.cyan);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (bannerTimer > 0f)
        {
            bannerTimer -= Time.deltaTime;
            if (bannerTimer <= 0f) bannerText = "";
        }

        if (GameManager.Instance == null || GameManager.Instance.IsGameOver)
            return;

        int score = GameManager.Instance.Score;

        // Wave 2 触发：10分触发突发陨石暴雨
        if (!wave2Triggered && score >= wave2TriggerScore)
        {
            wave2Triggered = true;
            currentStage = WaveStage.Wave2_MeteorStorm;

            if (RandomEventManager.Instance != null)
            {
                RandomEventManager.Instance.TriggerMeteorStorm(7.0f);
            }
        }

        // Wave 3 触发：25分触发 Boss 旗舰来袭
        if (!bossSpawned && score >= bossSpawnScore)
        {
            bossSpawned = true;
            currentStage = WaveStage.Wave3_BossBattle;
            StartCoroutine(SpawnBossSequence());
        }
    }

    private IEnumerator SpawnBossSequence()
    {
        // 1. 暂停普通陨石生成
        RockSpawner spawner = FindObjectOfType<RockSpawner>();
        if (spawner != null)
        {
            spawner.isPaused = true;
        }

        // 2. 警报横幅与镜头震颤
        ShowBanner("⚠ WARNING: ENEMY FLAGSHIP APPROACHING! ⚠", 3.5f, new Color(1f, 0.2f, 0.2f));
        CameraShake.Shake(0.35f, 0.25f);

        // 3. 背景降速烘托决战压迫感
        BackgroundScroller scroller = FindObjectOfType<BackgroundScroller>();
        if (scroller != null)
        {
            scroller.SetSpeed(0.8f);
            scroller.SetColor(new Color(1f, 0.82f, 0.82f, 1f));
        }

        yield return new WaitForSeconds(1.8f);

        // 4. 实例化 Boss 旗舰
        GameObject bossObj = new GameObject("Boss_Dreadnought");
        bossObj.transform.position = new Vector3(0, 7.5f, 0);
        bossObj.AddComponent<BossController>();
    }

    public void OnBossDefeated()
    {
        currentStage = WaveStage.Cleared;
        ShowBanner("★ VICTORY! SECTOR CLEARED! ★", 3.0f, new Color(0.2f, 1f, 0.3f));
        StartCoroutine(VictoryDelayRoutine());
    }

    private IEnumerator VictoryDelayRoutine()
    {
        yield return new WaitForSeconds(2.0f);
        if (GameManager.Instance != null && !GameManager.Instance.IsGameOver)
        {
            GameManager.Instance.TriggerVictory();
        }
    }

    public void ShowBanner(string text, float duration, Color color)
    {
        bannerText = text;
        bannerTimer = duration;
        bannerColor = color;
    }

    void OnGUI()
    {
        if (string.IsNullOrEmpty(bannerText) || bannerTimer <= 0f) return;

        // 警报发光闪烁
        float alpha = 0.7f + Mathf.PingPong(Time.time * 4f, 0.3f);
        bannerStyle.normal.textColor = new Color(bannerColor.r, bannerColor.g, bannerColor.b, alpha);

        bannerRect = new Rect(0, Screen.height * 0.38f, Screen.width, 40);
        GUI.Label(bannerRect, bannerText, bannerStyle);
    }
}
