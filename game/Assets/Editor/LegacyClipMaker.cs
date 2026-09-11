#nullable enable
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Produces a LEGACY copy of the walk clip, so the old Animation component can play it.
    ///
    /// Three approaches were tried before this one and all three failed silently, leaving the
    /// characters in a bind pose that looks identical to a working import nobody told to move:
    ///
    ///   1. AnimatorController with the imported clip. Generic rigs retarget through an avatar, and
    ///      the models imported with avatarSetup NoAvatar, so nothing bound.
    ///   2. The same, after copying the shared library's avatar onto every character. The avatar
    ///      then reported valid, and still nothing bound.
    ///   3. AnimationClip.SampleAnimation every frame. Works in the editor (the crowd baker relies
    ///      on it) but does not apply to a non-legacy clip in a player.
    ///
    /// The legacy Animation component binds by transform path, needs no avatar, no controller and no
    /// retargeting, and is exactly the right tool for "play one looping walk on a generic rig".
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.LegacyClipMaker.Build -quit
    /// </summary>
    public static class LegacyClipMaker
    {
        private const string SourceDir = "Assets/Resources/Characters";
        private const string OutDir = "Assets/Resources/Civilians";
        private const string OutPath = OutDir + "/WalkLegacy.anim";

        [MenuItem("Cipher/Crowd/Build Legacy Walk Clip")]
        public static void Build()
        {
            Directory.CreateDirectory(OutDir);

            AnimationClip? source = null;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { SourceDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                                         .OfType<AnimationClip>()
                                         .Where(c => !c.name.StartsWith("__preview__"))
                                         .ToList();
                var walk = clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains("walk"));
                if (walk != null) { source = walk; break; }
                source ??= clips.FirstOrDefault();
            }

            if (source == null) { Debug.LogError("[LegacyClip] no clip found"); return; }

            // Build the legacy clip by copying CURVES, not by instantiating and flipping the flag.
            // Legacy and non-legacy clips bind curves through different systems, so a copied clip
            // with legacy set afterwards keeps its data and animates nothing: the character holds a
            // bind pose while every diagnostic insists the clip is legacy and the right length.
            var copy = new AnimationClip
            {
                name = "WalkLegacy",
                legacy = true,
                frameRate = source.frameRate,
                wrapMode = WrapMode.Loop,
            };

            int curves = 0;
            foreach (var binding in AnimationUtility.GetCurveBindings(source))
            {
                var curve = AnimationUtility.GetEditorCurve(source, binding);
                if (curve == null) continue;
                copy.SetCurve(binding.path, binding.type, binding.propertyName, curve);
                curves++;
            }

            if (curves == 0)
            {
                Debug.LogError($"[LegacyClip] '{source.name}' produced no curves; nothing to play");
                return;
            }

            copy.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(copy, OutPath);
            Debug.Log($"[LegacyClip] copied {curves} curves");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var check = AssetDatabase.LoadAssetAtPath<AnimationClip>(OutPath);
            Debug.Log($"[LegacyClip] '{source.name}' -> {OutPath} " +
                      $"legacy={check?.legacy} length={check?.length:F2} wrap={check?.wrapMode}");
        }
    }
}
