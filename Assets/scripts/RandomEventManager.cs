using System.Collections;
using UnityEngine;

/// <summary>
/// 随机事件系统管理器：负责突发陨石暴雨（高频极速小陨石群）、战术补给空投以及环境氛围联动
/// </summary>
public class RandomEventManager : MonoBehaviour
{
    public static RandomEventManager Instance { get; private set; }

    private bool isEventRunning = false;
    private BackgroundScroller scroller;

    public bool IsEventRunning => isEventRunning;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        scroller = FindObjectOfType<BackgroundScroller>();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public void TriggerMeteorStorm(float duration = 6.5f)
    {
        if (isEventRunning) return;
        StartCoroutine(MeteorStormRoutine(duration));
    }

    public void TriggerSupplyDrop()
    {
        float x = Random.Range(-3.2f, 3.2f);
        ShieldPickup.Spawn(new Vector3(x, 6.0f, 0));

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ShowBanner("★ TACTICAL SUPPLY DROP INCOMING ★", 2.2f, Color.cyan);
        }
    }

    private IEnumerator MeteorStormRoutine(float duration)
    {
        isEventRunning = true;

        if (scroller == null) scroller = FindObjectOfType<BackgroundScroller>();
        if (scroller != null)
        {
            scroller.SetSpeed(3.2f);
            scroller.SetColor(new Color(1.0f, 0.7f, 0.7f, 1f));
        }

        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.ShowBanner("⚠ WARNING: METEOR STORM INCOMING! ⚠", duration, new Color(1f, 0.4f, 0.1f));
        }

        float elapsed = 0f;
        float nextSpawn = 0f;
        bool droppedShield = false;

        Camera cam = Camera.main;
        float minX = -3.5f;
        float maxX = 3.5f;
        float spawnY = 6.5f;

        if (cam != null)
        {
            Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, 10));
            Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, 10));
            minX = bl.x + 0.6f;
            maxX = tr.x - 0.6f;
            spawnY = tr.y + 1.2f;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            nextSpawn -= Time.deltaTime;

            if (nextSpawn <= 0f)
            {
                nextSpawn = Random.Range(0.18f, 0.28f);
                float x = Random.Range(minX, maxX);
                Vector3 pos = new Vector3(x, spawnY, 0);

                if (ObjectPool.Instance != null)
                {
                    ObjectPool.Instance.SpawnRock(pos, Rock.AsteroidType.Small, Random.Range(4.5f, 6.5f), Random.Range(-120f, 120f));
                }
            }

            // 在暴雨中段投放一次护盾救命补给
            if (!droppedShield && elapsed >= duration * 0.45f)
            {
                droppedShield = true;
                ShieldPickup.Spawn(new Vector3(Random.Range(minX + 0.5f, maxX - 0.5f), spawnY, 0));
            }

            yield return null;
        }

        if (scroller != null)
        {
            scroller.SetSpeed(1.6f);
            scroller.SetColor(Color.white);
        }

        isEventRunning = false;
    }
}
