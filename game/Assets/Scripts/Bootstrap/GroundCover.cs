#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Grid;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Dry winter grass, weeds and leaf litter over the open ground.
    ///
    /// Owner, 2026-09-12: "The ground should have some vegetation at this point." He is right, and
    /// the reason it matters is not decoration -- a flat coloured plane gives the eye nothing to
    /// measure movement against, so the whole level reads as a diorama no matter how good the
    /// buildings are. Ground cover is what turns a surface into a place you are walking across.
    ///
    /// ADR-004 puts this in a Virginia winter and the ground texture is already brown leaf litter,
    /// so this is **dead growth**: bleached tufts, dry weeds, the odd bramble. Not a lawn. A green
    /// meadow here would fight the lighting and the fiction at the same time.
    ///
    /// **Instanced and static.** The scatter is computed once from the map's seed and drawn every
    /// frame in batches -- no per-frame culling, no GameObjects, no colliders. A few thousand tufts
    /// cost a handful of draw calls, which is the whole reason this is affordable at all next to
    /// nine hundred prop renderers.
    /// </summary>
    public static class GroundCover
    {
        /// <summary>Tufts per open cell. Above about 2 this stops reading as ground and starts reading as a field.</summary>
        public const float Density = 2.4f;

        /// <summary>
        /// Builds one tuft: three quads crossed through the vertical axis, tapered to a point.
        ///
        /// Crossed quads rather than billboards on purpose. A billboard has to be rebuilt or rotated
        /// every frame, which forfeits the whole point of a static instanced scatter, and under an
        /// ink shader with a hard outline a rotating card reads as a flicker. Three fixed blades
        /// look the same from every angle for free.
        /// </summary>
        public static Mesh BuildTuft()
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var normals = new List<Vector3>();

            for (int blade = 0; blade < 3; blade++)
            {
                float yaw = blade * (Mathf.PI / 3f);
                var right = new Vector3(Mathf.Cos(yaw), 0f, Mathf.Sin(yaw));
                // Each blade leans a different way, so a tuft is not a perfect star.
                var lean = new Vector3(Mathf.Cos(yaw + 1.1f), 0f, Mathf.Sin(yaw + 1.1f)) * 0.13f;

                // NARROW. The first version used a half-metre base tapering to a point, which at
                // any useful density reads as a field of paper cones rather than grass -- a blade
                // is defined by being much taller than it is wide, and the moment it is not, the
                // eye calls it a shard. Base is a fifth of the height and the tip barely closes.
                int b = verts.Count;
                verts.Add(-right * 0.10f);                      // base left
                verts.Add(right * 0.10f);                       // base right
                verts.Add(Vector3.up + lean + right * 0.03f);   // tip
                verts.Add(Vector3.up + lean - right * 0.03f);

                var n = Vector3.Cross(right, Vector3.up).normalized;
                for (int i = 0; i < 4; i++) normals.Add(n);

                tris.Add(b); tris.Add(b + 3); tris.Add(b + 2);
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 1);
                // Backfaces, because a single-sided blade vanishes from half the map.
                tris.Add(b); tris.Add(b + 2); tris.Add(b + 3);
                tris.Add(b); tris.Add(b + 1); tris.Add(b + 2);
            }

            var mesh = new Mesh { name = "GrassTuft" };
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetNormals(normals);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// Where the tufts go. Deterministic from <paramref name="seed"/>, so a position looks the
        /// same every time it is loaded and a screenshot can be compared against another.
        ///
        /// <paramref name="isOpen"/> decides what counts as ground worth growing on -- the caller
        /// knows about walls, buildings and the road; this does not.
        /// </summary>
        public static List<Matrix4x4> Scatter(GridMap map, ulong seed, Func<int, int, bool> isOpen,
                                              float density = Density)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));
            if (isOpen == null) throw new ArgumentNullException(nameof(isOpen));

            var list = new List<Matrix4x4>(4096);
            ulong s = seed == 0 ? 0x9E3779B97F4A7C15UL : seed;

            float Next()
            {
                s ^= s << 13; s ^= s >> 7; s ^= s << 17;
                return (s >> 40) * (1f / 16777216f);
            }

            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    if (!isOpen(x, y)) continue;

                    // Fractional density becomes a per-cell chance, so 1.35 means "one always, and
                    // a second about a third of the time" rather than a hard round.
                    int whole = Mathf.FloorToInt(density);
                    int n = whole + (Next() < density - whole ? 1 : 0);

                    for (int i = 0; i < n; i++)
                    {
                        var at = new Vector3(x + Next(), 0f, y + Next());
                        // Ankle height, not knee. Squared so most of it is short and the taller
                        // weeds are the exception -- an even distribution reads as mown.
                        float height = Mathf.Lerp(0.10f, 0.34f, Next() * Next());
                        float spread = Mathf.Lerp(0.55f, 1.0f, Next());
                        var rot = Quaternion.Euler(0f, Next() * 360f, 0f);
                        list.Add(Matrix4x4.TRS(at, rot, new Vector3(spread, height, spread)));
                    }
                }
            }
            return list;
        }
    }
}
