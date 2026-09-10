#nullable enable
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Player builds for CI. Scenes come from Build Settings (Flood.unity is scene 0).
    ///   Unity.exe -batchmode -nographics -projectPath game -quit
    ///            -executeMethod Cipher.Game.Editor.CiBuild.BuildWindows -buildPath out/win
    /// Exit code is non-zero on any build error so the CI step fails loudly.
    /// </summary>
    public static class CiBuild
    {
        private const string ProductName = "CipherDeadTurf";

        [MenuItem("Cipher/Build/Windows x64")]
        public static void BuildWindows() =>
            Build(BuildTarget.StandaloneWindows64, BuildTargetGroup.Standalone, ProductName + ".exe");

        [MenuItem("Cipher/Build/Android APK")]
        public static void BuildAndroid() =>
            Build(BuildTarget.Android, BuildTargetGroup.Android, ProductName + ".apk");

        private static void Build(BuildTarget target, BuildTargetGroup group, string fileName)
        {
            string outDir = ArgAfter("-buildPath") ?? Path.Combine("Builds", target.ToString());
            Directory.CreateDirectory(outDir);

            string[] scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
                Fail("No enabled scenes in Build Settings — run Cipher/Create Flood Scene first.");

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, fileName),
                target = target,
                targetGroup = group,
                options = BuildOptions.None,
            };

            Debug.Log($"[Cipher] Building {target} → {options.locationPathName} ({scenes.Length} scene(s))");
            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary s = report.summary;
            Debug.Log($"[Cipher] Build {s.result}: {s.totalSize / (1024 * 1024)} MB, {s.totalErrors} errors, {s.totalWarnings} warnings, {s.totalTime.TotalSeconds:F0}s");

            if (s.result != BuildResult.Succeeded)
                Fail($"Build failed with result {s.result} ({s.totalErrors} errors)");
        }

        private static string? ArgAfter(string flag)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (string.Equals(args[i], flag, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        private static void Fail(string message)
        {
            Debug.LogError("[Cipher] " + message);
            if (Application.isBatchMode) EditorApplication.Exit(1);
            else throw new BuildFailedException(message);
        }
    }
}
