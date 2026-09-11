#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Grid;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Scatters trees, bushes and abandoned cars around the play area.
    ///
    /// Deterministic on purpose: the same seed dresses the same map the same way every run, so a
    /// screenshot is reproducible and a playtest note about "the tree by the gate" means something.
    ///
    /// Placement rules exist because random scatter looks wrong in a specific way. Trees cluster in
    /// the untravelled margins the way real woodland does rather than sprinkling evenly; nothing is
    /// allowed onto a wall cell or the lane the horde walks; and cars sit on the open ground where an
    /// abandoned vehicle plausibly stopped, not in the middle of a field.
    /// </summary>
    public sealed class EnvironmentDresser
    {
        private readonly GridMap _map;
        private readonly Transform _root;
        private readonly Func<Material, Material> _reskin;

        public EnvironmentDresser(GridMap map, Transform root, Func<Material, Material> reskin)
        {
            _map = map;
            _root = root;
            _reskin = reskin;
        }

        public int Placed { get; private set; }

        /// <summary>
        /// Cells this dresser turned solid, newest last.
        ///
        /// Scenery you can walk through is scenery, not cover. Owner, 2026-09-11: "I'm able to walk
        /// through everything in the environment except for the pre-placed walls." A tree trunk and
        /// an abandoned car should stop you, stop a bullet, and make the horde walk round -- which
        /// is the whole point of cover props in the design spec.
        ///
        /// Recorded in placement order so the caller can give cells BACK if the scatter turns out to
        /// have sealed a gate off from the vault. Nothing may quietly make a mission unplayable.
        /// </summary>
        public List<(int X, int Y)> SolidCells { get; } = new List<(int, int)>();

        /// <summary>
        /// Dresses the map. <paramref name="keepClear"/> is a predicate returning true for cells that
        /// must stay empty, which is how the spawn lane and the objective stay walkable.
        /// </summary>
        public void Dress(IReadOnlyList<GameObject> trees,
                          IReadOnlyList<GameObject> bushes,
                          IReadOnlyList<GameObject> cars,
                          Func<int, int, bool> keepClear,
                          ulong seed = 20260911UL)
        {
            var rng = new Rng(seed);

            // Trees hug the top and bottom margins, which is where a wooded lot would actually be,
            // and thin out toward the middle so the fighting ground stays readable.
            if (trees.Count > 0)
            {
                for (int x = 1; x < _map.Width - 1; x++)
                {
                    for (int y = 1; y < _map.Height - 1; y++)
                    {
                        float edge = Mathf.Min(y, _map.Height - 1 - y) / (float)(_map.Height * 0.5f);
                        float chance = Mathf.Lerp(0.34f, 0.0f, Mathf.Clamp01(edge * 2.2f));
                        if (chance <= 0.001f || rng.NextFloat() > chance) continue;
                        if (!Free(x, y, keepClear)) continue;

                        Place(trees[rng.NextInt(trees.Count)], x, y, rng,
                              scaleMin: 0.85f, scaleMax: 1.35f, pitchCorrection: -90f, solid: true);
                    }
                }
            }

            if (bushes.Count > 0)
            {
                for (int i = 0; i < 90; i++)
                {
                    int x = 1 + rng.NextInt(_map.Width - 2);
                    int y = 1 + rng.NextInt(_map.Height - 2);
                    if (!Free(x, y, keepClear)) continue;
                    Place(bushes[rng.NextInt(bushes.Count)], x, y, rng, 0.7f, 1.2f);
                }
            }

            // A handful of cars, stopped where a road would be: along the open middle band.
            if (cars.Count > 0)
            {
                int midLow = _map.Height / 2 - 6, midHigh = _map.Height / 2 + 6;
                for (int i = 0; i < 14; i++)
                {
                    int x = 3 + rng.NextInt(_map.Width - 6);
                    int y = midLow + rng.NextInt(Mathf.Max(1, midHigh - midLow));
                    if (!Free(x, y, keepClear)) continue;
                    Place(cars[rng.NextInt(cars.Count)], x, y, rng, 1f, 1f, flatRotation: true, solid: true);
                }
            }
        }

        /// <summary>
        /// Lays a road along a horizontal corridor.
        ///
        /// Everything about the tile is MEASURED, because an imported kit arrives at whatever scale
        /// and orientation its author used. Two guesses cost real time here. The first took the
        /// tile's largest horizontal extent as its width, which is its length, so the scale came out
        /// 1 and the road shipped as a one-cell bar standing proud of the ground like a kerbstone.
        /// The second assumed the tile lies flat in XZ: this one is authored Z-up, so its footprint
        /// is in X/Y and it stood on its edge like a fence panel.
        ///
        /// So nothing is assumed. The tile is measured, its THINNEST axis is rotated onto Y (that
        /// axis is the road surface's normal, whichever one it happens to be), its longest remaining
        /// axis is turned along the corridor, the short one is scaled to the corridor width, and the
        /// whole thing is sunk so its top face sits a hair above the ground plane rather than as a
        /// slab on top of it.
        /// </summary>
        public void LayRoad(GameObject tile, int centreY, int widthCells, Func<int, int, bool> blocked)
        {
            if (tile == null) return;

            var points = CollectMeshCorners(tile);
            if (points.Count == 0) return;

            // Stand the tile up the right way: the thinnest axis is the surface normal.
            var raw = BoundsOf(points, Quaternion.identity);
            Quaternion upright =
                raw.size.y <= raw.size.x && raw.size.y <= raw.size.z ? Quaternion.identity
                : raw.size.z <= raw.size.x ? Quaternion.Euler(-90f, 0f, 0f)   // Z-up authoring
                : Quaternion.Euler(0f, 0f, 90f);                              // X-up authoring

            // Then put its long axis along the corridor, which runs in X.
            var flat = BoundsOf(points, upright);
            Quaternion turn = flat.size.x >= flat.size.z
                ? upright
                : Quaternion.Euler(0f, 90f, 0f) * upright;

            var laid = BoundsOf(points, turn);
            float across = laid.size.z;   // kerb to kerb
            float along = laid.size.x;    // direction of travel
            if (across <= 1e-4f || along <= 1e-4f) return;

            // Uniform scale, so the surface markings keep their proportions. One tile then covers
            // `span` cells of road.
            float scale = widthCells / across;
            float span = along * scale;
            if (span < 0.5f) return;

            float lift = SurfaceLift - laid.max.y * scale;
            var offset = new Vector3(laid.center.x * scale, 0f, laid.center.z * scale);

            // One material per source material for the whole road, not one per tile.
            var tinted = new Dictionary<Material, Material>();

            for (float x = 0f; x < _map.Width; x += span)
            {
                // Pull the last tile back inside the map instead of letting it hang over the edge
                // of the ground plane. A few centimetres of overlap on one seam is invisible; a
                // road ending in mid-air is not.
                bool clamped = !(span >= _map.Width) && x > _map.Width - span;
                float left = clamped ? _map.Width - span : x;

                // Test EVERY cell the tile covers, not just the one under its centre. A six-cell
                // tile tested at its centre steps straight over a one-cell wall: on this map the
                // wall columns fell between the centres, so the predicate never fired once and the
                // road was laid clean through all three walls.
                if (FootprintBlocked(left, span, centreY, widthCells, blocked)) continue;

                var go = UnityEngine.Object.Instantiate(tile, _root);
                go.transform.rotation = turn;
                go.transform.localScale = tile.transform.localScale * scale;
                // The pulled-back last tile sits ON TOP of the one before it. Coplanar surfaces
                // z-fight, which is the one artefact SurfaceLift exists to avoid, so lift it clear.
                go.transform.position = new Vector3(
                    left + span * 0.5f - offset.x,
                    clamped ? lift + SurfaceLift : lift,
                    centreY + 0.5f - offset.z);

                ApplyShared(go, tinted, RoadTint);
                Placed++;
            }
        }

        /// <summary>True if any cell under a tile's footprint is blocked.</summary>
        private bool FootprintBlocked(float left, float span, int centreY, int widthCells,
                                      Func<int, int, bool> blocked)
        {
            int x0 = Mathf.Max(0, Mathf.FloorToInt(left));
            int x1 = Mathf.Min(_map.Width - 1, Mathf.CeilToInt(left + span) - 1);
            int half = Mathf.Max(0, widthCells / 2);
            int y0 = Mathf.Max(0, centreY - half);
            int y1 = Mathf.Min(_map.Height - 1, centreY + half);

            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (blocked(x, y)) return true;
            return false;
        }

        /// <summary>Wet winter asphalt, darker than the leaf litter either side of it.</summary>
        private static readonly Color RoadTint = new Color(0.17f, 0.17f, 0.18f);

        /// <summary>How far above the ground plane a flat surface sits. Coplanar faces flicker.</summary>
        private const float SurfaceLift = 0.012f;

        /// <summary>
        /// Every mesh corner of a prefab, in the prefab's own space at its own scale.
        ///
        /// This instantiates a throwaway copy at the origin rather than doing the matrix arithmetic
        /// against the asset's transform: an FBX root carries an import scale that the naive
        /// worldToLocal/localToWorld round trip does not cancel, and the first version of this
        /// measured a six-metre road tile as two centimetres across.
        /// </summary>
        private static List<Vector3> CollectMeshCorners(GameObject prefab)
        {
            var points = new List<Vector3>();
            var probe = UnityEngine.Object.Instantiate(prefab);
            try
            {
                probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

                foreach (var filter in probe.GetComponentsInChildren<MeshFilter>())
                {
                    var mesh = filter.sharedMesh;
                    if (mesh == null) continue;

                    var m = filter.transform.localToWorldMatrix;
                    var c = mesh.bounds.center;
                    var e = mesh.bounds.extents;
                    for (int i = 0; i < 8; i++)
                        points.Add(m.MultiplyPoint3x4(new Vector3(
                            c.x + ((i & 1) == 0 ? -e.x : e.x),
                            c.y + ((i & 2) == 0 ? -e.y : e.y),
                            c.z + ((i & 4) == 0 ? -e.z : e.z))));
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(probe);
            }
            return points;
        }

        /// <summary>Axis-aligned bounds of a point cloud after <paramref name="rotation"/>.</summary>
        private static Bounds BoundsOf(List<Vector3> points, Quaternion rotation)
        {
            var b = new Bounds(rotation * points[0], Vector3.zero);
            for (int i = 1; i < points.Count; i++) b.Encapsulate(rotation * points[i]);
            return b;
        }

        /// <summary>
        /// Reskins a prop, sharing one material per distinct source material via
        /// <paramref name="cache"/>. Without the cache a scattered kit produces a unique Material
        /// per renderer per instance: hundreds of identical materials, and as many draw calls that
        /// cannot batch.
        /// </summary>
        private void ApplyShared(GameObject go, Dictionary<Material, Material> cache, Color? forceTint)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                var swapped = new Material[mats.Length];
                for (int m = 0; m < mats.Length; m++)
                {
                    // An empty material slot has no key to cache under, so it gets its own single
                    // slot. Without this it was the one case that still allocated a fresh Material
                    // per renderer per instance, which is precisely what the cache is here to stop.
                    var key = mats[m];
                    Material? made;
                    if (key == null)
                    {
                        made = _emptySlotMaterial ??= Tint(_reskin(null), forceTint);
                    }
                    else if (!cache.TryGetValue(key, out made))
                    {
                        made = Tint(_reskin(key), forceTint);
                        cache[key] = made;
                    }
                    swapped[m] = made;
                }
                r.sharedMaterials = swapped;
            }
        }

        private void Reskin(GameObject go) => ApplyShared(go, _propMaterials, null);

        private readonly Dictionary<Material, Material> _propMaterials =
            new Dictionary<Material, Material>();

        private static Material Tint(Material mat, Color? forceTint)
        {
            if (!forceTint.HasValue) return mat;
            mat.color = forceTint.Value;
            mat.SetColor(BaseColorId, forceTint.Value);
            mat.mainTexture = null;
            return mat;
        }

        /// <summary>The one material standing in for every empty material slot in the kit.</summary>
        private Material? _emptySlotMaterial;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private bool Free(int x, int y, Func<int, int, bool> keepClear)
        {
            if (x < 0 || y < 0 || x >= _map.Width || y >= _map.Height) return false;
            if (_map.KindAt(x, y) != WallKind.None) return false;
            return !keepClear(x, y);
        }

        private void Place(GameObject prefab, int x, int y, Rng rng,
                           float scaleMin, float scaleMax, bool flatRotation = false,
                           float pitchCorrection = 0f, bool solid = false)
        {
            if (prefab == null) return;

            // Rock, not Wall: a tree is not something a sapper opens a hole in, and Rock also stops
            // a bullet, which is what makes a trunk worth standing behind.
            if (solid && MakeSolid(x, y)) { /* recorded below */ }

            var go = UnityEngine.Object.Instantiate(prefab, _root);
            go.transform.position = new Vector3(
                x + 0.5f + (rng.NextFloat() - 0.5f) * 0.7f,
                0f,
                y + 0.5f + (rng.NextFloat() - 0.5f) * 0.7f);

            float yaw = flatRotation ? (rng.NextInt(4) * 90f + (rng.NextFloat() - 0.5f) * 12f)
                                     : rng.NextFloat() * 360f;
            // pitchCorrection exists because some models in this pack arrive on their side. Cars and
            // bushes import upright; the bare trees do not, and a forest lying flat on the ground is
            // the most obvious possible tell that nobody looked at the result.
            go.transform.rotation = Quaternion.Euler(pitchCorrection, yaw, 0f);
            go.transform.localScale *= Mathf.Lerp(scaleMin, scaleMax, rng.NextFloat());

            Reskin(go);
            OnPlaced?.Invoke(prefab.name, go);
            Placed++;
        }

        /// <summary>Raised for every scattered prop, so the caller can dress specific ones further.</summary>
        public Action<string, GameObject>? OnPlaced;

        /// <summary>Turns one cell solid, if it is free to take. Returns whether it did.</summary>
        private bool MakeSolid(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _map.Width || y >= _map.Height) return false;
            if (_map.KindAt(x, y) != WallKind.None) return false;
            _map.SetWall(x, y, WallKind.Rock, GridMap.DefaultWallHp);
            SolidCells.Add((x, y));
            return true;
        }

        /// <summary>Gives a cell back, when the scatter turned out to block a route.</summary>
        public void ClearSolid(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _map.Width || y >= _map.Height) return;
            if (_map.KindAt(x, y) != WallKind.Rock) return;
            _map.SetWall(x, y, WallKind.None, 0);
        }

        /// <summary>Small deterministic generator, so dressing never touches UnityEngine.Random.</summary>
        private struct Rng
        {
            private ulong _s;
            public Rng(ulong seed) { _s = seed == 0 ? 0x9E3779B97F4A7C15UL : seed; }

            private ulong Next()
            {
                _s ^= _s << 13; _s ^= _s >> 7; _s ^= _s << 17;
                return _s;
            }

            public float NextFloat() => (Next() >> 40) * (1f / 16777216f);
            public int NextInt(int maxExclusive) =>
                maxExclusive <= 0 ? 0 : (int)(Next() % (ulong)maxExclusive);
        }
    }
}
