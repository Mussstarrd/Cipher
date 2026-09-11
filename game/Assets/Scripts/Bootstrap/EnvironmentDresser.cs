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
                              scaleMin: 0.85f, scaleMax: 1.35f, pitchCorrection: -90f);
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
                    Place(cars[rng.NextInt(cars.Count)], x, y, rng, 1f, 1f, flatRotation: true);
                }
            }
        }

        private bool Free(int x, int y, Func<int, int, bool> keepClear)
        {
            if (x < 0 || y < 0 || x >= _map.Width || y >= _map.Height) return false;
            if (_map.KindAt(x, y) != WallKind.None) return false;
            return !keepClear(x, y);
        }

        private void Place(GameObject prefab, int x, int y, Rng rng,
                           float scaleMin, float scaleMax, bool flatRotation = false,
                           float pitchCorrection = 0f)
        {
            if (prefab == null) return;

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

            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                var swapped = new Material[mats.Length];
                for (int m = 0; m < mats.Length; m++) swapped[m] = _reskin(mats[m]);
                r.sharedMaterials = swapped;
            }

            Placed++;
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
