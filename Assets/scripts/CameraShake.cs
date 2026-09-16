using System.Collections;
using UnityEngine;

/// <summary>
/// 可复用的独立相机震动组件，支持不同强度的震动与平滑衰减
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    private Vector3 originalPos;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
        originalPos = transform.position;
    }

    public static void Shake(float duration, float magnitude)
    {
        if (Instance == null)
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                Instance = mainCam.GetComponent<CameraShake>();
                if (Instance == null)
                {
                    Instance = mainCam.gameObject.AddComponent<CameraShake>();
                }
            }
        }

        if (Instance != null)
        {
            Instance.DoShake(duration, magnitude);
        }
    }

    public void DoShake(float duration, float magnitude)
    {
        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            transform.position = originalPos;
        }
        shakeCoroutine = StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float damper = 1.0f - (elapsed / duration);
            float offsetX = Random.Range(-1f, 1f) * magnitude * damper;
            float offsetY = Random.Range(-1f, 1f) * magnitude * damper;

            transform.position = originalPos + new Vector3(offsetX, offsetY, 0);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = originalPos;
        shakeCoroutine = null;
    }
}
