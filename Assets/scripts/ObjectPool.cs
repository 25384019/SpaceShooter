using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 高性能零 GC 对象池管理器，统筹管理子弹与陨石的生命周期
/// 消除运行期间高频产生/销毁 GameObject 以及物理组件注册注销的巨大开销
/// </summary>
public class ObjectPool : MonoBehaviour
{
    public static ObjectPool Instance { get; private set; }

    [Header("Pool Capacities")]
    public int initialProjectileCount = 30;
    public int initialEnemyProjectileCount = 20;
    public int initialRockCount = 18;

    private readonly Queue<Projectile> projectilePool = new Queue<Projectile>();
    private readonly Queue<EnemyProjectile> enemyProjectilePool = new Queue<EnemyProjectile>();
    private readonly Queue<Rock> rockPool = new Queue<Rock>();

    private Transform projectileRoot;
    private Transform enemyProjectileRoot;
    private Transform rockRoot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 确保预加载资源已经准备完毕
        GameResources.Initialize();

        // 创建池层级节点，保持 Hierarchy 视图整洁
        GameObject pRootObj = new GameObject("[Pool_Projectiles]");
        pRootObj.transform.SetParent(transform);
        projectileRoot = pRootObj.transform;

        GameObject epRootObj = new GameObject("[Pool_EnemyProjectiles]");
        epRootObj.transform.SetParent(transform);
        enemyProjectileRoot = epRootObj.transform;

        GameObject rRootObj = new GameObject("[Pool_Rocks]");
        rRootObj.transform.SetParent(transform);
        rockRoot = rRootObj.transform;

        PrewarmPools();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void PrewarmPools()
    {
        // 预热玩家子弹池
        for (int i = 0; i < initialProjectileCount; i++)
        {
            Projectile p = CreateNewProjectile();
            p.gameObject.SetActive(false);
            projectilePool.Enqueue(p);
        }

        // 预热敌方/Boss子弹池
        for (int i = 0; i < initialEnemyProjectileCount; i++)
        {
            EnemyProjectile ep = CreateNewEnemyProjectile();
            ep.gameObject.SetActive(false);
            enemyProjectilePool.Enqueue(ep);
        }

        // 预热陨石池
        for (int i = 0; i < initialRockCount; i++)
        {
            Rock r = CreateNewRock();
            r.gameObject.SetActive(false);
            rockPool.Enqueue(r);
        }
    }

    private Projectile CreateNewProjectile()
    {
        GameObject projObj = new GameObject("Projectile_Pooled");
        projObj.transform.SetParent(projectileRoot);

        var sr = projObj.AddComponent<SpriteRenderer>();
        sr.sprite = GameResources.ProjectileSprite;
        sr.sortingOrder = 5;

        var col = projObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.15f;

        var rb = projObj.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        Projectile projectile = projObj.AddComponent<Projectile>();
        return projectile;
    }

    private EnemyProjectile CreateNewEnemyProjectile()
    {
        GameObject epObj = new GameObject("EnemyProjectile_Pooled");
        epObj.transform.SetParent(enemyProjectileRoot);

        var sr = epObj.AddComponent<SpriteRenderer>();
        sr.sprite = GameResources.ProjectileSprite;
        sr.color = new Color(1f, 0.25f, 0.2f, 1f); // 警示赤红色
        sr.sortingOrder = 6;

        var col = epObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.16f;

        var rb = epObj.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        EnemyProjectile enemyProjectile = epObj.AddComponent<EnemyProjectile>();
        return enemyProjectile;
    }

    private Rock CreateNewRock()
    {
        GameObject rockObj = new GameObject("Rock_Pooled");
        rockObj.transform.SetParent(rockRoot);

        var sr = rockObj.AddComponent<SpriteRenderer>();
        sr.sprite = GameResources.RockSprite;
        sr.sortingOrder = 3;

        var col = rockObj.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.4f;

        var rb = rockObj.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        Rock rock = rockObj.AddComponent<Rock>();
        return rock;
    }

    // ==================== 子弹池借取与回收 ====================

    public Projectile SpawnProjectile(Vector3 position)
    {
        return SpawnProjectile(position, Quaternion.identity);
    }

    public Projectile SpawnProjectile(Vector3 position, Quaternion rotation)
    {
        Projectile proj;
        if (projectilePool.Count > 0)
        {
            proj = projectilePool.Dequeue();
        }
        else
        {
            proj = CreateNewProjectile();
        }

        proj.transform.position = position;
        proj.transform.rotation = rotation;
        proj.gameObject.SetActive(true);
        proj.Init();
        return proj;
    }

    public void RecycleProjectile(Projectile proj)
    {
        if (proj == null) return;
        proj.gameObject.SetActive(false);
        proj.transform.SetParent(projectileRoot);
        projectilePool.Enqueue(proj);
    }

    // ==================== 敌方子弹池借取与回收 ====================

    public EnemyProjectile SpawnEnemyProjectile(Vector3 position, Quaternion rotation, float speed = 7.5f)
    {
        EnemyProjectile proj;
        if (enemyProjectilePool.Count > 0)
        {
            proj = enemyProjectilePool.Dequeue();
        }
        else
        {
            proj = CreateNewEnemyProjectile();
        }

        proj.transform.position = position;
        proj.transform.rotation = rotation;
        proj.gameObject.SetActive(true);
        proj.Init(speed);
        return proj;
    }

    public void RecycleEnemyProjectile(EnemyProjectile proj)
    {
        if (proj == null) return;
        proj.gameObject.SetActive(false);
        proj.transform.SetParent(enemyProjectileRoot);
        enemyProjectilePool.Enqueue(proj);
    }

    // ==================== 陨石池借取与回收 ====================

    public Rock SpawnRock(Vector3 position, Rock.AsteroidType type, float speed, float rotationSpeed)
    {
        Rock rock;
        if (rockPool.Count > 0)
        {
            rock = rockPool.Dequeue();
        }
        else
        {
            rock = CreateNewRock();
        }

        rock.transform.position = position;
        rock.gameObject.SetActive(true);
        rock.Spawn(type, speed, rotationSpeed);
        return rock;
    }

    public void RecycleRock(Rock rock)
    {
        if (rock == null) return;
        rock.gameObject.SetActive(false);
        rock.transform.SetParent(rockRoot);
        rockPool.Enqueue(rock);
    }
}
