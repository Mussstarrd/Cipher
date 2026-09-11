#nullable enable
using System;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// The sky, the ground surface, and everything past the edge of the playable rectangle.
    ///
    /// Three separate problems that all have to be solved before a screenshot of this game is worth
    /// showing anyone:
    ///
    ///   THE SKY was a flat grey clear colour, which reads as "unfinished render" more than any
    ///   other single thing. It is now a banded procedural sky (see ComicSky.shader).
    ///
    ///   THE GROUND was one flat brown. It is most of the screen in a top-down-ish game, so one flat
    ///   colour across it is most of the screen doing nothing. It now gets a generated texture:
    ///   multi-octave value noise for leaf-litter mottle, a dry-grass tint that varies in patches,
    ///   and sparse dark specks. Generated at startup, not shipped, so a clean checkout rebuilds it
    ///   and there is still no texture file in this project.
    ///
    ///   THE EDGE was a cliff. The ground plane simply stopped and the sky started, which tells the
    ///   viewer exactly how big the level is and that nothing exists outside it. Beyond the play
    ///   area there is now a wider apron of ground, a treeline ring, low ridges at the horizon, and
    ///   the LAKE the community is named for, which ADR-004 put on one flank and which nothing had
    ///   ever drawn.
    ///
    /// All procedural, all deterministic from a seed, all built in code so CI rebuilds it.
    /// </summary>
    public sealed class WorldBackdrop
    {
        private readonly Transform _root;
        private readonly Func<Color, Material> _material;

        public WorldBackdrop(Transform root, Func<Color, Material> material)
        {
            _root = root;
            _material = material;
        }

        public int Placed { get; private set; }

        // ------------------------------------------------------------------ ground

        /// <summary>
        /// Builds the ground's surface texture. Brown leaf litter over dead grass, mottled.
        ///
        /// Value noise rather than Perlin because it is ten lines and, once the cel shader has
        /// banded it, the difference is invisible. The point is not realism; it is that the eye has
        /// something to land on so the ground stops reading as a solid fill.
        /// </summary>
        public static Texture2D BuildGroundTexture(int size, Color baseColour, ulong seed)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGB24, mipChain: true)
            {
                name = "GroundLitter",
                wrapMode = TextureWrapMode.Repeat,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 4,
            };

            var pixels = new Color32[size * size];
            var rng = new Rng(seed);

            // A dry-grass tint and a wet-earth tint either side of the base colour.
            var dry = new Color(baseColour.r * 1.22f + 0.06f, baseColour.g * 1.20f + 0.06f, baseColour.b * 0.92f);
            var wet = new Color(baseColour.r * 0.66f, baseColour.g * 0.66f, baseColour.b * 0.70f);

            // Hash tables for tileable value noise: the same lattice is reused at every octave, and
            // indexing it with a wrapped coordinate is what makes the result tile seamlessly.
            const int lattice = 64;
            var grid = new float[lattice * lattice];
            for (int i = 0; i < grid.Length; i++) grid[i] = rng.NextFloat();

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float u = x / (float)size;
                    float v = y / (float)size;

                    float n = Fbm(grid, lattice, u, v, 6f, 4);
                    float patch = Fbm(grid, lattice, u + 0.37f, v + 0.11f, 2f, 2);

                    var colour = Color.Lerp(wet, dry, Mathf.SmoothStep(0f, 1f, patch));
                    colour = Color.Lerp(colour, baseColour, 0.45f);
                    colour *= 0.86f + n * 0.30f;

                    // Sparse dark specks: twigs, stones, whatever the eye wants them to be.
                    float speck = Fbm(grid, lattice, u * 3.1f + 5.5f, v * 3.1f + 2.2f, 18f, 2);
                    if (speck > 0.80f) colour *= 0.70f;

                    pixels[y * size + x] = colour;
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: true);
            return tex;
        }

        /// <summary>Tileable fractal value noise sampled on a wrapped lattice.</summary>
        private static float Fbm(float[] grid, int lattice, float u, float v, float frequency, int octaves)
        {
            float sum = 0f, amp = 0.5f, total = 0f;
            for (int o = 0; o < octaves; o++)
            {
                sum += Value(grid, lattice, u * frequency, v * frequency) * amp;
                total += amp;
                frequency *= 2f;
                amp *= 0.5f;
            }
            return total <= 0f ? 0f : sum / total;
        }

        private static float Value(float[] grid, int lattice, float x, float y)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            fx = fx * fx * (3f - 2f * fx);
            fy = fy * fy * (3f - 2f * fy);

            float a = At(grid, lattice, x0, y0);
            float b = At(grid, lattice, x0 + 1, y0);
            float c = At(grid, lattice, x0, y0 + 1);
            float d = At(grid, lattice, x0 + 1, y0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        private static float At(float[] grid, int lattice, int x, int y)
        {
            int xi = ((x % lattice) + lattice) % lattice;
            int yi = ((y % lattice) + lattice) % lattice;
            return grid[yi * lattice + xi];
        }

        // ------------------------------------------------------------------ beyond the edge

        /// <summary>
        /// Everything outside the playable rectangle: an apron of ground, the lake on one flank,
        /// a treeline ring, and ridges at the horizon.
        ///
        /// None of it is solid, none of it is in the grid, and none of it is reachable. It exists so
        /// the level stops announcing its own dimensions.
        /// </summary>
        public void Build(int width, int height, Color groundColour, Texture2D? groundTexture,
                          GameObject[] treeModels, Func<Material, Material> reskin, ulong seed = 20260911UL)
        {
            var rng = new Rng(seed);
            var centre = new Vector3(width * 0.5f, 0f, height * 0.5f);

            // A wide apron, a hair below the play surface so the seam never z-fights.
            var apron = GameObject.CreatePrimitive(PrimitiveType.Plane);
            apron.name = "Apron";
            UnityEngine.Object.Destroy(apron.GetComponent<Collider>());
            apron.transform.SetParent(_root, false);
            apron.transform.position = centre + new Vector3(0f, -0.05f, 0f);
            // A Unity Plane is TEN units across at scale 1, which is why the ground uses GridW/10.
            // Scaling this one by the cell count made a 576-unit slab that swallowed the camera.
            float apronScale = Mathf.Max(width, height) * 0.6f;
            apron.transform.localScale = new Vector3(apronScale, 1f, apronScale);
            // Its OWN material. _material caches by colour and hands the same instance to every
            // prop of that colour, so setting a texture on the returned material would put leaf
            // litter on every brown box in the level.
            var apronMat = new Material(_material(groundColour * 0.92f));
            if (groundTexture != null)
            {
                apronMat.mainTexture = groundTexture;
                apronMat.mainTextureScale = new Vector2(apronScale * 1.25f, apronScale * 1.25f);
            }
            apron.GetComponent<Renderer>().sharedMaterial = apronMat;
            Placed++;

            BuildLake(width, height, centre);
            BuildTreeline(width, height, treeModels, reskin, ref rng);
            BuildRidges(centre, width, height, ref rng);
        }

        /// <summary>
        /// The lake. ADR-004 put it on one flank of the community and nothing had ever drawn it,
        /// which is a strange omission in a place called Wilderness Lake.
        /// </summary>
        private void BuildLake(int width, int height, Vector3 centre)
        {
            var lake = GameObject.CreatePrimitive(PrimitiveType.Plane);
            lake.name = "Lake";
            UnityEngine.Object.Destroy(lake.GetComponent<Collider>());
            lake.transform.SetParent(_root, false);
            // Off the south flank, wide enough to reach the horizon haze.
            // South of the map and stopping ten units short of it, in WORLD units. Wide enough that
            // its far edge is lost in the fog rather than ending in a visible line.
            lake.transform.position = new Vector3(centre.x, -0.02f, -110f);
            lake.transform.localScale = new Vector3(60f, 1f, 20f);
            // Slate under an overcast sky. Lake water in February is not blue.
            lake.GetComponent<Renderer>().sharedMaterial = _material(new Color(0.30f, 0.35f, 0.38f));
            Placed++;

            // A pale shore strip, which is what actually sells the join.
            var shore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shore.name = "Shore";
            UnityEngine.Object.Destroy(shore.GetComponent<Collider>());
            shore.transform.SetParent(_root, false);
            shore.transform.position = new Vector3(centre.x, 0.02f, -6f);
            shore.transform.localScale = new Vector3(600f, 0.06f, 9f);
            shore.GetComponent<Renderer>().sharedMaterial = _material(new Color(0.52f, 0.48f, 0.41f));
            Placed++;
        }

        /// <summary>A ring of trees outside the fence, so the woods do not stop where the level does.</summary>
        private void BuildTreeline(int width, int height, GameObject[] models,
                                   Func<Material, Material> reskin, ref Rng rng)
        {
            if (models == null || models.Length == 0) return;

            var centre = new Vector3(width * 0.5f, 0f, height * 0.5f);
            float rx = width * 0.72f + 8f, rz = height * 0.72f + 8f;

            for (int i = 0; i < 260; i++)
            {
                float a = (i / 260f) * Mathf.PI * 2f + rng.NextFloat() * 0.03f;
                float spread = 1f + rng.NextFloat() * 0.85f;

                float x = centre.x + Mathf.Cos(a) * rx * spread;
                float z = centre.z + Mathf.Sin(a) * rz * spread;

                // Nothing on the lake side; it would be standing in the water.
                if (z < centre.z - height * 0.5f) continue;

                var model = models[rng.NextInt(models.Length)];
                var go = UnityEngine.Object.Instantiate(model, _root);
                go.transform.position = new Vector3(x, 0f, z);
                go.transform.rotation = Quaternion.Euler(-90f, rng.NextFloat() * 360f, 0f);
                go.transform.localScale *= 1.0f + rng.NextFloat() * 0.6f;

                foreach (var r in go.GetComponentsInChildren<Renderer>())
                {
                    var mats = r.sharedMaterials;
                    var swapped = new Material[mats.Length];
                    for (int m = 0; m < mats.Length; m++) swapped[m] = reskin(mats[m]);
                    r.sharedMaterials = swapped;
                }
                Placed++;
            }
        }

        /// <summary>
        /// Low ridges at the horizon. Virginia is not flat, and a flat horizon under a banded sky
        /// looks like a stage set. Three overlapping rings at decreasing saturation read as distance.
        /// </summary>
        private void BuildRidges(Vector3 centre, int width, int height, ref Rng rng)
        {
            // Far enough out that nothing can reach them and the fog is already doing most of the
            // work on them. The first version put them at barely one map-width, which with a
            // 160-unit-wide box rotated the WRONG WAY reached back across the level and put the
            // camera inside a hill.
            float baseRadius = Mathf.Max(width, height) * 2.6f + 60f;

            for (int ring = 0; ring < 3; ring++)
            {
                float radius = baseRadius + ring * 70f;
                // Further ridges sit closer to the haze colour, which is the whole trick.
                // Wooded hills, not grey cardboard: start from the treeline's own colour and walk
                // it toward the haze with distance. A ridge the same grey as the sky reads as a
                // wall; a ridge that is a desaturated version of the woods in front of it reads as
                // more woods, further away, which is what it is.
                var colour = Color.Lerp(new Color(0.20f, 0.24f, 0.19f),
                                        new Color(0.56f, 0.58f, 0.60f),
                                        0.34f + ring * 0.24f);

                int count = 26 + ring * 6;
                for (int i = 0; i < count; i++)
                {
                    float a = (i / (float)count) * Mathf.PI * 2f;
                    float jitter = 0.9f + rng.NextFloat() * 0.25f;

                    var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = "Ridge";
                    UnityEngine.Object.Destroy(go.GetComponent<Collider>());
                    go.transform.SetParent(_root, false);
                    go.transform.position = centre + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * jitter;

                    // Wide and low. Tall narrow boxes read as buildings; a ridge is mostly width.
                    float w = 150f + rng.NextFloat() * 170f;
                    float h = 10f + rng.NextFloat() * 14f + ring * 6f;

                    // TANGENTIAL. Facing the centre puts the box's long X axis across the line of
                    // sight, which is what makes a ridge; pointing it along the radius makes a wall
                    // that runs straight at the player.
                    var toCentre = centre - go.transform.position;
                    toCentre.y = 0f;
                    go.transform.rotation = Quaternion.LookRotation(toCentre.normalized, Vector3.up)
                                            * Quaternion.Euler(0f, rng.NextFloat() * 14f - 7f, 0f);
                    go.transform.localScale = new Vector3(w, h, 40f);
                    go.transform.position += new Vector3(0f, h * 0.5f - 7f, 0f);
                    go.GetComponent<Renderer>().sharedMaterial = _material(colour);
                    Placed++;
                }
            }
        }

        /// <summary>Deterministic generator, so the backdrop is identical on every run.</summary>
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
