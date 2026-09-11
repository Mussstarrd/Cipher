#nullable enable
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Produces LEGACY copies of the clips the game plays, so the old Animation component can run
    /// them. The characters ship with twenty-four each; we import the nine we use.
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

        /// <summary>
        /// What we take from the pack, and what the game calls it.
        ///
        /// The characters ship with TWENTY-FOUR clips each and we were using one. That is why the
        /// owner saw a crowd permanently miming a walk: not a missing asset, a missing import. The
        /// set below is everything the game currently has a use for; the rest (sword, roll, wave,
        /// interact) stays on the shelf until something needs it.
        /// </summary>
        private static readonly (string Clip, string Name, WrapMode Wrap)[] Wanted =
        {
            ("Walk",        "walk",   WrapMode.Loop),
            ("Idle",        "idle",   WrapMode.Loop),
            ("Run",         "run",    WrapMode.Loop),
            ("Punch_Right", "punch",  WrapMode.Loop),
            ("Death",       "death",  WrapMode.ClampForever),
            ("HitRecieve",  "hit",    WrapMode.Once),

            // The hero holds a rifle, so his set is the gun one.
            ("Idle_Gun",    "aim",    WrapMode.Loop),
            ("Run_Shoot",   "advance", WrapMode.Loop),
            ("Gun_Shoot",   "fire",   WrapMode.Once),
        };

        [MenuItem("Cipher/Crowd/Build Legacy Clips")]
        public static void Build()
        {
            Directory.CreateDirectory(OutDir);

            var source = FindSourceModel();
            if (source == null) { Debug.LogError("[LegacyClip] no character model with clips"); return; }

            var clips = AssetDatabase.LoadAllAssetsAtPath(source)
                                     .OfType<AnimationClip>()
                                     .Where(c => !c.name.StartsWith("__preview__"))
                                     .ToList();

            int built = 0;
            foreach (var (clipName, gameName, wrap) in Wanted)
            {
                // Names arrive as "CharacterArmature|Walk", so match on the tail and exactly, or
                // "Run" silently picks up "Run_Back" and the crowd moonwalks.
                var match = clips.FirstOrDefault(c => Tail(c.name) == clipName);
                if (match == null)
                {
                    Debug.LogWarning($"[LegacyClip] '{clipName}' is not in this pack; skipped");
                    continue;
                }

                if (CopyToLegacy(match, gameName, wrap)) built++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[LegacyClip] built {built} legacy clips from {source}");
        }

        private static string Tail(string name)
        {
            int bar = name.LastIndexOf('|');
            return bar >= 0 ? name.Substring(bar + 1) : name;
        }

        private static string? FindSourceModel()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { SourceDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                bool hasClips = AssetDatabase.LoadAllAssetsAtPath(path)
                                             .OfType<AnimationClip>()
                                             .Any(c => !c.name.StartsWith("__preview__"));
                if (hasClips) return path;
            }
            return null;
        }

        /// <summary>
        /// Builds the legacy clip by copying CURVES, not by instantiating and flipping the flag.
        /// Legacy and non-legacy clips bind curves through different systems, so a copied clip with
        /// legacy set afterwards keeps its data and animates nothing: the character holds a bind
        /// pose while every diagnostic insists the clip is legacy and the right length.
        /// </summary>
        private static bool CopyToLegacy(AnimationClip source, string gameName, WrapMode wrap)
        {
            var copy = new AnimationClip
            {
                name = gameName,
                legacy = true,
                frameRate = source.frameRate,
                wrapMode = wrap,
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
                return false;
            }

            copy.EnsureQuaternionContinuity();
            string path = $"{OutDir}/{gameName}.anim";
            AssetDatabase.CreateAsset(copy, path);
            Debug.Log($"[LegacyClip] {Tail(source.name)} -> {path} ({curves} curves, {copy.length:F2}s, {wrap})");
            return true;
        }
    }
}
