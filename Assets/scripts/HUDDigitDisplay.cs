using UnityEngine;

/// <summary>
/// 顶部 HUD 倒计时数字渲染器：基于脏标记检查驱动，使用预缓存数字精灵，消除每帧无效计算与渲染更新
/// </summary>
public class HUDDigitDisplay : MonoBehaviour
{
    private SpriteRenderer tensRenderer;
    private SpriteRenderer unitsRenderer;
    private int lastDisplayedValue = -1;

    void Start()
    {
        GameResources.Initialize();
        CreateDigitObjects();
    }

    private void CreateDigitObjects()
    {
        Camera cam = Camera.main;
        float topY = 4.2f;
        if (cam != null)
        {
            topY = cam.ViewportToWorldPoint(new Vector3(0.5f, 0.92f, 10)).y;
        }

        GameObject root = new GameObject("DigitDisplay_Root");
        root.transform.position = new Vector3(0, topY, 0);

        GameObject tensObj = new GameObject("Tens");
        tensObj.transform.SetParent(root.transform);
        tensObj.transform.localPosition = new Vector3(-0.35f, 0, 0);
        tensObj.transform.localScale = Vector3.one * 1.5f;
        tensRenderer = tensObj.AddComponent<SpriteRenderer>();
        tensRenderer.sortingOrder = 20;

        GameObject unitsObj = new GameObject("Units");
        unitsObj.transform.SetParent(root.transform);
        unitsObj.transform.localPosition = new Vector3(0.35f, 0, 0);
        unitsObj.transform.localScale = Vector3.one * 1.5f;
        unitsRenderer = unitsObj.AddComponent<SpriteRenderer>();
        unitsRenderer.sortingOrder = 20;

        // 默认初始化显示
        if (lastDisplayedValue >= 0)
        {
            int val = lastDisplayedValue;
            lastDisplayedValue = -1;
            UpdateDisplay(val);
        }
    }

    /// <summary>
    /// 更新倒计时数字显示，内部包含脏标记比对，只有数字发生变化时才更新 Sprite
    /// </summary>
    public void UpdateDisplay(int value)
    {
        value = Mathf.Clamp(value, 0, 99);

        // 脏标记过滤：秒数不变时不执行任何 Sprite 替换逻辑
        if (value == lastDisplayedValue) return;
        lastDisplayedValue = value;

        int tens = value / 10;
        int units = value % 10;

        Sprite[] digits = GameResources.DigitSprites;
        if (digits != null && digits.Length >= 10)
        {
            if (tensRenderer != null && digits[tens] != null)
            {
                tensRenderer.sprite = digits[tens];
            }
            if (unitsRenderer != null && digits[units] != null)
            {
                unitsRenderer.sprite = digits[units];
            }
        }
    }
}
