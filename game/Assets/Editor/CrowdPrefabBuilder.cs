#nullable enable
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Builds a ready-to-instantiate civilian prefab per character: a skinned mesh with smoothed
    /// normals baked into its tangent channel for the ink outline. That is the whole job.
    ///
    /// IT DOES NOT SET UP ANIMATION, and no amount of editing it will make a civilian walk. The
    /// runtime destroys every Animator on the way in (an Animator suppresses the legacy Animation
    /// component even while disabled) and plays Resources/Civilians/WalkLegacy.anim through a legacy
    /// Animation component instead. An AnimatorController used to be built here and looked
    /// load-bearing; it was not. FloodBootstrap.BuildCivilian deletes it three lines after
    /// instantiating the prefab, so the whole branch was dead.
    ///
    /// The walk cycle therefore lives in exactly two places. To change how a civilian moves, edit
    /// <see cref="LegacyClipMaker"/> — which builds that clip by copying curves explicitly — or the
    /// playback in FloodBootstrap.BuildCivilian. CLAUDE.md records the five approaches that failed
    /// silently before this one, each leaving a bind pose indistinguishable from a working import.
    ///
    /// This is a bridge, not the destination: the vertex-animation-texture bake in
    /// <see cref="CrowdBaker"/> is what eventually makes all thousand agents real. The swarm keeps
    /// its instanced capsules behind the promoted few until then.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.CrowdPrefabBuilder.BuildAll -quit
    /// </summary>
    public static class CrowdPrefabBuilder
    {
        private const string SourceDir = "Assets/Resources/Characters";
        private const string OutDir = "Assets/Resources/Civilians";

        [MenuItem("Cipher/Crowd/Build Civilian Prefabs")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutDir);

            int built = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { SourceDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith("_Animations.fbx")) continue;

                string name = Path.GetFileNameWithoutExtension(path);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                try
                {
                    var smr = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr == null) { Debug.LogWarning($"[Civilians] {name}: no skinned mesh"); continue; }

                    // NO rescaling. The first in-engine render of these raw models measured 1.98 m
                    // tall, which is already a person. Two different attempts to "normalise" them
                    // produced a sixty-metre pedestrian and then a quarter-size one, both because
                    // bounds read in edit mode on a freshly instantiated prefab are not real: the
                    // renderer has never been through a frame. Measure in play, or do not measure.

                    // Smoothed normals for the outline hull. Without this the ink tears at every
                    // seam and the character looks like a smudged pencil sketch.
                    SmoothNormals.ApplyToHierarchy(instance, $"{OutDir}/{name}_smoothmesh.asset");

                    string outPath = $"{OutDir}/{name}_Civilian.prefab";
                    PrefabUtility.SaveAsPrefabAsset(instance, outPath);
                    built++;
                    Debug.Log($"[Civilians] {name}: saved {outPath}");
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Civilians] built {built} prefabs");
        }
    }
}
