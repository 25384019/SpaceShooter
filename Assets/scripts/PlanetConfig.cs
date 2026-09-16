using System;

[Serializable]
public class PlanetConfig
{
    public enum PlanetAtmosphere
    {
        FrozenTundra,    // 极寒冰原
        AncientRuins,    // 废墟遗迹
        MoltenDesert,    // 烈焰荒漠
        ToxicSwamp       // 剧毒瘴气
    }

    public string planetName;
    public PlanetAtmosphere atmosphere;
    public int hazardLevel;     // 1 ~ 5
    public int resourceLevel;   // 1 ~ 5
    public int energyCostToLand = 10;
    public bool isScanned = false;
    public string groundMissionPreview;

    public static PlanetConfig GenerateRandomPlanet()
    {
        string[] names = { "Kepler-186f", "LV-426", "Aegis Prime", "Chronos-IX", "Elysium-7", "Vespera-3" };
        string chosenName = names[UnityEngine.Random.Range(0, names.Length)];

        PlanetAtmosphere atmo = (PlanetAtmosphere)UnityEngine.Random.Range(0, 4);
        int hazard = UnityEngine.Random.Range(2, 6);
        int resource = UnityEngine.Random.Range(2, 6);

        string preview = "";
        switch (atmo)
        {
            case PlanetAtmosphere.FrozenTundra:
                preview = "Cryo-Crystal Harvesting & Frozen Outpost Scan";
                break;
            case PlanetAtmosphere.AncientRuins:
                preview = "Decipher Precursor Monolith & Artifact Salvage";
                break;
            case PlanetAtmosphere.MoltenDesert:
                preview = "Magma Core Extraction under Extreme Heat";
                break;
            case PlanetAtmosphere.ToxicSwamp:
                preview = "Neutralize Spore Nests & Specimen Collection";
                break;
        }

        PlanetConfig cfg = new PlanetConfig
        {
            planetName = chosenName,
            atmosphere = atmo,
            hazardLevel = hazard,
            resourceLevel = resource,
            energyCostToLand = 10,
            isScanned = false,
            groundMissionPreview = preview
        };
        return cfg;
    }
}
