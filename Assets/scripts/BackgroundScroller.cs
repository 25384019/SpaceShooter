using UnityEngine;

/// <summary>
/// 无缝星空背景滚动器：精确计算精灵边界，自愈丢失材质，消除拼缝抖动，实现平滑纵深飞行效果
/// </summary>
public class BackgroundScroller : MonoBehaviour
{
    [Header("Scroll Speed")]
    public float scrollSpeed = 1.6f;

    private float backgroundHeight = 10f;
    private float resetThresholdY = 0f;
    private Transform bg1;
    private Transform bg2;
    private SpriteRenderer sr1;
    private SpriteRenderer sr2;

    public void SetSpeed(float newSpeed)
    {
        scrollSpeed = newSpeed;
    }

    public void SetColor(Color tint)
    {
        if (sr1 != null) sr1.color = tint;
        if (sr2 != null) sr2.color = tint;
    }

    void Start()
    {
        sr1 = GetComponent<SpriteRenderer>();
        if (sr1 != null)
        {
            // 自愈保护：若 Sprite 丢失，自动使用预加载的 StarsSprite
            if (sr1.sprite == null)
            {
                GameResources.Initialize();
                sr1.sprite = GameResources.StarsSprite;
            }

            if (sr1.sprite != null)
            {
                backgroundHeight = sr1.bounds.size.y;
                if (backgroundHeight <= 0.1f) backgroundHeight = 10f;

                resetThresholdY = transform.position.y - backgroundHeight;
                bg1 = transform;

                // 动态创建第二张背景贴图实现无缝首尾相接循环
                GameObject secondBg = new GameObject("Background_LoopCopy");
                secondBg.transform.position = transform.position + Vector3.up * backgroundHeight;
                secondBg.transform.localScale = transform.localScale;
                sr2 = secondBg.AddComponent<SpriteRenderer>();
                sr2.sprite = sr1.sprite;
                sr2.sortingOrder = sr1.sortingOrder;
                sr2.color = sr1.color;
                bg2 = secondBg.transform;
            }
        }
    }

    void Update()
    {
        if (bg1 == null || bg2 == null) return;

        float moveY = scrollSpeed * Time.deltaTime;
        bg1.Translate(Vector3.down * moveY, Space.World);
        bg2.Translate(Vector3.down * moveY, Space.World);

        // 达到重置阀值时，精准对接到另一张背景正上方，杜绝浮点漂移缝隙
        if (bg1.position.y <= resetThresholdY)
        {
            bg1.position = new Vector3(bg1.position.x, bg2.position.y + backgroundHeight, bg1.position.z);
        }

        if (bg2.position.y <= resetThresholdY)
        {
            bg2.position = new Vector3(bg2.position.x, bg1.position.y + backgroundHeight, bg2.position.z);
        }
    }
}
