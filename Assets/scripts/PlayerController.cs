using UnityEngine;

/// <summary>
/// 玩家飞船控制器：多重射击系统（Normal/Dual/Triple）、能量护盾光环与碰撞吸收判定
/// </summary>
public class PlayerController : MonoBehaviour
{
    public enum WeaponMode
    {
        Normal,
        Dual,
        Triple
    }

    [Header("Movement")]
    public float moveSpeed = 7.5f;
    public float padding = 0.5f;

    [Header("Shooting")]
    public WeaponMode weaponMode = WeaponMode.Normal;
    public Transform firePoint;
    public float fireRate = 0.20f;

    [Header("Shield System")]
    private bool hasShield = false;
    private GameObject shieldAuraObj;
    private LineRenderer shieldLine;

    private float nextFireTime = 0f;
    private Camera mainCamera;
    private Vector2 minBounds;
    private Vector2 maxBounds;
    private bool isAlive = true;

    private SpriteRenderer spriteRenderer;
    private Collider2D col;

    public bool HasShield => hasShield;
    public bool IsAlive => isAlive;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        mainCamera = Camera.main;

        CreateShieldAura();
    }

    void Start()
    {
        CalculateBounds();
    }

    void Update()
    {
        if (!isAlive) return;

        HandleMovement();
        HandleShooting();
        HandleWeaponModeShortcuts();
        UpdateShieldVisual();
    }

    private void CreateShieldAura()
    {
        shieldAuraObj = new GameObject("ShieldAura");
        shieldAuraObj.transform.SetParent(transform);
        shieldAuraObj.transform.localPosition = Vector3.zero;

        shieldLine = shieldAuraObj.AddComponent<LineRenderer>();
        shieldLine.useWorldSpace = false;
        shieldLine.loop = true;
        shieldLine.startWidth = 0.07f;
        shieldLine.endWidth = 0.07f;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
        shieldLine.material = new Material(shader);

        Color cyan = new Color(0.15f, 0.9f, 1f, 0.85f);
        shieldLine.startColor = cyan;
        shieldLine.endColor = cyan;
        shieldLine.sortingOrder = 14;

        int segments = 28;
        shieldLine.positionCount = segments;
        float radius = 0.85f;
        for (int i = 0; i < segments; i++)
        {
            float angle = (i / (float)segments) * Mathf.PI * 2f;
            shieldLine.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0));
        }

        shieldAuraObj.SetActive(false);
    }

    private void UpdateShieldVisual()
    {
        if (shieldAuraObj != null && hasShield)
        {
            // 护盾微光旋转与呼吸动效
            shieldAuraObj.transform.Rotate(0, 0, 45f * Time.deltaTime);
            float pulseAlpha = 0.7f + Mathf.PingPong(Time.time * 2.5f, 0.3f);
            Color c = new Color(0.15f, 0.9f, 1f, pulseAlpha);
            shieldLine.startColor = c;
            shieldLine.endColor = c;
        }
    }

    public void AddShield()
    {
        hasShield = true;
        if (shieldAuraObj != null)
        {
            shieldAuraObj.SetActive(true);
        }
    }

    public void RemoveShield()
    {
        hasShield = false;
        if (shieldAuraObj != null)
        {
            shieldAuraObj.SetActive(false);
        }
    }

    private void HandleWeaponModeShortcuts()
    {
        // 快捷键 1/2/3 支持手动切枪测试，或根据当前分数自动提升武器等级
        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            weaponMode = WeaponMode.Normal;
        else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            weaponMode = WeaponMode.Dual;
        else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            weaponMode = WeaponMode.Triple;

        // 根据得分自动进阶武器体验
        if (GameManager.Instance != null)
        {
            int currentScore = GameManager.Instance.Score;
            if (currentScore >= 18 && weaponMode < WeaponMode.Triple)
            {
                weaponMode = WeaponMode.Triple;
            }
            else if (currentScore >= 7 && weaponMode < WeaponMode.Dual)
            {
                weaponMode = WeaponMode.Dual;
            }
        }
    }

    public void CalculateBounds()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null) return;

        float distance = Mathf.Abs(mainCamera.transform.position.z - transform.position.z);
        Vector3 bottomLeft = mainCamera.ViewportToWorldPoint(new Vector3(0, 0, distance));
        Vector3 topRight = mainCamera.ViewportToWorldPoint(new Vector3(1, 1, distance));

        minBounds = new Vector2(bottomLeft.x + padding, bottomLeft.y + padding);
        maxBounds = new Vector2(topRight.x - padding, topRight.y - padding);

        Projectile.SetMaxY(topRight.y + 1f);
        Rock.SetMinY(bottomLeft.y - 1.8f);
    }

    private void HandleMovement()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        Vector3 move = new Vector3(h, v, 0).normalized * moveSpeed * Time.deltaTime;
        Vector3 targetPos = transform.position + move;

        targetPos.x = Mathf.Clamp(targetPos.x, minBounds.x, maxBounds.x);
        targetPos.y = Mathf.Clamp(targetPos.y, minBounds.y, maxBounds.y);

        transform.position = targetPos;
    }

    private void HandleShooting()
    {
        bool shootKey = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J) || Input.GetButton("Fire1");
        if (shootKey && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireRate;
            Shoot();
        }
    }

    private void Shoot()
    {
        Vector3 basePos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 0.45f;

        switch (weaponMode)
        {
            case WeaponMode.Normal:
                SpawnBullet(basePos, 0f);
                break;

            case WeaponMode.Dual:
                // 双侧平行直射
                SpawnBullet(basePos + Vector3.left * 0.28f, 0f);
                SpawnBullet(basePos + Vector3.right * 0.28f, 0f);
                break;

            case WeaponMode.Triple:
                // 中央直射，左右带小角度微散射
                SpawnBullet(basePos, 0f);
                SpawnBullet(basePos + Vector3.left * 0.22f, -14f);
                SpawnBullet(basePos + Vector3.right * 0.22f, 14f);
                break;
        }

        if (GameManager.Instance != null)
        {
            GameManager.Instance.PlayShootSound();
        }
    }

    private void SpawnBullet(Vector3 position, float zAngle)
    {
        Quaternion rotation = Quaternion.Euler(0, 0, -zAngle);

        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.SpawnProjectile(position, rotation);
        }
        else
        {
            GameObject proj = new GameObject("Projectile_Fallback");
            proj.transform.position = position;
            proj.transform.rotation = rotation;
            var sr = proj.AddComponent<SpriteRenderer>();
            sr.sprite = GameResources.ProjectileSprite;
            sr.sortingOrder = 5;
            var pCol = proj.AddComponent<CircleCollider2D>();
            pCol.isTrigger = true;
            pCol.radius = 0.15f;
            var rb = proj.AddComponent<Rigidbody2D>();
            rb.isKinematic = true;
            proj.AddComponent<Projectile>();
        }
    }

    public void TakeHit()
    {
        if (!isAlive) return;

        if (hasShield)
        {
            // 护盾抵挡：消耗护盾，免除死亡，震动并播放破盾音效
            RemoveShield();
            CameraShake.Shake(0.18f, 0.25f);

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayShieldBreakSound();
            }
        }
        else
        {
            Die();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!isAlive) return;

        Rock rock = collision.GetComponent<Rock>();
        if (rock != null)
        {
            if (hasShield)
            {
                RemoveShield();
                rock.Die();
                CameraShake.Shake(0.18f, 0.25f);

                if (GameManager.Instance != null)
                {
                    GameManager.Instance.PlayShieldBreakSound();
                }
            }
            else
            {
                Die();
            }
            return;
        }

        EnemyProjectile enemyProj = collision.GetComponent<EnemyProjectile>();
        if (enemyProj != null)
        {
            enemyProj.Recycle();
            TakeHit();
            return;
        }

        BossController boss = collision.GetComponent<BossController>();
        if (boss != null)
        {
            TakeHit();
            return;
        }
    }

    public void Die()
    {
        if (!isAlive) return;
        isAlive = false;

        RemoveShield();

        if (spriteRenderer != null)
            spriteRenderer.enabled = false;
        if (col != null)
            col.enabled = false;

        ExplosionFX.Spawn(transform.position, 1.8f, ExplosionStyle.PlasmaBlue);
        CameraShake.Shake(0.38f, 0.45f);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied();
        }
    }
}
