using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class AutoBuildTrigger
{
    private static readonly string TriggerPath = "build_request.txt";

    static AutoBuildTrigger()
    {
        EditorApplication.delayCall += CheckAndBuild;
    }

    private static void CheckAndBuild()
    {
        if (File.Exists(TriggerPath))
        {
            try
            {
                File.Delete(TriggerPath);
            }
            catch { }

            Debug.Log("[AutoBuildTrigger] Trigger detected! Executing BuildStandalonePlayer now...");
            BuildScript.BuildStandalonePlayer();
        }
    }
}
