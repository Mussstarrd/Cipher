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
        public static void BuildAndroid()
        {
            ConfigureAndroid();
            Build(BuildTarget.Android, BuildTargetGroup.Android, ProductName + ".apk");
        }

        /// <summary>
        /// Android player settings applied from code so a headless build never depends on whatever
        /// the last person left in the inspector. Landscape and a gamepad are the design centre
        /// (ADR-001); debug signing is fine for sideloading and is replaced before any store upload.
        /// </summary>
        private static void ConfigureAndroid()
        {
            PlayerSettings.applicationIdentifier = "com.cipher.deadturf";
            PlayerSettings.companyName = "Cipher";
            PlayerSettings.productName = ProductName;

            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.Android.useCustomKeystore = false;

            // Landscape only: this is a controller game held sideways, never a portrait one.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // The graybox draws with Shader.Find("Standard"); device builds strip it without this.
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { UnityEngine.Rendering.GraphicsDeviceType.Vulkan, UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });
        }

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
