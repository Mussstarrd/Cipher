#nullable enable
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Turns animation import on for the machine FBXs and reports what clips they brought.
    ///
    /// WHY THIS IS NOT <see cref="ModelRigSetup"/>. That one exists because the civilian pack ships
    /// its characters with no animation at all, so a separate clip library had to own the rig and
    /// <see cref="LegacyClipMaker"/> had to copy 630 curves onto every body by hand. CLAUDE.md
    /// records the five approaches that failed silently before that worked.
    ///
    /// None of that applies here. The Quaternius robot carries its OWN skeleton and its own clips
    /// in the same file -- 16 animation stacks, 116 limb nodes -- so the swamp that
    /// <see cref="MachineBody"/>'s header warns about ("a robot from any other pack is a different
    /// skeleton, so it would arrive with none of that") is not the situation we are in. It arrives
    /// with all of it. The job is to import it, not to rebuild it.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.MachineRigSetup.Configure -quit
    /// </summary>
    public static class MachineRigSetup
    {
        private const string Dir = "Assets/Resources/Machines";

        [MenuItem("Cipher/Crowd/Configure Machine Rigs")]
        public static void Configure()
        {
            var guids = AssetDatabase.FindAssets("t:Model", new[] { Dir });
            if (guids.Length == 0)
            {
                Debug.LogError($"[MachineRig] no models under {Dir}");
                return;
            }

            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null) continue;

                // Generic, not Humanoid. A humanoid avatar retargets through a bone mapping that a
                // four-fingered robot with no neck will not satisfy cleanly, and we do not need
                // retargeting at all -- the clips in this file were authored against this exact
                // skeleton, so the identity mapping is the correct one.
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.importAnimation = true;

                // The clips are authored in place and we drive position ourselves from the sim, so
                // root motion would fight the flow field for control of where a body actually is.
                importer.animationCompression = ModelImporterAnimationCompression.KeyframeReduction;

                importer.SaveAndReimport();
                Debug.Log($"[MachineRig] configured {path}");
            }

            AssetDatabase.Refresh();
            List();
        }

        /// <summary>Prints every clip on every machine model, so the wiring can name real clips.</summary>
        [MenuItem("Cipher/Crowd/List Machine Clips")]
        public static void List()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { Dir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                                         .OfType<AnimationClip>()
                                         .Where(c => !c.name.StartsWith("__preview__"))
                                         .Select(c => $"{c.name}({c.length:F2}s{(c.legacy ? ",legacy" : "")})")
                                         .ToArray();
                Debug.Log($"[MachineClips] {path}: {clips.Length} -> {string.Join(", ", clips)}");
            }
        }
    }
}
