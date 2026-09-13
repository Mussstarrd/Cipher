#nullable enable
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Cipher.Game.Editor
{
    /// <summary>
    /// Converts the bought Town buildings into props the site dresser can place.
    ///
    /// The seam this uses already existed and is the reason a swap like this is safe at all:
    /// <c>PropCatalog</c> owns a prop's FOOTPRINT in grid cells and <c>SiteProps</c> owns its boxes.
    /// Replacing the boxes touches only the second of those, so the footprints written into the one
    /// GridMap before the first flow field are unchanged and hard rule 4 -- the preview can never
    /// lie -- holds exactly as it did.
    ///
    /// SCALE IS THE WHOLE DESIGN CONSTRAINT HERE, not the art. A model is placed at NATIVE scale and
    /// the footprint is authored to match it, never the other way round. Squeezing a ten-metre house
    /// into a six-metre footprint also squeezes its doors to four feet, and a doorway shorter than
    /// the people walking past it is the kind of wrongness nobody can name but everybody sees.
    /// Measured with SyntyPropMeasure: House_Preset_01 is 6.4 x 8.9m, which is why House went from
    /// 6x5 cells to 6x9 rather than the model going to 0.6 scale.
    ///
    /// Run headless:
    ///   Unity.exe -batchmode -projectPath game -executeMethod Cipher.Game.Editor.SyntyPropBuilder.BuildAll -quit
    /// </summary>
    public static class SyntyPropBuilder
    {
        private const string OutDir = "Assets/Resources/Props";
        private const string TownPrefabs = "Assets/Synty/PolygonTown/Prefabs";

        /// <summary>
        /// Which bought prefab stands in for which of our prop kinds.
        ///
        /// Houses get THREE variants under one kind. A street of one repeated house reads as a
        /// texture rather than a neighbourhood, and the dresser already has a stable per-cell seed
        /// to choose with -- the same one that decides siding colour today.
        /// </summary>
        private static readonly Dictionary<string, string[]> Mapping = new()
        {
            ["House"] = new[]
            {
                TownPrefabs + "/Buildings/Presets/SM_Bld_House_Preset_01.prefab",
                TownPrefabs + "/Buildings/Presets/SM_Bld_House_Preset_02.prefab",
                TownPrefabs + "/Buildings/Presets/SM_Bld_House_Preset_03.prefab",
            },

            // The clubhouse is the biggest single obstruction on the map, so it was also the
            // biggest remaining box. Shop_02 measures 15.5 x 10.9m against a 16x12 footprint --
            // the closest native fit in the pack, and no scaling needed.
            ["Clubhouse"] = new[] { TownPrefabs + "/Buildings/SM_Bld_Shop_02.prefab" },
        };

        [MenuItem("Cipher/Art/Build Synty Props")]
        public static void BuildAll()
        {
            if (!AssetDatabase.IsValidFolder(OutDir))
            {
                AssetDatabase.CreateFolder("Assets/Resources", "Props");
                AssetDatabase.Refresh();
            }

            var shader = Shader.Find("Exodus/InstancedLit");
            if (shader == null) { Debug.LogError("[Props] Exodus/InstancedLit missing"); return; }

            var cache = new Dictionary<Texture, Material>();
            int built = 0;

            foreach (var (kind, paths) in Mapping)
            {
                for (int v = 0; v < paths.Length; v++)
                {
                    var src = AssetDatabase.LoadAssetAtPath<GameObject>(paths[v]);
                    if (src == null) { Debug.LogWarning($"[Props] missing {paths[v]}"); continue; }

                    var instance = (GameObject)PrefabUtility.InstantiatePrefab(src);
                    try
                    {
                        Reskin(instance, shader, cache);

                        // Buildings are the one thing that must NOT get a smoothed-normal hull. A
                        // house is all hard edges by design, and averaging its normals rounds the
                        // eaves and the corner boards into mush. The flat-normal hull is correct
                        // here, which is why SmoothNormals is deliberately not called.
                        string outPath = $"{OutDir}/{kind}_{v:00}.prefab";
                        PrefabUtility.SaveAsPrefabAsset(instance, outPath);
                        Debug.Log($"[Props] {kind} variant {v}: {Path.GetFileNameWithoutExtension(paths[v])}");
                        built++;
                    }
                    finally { Object.DestroyImmediate(instance); }
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Props] built {built} prop prefabs, {cache.Count} shared materials");
        }

        private static void Reskin(GameObject root, Shader shader, Dictionary<Texture, Material> cache)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];

                for (int i = 0; i < src.Length; i++)
                {
                    Texture? tex = null;
                    var m = src[i];
                    if (m != null)
                    {
                        foreach (var prop in new[] { "_Albedo_Map", "_BaseMap", "_MainTex" })
                        {
                            if (!m.HasProperty(prop)) continue;
                            tex = m.GetTexture(prop);
                            if (tex != null) break;
                        }
                        if (tex == null) tex = m.mainTexture;
                    }

                    if (tex != null && cache.TryGetValue(tex, out var got)) { dst[i] = got; continue; }

                    var made = new Material(shader) { name = tex != null ? $"Prop_{tex.name}" : "Prop_Untextured" };
                    made.SetColor("_BaseColor", Color.white);
                    if (tex != null) made.SetTexture("_BaseMap", tex);

                    // A LIGHTER INK LINE THAN A CHARACTER GETS. A building is large and close to the
                    // camera, and an outline scaled for a body at forty metres turns every eave into
                    // a thick black band up close.
                    made.SetFloat("_OutlineWidth", 0.02f);
                    made.enableInstancing = true;

                    string path = $"{OutDir}/{made.name}.mat";
                    AssetDatabase.CreateAsset(made, path);
                    if (tex != null) cache[tex] = made;
                    dst[i] = made;
                }

                r.sharedMaterials = dst;
            }
        }
    }
}
