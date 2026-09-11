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
        /// Lays the carriageway along a horizontal corridor: asphalt, shoulders, a dashed centre
        /// line and solid edge lines, as ONE road rather than two strips with a gap down it.
        ///
        /// THIS USED TO INSTANTIATE THE IMPORTED STREET TILE AND THAT WAS THE BUG. `Street_Straight`
        /// is a divided kit piece -- two carriageways with a median between them -- so laying it
        /// down the middle of the map produced exactly what the owner reported: "the two Road Lanes
        /// need to be together". It is also Z-up authored, which had already cost this project a
        /// session, and the surface markings it carries were being flattened to a single tint by
        /// the reskin anyway. Three problems, all of them downstream of using a model for something
        /// a model is not needed for.
        ///
        /// So the road is drawn, not imported. Under the ink shader a road IS flat bands of colour:
        /// dark asphalt, a pale gravel shoulder either side, white edge lines and a dashed yellow
        /// centre. That is the whole picture, it is what the art direction already is, and it costs
        /// six boxes per run of clear cells instead of a mesh every six metres.
        ///
        /// <paramref name="tile"/> is kept only as the source of a material to reskin, so the road
        /// still picks up whatever shader setup the kit's asphalt had; its GEOMETRY is not used.
        /// The parameter stays because the bootstrap's call site is not ours to change.
        ///
        /// Markings are BOXES WITH THICKNESS sitting proud of the asphalt, not coplanar decals. A
        /// stripe painted at the same height as the surface under it z-fights from the overhead
        /// camera, which is the one view the player plans in.
        /// </summary>
        public void LayRoad(GameObject tile, int centreY, int widthCells, Func<int, int, bool> blocked)
        {
            if (widthCells < 2) return;

            Material? source = null;
            if (tile != null)
                foreach (var r in tile.GetComponentsInChildren<Renderer>())
                {
                    if (r.sharedMaterial == null) continue;
                    source = r.sharedMaterial;
                    break;
                }

            var asphalt = Tint(_reskin(source), RoadTint);
            var shoulder = Tint(_reskin(source), ShoulderTint);
            var paintWhite = Tint(_reskin(source), EdgeLineTint);
            var paintYellow = Tint(_reskin(source), CentreLineTint);

            float half = widthCells * 0.5f;
            float mid = centreY + 0.5f;

            // Walk the corridor and lay one slab per contiguous run of clear cells. A wall across
            // the road leaves a HOLE in the road, which is what a road with something dragged
            // across it should look like; the old code skipped a whole six-metre tile instead and
            // left the carriageway ending in mid-air either side of the barricade.
            int runStart = -1;
            for (int x = 0; x <= _map.Width; x++)
            {
                bool clear = x < _map.Width && !ColumnBlocked(x, centreY, widthCells, blocked);
                if (clear && runStart < 0) runStart = x;
                if (clear || runStart < 0) continue;

                LayRun(runStart, x, mid, half, asphalt, shoulder, paintWhite, paintYellow);
                runStart = -1;
            }
        }

        /// <summary>One contiguous stretch of carriageway, from cell <paramref name="x0"/> to
        /// <paramref name="x1"/> exclusive.</summary>
        private void LayRun(int x0, int x1, float mid, float half,
                            Material asphalt, Material shoulder, Material white, Material yellow)
        {
            float length = x1 - x0;
            if (length < 0.5f) return;
            float cx = (x0 + x1) * 0.5f;

            // Gravel shoulder, slightly wider than the asphalt and slightly lower, so the edge of
            // the road is a step rather than a line where asphalt meets field.
            Slab(shoulder, cx, mid, length, half * 2f + ShoulderWidth * 2f, SurfaceLift * 0.5f);
            Slab(asphalt, cx, mid, length, half * 2f, SurfaceLift);

            // Edge lines, one per side, inset from the kerb the way real ones are.
            float edge = half - EdgeInset;
            Slab(white, cx, mid - edge, length, LineWidth, PaintLift);
            Slab(white, cx, mid + edge, length, LineWidth, PaintLift);

            // Dashed centre line. The dash phase is taken from the WORLD position, not from the
            // start of this run, so the dashes stay in step across a gap in the carriageway.
            for (float d = Mathf.Ceil(x0 / DashPitch) * DashPitch; d < x1; d += DashPitch)
            {
                float from = Mathf.Max(d, x0);
                float to = Mathf.Min(d + DashLength, x1);
                if (to - from < 0.15f) continue;
                Slab(yellow, (from + to) * 0.5f, mid, to - from, LineWidth, PaintLift);
            }
        }

        /// <summary>
        /// A flat box lying on the ground: top face at <paramref name="top"/>, and real thickness
        /// downward so nothing here is ever coplanar with anything else.
        /// </summary>
        private void Slab(Material material, float cx, float cz, float length, float width, float top)
        {
            const float thickness = 0.35f;
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Road";
            UnityEngine.Object.Destroy(go.GetComponent<Collider>());
            go.transform.SetParent(_root, false);
            go.transform.position = new Vector3(cx, top - thickness * 0.5f, cz);
            go.transform.localScale = new Vector3(length, thickness, width);
            go.GetComponent<Renderer>().sharedMaterial = material;
            Placed++;
        }

        /// <summary>True if any cell in the corridor at this x is blocked.</summary>
        private bool ColumnBlocked(int x, int centreY, int widthCells, Func<int, int, bool> blocked)
        {
            int halfCells = Mathf.Max(0, widthCells / 2);
            int y0 = Mathf.Max(0, centreY - halfCells);
            int y1 = Mathf.Min(_map.Height - 1, centreY + halfCells);
            for (int y = y0; y <= y1; y++)
                if (blocked(x, y)) return true;
            return false;
        }

        /// <summary>Wet winter asphalt, darker than the leaf litter either side of it.</summary>
        private static readonly Color RoadTint = new Color(0.17f, 0.17f, 0.18f);
        /// <summary>Gravel and grit swept to the kerb.</summary>
        private static readonly Color ShoulderTint = new Color(0.33f, 0.31f, 0.27f);
        private static readonly Color EdgeLineTint = new Color(0.76f, 0.75f, 0.71f);
        private static readonly Color CentreLineTint = new Color(0.72f, 0.60f, 0.20f);

        private const float ShoulderWidth = 0.9f;
        private const float EdgeInset = 0.45f;
        private const float LineWidth = 0.16f;
        private const float DashLength = 3.0f;
        private const float DashPitch = 7.0f;
        /// <summary>Top of a painted line: proud of the asphalt, under the decals at 0.02.</summary>
        private const float PaintLift = 0.017f;

        /// <summary>How far above the ground plane a flat surface sits. Coplanar faces flicker.</summary>
        private const float SurfaceLift = 0.012f;

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

            // A contact shadow, measured off this instance. The scatter is where the grounding
            // problem is most visible: a few hundred trees and cars, every one of them apparently
            // resting a centimetre above the ground under a single flat overcast key light.
            // Measured AFTER the scale and rotation above, so a half-size bush gets a half-size
            // mark and a tree lying on its side does not get a shadow the length of its trunk.
            BlobShadows.RegisterProp(go);

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
