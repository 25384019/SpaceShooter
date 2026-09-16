using UnityEngine;

/// <summary>
/// 能量护盾掉落道具：自然旋转并向下缓慢漂移，接触玩家后赋予 1 层能量护盾
/// </summary>
public class ShieldPickup : MonoBehaviour
{
    public float fallSpeed = 1.8f;
    public float rotationSpeed = 60f;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D col;

    void Awake()
    {
        col = gameObject.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.35f;

        var rb = gameObject.AddComponent<Rigidbody2D>();
        rb.isKinematic = true;

        spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        // 复用现有子弹贴图，调为微光蔚蓝色，并稍作放大作为能量徽记
        spriteRenderer.sprite = GameResources.ProjectileSprite;
        spriteRenderer.color = new Color(0.2f, 0.9f, 1.0f, 0.95f);
        spriteRenderer.sortingOrder = 12;
        transform.localScale = Vector3.one * 1.8f;
    }

    void Update()
    {
        transform.Translate(Vector3.down * fallSpeed * Time.deltaTime, Space.World);
        transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);

        // 微妙呼吸微光
        float pulse = 0.8f + Mathf.PingPong(Time.time * 2f, 0.4f);
        spriteRenderer.color = new Color(0.2f, 0.9f, 1.0f, pulse);

        if (transform.position.y < -7f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.GetComponent<PlayerController>();
        if (player != null && player.IsAlive)
        {
            player.AddShield();

            if (GameManager.Instance != null)
            {
                GameManager.Instance.PlayShieldPickupSound();
            }

            Destroy(gameObject);
        }
    }

    public static ShieldPickup Spawn(Vector3 position)
    {
        GameObject itemObj = new GameObject("Shield_Pickup");
        itemObj.transform.position = position;
        return itemObj.AddComponent<ShieldPickup>();
    }
}
