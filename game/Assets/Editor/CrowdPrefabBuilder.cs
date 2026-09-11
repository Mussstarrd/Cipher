#nullable enable
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Builds a ready-to-instantiate walking civilian prefab per character.
    ///
    /// This is the pragmatic path, chosen deliberately over finishing the vertex-animation-texture
    /// bake first. The VAT bake (see CrowdBaker) is the right technique for a thousand agents and it
    /// is still the plan, but it is fighting this particular FBX's rig scale, and the thing that
    /// actually matters right now is seeing real people walk. A plain Animator handles a few hundred
    /// skinned characters comfortably on this hardware, which is enough for a demo level, and the
    /// swarm keeps its instanced capsules behind them until the bake lands.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.CrowdPrefabBuilder.BuildAll -quit
    /// </summary>
    public static class CrowdPrefabBuilder
    {
        private const string SourceDir = "Assets/Resources/Characters";
        private const string OutDir = "Assets/Resources/Civilians";
        private const string ControllerPath = OutDir + "/WalkController.controller";

        [MenuItem("Cipher/Crowd/Build Civilian Prefabs")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(OutDir);

            var clip = FindWalkClip();
            if (clip == null) { Debug.LogError("[Civilians] no Walk clip found"); return; }

            var controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(ControllerPath, clip);
            if (controller == null) { Debug.LogError("[Civilians] could not create controller"); return; }
            // Loop the walk. Without this the crowd takes one stride and freezes mid-air.
            foreach (var layer in controller.layers)
                foreach (var st in layer.stateMachine.states)
                    if (st.state.motion is AnimationClip c)
                    {
                        var settings = AnimationUtility.GetAnimationClipSettings(c);
                        settings.loopTime = true;
                        AnimationUtility.SetAnimationClipSettings(c, settings);
                    }

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

                    // Unity objects use a "fake null" that ?? does not recognise, so the coalescing
                    // operator hands back a destroyed component instead of adding a live one.
                    var animator = instance.GetComponent<Animator>();
                    if (animator == null) animator = instance.AddComponent<Animator>();
                    animator.runtimeAnimatorController = controller;
                    animator.applyRootMotion = false;   // the simulation moves them, not the clip
                    animator.cullingMode = AnimatorCullingMode.CullCompletely;

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

        private static AnimationClip? FindWalkClip()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { SourceDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var clips = AssetDatabase.LoadAllAssetsAtPath(path)
                                         .OfType<AnimationClip>()
                                         .Where(c => !c.name.StartsWith("__preview__"))
                                         .ToList();
                var walk = clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains("walk"));
                if (walk != null) return walk;
            }
            // Fall back to any clip on the shared animation model.
            var all = AssetDatabase.LoadAllAssetsAtPath(SourceDir + "/_Animations.fbx")
                                   .OfType<AnimationClip>()
                                   .Where(c => !c.name.StartsWith("__preview__"))
                                   .ToList();
            return all.FirstOrDefault(c => c.name.ToLowerInvariant().Contains("walk")) ?? all.FirstOrDefault();
        }
    }
}
