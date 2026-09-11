#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Bakes a rigged, animated character into a static mesh plus an animation texture, so a
    /// thousand of them can be drawn with GPU instancing.
    ///
    /// Why: one SkinnedMeshRenderer per agent dies in the low hundreds, and we need 1,000+ on PC
    /// and 300-500 on a phone (ADR-007). A skinned mesh cannot be instanced because every instance
    /// needs its own bone matrices. So we throw the rig away and record the RESULT instead: for
    /// every frame of the walk cycle, every vertex position, written into a texture. The shader
    /// then reconstructs the pose by sampling that texture, which makes the character an ordinary
    /// static mesh that instances like any other.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.CrowdBaker.BakeAll -quit
    /// </summary>
    public static class CrowdBaker
    {
        private const string SourceDir = "Assets/Resources/Characters";
        private const string OutDir = "Assets/Resources/Crowd";

        /// <summary>Frames sampled per clip. 24 is plenty for a walk and keeps the texture small.</summary>
        private const int Frames = 24;

        /// <summary>Every baked character is normalised to this height in metres.</summary>
        private const float TargetHeightMetres = 1.8f;

        /// <summary>Shared clip library, used when a character FBX carries no clips of its own.</summary>
        private const string SharedClipPath = SourceDir + "/_Animations.fbx";

        /// <summary>Clip names we prefer, in order. Falls back to the longest clip available.</summary>
        private static readonly string[] Preferred = { "walk", "run", "idle" };

        [MenuItem("Cipher/Crowd/Bake All Characters")]
        public static void BakeAll()
        {
            Directory.CreateDirectory(OutDir);

            var guids = AssetDatabase.FindAssets("t:Model", new[] { SourceDir })
                                     .Where(g => !AssetDatabase.GUIDToAssetPath(g).EndsWith("_Animations.fbx"))
                                     .ToArray();
            if (guids.Length == 0)
            {
                Debug.LogError($"[CrowdBaker] no models under {SourceDir}");
                return;
            }

            int ok = 0, failed = 0;
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                try
                {
                    if (Bake(path)) ok++; else failed++;
                }
                catch (Exception e)
                {
                    Debug.LogError($"[CrowdBaker] {Path.GetFileName(path)}: {e.Message}");
                    failed++;
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CrowdBaker] baked {ok}, failed {failed}");
        }

        private static bool Bake(string modelPath)
        {
            string name = Path.GetFileNameWithoutExtension(modelPath);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            if (prefab == null) { Debug.LogWarning($"[CrowdBaker] {name}: not a model"); return false; }

            // Quaternius ships one shared Animations.fbx rather than embedding clips per character,
            // which suits us: every character rides the same skeleton, so one bake source covers all.
            var clip = PickClip(modelPath) ?? PickClip(SharedClipPath);
            if (clip == null) { Debug.LogWarning($"[CrowdBaker] {name}: no animation clip"); return false; }

            // BakeMesh only works on a live instance, so put one in the scene and take it out after.
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            try
            {
                var smr = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                if (smr == null) { Debug.LogWarning($"[CrowdBaker] {name}: no skinned mesh"); return false; }

                int vertexCount = smr.sharedMesh.vertexCount;
                var positions = new Texture2D(vertexCount, Frames, TextureFormat.RGBAHalf, false, true)
                {
                    name = name + "_pos",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };
                var normals = new Texture2D(vertexCount, Frames, TextureFormat.RGBAHalf, false, true)
                {
                    name = name + "_nrm",
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp,
                };

                var posPixels = new Color[vertexCount * Frames];
                var nrmPixels = new Color[vertexCount * Frames];
                var baked = new Mesh();
                var bounds = new Bounds(Vector3.zero, Vector3.zero);
                bool first = true;

                // A walk clip carries root motion: the character travels forward as it plays. Baked
                // straight, that smears one body across several metres. The crowd needs a walk IN
                // PLACE, with the sim supplying the travel, so remove the horizontal drift per frame
                // while keeping the vertical bob, which is what makes a walk read as a walk.
                Vector3 frameZeroCentre = Vector3.zero;

                for (int f = 0; f < Frames; f++)
                {
                    float t = clip.length * (f / (float)Frames);   // loop: never sample the end twice
                    clip.SampleAnimation(instance, t);
                    smr.BakeMesh(baked, true);

                    var verts = baked.vertices;
                    var norms = baked.normals;

                    Vector3 sum = Vector3.zero;
                    int n0 = Mathf.Min(vertexCount, verts.Length);
                    for (int v = 0; v < n0; v++) sum += verts[v];
                    Vector3 centre = n0 > 0 ? sum / n0 : Vector3.zero;
                    if (f == 0) frameZeroCentre = centre;
                    var drift = new Vector3(centre.x - frameZeroCentre.x, 0f, centre.z - frameZeroCentre.z);

                    for (int v = 0; v < vertexCount && v < verts.Length; v++)
                    {
                        var p = verts[v] - drift;
                        posPixels[f * vertexCount + v] = new Color(p.x, p.y, p.z, 1f);
                        var n = v < norms.Length ? norms[v] : Vector3.up;
                        nrmPixels[f * vertexCount + v] = new Color(n.x, n.y, n.z, 0f);

                        if (first) { bounds = new Bounds(p, Vector3.zero); first = false; }
                        else bounds.Encapsulate(p);
                    }
                }

                // Source scale is unknowable in general: this FBX puts 100x on the renderer
                // transform and BakeMesh does not reliably fold it in. Rather than guess, measure
                // the animated height we actually produced and normalise it to a real person.
                // Self-correcting, and it makes every character the same height, which a crowd wants.
                float measured = Mathf.Max(1e-5f, bounds.size.y);
                float norm = TargetHeightMetres / measured;
                for (int i = 0; i < posPixels.Length; i++)
                {
                    var c = posPixels[i];
                    posPixels[i] = new Color(c.r * norm, c.g * norm, c.b * norm, 1f);
                }
                bounds = new Bounds(bounds.center * norm, bounds.size * norm);

                var stillVerts = smr.sharedMesh.vertices;
                for (int v = 0; v < stillVerts.Length; v++) stillVerts[v] *= norm;

                Debug.Log($"[CrowdBaker] {name}: measured {measured:F3} -> x{norm:F2} for {TargetHeightMetres} m");

                positions.SetPixels(posPixels);
                positions.Apply(false, false);
                normals.SetPixels(nrmPixels);
                normals.Apply(false, false);

                // The mesh the shader displaces. Vertex positions are irrelevant (the texture
                // supplies them) but UV1.x carries the vertex index so the shader can find its row.
                var still = new Mesh { name = name + "_crowd" };
                still.indexFormat = vertexCount > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16;
                still.vertices = stillVerts;
                still.normals = smr.sharedMesh.normals;
                still.uv = smr.sharedMesh.uv;
                var ids = new Vector2[vertexCount];
                for (int v = 0; v < vertexCount; v++)
                    ids[v] = new Vector2((v + 0.5f) / vertexCount, 0f);
                still.uv2 = ids;
                still.triangles = smr.sharedMesh.triangles;
                // Generous bounds so the animation never clips against frustum culling.
                bounds.Expand(0.5f);
                still.bounds = bounds;

                AssetDatabase.CreateAsset(still, $"{OutDir}/{name}_crowd.asset");
                AssetDatabase.CreateAsset(positions, $"{OutDir}/{name}_pos.asset");
                AssetDatabase.CreateAsset(normals, $"{OutDir}/{name}_nrm.asset");

                Debug.Log($"[CrowdBaker] {name}: {vertexCount} verts x {Frames} frames, " +
                          $"clip '{clip.name}' {clip.length:F2}s, bounds {bounds.size}");
                return true;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(instance);
            }
        }

        private static AnimationClip? PickClip(string modelPath)
        {
            var clips = AssetDatabase.LoadAllAssetsAtPath(modelPath)
                                     .OfType<AnimationClip>()
                                     .Where(c => !c.name.StartsWith("__preview__"))
                                     .ToList();
            if (clips.Count == 0) return null;

            foreach (var want in Preferred)
            {
                var hit = clips.FirstOrDefault(c => c.name.ToLowerInvariant().Contains(want));
                if (hit != null) return hit;
            }
            return clips.OrderByDescending(c => c.length).First();
        }
    }
}
