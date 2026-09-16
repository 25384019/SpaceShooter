using UnityEngine;

/// <summary>
/// 纯原生 ParticleSystem 爆炸特效生成器：包含岩石碎片与高速火花双层视觉元素，自销毁无残留
/// </summary>
public static class ExplosionFX
{
    private static Material defaultParticleMaterial;

    private static Material GetParticleMaterial()
    {
        if (defaultParticleMaterial == null)
        {
            Shader shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            defaultParticleMaterial = new Material(shader);
        }
        return defaultParticleMaterial;
    }

    public static void Spawn(Vector3 position, float scale = 1.0f)
    {
        GameObject fxObj = new GameObject("Explosion_FX");
        fxObj.transform.position = position;

        Material mat = GetParticleMaterial();

        // 1. 岩石碎屑粒子 (Rock Debris)
        ParticleSystem debrisPS = fxObj.AddComponent<ParticleSystem>();
        var main = debrisPS.main;
        main.duration = 0.5f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f * scale, 0.55f * scale);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f * scale, 5.0f * scale);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f * scale, 0.30f * scale);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.48f, 0.42f, 0.38f), new Color(0.72f, 0.65f, 0.58f));
        main.gravityModifier = 0.5f;
        main.stopAction = ParticleSystemStopAction.Destroy;

        var emission = debrisPS.emission;
        emission.rateOverTime = 0;
        int debrisCount = Mathf.Clamp(Mathf.RoundToInt(14 * scale), 8, 35);
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, debrisCount) });

        var shape = debrisPS.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f * scale;

        var sizeOverLifetime = debrisPS.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve curve = new AnimationCurve();
        curve.AddKey(0f, 1f);
        curve.AddKey(1f, 0f);
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, curve);

        var renderer = fxObj.GetComponent<ParticleSystemRenderer>();
        renderer.material = mat;
        renderer.sortingOrder = 15;

        // 2. 高速明亮火花 (Sparks)
        GameObject sparksObj = new GameObject("Sparks");
        sparksObj.transform.SetParent(fxObj.transform);
        sparksObj.transform.localPosition = Vector3.zero;

        ParticleSystem sparksPS = sparksObj.AddComponent<ParticleSystem>();
        var sMain = sparksPS.main;
        sMain.duration = 0.3f;
        sMain.loop = false;
        sMain.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.28f);
        sMain.startSpeed = new ParticleSystem.MinMaxCurve(5.0f * scale, 8.5f * scale);
        sMain.startSize = new ParticleSystem.MinMaxCurve(0.04f * scale, 0.10f * scale);
        sMain.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.85f, 0.3f), new Color(1f, 0.45f, 0.1f));

        var sEmission = sparksPS.emission;
        sEmission.rateOverTime = 0;
        int sparkCount = Mathf.Clamp(Mathf.RoundToInt(12 * scale), 6, 25);
        sEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, sparkCount) });

        var sShape = sparksPS.shape;
        sShape.shapeType = ParticleSystemShapeType.Sphere;
        sShape.radius = 0.1f;

        var sRenderer = sparksObj.GetComponent<ParticleSystemRenderer>();
        sRenderer.material = mat;
        sRenderer.sortingOrder = 16;

        debrisPS.Play();
        sparksPS.Play();
    }
}
