using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 战主力旗舰控制器：多阶段行为状态机（巡航/狂暴/召唤/弹幕）、原生顶部血条、连环大爆炸与破关结算
/// </summary>
public class BossController : MonoBehaviour
{
    public enum BossState
    {
        Entering,
        Phase1_Normal,
        Phase2_Enraged,
        Dying,
        Defeated
    }

    [Header("Boss Attributes")]
    public int maxHp = 25;
    public int currentHp = 25;
    public BossState state = BossState.Entering;

    [Header("Movement & Positioning")]
    public float targetY = 3.2f;
    public float enterSpeed = 2.2f;
    public float patrolSpeed = 1.4f;
    public float patrolRange = 3.2f;

    [Header("Attack Settings")]
    public float phase1FireInterval = 1.8f;
    public float phase2FireInterval = 1.1f;
    public float summonInterval = 4.5f;

    private float fireTimer = 0f;
    private float summonTimer = 0f;
    private float patrolTime = 0f;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;
    private Color normalBossColor = new Color(0.92f, 0.28f, 0.28f, 1f);
    private Color enragedBossColor = new Color(1f, 0.12f, 0.08f, 1f);
    private Coroutine flashCoroutine;

    // --- OnGUI 零分配血条缓存 ---
    private Texture2D bgTexture;
    private Texture2D hpTexture;
    private GUIStyle bossTitleStyle;
    private GUIStyle hpTextStyle;
    private Rect hpBarBgRect;
    private Rect hpBarFillRect;
    private Rect titleRect;
    private Rect textRect;

    public bool IsAlive => state != BossState.Dying && state != BossState.Defeated;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 复用现有飞船贴图，反向朝下，缩放2.3倍，调配敌军重装母舰外观
        if (spriteRenderer.sprite == null)
        {
            spriteRenderer.sprite = GameResources.PlayerSprite;
        }
        transform.localScale = Vector3.one * 2.3f;
        transform.rotation = Quaternion.Euler(0, 0, 180f);
        spriteRenderer.color = normalBossColor;
        spriteRenderer.sortingOrder = 9;

        col = GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circle = gameObject.AddComponent<CircleCollider2D>();
            circle.isTrigger = true;
            circle.radius = 0.55f;
            col = circle;
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
        }

        currentHp = maxHp;
        state = BossState.Entering;

        SetupGuiResources();
    }

    private void SetupGuiResources()
    {
        // 动态创建 1x1 像素纯色纹理作为血条背景与前景（零垃圾分配）
        bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, new Color(0.12f, 0.12f, 0.15f, 0.85f));
        bgTexture.Apply();

        hpTexture = new Texture2D(1, 1);
        hpTexture.SetPixel(0, 0, new Color(0.95f, 0.2f, 0.2f, 0.95f));
        hpTexture.Apply();

        bossTitleStyle = new GUIStyle();
        bossTitleStyle.alignment = TextAnchor.MiddleCenter;
        bossTitleStyle.fontSize = 17;
        bossTitleStyle.fontStyle = FontStyle.Bold;
        bossTitleStyle.normal.textColor = new Color(1f, 0.85f, 0.25f);

        hpTextStyle = new GUIStyle();
        hpTextStyle.alignment = TextAnchor.MiddleCenter;
        hpTextStyle.fontSize = 12;
        hpTextStyle.fontStyle = FontStyle.Bold;
        hpTextStyle.normal.textColor = Color.white;
    }

    void OnDestroy()
    {
        if (bgTexture != null) Destroy(bgTexture);
        if (hpTexture != null) Destroy(hpTexture);
    }

    void Update()
    {
        switch (state)
        {
            case BossState.Entering:
                UpdateEntering();
                break;
            case BossState.Phase1_Normal:
                UpdateCombat(phase1FireInterval, 1.0f);
                break;
            case BossState.Phase2_Enraged:
                UpdateCombat(phase2FireInterval, 1.5f);
                UpdateSummons();
                break;
            case BossState.Dying:
            case BossState.Defeated:
                break;
        }
    }

    private void UpdateEntering()
    {
        // 平滑从屏幕上方驶入战斗战位
        transform.position = Vector3.MoveTowards(
            transform.position,
            new Vector3(0, targetY, 0),
            enterSpeed * Time.deltaTime
        );

        if (Mathf.Abs(transform.position.y - targetY) < 0.05f)
        {
            transform.position = new Vector3(0, targetY, 0);
            state = BossState.Phase1_Normal;
        }
    }

    private void UpdateCombat(float fireInterval, float speedMultiplier)
    {
        // 巡航平移
        patrolTime += Time.deltaTime * patrolSpeed * speedMultiplier;
        float newX = Mathf.Sin(patrolTime) * patrolRange;
        float newY = targetY + (state == BossState.Phase2_Enraged ? Mathf.Sin(patrolTime * 2.2f) * 0.25f : 0f);
        transform.position = new Vector3(newX, newY, 0);

        // 攻击计时
        fireTimer += Time.deltaTime;
        if (fireTimer >= fireInterval)
        {
            fireTimer = 0f;
            if (state == BossState.Phase1_Normal)
            {
                FirePhase1();
            }
            else
            {
                FirePhase2();
            }
        }
    }

    private void UpdateSummons()
    {
        summonTimer += Time.deltaTime;
        if (summonTimer >= summonInterval)
        {
            summonTimer = 0f;
            SummonMinionRocks();
        }
    }

    private void FirePhase1()
    {
        // 双联平行激光弹
        Vector3 leftPos = transform.position + Vector3.left * 0.55f + Vector3.down * 0.4f;
        Vector3 rightPos = transform.position + Vector3.right * 0.55f + Vector3.down * 0.4f;
        Quaternion rot = Quaternion.Euler(0, 0, 180f);

        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.SpawnEnemyProjectile(leftPos, rot, 6.8f);
            ObjectPool.Instance.SpawnEnemyProjectile(rightPos, rot, 6.8f);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayShootSound();
        }
    }

    private void FirePhase2()
    {
        // 狂暴 3 向散射弹幕
        Vector3 basePos = transform.position + Vector3.down * 0.5f;

        Quaternion rotMid = Quaternion.Euler(0, 0, 180f);
        Quaternion rotLeft = Quaternion.Euler(0, 0, 180f - 18f);
        Quaternion rotRight = Quaternion.Euler(0, 0, 180f + 18f);

        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.SpawnEnemyProjectile(basePos, rotMid, 7.8f);
            ObjectPool.Instance.SpawnEnemyProjectile(basePos + Vector3.left * 0.35f, rotLeft, 7.4f);
            ObjectPool.Instance.SpawnEnemyProjectile(basePos + Vector3.right * 0.35f, rotRight, 7.4f);
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayShootSound();
        }
    }

    private void SummonMinionRocks()
    {
        // 召唤 2 颗快速小型陨石护航
        if (ObjectPool.Instance != null)
        {
            Vector3 spawn1 = new Vector3(transform.position.x - 2.0f, transform.position.y + 1.2f, 0);
            Vector3 spawn2 = new Vector3(transform.position.x + 2.0f, transform.position.y + 1.2f, 0);
            ObjectPool.Instance.SpawnRock(spawn1, Rock.AsteroidType.Small, 4.2f, 80f);
            ObjectPool.Instance.SpawnRock(spawn2, Rock.AsteroidType.Small, 4.2f, -80f);
        }
    }

    public void TakeDamage(int damage)
    {
        if (!IsAlive) return;

        currentHp -= damage;
        if (currentHp < 0) currentHp = 0;

        // 受击白炽闪烁反馈
        if (flashCoroutine != null) StopCoroutine(flashCoroutine);
        flashCoroutine = StartCoroutine(HitFlashRoutine());

        // 轻微击中微震
        CameraShake.Shake(0.06f, 0.10f);

        // 阶段转换判定（进入狂暴阶段）
        if (state == BossState.Phase1_Normal && currentHp <= maxHp / 2)
        {
            state = BossState.Phase2_Enraged;
            spriteRenderer.color = enragedBossColor;
            ExplosionFX.Spawn(transform.position, 1.5f);
            CameraShake.Shake(0.25f, 0.30f);
        }

        if (currentHp <= 0)
        {
            Die();
        }
    }

    private IEnumerator HitFlashRoutine()
    {
        spriteRenderer.color = Color.white;
        yield return new WaitForSeconds(0.06f);
        spriteRenderer.color = state == BossState.Phase2_Enraged ? enragedBossColor : normalBossColor;
        flashCoroutine = null;
    }

    private void Die()
    {
        if (state == BossState.Dying || state == BossState.Defeated) return;
        state = BossState.Dying;

        if (col != null) col.enabled = false;
        StartCoroutine(DeathSequenceRoutine());
    }

    private IEnumerator DeathSequenceRoutine()
    {
        // 连环大爆炸表现与渐进式震屏反馈
        for (int i = 0; i < 5; i++)
        {
            Vector3 randomOffset = new Vector3(Random.Range(-0.8f, 0.8f), Random.Range(-0.6f, 0.6f), 0);
            ExplosionFX.Spawn(transform.position + randomOffset, 2.0f, ExplosionStyle.DreadnoughtNuke);
            CameraShake.Shake(0.20f, 0.28f);
            if (GameManager.Instance != null) GameManager.Instance.PlayExplosionSound();
            yield return new WaitForSeconds(0.22f);
        }

        // 最终震撼大引爆
        ExplosionFX.Spawn(transform.position, 3.8f, ExplosionStyle.DreadnoughtNuke);
        CameraShake.Shake(0.65f, 0.50f);
        if (GameManager.Instance != null) GameManager.Instance.PlayExplosionSound();

        // 掉落 2 个强力护盾道具回馈玩家
        ShieldPickup.Spawn(transform.position + Vector3.left * 0.9f);
        ShieldPickup.Spawn(transform.position + Vector3.right * 0.9f);

        // 结算奖励丰厚得分 (+15分)
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(15);
        }

        // 任务契约计数
        if (MissionManager.Instance != null)
        {
            MissionManager.Instance.OnBossKilled();
        }

        state = BossState.Defeated;
        if (spriteRenderer != null) spriteRenderer.enabled = false;

        // 通知波次管理器 Boss 击杀完成
        if (WaveManager.Instance != null)
        {
            WaveManager.Instance.OnBossDefeated();
        }

        yield return new WaitForSeconds(1.0f);
        Destroy(gameObject);
    }

    void OnGUI()
    {
        if (!IsAlive) return;

        // 居中自适应绘制顶部 Boss 血条
        float barWidth = Mathf.Min(Screen.width * 0.6f, 380f);
        float barHeight = 16f;
        float barX = (Screen.width - barWidth) * 0.5f;
        float barY = 28f;

        titleRect = new Rect(barX, barY - 24f, barWidth, 22f);
        hpBarBgRect = new Rect(barX, barY, barWidth, barHeight);

        float fillPct = Mathf.Clamp01((float)currentHp / maxHp);
        hpBarFillRect = new Rect(barX, barY, barWidth * fillPct, barHeight);
        textRect = new Rect(barX, barY - 1f, barWidth, barHeight);

        // 标题
        string bossTitle = state == BossState.Phase2_Enraged 
            ? "★ WARNING: ENRAGED DREADNOUGHT ★" 
            : "★ BOSS: DREADNOUGHT FLAGSHIP ★";
        GUI.Label(titleRect, bossTitle, bossTitleStyle);

        // 背景槽与血条
        if (bgTexture != null) GUI.DrawTexture(hpBarBgRect, bgTexture);
        if (hpTexture != null) GUI.DrawTexture(hpBarFillRect, hpTexture);

        // 实时数字
        GUI.Label(textRect, $"{currentHp} / {maxHp}", hpTextStyle);
    }
}
