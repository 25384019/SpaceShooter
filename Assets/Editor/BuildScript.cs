using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public class BuildScript
{
    private static readonly string[] ScenePaths = new string[]
    {
        "Assets/Scenes/start.scene",
        "Assets/Scenes/game.scene",
        "Assets/Scenes/win.scene",
        "Assets/Scenes/lose.scene"
    };

    [MenuItem("Build/Configure Build Settings (配置场景顺序)")]
    public static void ConfigureBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = new EditorBuildSettingsScene[ScenePaths.Length];
        for (int i = 0; i < ScenePaths.Length; i++)
        {
            scenes[i] = new EditorBuildSettingsScene(ScenePaths[i], true);
        }
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[BuildScript] Successfully configured 4 scenes in EditorBuildSettings!");
    }

    [MenuItem("Build/Build Windows (封装游戏独立程序)")]
    public static void BuildStandalonePlayer()
    {
        ConfigureBuildSettings();

        string outputDir = "Build/SpaceShooter";
        string exePath = Path.Combine(outputDir, "SpaceShooter.exe");

        if (Directory.Exists(outputDir))
        {
            try { Directory.Delete(outputDir, true); } catch { }
        }
        Directory.CreateDirectory(outputDir);

        BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
        buildPlayerOptions.scenes = ScenePaths;
        buildPlayerOptions.locationPathName = exePath;
        buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
        buildPlayerOptions.options = BuildOptions.None;

        Debug.Log("[BuildScript] Starting build to: " + exePath);
        BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
        BuildSummary summary = report.summary;

        if (summary.result == BuildResult.Succeeded)
        {
            Debug.Log($"[BuildScript] Build SUCCESS! Total size: {summary.totalSize / 1024 / 1024} MB, Output: {exePath}");

            // Create ZIP archive for convenient packaging
            try
            {
                string zipPath = "Build/SpaceShooter_Windows64.zip";
                if (File.Exists(zipPath)) File.Delete(zipPath);
                ZipFile.CreateFromDirectory(outputDir, zipPath);
                Debug.Log($"[BuildScript] Packaged zip archive created at: {zipPath}");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[BuildScript] Zip packaging skipped: " + ex.Message);
            }
        }
        else if (summary.result == BuildResult.Failed)
        {
            Debug.LogError($"[BuildScript] Build FAILED with {summary.totalErrors} errors!");
        }
    }
}
