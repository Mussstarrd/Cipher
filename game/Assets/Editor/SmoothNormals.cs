#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Writes averaged "smooth" normals into a mesh's tangent channel, for inverted-hull outlines.
    ///
    /// The problem this solves: an inverted-hull outline extrudes every vertex along its normal. That
    /// works on a sphere and falls apart on a character. Any hard edge or UV seam splits one position
    /// into several vertices with different normals, so the hull tears open at every seam and the
    /// outline reads as ragged, blurry and smeared rather than as a drawn line.
    ///
    /// The fix is standard: group vertices by POSITION, average their normals, and extrude along that
    /// average instead. The shading normal stays untouched, so lighting is unaffected; only the
    /// outline hull uses the smoothed copy. Storing it in the tangent channel means it is skinned
    /// along with the mesh, which matters because these characters are animated.
    /// </summary>
    public static class SmoothNormals
    {
        /// <summary>Positions closer than this are treated as the same point.</summary>
        private const float WeldEpsilon = 1e-4f;

        public static void Write(Mesh mesh)
        {
            if (mesh == null) return;

            var vertices = mesh.vertices;
            var normals = mesh.normals;
            if (vertices.Length == 0 || normals.Length != vertices.Length) return;

            // Bucket by quantised position so float noise does not split a weld.
            var groups = new Dictionary<Vector3Int, List<int>>(vertices.Length);
            float inv = 1f / WeldEpsilon;
            for (int i = 0; i < vertices.Length; i++)
            {
                var p = vertices[i];
                var key = new Vector3Int(
                    Mathf.RoundToInt(p.x * inv),
                    Mathf.RoundToInt(p.y * inv),
                    Mathf.RoundToInt(p.z * inv));
                if (!groups.TryGetValue(key, out var list))
                {
                    list = new List<int>(4);
                    groups[key] = list;
                }
                list.Add(i);
            }

            var smoothed = new Vector4[vertices.Length];
            foreach (var pair in groups)
            {
                var members = pair.Value;

                Vector3 sum = Vector3.zero;
                for (int k = 0; k < members.Count; k++) sum += normals[members[k]];

                Vector3 avg = sum.sqrMagnitude > 1e-12f ? sum.normalized : Vector3.up;
                var packed = new Vector4(avg.x, avg.y, avg.z, 1f);
                for (int k = 0; k < members.Count; k++) smoothed[members[k]] = packed;
            }

            mesh.tangents = smoothed;
        }

        /// <summary>
        /// Applies smooth normals to every mesh under a hierarchy. Meshes are shared assets, so this
        /// operates on copies to avoid writing tangents back into the imported FBX.
        /// </summary>
        public static void ApplyToHierarchy(GameObject root, string meshAssetPath)
        {
            if (root == null || string.IsNullOrEmpty(meshAssetPath)) return;

            // The copies MUST be saved as assets. A mesh created in memory and assigned to a prefab
            // does not survive the save: the prefab ends up referencing nothing and the character
            // renders as empty space, which is exactly what happened the first time.
            Mesh? container = null;
            int index = 0;

            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (smr.sharedMesh == null) continue;
                smr.sharedMesh = Persist(smr.sharedMesh, meshAssetPath, ref container, ref index);
            }

            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (filter.sharedMesh == null) continue;
                filter.sharedMesh = Persist(filter.sharedMesh, meshAssetPath, ref container, ref index);
            }

            if (container != null)
            {
                UnityEditor.AssetDatabase.SaveAssets();
                UnityEditor.AssetDatabase.ImportAsset(meshAssetPath);
            }
        }

        private static Mesh Persist(Mesh source, string path, ref Mesh? container, ref int index)
        {
            var copy = Object.Instantiate(source);
            copy.name = source.name + "_smooth" + (index == 0 ? "" : index.ToString());
            Write(copy);

            if (container == null)
            {
                UnityEditor.AssetDatabase.CreateAsset(copy, path);
                container = copy;
            }
            else
            {
                UnityEditor.AssetDatabase.AddObjectToAsset(copy, path);
            }
            index++;
            return copy;
        }
    }
}
