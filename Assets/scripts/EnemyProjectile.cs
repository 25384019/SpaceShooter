using UnityEngine;

/// <summary>
/// 敌方/Boss 子弹控制组件：向下或沿朝向飞行，碰撞玩家判定伤害，支持对象池零分配循环回收
/// </summary>
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 7.5f;
    private static float cachedMinY = -8f;
    private static bool hasCachedBounds = false;

    private bool isRecycled = false;

    public void Init(float bulletSpeed)
    {
        this.speed = bulletSpeed;
        this.isRecycled = false;

        if (!hasCachedBounds)
        {
            Camera cam = Camera.main;
            if (cam != null)
            {
                cachedMinY = cam.ViewportToWorldPoint(new Vector3(0, 0, 0)).y - 1f;
                hasCachedBounds = true;
            }
            else
            {
                cachedMinY = -8f;
            }
        }
    }

    void Update()
    {
        if (isRecycled) return;

        // 沿自身朝向飞行
        transform.position += transform.up * speed * Time.deltaTime;

        if (transform.position.y < cachedMinY || transform.position.y > 10f || Mathf.Abs(transform.position.x) > 12f)
        {
            Recycle();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isRecycled) return;

        PlayerController player = collision.GetComponent<PlayerController>();
        if (player != null && player.IsAlive)
        {
            player.TakeHit();
            Recycle();
        }
    }

    public void Recycle()
    {
        if (isRecycled) return;
        isRecycled = true;

        if (ObjectPool.Instance != null)
        {
            ObjectPool.Instance.RecycleEnemyProjectile(this);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
