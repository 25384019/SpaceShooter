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
    public int initialRockCount = 18;

    private readonly Queue<Projectile> projectilePool = new Queue<Projectile>();
    private readonly Queue<Rock> rockPool = new Queue<Rock>();

    private Transform projectileRoot;
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
        // 预热子弹池
        for (int i = 0; i < initialProjectileCount; i++)
        {
            Projectile p = CreateNewProjectile();
            p.gameObject.SetActive(false);
            projectilePool.Enqueue(p);
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
