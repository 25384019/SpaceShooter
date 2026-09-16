using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 爆炸表现风格定义
/// </summary>
public enum ExplosionStyle
{
    Standard,       // 经典高亮金橙烈焰冲击波
    FieryRock,      // 陨石碎屑爆裂
    PlasmaBlue,     // 飞船战损等离子电弧
    DreadnoughtNuke // Boss 旗舰毁灭性核爆
}

/// <summary>
/// 超级优化五层电影级粒子爆炸特效总控系统：
/// 1. Core Flash（中心超新星瞬时强光闪烁）
/// 2. Shockwave Ring（高科技膨胀冲击波光环）
/// 3. Plasma Fireball（高能等离子火球烈焰团）
/// 4. High-Velocity Sparks（360° 高速发光飞溅火星流）
/// 5. Smoke & Debris（物理重力翻滚残骸碎屑）
/// 采用独立对象池与层次缩放，实现真正 0-GC 零运行时分配与极致视效打击感
/// </summary>
public static class ExplosionFX
{
    private static Material glowMaterial;
    private static Material ringMaterial;
    private static Material solidMaterial;

    private static readonly Queue<PooledExplosionInstance> pool = new Queue<PooledExplosionInstance>();
    private static GameObject poolRoot;
    private static bool isInitialized = false;
    private const int PrewarmCount = 8;

    public static void InitializePool()
    {
        if (isInitialized && poolRoot != null) return;
        isInitialized = true;

        EnsureMaterials();

        poolRoot = new GameObject("[Pool_ExplosionFX]");
        Object.DontDestroyOnLoad(poolRoot);

        for (int i = 0; i < PrewarmCount; i++)
        {
            PooledExplosionInstance inst = CreateNewInstance();
            inst.gameObject.SetActive(false);
            pool.Enqueue(inst);
        }
    }

    private static void EnsureMaterials()
    {
        if (glowMaterial != null && ringMaterial != null) return;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");

        // 1. 发光柔和光晕材质
        Texture2D glowTex = CreateGlowTexture();
        glowMaterial = new Material(shader);
        if (glowMaterial.HasProperty("_MainTex")) glowMaterial.mainTexture = glowTex;
        if (glowMaterial.HasProperty("_BaseMap")) glowMaterial.SetTexture("_BaseMap", glowTex);

        // 2. 冲击波环状光圈材质
        Texture2D ringTex = CreateRingTexture();
        ringMaterial = new Material(shader);
        if (ringMaterial.HasProperty("_MainTex")) ringMaterial.mainTexture = ringTex;
        if (ringMaterial.HasProperty("_BaseMap")) ringMaterial.SetTexture("_BaseMap", ringTex);

        // 3. 碎片实体材质
        solidMaterial = new Material(shader);
    }

    private static Texture2D CreateGlowTexture()
    {
        int size = 32;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy));
                float alpha = Mathf.Clamp01(1f - (d * d));
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    private static Texture2D CreateRingTexture()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        float center = (size - 1) * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float d = Mathf.Sqrt(dx * dx + dy * dy);

                // 在半径 0.7 处形成宽度 0.25 的平滑亮环
                float ringDist = Mathf.Abs(d - 0.72f);
                float alpha = Mathf.Clamp01(1f - (ringDist / 0.22f));
                alpha = alpha * alpha; // 二次平滑
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        return tex;
    }

    private static PooledExplosionInstance CreateNewInstance()
    {
        EnsureMaterials();

        GameObject rootObj = new GameObject("ExplosionFX_Pooled");
        if (poolRoot != null) rootObj.transform.SetParent(poolRoot.transform);

        PooledExplosionInstance inst = rootObj.AddComponent<PooledExplosionInstance>();

        // Layer 1: Core Flash (中心超新星闪烁)
        GameObject flashObj = new GameObject("L1_CoreFlash");
        flashObj.transform.SetParent(rootObj.transform, false);
        inst.flashPS = SetupFlashPS(flashObj);

        // Layer 2: Shockwave Ring (冲击波光环)
        GameObject shockObj = new GameObject("L2_Shockwave");
        shockObj.transform.SetParent(rootObj.transform, false);
        inst.shockwavePS = SetupShockwavePS(shockObj);

        // Layer 3: Plasma Fireball (高能等离子火球烈焰团)
        GameObject fireballObj = new GameObject("L3_Fireball");
        fireballObj.transform.SetParent(rootObj.transform, false);
        inst.fireballPS = SetupFireballPS(fireballObj);

        // Layer 4: Sparks (高初速火花流)
        GameObject sparksObj = new GameObject("L4_Sparks");
        sparksObj.transform.SetParent(rootObj.transform, false);
        inst.sparksPS = SetupSparksPS(sparksObj);

        // Layer 5: Debris (重力物理翻滚残骸)
        GameObject debrisObj = new GameObject("L5_Debris");
        debrisObj.transform.SetParent(rootObj.transform, false);
        inst.debrisPS = SetupDebrisPS(debrisObj);

        return inst;
    }

    private static ParticleSystem SetupFlashPS(GameObject go)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var m = ps.main;
        m.duration = 0.12f;
        m.loop = false;
        m.scalingMode = ParticleSystemScalingMode.Hierarchy;
        m.startLifetime = 0.08f;
        m.startSpeed = 0f;
        m.startSize = 2.6f;
        m.startColor = new Color(1f, 1f, 0.95f, 0.95f);

        var e = ps.emission;
        e.rateOverTime = 0;
        e.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = glowMaterial;
        r.sortingOrder = 22;
        return ps;
    }

    private static ParticleSystem SetupShockwavePS(GameObject go)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var m = ps.main;
        m.duration = 0.35f;
        m.loop = false;
        m.scalingMode = ParticleSystemScalingMode.Hierarchy;
        m.startLifetime = 0.28f;
        m.startSpeed = 0f;
        m.startSize = 0.8f;
        m.startColor = new Color(1f, 0.85f, 0.4f, 0.9f);

        var e = ps.emission;
        e.rateOverTime = 0;
        e.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 1) });

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.8f);
        curve.AddKey(1f, 4.2f); // 快速向外扩张
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var col = ps.colorOverLifetime;
        col.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f), new GradientColorKey(new Color(1f, 0.4f, 0.1f), 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) }
        );
        col.color = grad;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = ringMaterial;
        r.sortingOrder = 21;
        return ps;
    }

    private static ParticleSystem SetupFireballPS(GameObject go)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var m = ps.main;
        m.duration = 0.45f;
        m.loop = false;
        m.scalingMode = ParticleSystemScalingMode.Hierarchy;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.22f, 0.38f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(1.5f, 3.8f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.2f, 0.95f), new Color(1f, 0.25f, 0.05f, 0.9f));

        var e = ps.emission;
        e.rateOverTime = 0;
        e.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 10) });

        var s = ps.shape;
        s.shapeType = ParticleSystemShapeType.Sphere;
        s.radius = 0.35f;

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 0.4f);
        curve.AddKey(0.25f, 1.1f);
        curve.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = glowMaterial;
        r.sortingOrder = 20;
        return ps;
    }

    private static ParticleSystem SetupSparksPS(GameObject go)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var m = ps.main;
        m.duration = 0.4f;
        m.loop = false;
        m.scalingMode = ParticleSystemScalingMode.Hierarchy;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.36f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(5.5f, 10.0f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        m.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.5f, 1f), new Color(1f, 0.4f, 0.1f, 1f));
        m.gravityModifier = 0.25f;

        var e = ps.emission;
        e.rateOverTime = 0;
        e.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 18) });

        var s = ps.shape;
        s.shapeType = ParticleSystemShapeType.Sphere;
        s.radius = 0.15f;

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = glowMaterial;
        r.sortingOrder = 23;
        return ps;
    }

    private static ParticleSystem SetupDebrisPS(GameObject go)
    {
        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        var m = ps.main;
        m.duration = 0.6f;
        m.loop = false;
        m.scalingMode = ParticleSystemScalingMode.Hierarchy;
        m.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        m.startSpeed = new ParticleSystem.MinMaxCurve(2.8f, 6.2f);
        m.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.32f);
        m.startColor = new ParticleSystem.MinMaxGradient(new Color(0.42f, 0.38f, 0.35f, 1f), new Color(0.68f, 0.62f, 0.56f, 1f));
        m.gravityModifier = 0.65f;

        var e = ps.emission;
        e.rateOverTime = 0;
        e.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 14) });

        var s = ps.shape;
        s.shapeType = ParticleSystemShapeType.Sphere;
        s.radius = 0.25f;

        var rol = ps.rotationOverLifetime;
        rol.enabled = true;
        rol.z = new ParticleSystem.MinMaxCurve(-180f, 180f);

        var sol = ps.sizeOverLifetime;
        sol.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sol.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var r = go.GetComponent<ParticleSystemRenderer>();
        r.material = solidMaterial;
        r.sortingOrder = 19;
        return ps;
    }

    // ==================== 统一公开播放入口 ====================

    public static void Spawn(Vector3 position, float scale = 1.0f)
    {
        ExplosionStyle style = ExplosionStyle.Standard;
        if (scale > 2.0f) style = ExplosionStyle.DreadnoughtNuke;
        else if (scale < 0.8f) style = ExplosionStyle.FieryRock;

        Spawn(position, scale, style);
    }

    public static void Spawn(Vector3 position, float scale, ExplosionStyle style)
    {
        InitializePool();

        PooledExplosionInstance inst;
        if (pool.Count > 0)
        {
            inst = pool.Dequeue();
        }
        else
        {
            inst = CreateNewInstance();
        }

        inst.Play(position, scale, style);
    }

    public static void ReturnToPool(PooledExplosionInstance inst)
    {
        if (inst == null) return;
        inst.gameObject.SetActive(false);
        if (poolRoot != null) inst.transform.SetParent(poolRoot.transform);
        pool.Enqueue(inst);
    }
}

/// <summary>
/// 池化爆炸粒子实例载体
/// </summary>
public class PooledExplosionInstance : MonoBehaviour
{
    public ParticleSystem flashPS;
    public ParticleSystem shockwavePS;
    public ParticleSystem fireballPS;
    public ParticleSystem sparksPS;
    public ParticleSystem debrisPS;

    private bool isPlaying = false;
    private float timer = 0f;
    private float activeDuration = 0.65f;

    public void Play(Vector3 position, float scale, ExplosionStyle style)
    {
        transform.position = position;
        transform.localScale = Vector3.one * Mathf.Clamp(scale, 0.4f, 4.5f);
        gameObject.SetActive(true);

        ApplyStyleTuning(style, scale);

        flashPS.Clear();
        shockwavePS.Clear();
        fireballPS.Clear();
        sparksPS.Clear();
        debrisPS.Clear();

        flashPS.Play();
        shockwavePS.Play();
        fireballPS.Play();
        sparksPS.Play();
        debrisPS.Play();

        isPlaying = true;
        timer = 0f;
        activeDuration = Mathf.Clamp(0.55f * scale, 0.45f, 1.2f);
    }

    private void ApplyStyleTuning(ExplosionStyle style, float scale)
    {
        switch (style)
        {
            case ExplosionStyle.PlasmaBlue:
                // 战机等离子蓝光爆炸
                var fMain = fireballPS.main;
                fMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.2f, 0.9f, 1f, 0.95f), new Color(0.1f, 0.4f, 1f, 0.9f));
                var sMain = sparksPS.main;
                sMain.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 1f, 1f, 1f), new Color(0.1f, 0.7f, 1f, 1f));
                var swMain = shockwavePS.main;
                swMain.startColor = new Color(0.2f, 0.8f, 1f, 0.9f);
                break;

            case ExplosionStyle.DreadnoughtNuke:
                // Boss 毁灭性暗红/紫金核爆
                var fbMain = fireballPS.main;
                fbMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.15f, 0.05f, 1f), new Color(0.8f, 0f, 0.3f, 0.9f));
                var spMain = sparksPS.main;
                spMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.9f, 0.3f, 1f), new Color(1f, 0.1f, 0.1f, 1f));
                var shockMain = shockwavePS.main;
                shockMain.startColor = new Color(1f, 0.3f, 0.1f, 0.95f);
                break;

            case ExplosionStyle.Standard:
            case ExplosionStyle.FieryRock:
            default:
                // 经典金橙耀眼烈焰
                var defFb = fireballPS.main;
                defFb.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.8f, 0.2f, 0.95f), new Color(1f, 0.25f, 0.05f, 0.9f));
                var defSp = sparksPS.main;
                defSp.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.95f, 0.5f, 1f), new Color(1f, 0.4f, 0.1f, 1f));
                var defSw = shockwavePS.main;
                defSw.startColor = new Color(1f, 0.85f, 0.4f, 0.9f);
                break;
        }
    }

    void Update()
    {
        if (!isPlaying) return;

        timer += Time.deltaTime;
        if (timer >= activeDuration)
        {
            isPlaying = false;
            ExplosionFX.ReturnToPool(this);
        }
    }
}

