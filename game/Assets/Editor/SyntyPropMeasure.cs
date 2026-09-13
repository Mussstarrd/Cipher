#nullable enable
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Reports the real footprint, in metres, of the Synty props we are considering.
    ///
    /// Exists because swapping a box for a model is only safe if the model FITS THE FOOTPRINT the
    /// grid already believes is solid. PropCatalog says a House is 6x5 cells and one cell is one
    /// metre; if a Synty house is ten metres wide, then either the footprint grows (which changes
    /// every gap on the map and could seal a gate) or the model shrinks (which shrinks its doors
    /// below the height of the people walking past them). Both are real costs, and choosing between
    /// them on a guess is how you get a neighbourhood of dolls' houses.
    ///
    /// Measures the way CLAUDE.md insists on: mesh bounds through the renderer's transform, on a
    /// throwaway instance, because renderer bounds on a fresh prefab are not real.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.SyntyPropMeasure.Report -quit
    /// </summary>
    public static class SyntyPropMeasure
    {
        private static readonly string[] Wanted =
        {
            "Assets/Synty/PolygonTown/Prefabs/Buildings/SM_Bld_Church_01.prefab",
            "Assets/Synty/PolygonTown/Prefabs/Buildings/SM_Bld_Shop_01.prefab",
            "Assets/Synty/PolygonTown/Prefabs/Buildings/SM_Bld_Shop_02.prefab",
            "Assets/Synty/PolygonTown/Prefabs/Buildings/SM_Bld_Shop_03.prefab",
            "Assets/Synty/PolygonTown/Prefabs/Buildings/SM_Bld_Shop_Concrete_01.prefab",
            "Assets/Synty/PolygonTown/Prefabs/Buildings/SM_Bld_GardenShed_01.prefab",
            "Assets/Synty/PolygonTown/Prefabs/Buildings/Presets/SM_Bld_House_Preset_Garage_01.prefab",
        };

        [MenuItem("Cipher/Art/Measure Synty Props")]
        public static void Report()
        {
            foreach (string path in Wanted)
            {
                var src = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (src == null) { Debug.LogWarning($"[Measure] missing {path}"); continue; }

                var go = (GameObject)PrefabUtility.InstantiatePrefab(src);
                try
                {
                    var size = Measure(go);
                    Debug.Log($"[Measure] {Path.GetFileNameWithoutExtension(path)}: " +
                              $"{size.x:F1} x {size.z:F1} m footprint, {size.y:F1} m tall");
                }
                finally { Object.DestroyImmediate(go); }
            }
        }

        private static Vector3 Measure(GameObject root)
        {
            var toRoot = root.transform.worldToLocalMatrix;
            bool any = false;
            Vector3 lo = Vector3.positiveInfinity, hi = Vector3.negativeInfinity;

            void Accumulate(Mesh? mesh, Transform t)
            {
                if (mesh == null) return;
                var m = toRoot * t.localToWorldMatrix;
                var b = mesh.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = new Vector3(
                        (i & 1) == 0 ? b.min.x : b.max.x,
                        (i & 2) == 0 ? b.min.y : b.max.y,
                        (i & 4) == 0 ? b.min.z : b.max.z);
                    var p = m.MultiplyPoint3x4(corner);
                    lo = Vector3.Min(lo, p);
                    hi = Vector3.Max(hi, p);
                    any = true;
                }
            }

            foreach (var mf in root.GetComponentsInChildren<MeshFilter>(true)) Accumulate(mf.sharedMesh, mf.transform);
            foreach (var smr in root.GetComponentsInChildren<SkinnedMeshRenderer>(true)) Accumulate(smr.sharedMesh, smr.transform);

            return any ? hi - lo : Vector3.zero;
        }
    }
}
