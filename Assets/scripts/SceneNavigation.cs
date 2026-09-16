using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景流转导航器：监听按键/点击触发场景切换，自动自愈封面图，展示 SCORE、BEST 及 NEW HIGH SCORE 结算面板
/// </summary>
public class SceneNavigation : MonoBehaviour
{
    [Header("Navigation Settings")]
    public string targetScene = "game";
    public string promptText = "PRESS SPACE OR CLICK TO START";

    private float timer = 0f;
    private GUIStyle promptStyle;
    private GUIStyle scoreSummaryStyle;
    private GUIStyle bestSummaryStyle;
    private GUIStyle bannerStyle;
    private Color textColor = Color.white;

    private int lastScore = 0;
    private int bestScore = 0;
    private bool isHighScoreRun = false;
    private bool isSettlementScene = false;

    void Awake()
    {
        promptStyle = new GUIStyle();
        promptStyle.alignment = TextAnchor.MiddleCenter;
        promptStyle.fontSize = 24;
        promptStyle.fontStyle = FontStyle.Bold;

        scoreSummaryStyle = new GUIStyle();
        scoreSummaryStyle.alignment = TextAnchor.MiddleCenter;
        scoreSummaryStyle.fontSize = 22;
        scoreSummaryStyle.fontStyle = FontStyle.Bold;
        scoreSummaryStyle.normal.textColor = Color.yellow;

        bestSummaryStyle = new GUIStyle();
        bestSummaryStyle.alignment = TextAnchor.MiddleCenter;
        bestSummaryStyle.fontSize = 20;
        bestSummaryStyle.fontStyle = FontStyle.Bold;
        bestSummaryStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);

        bannerStyle = new GUIStyle();
        bannerStyle.alignment = TextAnchor.MiddleCenter;
        bannerStyle.fontSize = 22;
        bannerStyle.fontStyle = FontStyle.Bold;
        bannerStyle.normal.textColor = new Color(0.2f, 1f, 0.3f);

        GameResources.Initialize();
        EnsureCoverSprite();

        string sceneName = SceneManager.GetActiveScene().name.ToLower();
        isSettlementScene = sceneName.Contains("win") || sceneName.Contains("lose");

        lastScore = PlayerPrefs.GetInt("LastScore", 0);
        bestScore = PlayerPrefs.GetInt("BestScore", 0);
        isHighScoreRun = isSettlementScene && (lastScore >= bestScore) && (lastScore > 0);
    }

    private void EnsureCoverSprite()
    {
        string sceneName = SceneManager.GetActiveScene().name.ToLower();

        SpriteRenderer[] renderers = FindObjectsOfType<SpriteRenderer>();
        foreach (var sr in renderers)
        {
            if (sr.sprite == null)
            {
                if (sceneName.Contains("start"))
                {
                    sr.sprite = GameResources.StartSprite;
                }
                else if (sceneName.Contains("win"))
                {
                    sr.sprite = GameResources.WinSprite;
                }
                else if (sceneName.Contains("lose"))
                {
                    sr.sprite = GameResources.LoseSprite;
                }
            }
        }
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.J) ||
            Input.GetMouseButtonDown(0))
        {
            LoadTargetScene();
        }
    }

    public void LoadTargetScene()
    {
        if (!string.IsNullOrEmpty(targetScene))
        {
            SceneManager.LoadScene(targetScene);
        }
    }

    void OnGUI()
    {
        float width = Screen.width;
        float height = Screen.height;

        // 结算界面展示最终得分与历史最高分
        if (isSettlementScene)
        {
            if (isHighScoreRun)
            {
                GUI.Label(new Rect(0, height - 165, width, 30), "★ NEW HIGH SCORE! ★", bannerStyle);
            }
            GUI.Label(new Rect(0, height - 135, width, 28), $"FINAL SCORE: {lastScore}", scoreSummaryStyle);
            GUI.Label(new Rect(0, height - 105, width, 28), $"BEST SCORE: {bestScore}", bestSummaryStyle);
        }
        else if (bestScore > 0)
        {
            // 开始界面展示历史最高分
            GUI.Label(new Rect(0, height - 110, width, 28), $"BEST SCORE: {bestScore}", bestSummaryStyle);
        }

        // 呼吸闪烁操作提示
        float alpha = 0.35f + ((Mathf.Sin(timer * 4.5f) + 1f) * 0.5f) * 0.65f;
        textColor.a = alpha;
        promptStyle.normal.textColor = textColor;

        GUI.Label(new Rect(0, height - 70, width, 50), promptText, promptStyle);
    }
}
