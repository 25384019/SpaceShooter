using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 游戏全局自举引导：在游戏启动时注册 sceneLoaded 事件，确保任意场景流转均能稳定自愈并生成所需控制器
/// </summary>
public static class GameBootstrap
{
    private static bool isInitialized = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void OnBeforeSceneLoad()
    {
        if (!isInitialized)
        {
            isInitialized = true;
            GameResources.Initialize();
            SceneManager.sceneLoaded += OnSceneLoadedCallback;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void OnInitialSceneLoad()
    {
        // 确保编辑器内直接点击 Play 某场景时能触发初始化
        SetupScene(SceneManager.GetActiveScene().name);
    }

    private static void OnSceneLoadedCallback(Scene scene, LoadSceneMode mode)
    {
        // 每次通过 SceneManager.LoadScene 切换场景时均会执行此回调
        SetupScene(scene.name);
    }

    public static void SetupScene(string sceneName)
    {
        sceneName = sceneName.ToLower();

        // 确保资源已就位
        GameResources.Initialize();

        if (sceneName.Contains("start"))
        {
            SceneNavigation nav = Object.FindObjectOfType<SceneNavigation>();
            if (nav == null)
            {
                GameObject navObj = new GameObject("SceneNavigation");
                nav = navObj.AddComponent<SceneNavigation>();
                nav.targetScene = "game";
                nav.promptText = "PRESS SPACE OR CLICK TO START";
            }
        }
        else if (sceneName.Contains("win"))
        {
            SceneNavigation nav = Object.FindObjectOfType<SceneNavigation>();
            if (nav == null)
            {
                GameObject navObj = new GameObject("SceneNavigation");
                nav = navObj.AddComponent<SceneNavigation>();
                nav.targetScene = "game";
                nav.promptText = "VICTORY! PRESS SPACE OR CLICK TO PLAY AGAIN";
            }
        }
        else if (sceneName.Contains("lose"))
        {
            SceneNavigation nav = Object.FindObjectOfType<SceneNavigation>();
            if (nav == null)
            {
                GameObject navObj = new GameObject("SceneNavigation");
                nav = navObj.AddComponent<SceneNavigation>();
                nav.targetScene = "game";
                nav.promptText = "GAME OVER! PRESS SPACE OR CLICK TO RETRY";
            }
        }
        else // game.scene 或者其他玩法场景
        {
            if (Object.FindObjectOfType<ObjectPool>() == null)
            {
                GameObject poolObj = new GameObject("ObjectPool");
                poolObj.AddComponent<ObjectPool>();
            }

            GameManager gm = Object.FindObjectOfType<GameManager>();
            if (gm == null)
            {
                GameObject gmObj = new GameObject("GameManager");
                gm = gmObj.AddComponent<GameManager>();
            }
            else
            {
                gm.EnsureAllEntities();
            }
        }
    }
}
