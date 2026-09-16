using System.Collections;
using UnityEngine;

/// <summary>
/// 陨石障碍物组件：支持 Small、Normal、Large 三种陨石类型、HP与受击闪烁反馈、原生粒子爆炸与分级震动
/// </summary>
public class Rock : MonoBehaviour
{
    public enum AsteroidType
    {
        Small,
        Normal,
        Large
    }

    [Header("Asteroid State")]
    public AsteroidType asteroidType = AsteroidType.Normal;
    public int maxHp = 1;
    private int currentHp = 1;

    public float speed = 3.5f;
    public float rotationSpeed = 45f;
    public int scoreValue = 1;

    private static float cachedMinY = 0f;
    private static bool hasCachedMinY = false;

    private bool isDestroyed = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor = Color.white;
    private Coroutine flashCoroutine;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    public static void SetMinY(float minY)
    {
        cachedMinY = minY;
        hasCachedMinY = true;
    }

    public void Spawn(AsteroidType type, float fallSpeed, float rotSpeed)
    {
        this.asteroidType = type;
        this.isDestroyed = false;
        this.rotationSpeed = rotSpeed;

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        // 根据类型配置规格、血量、缩放与速度
        switch (type)
        {
            case AsteroidType.Small:
                transform.localScale = Vector3.one * 0.6f;
                maxHp = 1;
                scoreValue = 2;
                this.speed = fallSpeed * 1.4f; // 明显更快速
                if (spriteRenderer != null) spriteRenderer.color = new Color(0.95f, 0.85f, 0.8f);
                break;

            case AsteroidType.Large:
                transform.localScale = Vector3.one * 1.75f;
                maxHp = 3;
                scoreValue = 3;
                this.speed = Mathf.Max(fallSpeed * 0.65f, 1.8f); // 移动较慢
                if (spriteRenderer != null) spriteRenderer.color = new Color(0.85f, 0.82f, 0.80f);
                break;

            case AsteroidType.Normal:
            default:
                transform.localScale = Vector3.one * 1.0f;
                maxHp = 1;
                scoreValue = 1;
                this.speed = fallSpeed;
                if (spriteRenderer != null) spriteRenderer.color = Color.white;
                break;
        }

        originalColor = spriteRenderer != null ? spriteRenderer.color : Color.white;
        currentHp = maxHp;

        if (!hasCachedMinY)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cachedMinY = cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).y - 1.8f;
                hasCachedMinY = true;
            }
            else
            {
                cachedMinY = -10f;
            }
        }
    }

    void Update()
    {
        if (isDestroyed) return;

        transform.Translate(Vector3.down * speed * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

        if (transform.position.y < cachedMinY)
        {
            Recycle();
        }
    }

    public void TakeDamage()
    {
        if (isDestroyed) return;

        currentHp--;

        if (currentHp > 0)
        {
            // 巨型陨石中弹但尚未死亡：受击白闪与微击打反馈
            if (flashCoroutine != null) StopCoroutine(flashCoroutine);
            flashCoroutine = StartCoroutine(HitFlashRoutine());

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayShootSound();
            }
            CameraShake.Shake(0.06f, 0.08f);
        }
        else
        {
            Die();
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.red * 0.8f + Color.white * 0.2f;
            yield return new WaitForSeconds(0.08f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }
        flashCoroutine = null;
    }

    public void Die()
    {
        if (isDestroyed) return;
        isDestroyed = true;

        float visualScale = transform.localScale.x;

        // 1. 生成原生 ParticleSystem 爆炸火花与碎屑
        ExplosionFX.Spawn(transform.position, visualScale);

        // 2. 根据陨石体量触发分级屏幕微震动
        switch (asteroidType)
        {
            case AsteroidType.Small:
                CameraShake.Shake(0.08f, 0.08f);
                break;
            case AsteroidType.Large:
                CameraShake.Shake(0.22f, 0.28f);
                break;
            case AsteroidType.Normal:
            default:
                CameraShake.Shake(0.12f, 0.15f);
                break;
        }

        // 3. 计分与音效
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(scoreValue);
            GameManager.Instance.PlayExplosionSound();
        }

        // 4. 概率掉落护盾道具
        float dropChance = (asteroidType == AsteroidType.Large) ? 0.35f : 0.12f;
        if (Random.value < dropChance)
        {
            ShieldPickup.Spawn(transform.position);
        }

        Recycle();
    }

    public void Recycle()
    {
        isDestroyed = true;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.RecycleRock(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
