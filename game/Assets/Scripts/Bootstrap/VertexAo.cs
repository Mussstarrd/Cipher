#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Bakes occlusion into the vertex-colour channel of a built prop, so a stack of boxes stops
    /// reading as a stack of boxes.
    ///
    /// The guardhouse, the gate pillars, the sentry and the strike drone are all made of cubes,
    /// which is the decision (CLAUDE.md: the free kits have no buildings and a photoreal house
    /// beside a Quaternius tree would kill the direction outright). What makes a box read as a
    /// MADE THING rather than a primitive is not polygons; it is that the places boxes meet are
    /// darker than the places they do not, and that things get dirtier towards the ground. Both are
    /// geometry facts we already know at build time and neither costs a single instruction at
    /// runtime -- the colour rides in the vertex stream and the shader multiplies by it.
    ///
    /// TWO THINGS ARE DELIBERATE AND EASY TO "FIX" WRONGLY.
    ///
    /// It is QUANTISED. A smooth vertex gradient is a soft-shaded photograph, which is the look the
    /// ink outline and the banded lighting exist to avoid; three flat steps read as shading someone
    /// drew. <see cref="Occlusion"/> owns the ladder.
    ///
    /// It is OPT-IN AT THE MATERIAL. A mesh with no colour channel does not reliably hand the
    /// shader white, so InstancedLit's _VertexAoStrength defaults to zero and this class turns it on
    /// only for materials whose meshes it actually baked -- by cloning the material once per source,
    /// because the prop palette is shared with things that are never baked (the actors, the
    /// cruiser's light bar) and flipping the shared material would darken them by whatever garbage
    /// their colour stream happens to contain.
    ///
    /// Meshes are CLONED and cached by prop kind. Never write to a mesh that came out of
    /// GameObject.CreatePrimitive: that is the shared built-in cube and every cube in the project
    /// would inherit the guardhouse's shading. Call <see cref="ClearCache"/> when the props are
    /// destroyed -- i.e. on a fall-back to the next position -- or the baked meshes outlive them.
    /// </summary>
    public static class VertexAo
    {
        /// <summary>How far up from the prop's base the ground grime reaches, in metres.</summary>
        public const float GroundFalloff = 1.1f;

        /// <summary>How close another box has to be before a vertex counts as being in a crevice.</summary>
        public const float CreviceRadius = 0.3f;

        /// <summary>Darkest the bake is allowed to go. Below this a box reads as a hole, not a corner.</summary>
        public const float Floor = 0.58f;

        /// <summary>Flat steps between <see cref="Floor"/> and white.</summary>
        public const int Steps = 3;

        private static readonly Dictionary<string, List<Mesh>> Cache = new Dictionary<string, List<Mesh>>();
        private static readonly Dictionary<Material, Material> AoMaterials = new Dictionary<Material, Material>();
        private static readonly int VertexAoStrengthId = Shader.PropertyToID("_VertexAoStrength");

        /// <summary>How many baked meshes are cached. Diagnostics and tests.</summary>
        public static int CachedKinds => Cache.Count;

        /// <summary>
        /// The occlusion value for one vertex, in 0..1, to be multiplied into albedo.
        ///
        /// Pure, and the only part of this with any judgement in it, which is why it is separable
        /// and tested.
        /// </summary>
        /// <param name="heightAboveBase">Metres above the lowest point of the whole prop.</param>
        /// <param name="nearestGap">
        /// Distance to the nearest OTHER box in the prop; zero means the vertex is inside one.
        /// Pass <see cref="float.PositiveInfinity"/> when the prop is a single box.
        /// </param>
        public static float Occlusion(float heightAboveBase, float nearestGap)
        {
            // Grime at the base. Squared so the darkening hugs the ground instead of washing
            // halfway up a wall.
            float h = Mathf.Clamp01(heightAboveBase / GroundFalloff);
            float ground = (1f - h) * (1f - h);

            // Crevices. Anything within CreviceRadius of another box is in a corner.
            float crevice = 0f;
            if (nearestGap < CreviceRadius && !float.IsNaN(nearestGap))
            {
                float t = Mathf.Clamp01(nearestGap / CreviceRadius);
                crevice = 1f - t;
            }

            // The stronger of the two rather than their sum: a corner at ground level is a corner,
            // not twice as dark as either.
            float dark = Mathf.Max(ground, crevice);

            // Quantise, then map onto Floor..1. floor() on the value rather than smoothing it is
            // the whole point; see the class comment.
            float stepped = Mathf.Floor(dark * Steps) / Steps;
            return Mathf.Lerp(1f, Floor, Mathf.Clamp01(stepped));
        }

        /// <summary>
        /// Bakes every mesh under <paramref name="root"/>. <paramref name="kind"/> names the prop
        /// so identical props share one set of baked meshes -- twelve jersey barriers cost one bake.
        /// </summary>
        public static void Bake(GameObject root, string kind)
        {
            if (root == null) return;

            var filters = root.GetComponentsInChildren<MeshFilter>(includeInactive: true);
            if (filters.Length == 0) return;

            // All or nothing. A half-baked prop would have the AO multiply switched on for meshes
            // that never got a colour stream, which is the exact silent-garbage case the opt-in
            // material flag exists to prevent. An imported mesh with Read/Write disabled cannot be
            // read at runtime; say so once rather than baking nonsense.
            foreach (var f in filters)
            {
                var m = f.sharedMesh;
                if (m == null || !m.isReadable)
                {
                    Debug.LogWarning($"[VertexAo] {kind}: '{(m == null ? "missing" : m.name)}' is not "
                                   + "readable; skipping the whole prop rather than baking part of it");
                    return;
                }
            }

            if (Cache.TryGetValue(kind, out var cached) && cached.Count == filters.Length)
            {
                bool matched = true;
                for (int i = 0; i < filters.Length; i++)
                {
                    var source = filters[i].sharedMesh;
                    if (cached[i] == null || cached[i].vertexCount != source.vertexCount)
                    {
                        matched = false;
                        break;
                    }
                }
                if (matched)
                {
                    for (int i = 0; i < filters.Length; i++) filters[i].sharedMesh = cached[i];
                    EnableOnMaterials(root);
                    return;
                }
                // A kind whose geometry changed between calls is a bug in the caller's naming, not
                // something to paper over: drop the stale bake and redo it rather than assigning
                // meshes with the wrong vertex counts.
                Release(cached);
                Cache.Remove(kind);
            }

            // Everything is measured in the PROP's space, so the bake is independent of where the
            // prop was placed and two guardhouses at opposite ends of the map share one result.
            var toRoot = root.transform.worldToLocalMatrix;
            var boxes = new Bounds[filters.Length];
            bool any = false;
            float baseY = float.PositiveInfinity;

            for (int i = 0; i < filters.Length; i++)
            {
                var mesh = filters[i].sharedMesh;
                var m = toRoot * filters[i].transform.localToWorldMatrix;
                var b = TransformBounds(mesh.bounds, m);
                boxes[i] = b;
                baseY = Mathf.Min(baseY, b.min.y);
                any = true;
            }
            if (!any) return;

            var baked = new List<Mesh>(filters.Length);
            var vertices = new List<Vector3>(64);
            var colours = new List<Color32>(64);

            for (int i = 0; i < filters.Length; i++)
            {
                var source = filters[i].sharedMesh;
                var clone = Object.Instantiate(source);
                clone.name = $"{source.name}_AO_{kind}";

                var m = toRoot * filters[i].transform.localToWorldMatrix;
                source.GetVertices(vertices);
                colours.Clear();

                for (int v = 0; v < vertices.Count; v++)
                {
                    Vector3 p = m.MultiplyPoint3x4(vertices[v]);

                    float gap = float.PositiveInfinity;
                    for (int b = 0; b < boxes.Length; b++)
                    {
                        if (b == i) continue;
                        float d = DistanceToBox(p, boxes[b]);
                        if (d < gap) gap = d;
                    }

                    float ao = Occlusion(p.y - baseY, gap);
                    byte c = (byte)Mathf.RoundToInt(Mathf.Clamp01(ao) * 255f);
                    colours.Add(new Color32(c, c, c, 255));
                }

                clone.SetColors(colours);
                clone.UploadMeshData(markNoLongerReadable: false);
                filters[i].sharedMesh = clone;
                baked.Add(clone);
            }

            Cache[kind] = baked;
            EnableOnMaterials(root);
        }

        /// <summary>
        /// Destroys every cached mesh and AO material variant. Call when the props they belong to
        /// are destroyed; a bake that outlives its position is a leak that grows per mission.
        /// </summary>
        public static void ClearCache()
        {
            foreach (var kvp in Cache) Release(kvp.Value);
            Cache.Clear();
            foreach (var kvp in AoMaterials) Discard(kvp.Value);
            AoMaterials.Clear();
        }

        // ------------------------------------------------------------------ internals

        /// <summary>
        /// Swaps each renderer onto a clone of its material with the AO multiply switched on.
        /// One clone per source material, cached, so a prop palette of seven colours becomes at
        /// most seven more.
        /// </summary>
        private static void EnableOnMaterials(GameObject root)
        {
            var renderers = root.GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (var r in renderers)
            {
                var source = r.sharedMaterial;
                if (source == null) continue;
                if (!source.HasProperty(VertexAoStrengthId)) continue;   // not our lit shader
                if (source.GetFloat(VertexAoStrengthId) > 0.5f) continue; // already an AO variant

                if (!AoMaterials.TryGetValue(source, out var variant) || variant == null)
                {
                    variant = new Material(source) { name = source.name + " (AO)" };
                    variant.SetFloat(VertexAoStrengthId, 1f);
                    AoMaterials[source] = variant;
                }
                r.sharedMaterial = variant;
            }
        }

        private static void Release(List<Mesh> meshes)
        {
            foreach (var mesh in meshes) Discard(mesh);
        }

        /// <summary>
        /// Object.Destroy is a runtime-only call and throws in edit mode, which would make this
        /// class untestable and would break the moment anything bakes from an editor tool.
        /// </summary>
        private static void Discard(Object? o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        /// <summary>Axis-aligned bounds of a transformed box. Eight corners, no cleverness.</summary>
        private static Bounds TransformBounds(Bounds local, Matrix4x4 m)
        {
            Vector3 c = local.center, e = local.extents;
            var first = m.MultiplyPoint3x4(c + new Vector3(-e.x, -e.y, -e.z));
            var result = new Bounds(first, Vector3.zero);
            for (int i = 1; i < 8; i++)
            {
                var corner = new Vector3(
                    (i & 1) == 0 ? -e.x : e.x,
                    (i & 2) == 0 ? -e.y : e.y,
                    (i & 4) == 0 ? -e.z : e.z);
                result.Encapsulate(m.MultiplyPoint3x4(c + corner));
            }
            return result;
        }

        /// <summary>Distance from a point to a box; zero inside it.</summary>
        private static float DistanceToBox(Vector3 p, Bounds b)
        {
            if (b.size.sqrMagnitude <= 0f) return float.PositiveInfinity;
            float dx = Mathf.Max(b.min.x - p.x, 0f, p.x - b.max.x);
            float dy = Mathf.Max(b.min.y - p.y, 0f, p.y - b.max.y);
            float dz = Mathf.Max(b.min.z - p.z, 0f, p.z - b.max.z);
            return Mathf.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
