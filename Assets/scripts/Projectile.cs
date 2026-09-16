using UnityEngine;

/// <summary>
/// 子弹控制组件，支持任意飞行朝向、对象池循环复用与零内存分配
/// </summary>
public class Projectile : MonoBehaviour
{
    public float speed = 12f;
    private static float cachedMaxY = 0f;
    private static bool hasCachedMaxY = false;

    private bool isRecycled = false;

    public static void SetMaxY(float maxY)
    {
        cachedMaxY = maxY;
        hasCachedMaxY = true;
    }

    public void Init()
    {
        isRecycled = false;

        if (!hasCachedMaxY)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cachedMaxY = cam.ViewportToWorldPoint(new Vector3(0, 1, 0)).y + 1f;
                hasCachedMaxY = true;
            }
            else
            {
                cachedMaxY = 10f;
            }
        }
    }

    void Update()
    {
        if (isRecycled) return;

        // 沿着自身朝向飞行，支持多重射击散射角度
        transform.position += transform.up * speed * Time.deltaTime;

        if (transform.position.y > cachedMaxY || Mathf.Abs(transform.position.x) > 15f || transform.position.y < -8f)
        {
            Recycle();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isRecycled) return;

        Rock rock = collision.GetComponent<Rock>();
        if (rock != null)
        {
            rock.TakeDamage();
            Recycle();
            return;
        }

        BossController boss = collision.GetComponent<BossController>();
        if (boss != null)
        {
            boss.TakeDamage(1);
            Recycle();
            return;
        }
    }

    public void Recycle()
    {
        if (isRecycled) return;
        isRecycled = true;

        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.RecycleProjectile(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
