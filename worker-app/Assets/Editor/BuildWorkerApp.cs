using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace IndustrialSafetyAR.Editor
{
    [InitializeOnLoad]
    public static class BuildWorkerApp
    {
        private static bool s_Subscribed;

        static BuildWorkerApp()
        {
            if (!s_Subscribed)
            {
                s_Subscribed = true;
                EditorApplication.update += CheckTrigger;
            }
            EditorApplication.delayCall += CheckTrigger;
        }

        public static void PerformBuild()
        {
            DoBuild("worker-app-clean-foundation.apk");
        }

        public static void PerformBuildAndRun()
        {
            DoBuild("worker-app-clean-foundation.apk");
        }

        private static void CheckTrigger()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string triggerPath = Path.Combine(projectRoot, "build_trigger.txt");

            if (!File.Exists(triggerPath))
                return;

            EditorApplication.update -= CheckTrigger;
            s_Subscribed = false;

            string targetApkName = "worker-app-clean-foundation.apk";
            try
            {
                string content = File.ReadAllText(triggerPath).Trim();
                if (!string.IsNullOrEmpty(content) && content.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                {
                    targetApkName = content;
                }
                File.Delete(triggerPath);
            }
            catch {}

            try
            {
                DoBuild(targetApkName);
            }
            finally
            {
                if (!s_Subscribed)
                {
                    s_Subscribed = true;
                    EditorApplication.update += CheckTrigger;
                }
            }
        }

        private static void DoBuild(string targetApkName = "worker-app-clean-foundation.apk")
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string logPath = Path.Combine(projectRoot, "../builds/build_result.txt");

            try
            {
                File.WriteAllText(logPath, "BUILD_STARTED at " + DateTime.Now.ToString("o") + "\n");

                string outputApk = Path.IsPathRooted(targetApkName)
                    ? Path.GetFullPath(targetApkName)
                    : Path.GetFullPath(Path.Combine(projectRoot, "../builds", targetApkName));

                string buildDir = Path.GetDirectoryName(outputApk);
                if (!string.IsNullOrEmpty(buildDir) && !Directory.Exists(buildDir))
                {
                    Directory.CreateDirectory(buildDir);
                }

                BuildPlayerOptions options = new BuildPlayerOptions
                {
                    scenes = new[] { "Assets/Scenes/SampleScene.unity" },
                    locationPathName = outputApk,
                    target = BuildTarget.Android,
                    options = BuildOptions.None
                };

                Debug.Log($"[BuildWorkerApp] Starting build to: {outputApk}");
                BuildReport report = BuildPipeline.BuildPlayer(options);
                BuildSummary summary = report.summary;

                string status = summary.result == BuildResult.Succeeded ? "SUCCESS" : "FAILED";
                string result = $"Status: {status}\n" +
                                $"TotalErrors: {summary.totalErrors}\n" +
                                $"TotalWarnings: {summary.totalWarnings}\n" +
                                $"TotalSize: {summary.totalSize}\n" +
                                $"OutputPath: {outputApk}\n";
                File.WriteAllText(logPath, result);
                Debug.Log($"[BuildWorkerApp] Finished build: {status}, TotalSize: {summary.totalSize}");

                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
                }
            }
            catch (Exception ex)
            {
                File.WriteAllText(logPath, "ERROR: " + ex.ToString());
                Debug.LogError($"[BuildWorkerApp] Build failed with exception: {ex}");
                if (Application.isBatchMode)
                {
                    EditorApplication.Exit(1);
                }
            }
        }
    }
}
