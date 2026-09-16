using UnityEngine;

/// <summary>
/// 陨石生成器：基于对象池随机派发 Small / Normal / Large 三种陨石类型，并随时间推进平滑加速波次
/// </summary>
public class RockSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public float baseSpawnInterval = 0.95f;
    public float minSpawnInterval = 0.35f;

    [Header("Speed Settings")]
    public float minFallSpeed = 2.8f;
    public float maxFallSpeed = 4.8f;

    private float timer = 0f;
    private Camera cam;
    private float minX;
    private float maxX;
    private float spawnY;
    private int lastScreenWidth = 0;
    private int lastScreenHeight = 0;

    void Start()
    {
        cam = Camera.main;
        UpdateSpawnBounds();
    }

    private void UpdateSpawnBounds()
    {
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        Vector3 bl = cam.ViewportToWorldPoint(new Vector3(0, 0, 10));
        Vector3 tr = cam.ViewportToWorldPoint(new Vector3(1, 1, 10));

        minX = bl.x + 0.8f;
        maxX = tr.x - 0.8f;
        spawnY = tr.y + 1.5f;
    }

    void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver)
            return;

        if (Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            UpdateSpawnBounds();
        }

        // 难度随时间平滑递增
        float currentInterval = baseSpawnInterval;
        if (GameManager.Instance != null)
        {
            float progress = GameManager.Instance.GameProgress;
            currentInterval = Mathf.Lerp(baseSpawnInterval, minSpawnInterval, progress);
        }

        timer += Time.deltaTime;
        if (timer >= currentInterval)
        {
            timer = 0f;
            SpawnRandomRock();
        }
    }

    private void SpawnRandomRock()
    {
        float spawnX = Random.Range(minX, maxX);
        Vector3 spawnPos = new Vector3(spawnX, spawnY, 0);

        float fallSpeed = Random.Range(minFallSpeed, maxFallSpeed);
        float rotSpeed = Random.Range(-90f, 90f);

        // 随机陨石规格：60% 普通，25% 小型高速，15% 巨型高血量
        float roll = Random.value;
        Rock.AsteroidType type;
        if (roll < 0.25f)
        {
            type = Rock.AsteroidType.Small;
        }
        else if (roll < 0.40f)
        {
            type = Rock.AsteroidType.Large;
        }
        else
        {
            type = Rock.AsteroidType.Normal;
        }

        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.SpawnRock(spawnPos, type, fallSpeed, rotSpeed);
        }
        else
        {
            GameObject r = new GameObject("Rock_Fallback");
            r.transform.position = spawnPos;
            var sr = r.AddComponent<SpriteRenderer>();
            sr.sprite = GameResources.RockSprite;
            sr.sortingOrder = 3;
            var col = r.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.4f;
            var rb = r.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            Rock rock = r.AddComponent<Rock>();
            rock.Spawn(type, fallSpeed, rotSpeed);
        }
    }
}
