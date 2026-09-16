using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏全局单例总控：统筹倒计时、胜负判定、零 GC OnGUI 渲染、历史最高分持久化、音频管理与独立相机震动对接
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Game Settings")]
    public float targetSurvivalTime = 30f;
    public string winSceneName = "win";
    public string loseSceneName = "lose";

    private float timeRemaining;
    private int score = 0;
    private int bestScore = 0;
    private bool isNewHighScore = false;
    private bool isGameOver = false;
    private bool isGameWon = false;

    private const string BestScoreKey = "BestScore";
    private const string LastScoreKey = "LastScore";

    private AudioSource bgmSource;
    private AudioSource sfxSource;

    private HUDDigitDisplay digitDisplay;

    // --- 零分配 OnGUI 缓存 ---
    private GUIStyle scoreStyle;
    private GUIStyle bestStyle;
    private GUIStyle timeStyle;
    private GUIStyle newHighStyle;
    private Rect scoreRect = new Rect(25, 20, 220, 28);
    private Rect bestRect = new Rect(25, 50, 220, 28);
    private Rect timeRect = new Rect(25, 80, 220, 28);
    private Rect newHighRect = new Rect(25, 110, 240, 30);

    private string cachedScoreText = "SCORE: 0";
    private string cachedBestText = "BEST: 0";
    private string cachedTimeText = "TIME: 30s";
    private int lastRenderedSecond = -1;

    public bool IsGameOver => isGameOver;
    public bool IsGameWon => isGameWon;
    public int Score => score;
    public int BestScore => bestScore;
    public bool IsNewHighScore => isNewHighScore;
    public float TimeRemaining => timeRemaining;
    public float GameProgress => Mathf.Clamp01(1f - (timeRemaining / targetSurvivalTime));

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        timeRemaining = targetSurvivalTime;
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);

        cachedBestText = $"BEST: {bestScore}";
        cachedScoreText = $"SCORE: {score}";

        // 初始化预加载资源
        GameResources.Initialize();

        SetupAudio();
        SetupOnGUIStyles();
        SetupDigitDisplay();
        EnsureObjectPool();
    }

    void Start()
    {
        EnsureSceneEntities();
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void EnsureAllEntities()
    {
        timeRemaining = targetSurvivalTime;
        isGameOver = false;
        isGameWon = false;
        isNewHighScore = false;

        EnsureObjectPool();
        EnsureSceneEntities();
    }

    private void SetupAudio()
    {
        bgmSource = gameObject.AddComponent<AudioSource>();
        sfxSource = gameObject.AddComponent<AudioSource>();

        if (GameResources.BgmClip != null)
        {
            bgmSource.clip = GameResources.BgmClip;
            bgmSource.loop = true;
            bgmSource.volume = 0.55f;
            bgmSource.Play();
        }
    }

    private void SetupOnGUIStyles()
    {
        scoreStyle = new GUIStyle();
        scoreStyle.fontSize = 22;
        scoreStyle.normal.textColor = Color.yellow;
        scoreStyle.fontStyle = FontStyle.Bold;

        bestStyle = new GUIStyle();
        bestStyle.fontSize = 20;
        bestStyle.normal.textColor = new Color(1f, 0.85f, 0.2f);
        bestStyle.fontStyle = FontStyle.Bold;

        timeStyle = new GUIStyle();
        timeStyle.fontSize = 22;
        timeStyle.normal.textColor = Color.cyan;
        timeStyle.fontStyle = FontStyle.Bold;

        newHighStyle = new GUIStyle();
        newHighStyle.fontSize = 18;
        newHighStyle.normal.textColor = new Color(0.2f, 1f, 0.3f);
        newHighStyle.fontStyle = FontStyle.Bold;
    }

    private void SetupDigitDisplay()
    {
        digitDisplay = FindObjectOfType<HUDDigitDisplay>();
        if (digitDisplay == null)
        {
            GameObject hudObj = new GameObject("HUDDigitDisplay");
            digitDisplay = hudObj.AddComponent<HUDDigitDisplay>();
        }
    }

    private void EnsureObjectPool()
    {
        if (ObjectPool.Instance == null && FindObjectOfType<ObjectPool>() == null)
        {
            GameObject poolObj = new GameObject("ObjectPool");
            poolObj.AddComponent<ObjectPool>();
        }
    }

    private void EnsureSceneEntities()
    {
        // 1. Ensure Player
        PlayerController player = FindObjectOfType<PlayerController>();
        if (player == null)
        {
            GameObject pObj = new GameObject("Player");
            pObj.transform.position = new Vector3(0, -3.2f, 0);
            pObj.transform.localScale = Vector3.one * 1.5f;

            var sr = pObj.AddComponent<SpriteRenderer>();
            sr.sprite = GameResources.PlayerSprite;
            sr.sortingOrder = 10;

            var col = pObj.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.35f;

            var rb = pObj.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;

            pObj.AddComponent<PlayerController>();
        }

        // 2. Ensure RockSpawner
        RockSpawner spawner = FindObjectOfType<RockSpawner>();
        if (spawner == null)
        {
            GameObject spawnerObj = new GameObject("RockSpawner");
            spawnerObj.AddComponent<RockSpawner>();
        }

        // 3. Ensure Background Scroller
        BackgroundScroller scroller = FindObjectOfType<BackgroundScroller>();
        if (scroller == null)
        {
            GameObject bgObj = GameObject.Find("Stars_large");
            if (bgObj != null)
            {
                var sr = bgObj.GetComponent<SpriteRenderer>();
                if (sr != null)
                {
                    if (sr.sprite == null) sr.sprite = GameResources.StarsSprite;
                    sr.sortingOrder = -10;
                }
                bgObj.AddComponent<BackgroundScroller>();
            }
            else
            {
                GameObject newBg = new GameObject("Stars_large");
                var sr = newBg.AddComponent<SpriteRenderer>();
                sr.sprite = GameResources.StarsSprite;
                sr.sortingOrder = -10;
                newBg.transform.position = Vector3.zero;
                newBg.transform.localScale = new Vector3(3.1f, 2.3f, 1f);
                newBg.AddComponent<BackgroundScroller>();
            }
        }
    }

    void Update()
    {
        if (isGameOver || isGameWon) return;

        timeRemaining -= Time.deltaTime;
        if (timeRemaining < 0f) timeRemaining = 0f;

        int currentSecond = Mathf.CeilToInt(timeRemaining);

        // 仅在秒数发生整秒跳动时，才更新 HUD 与文本缓存（零 GC 策略）
        if (currentSecond != lastRenderedSecond)
        {
            lastRenderedSecond = currentSecond;
            cachedTimeText = $"TIME: {currentSecond}s";

            if (digitDisplay != null)
            {
                digitDisplay.UpdateDisplay(currentSecond);
            }
        }

        if (timeRemaining <= 0f)
        {
            OnTimeUp();
        }
    }

    public void AddScore(int amount)
    {
        if (isGameOver || isGameWon) return;
        score += amount;
        cachedScoreText = $"SCORE: {score}";

        if (score > bestScore)
        {
            bestScore = score;
            cachedBestText = $"BEST: {bestScore}";
            isNewHighScore = true;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
            PlayerPrefs.Save();
        }
    }

    public void PlayShootSound()
    {
        if (sfxSource != null && GameResources.ShootClip != null)
        {
            sfxSource.pitch = Random.Range(0.96f, 1.04f);
            sfxSource.PlayOneShot(GameResources.ShootClip, 0.65f);
        }
    }

    public void PlayExplosionSound()
    {
        if (sfxSource != null && GameResources.ExplosionClip != null)
        {
            sfxSource.pitch = Random.Range(0.92f, 1.08f);
            sfxSource.PlayOneShot(GameResources.ExplosionClip, 0.85f);
        }
    }

    public void PlayShieldPickupSound()
    {
        if (sfxSource != null && GameResources.ShieldPickupClip != null)
        {
            sfxSource.pitch = 1.15f;
            sfxSource.PlayOneShot(GameResources.ShieldPickupClip, 0.90f);
        }
    }

    public void PlayShieldBreakSound()
    {
        if (sfxSource != null && GameResources.ShieldPickupClip != null)
        {
            sfxSource.pitch = 0.70f;
            sfxSource.PlayOneShot(GameResources.ShieldPickupClip, 0.95f);
        }
    }

    // ==================== 胜负判定与场景流转 ====================

    private void SaveScores()
    {
        PlayerPrefs.SetInt(LastScoreKey, score);
        if (score > PlayerPrefs.GetInt(BestScoreKey, 0))
        {
            PlayerPrefs.SetInt(BestScoreKey, score);
        }
        PlayerPrefs.Save();
    }

    public void OnPlayerDied()
    {
        if (isGameOver || isGameWon) return;
        isGameOver = true;

        SaveScores();
        PlayExplosionSound();
        StartCoroutine(LoadSceneWithDelay(loseSceneName, 1.2f));
    }

    private void OnTimeUp()
    {
        if (isGameOver || isGameWon) return;
        isGameWon = true;

        SaveScores();
        StartCoroutine(LoadSceneWithDelay(winSceneName, 1.0f));
    }

    private IEnumerator LoadSceneWithDelay(string sceneName, float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene(sceneName);
    }

    void OnGUI()
    {
        GUI.Label(scoreRect, cachedScoreText, scoreStyle);
        GUI.Label(bestRect, cachedBestText, bestStyle);
        GUI.Label(timeRect, cachedTimeText, timeStyle);

        if (isNewHighScore)
        {
            GUI.Label(newHighRect, "★ NEW HIGH SCORE! ★", newHighStyle);
        }
    }
}
