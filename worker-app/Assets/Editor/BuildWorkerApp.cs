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
        private static bool s_RefreshTriggered;
        private static int s_RefreshWaitFrames;

        static BuildWorkerApp()
        {
            if (!s_Subscribed)
            {
                s_Subscribed = true;
                EditorApplication.update += CheckTrigger;
            }
            EditorApplication.delayCall += CheckTrigger;

            try
            {
                string projectRoot = Path.GetDirectoryName(Application.dataPath);
                string refreshResult = Path.Combine(projectRoot, "../builds/refresh_result.txt");
                File.WriteAllText(refreshResult, "reloaded:" + DateTime.Now.ToString("o"));
            }
            catch {}
        }

        [MenuItem("Industrial Safety AR/Build Fire Training APK")]
        public static void PerformBuild()
        {
            DoBuild("worker-app-ui-camera-v2.apk", autoRun: false);
        }

        [MenuItem("Industrial Safety AR/Build and Run Fire Training APK")]
        public static void PerformBuildAndRun()
        {
            DoBuild("worker-app-ui-camera-v2.apk", autoRun: true);
        }

        [MenuItem("Industrial Safety AR/Run Fire Training Tests")]
        public static void PerformRunTests()
        {
            DoRunTests();
            if (Application.isBatchMode)
            {
                EditorApplication.Exit(0);
            }
        }

        private static void CheckTrigger()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            try
            {
                File.WriteAllText(Path.Combine(projectRoot, "../builds/heartbeat.txt"), DateTime.Now.ToString("o"));
            }
            catch {}

            if (EditorApplication.isCompiling)
            {
                return;
            }

            string refreshTriggerPath = Path.Combine(projectRoot, "refresh_trigger.txt");
            if (File.Exists(refreshTriggerPath))
            {
                try { File.Delete(refreshTriggerPath); } catch {}
                AssetDatabase.Refresh();
                return;
            }

            string buildTriggerPath = Path.Combine(projectRoot, "build_trigger.txt");
            string testTriggerPath = Path.Combine(projectRoot, "test_trigger.txt");

            bool hasTest = File.Exists(testTriggerPath);
            bool hasBuild = File.Exists(buildTriggerPath);

            if (!hasTest && !hasBuild)
            {
                return;
            }

            if (hasTest)
            {
                EditorApplication.update -= CheckTrigger;
                s_Subscribed = false;
                try
                {
                    File.Delete(testTriggerPath);
                }
                catch {}

                try
                {
                    DoRunTests();
                }
                finally
                {
                    if (!s_Subscribed)
                    {
                        s_Subscribed = true;
                        EditorApplication.update += CheckTrigger;
                    }
                }
                return;
            }

            if (hasBuild)
            {
                EditorApplication.update -= CheckTrigger;
                s_Subscribed = false;

                string targetApkName = "worker-app-clean-foundation.apk";
                try
                {
                    string content = File.ReadAllText(buildTriggerPath).Trim();
                    if (!string.IsNullOrEmpty(content) && content.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
                    {
                        targetApkName = content;
                    }
                    File.Delete(buildTriggerPath);
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
        }

        private static void DoRunTests()
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string logPath = Path.Combine(projectRoot, "../builds/test_result.txt");

            try
            {
                LocalizationFontSetup.SetupFonts();
                Debug.Log("[BuildWorkerApp] Running FireTrainingEventTests.RunAllTests()...");
                bool passed = IndustrialSafetyAR.Tests.FireTrainingEventTests.RunAllTests(out var logs);
                string status = passed ? "SUCCESS" : "FAILED";
                var sb = new System.Text.StringBuilder();
                sb.AppendLine($"Status: {status}");
                sb.AppendLine($"Timestamp: {DateTime.Now:o}");
                sb.AppendLine($"TotalLogEntries: {logs.Count}");
                sb.AppendLine("=== LOGS ===");
                foreach (var line in logs)
                {
                    sb.AppendLine(line);
                }
                File.WriteAllText(logPath, sb.ToString());
                Debug.Log($"[BuildWorkerApp] Finished FireTrainingEventTests: {status}");
            }
            catch (Exception ex)
            {
                File.WriteAllText(logPath, "ERROR: " + ex);
                Debug.LogError($"[BuildWorkerApp] Test execution failed: {ex}");
            }
        }

        private static void DoBuild(string targetApkName = "worker-app-ui-camera-v2.apk", bool autoRun = false)
        {
            string projectRoot = Path.GetDirectoryName(Application.dataPath);
            string logPath = Path.Combine(projectRoot, "../builds/build_result.txt");

            try
            {
                DoRunTests();
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
                    options = autoRun ? BuildOptions.AutoRunPlayer : BuildOptions.None
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
