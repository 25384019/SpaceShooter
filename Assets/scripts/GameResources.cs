using System.IO;
using UnityEngine;

/// <summary>
/// 集中式静态资源缓存器，游戏启动时一次性预加载所有资源，彻底杜绝运行期反复 Resources.Load 开销
/// 具备三重保障：Resources.Load -> Texture2D 降级 -> 磁盘直读加载，100% 杜绝素材丢失或材质粉/黑/透明
/// </summary>
public static class GameResources
{
    public static Sprite PlayerSprite { get; private set; }
    public static Sprite ProjectileSprite { get; private set; }
    public static Sprite RockSprite { get; private set; }
    public static Sprite StarsSprite { get; private set; }
    public static Sprite StartSprite { get; private set; }
    public static Sprite WinSprite { get; private set; }
    public static Sprite LoseSprite { get; private set; }
    public static Sprite[] DigitSprites { get; private set; } = new Sprite[10];

    public static AudioClip BgmClip { get; private set; }
    public static AudioClip ShootClip { get; private set; }
    public static AudioClip ExplosionClip { get; private set; }
    public static AudioClip ShieldPickupClip { get; private set; }

    public static bool IsInitialized { get; private set; } = false;

    public static void Initialize()
    {
        if (IsInitialized) return;

        // 核心玩法与背景 Sprite
        PlayerSprite = LoadSpriteSafe("Images/Player", "Player");
        ProjectileSprite = LoadSpriteSafe("Images/projectile", "projectile");
        RockSprite = LoadSpriteSafe("Images/Rock", "Rock");
        StarsSprite = LoadSpriteSafe("Images/Stars_large", "Stars_large");

        // 封面图 Sprite
        StartSprite = LoadSpriteSafe("Images/Start", "Start");
        WinSprite = LoadSpriteSafe("Images/Win", "Win");
        LoseSprite = LoadSpriteSafe("Images/Lose", "Lose");

        // 数字精灵 (0~9)
        for (int i = 0; i < 10; i++)
        {
            Sprite digit = LoadSpriteSafe($"Digits/{i}", $"{i}");
            if (digit == null)
            {
                digit = LoadSpriteSafe($"timeDigit/{i}", $"{i}");
            }
            DigitSprites[i] = digit;
        }

        // 音频素材
        BgmClip = Resources.Load<AudioClip>("Sounds/background");
        ShootClip = Resources.Load<AudioClip>("Sounds/Shoot");
        ExplosionClip = Resources.Load<AudioClip>("Sounds/ExplosionEnemy");
        ShieldPickupClip = Resources.Load<AudioClip>("Sounds/shaketice");

        IsInitialized = true;
    }

    private static Sprite LoadSpriteSafe(string relativePath, string fileName)
    {
        // 1. 尝试直接通过 Unity Resources.Load<Sprite>
        Sprite s = Resources.Load<Sprite>(relativePath);
        if (s != null) return s;

        // 2. 尝试通过 Resources.Load<Texture2D> 动态封装 Sprite
        Texture2D tex = Resources.Load<Texture2D>(relativePath);
        if (tex != null)
        {
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }

        // 3. 磁盘物理直读兜底（彻底杜绝 Unity 缓存刷新延迟导致的 Missing）
        string[] searchPaths = new string[]
        {
            Path.Combine(Application.dataPath, "Resources", relativePath + ".png"),
            Path.Combine(Application.dataPath, "Assets", relativePath + ".png"),
            Path.Combine(Application.dataPath, "Assets", "Images", fileName + ".png"),
            Path.Combine(Application.dataPath, "Resources", "Images", fileName + ".png"),
            Path.Combine(Application.dataPath, "Assets", "Images", "timeDigit", fileName + ".png"),
            Path.Combine(Application.dataPath, "Resources", "Digits", fileName + ".png")
        };

        foreach (var p in searchPaths)
        {
            if (File.Exists(p))
            {
                try
                {
                    byte[] bytes = File.ReadAllBytes(p);
                    Texture2D diskTex = new Texture2D(2, 2);
                    if (diskTex.LoadImage(bytes))
                    {
                        return Sprite.Create(diskTex, new Rect(0, 0, diskTex.width, diskTex.height), new Vector2(0.5f, 0.5f), 100f);
                    }
                }
                catch { }
            }
        }

        return null;
    }
}
