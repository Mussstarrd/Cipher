#nullable enable
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Shared plumbing for everything that is drawn FLAT ON THE GROUND: the contact shadows under
    /// bodies and props, and the scorch rings, stains and drag marks the fight leaves behind.
    ///
    /// Three things live here because both systems need all three and neither should own them:
    ///
    ///   THE QUAD. Not the bootstrap's cylinder disc. A blob shadow is two triangles with an alpha
    ///   stamp on them; a cylinder is 80-odd triangles of which all but the cap are invisible, and
    ///   at a thousand marks on screen that is 40x the vertex work for a shape the texture draws
    ///   anyway. Built here rather than harvested from a primitive because Unity's built-in Quad
    ///   stands UP in XY and would have to be rotated by every single caller.
    ///
    ///   THE TEXTURES. Generated, like every other texture in this project -- there is still not
    ///   one image file in the repo and this does not add the first.
    ///
    ///   THE SHADER LOOKUP, so one missing-shader warning exists instead of three.
    ///
    /// On the art direction: the falloffs here are HARD-EDGED on purpose. A smooth radial gradient
    /// is a photographic ambient-occlusion cheat, and under an ink outline it reads as a blur under
    /// the character. Two or three flat steps read as a shape somebody drew, which is the whole
    /// direction. <see cref="BuildBlob"/> can make either and the two-tone one is the default;
    /// the smooth one is kept so the choice can be re-made from a screenshot rather than an opinion.
    /// </summary>
    public static class GroundStamp
    {
        public const string ShaderName = "Exodus/BlobShadow";

        private static Shader? _shader;
        private static Mesh? _quad;

        /// <summary>The unlit alpha-blended stamp shader. Null only if the shader failed to import.</summary>
        public static Shader? StampShader
        {
            get
            {
                if (_shader != null) return _shader;
                _shader = Shader.Find(ShaderName);
                if (_shader == null)
                    Debug.LogWarning($"[GroundStamp] {ShaderName} not found; ground marks will not draw");
                return _shader;
            }
        }

        /// <summary>
        /// A one-unit quad lying in the XZ plane, centred on the origin, facing up. Scale it by the
        /// diameter you want; the caller never has to think about a rotation to get it flat.
        /// </summary>
        public static Mesh Quad
        {
            get
            {
                if (_quad != null) return _quad;
                var mesh = new Mesh { name = "GroundStampQuad" };
                mesh.SetVertices(new[]
                {
                    new Vector3(-0.5f, 0f, -0.5f),
                    new Vector3(-0.5f, 0f,  0.5f),
                    new Vector3( 0.5f, 0f,  0.5f),
                    new Vector3( 0.5f, 0f, -0.5f),
                });
                mesh.SetUVs(0, new[]
                {
                    new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f),
                });
                mesh.SetNormals(new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up });
                mesh.SetTriangles(new[] { 0, 1, 2, 0, 2, 3 }, 0);
                // Generous bounds. These are drawn with DrawMeshInstanced, which culls per batch
                // against the mesh bounds, and a half-metre box around the origin gets a batch
                // spread over the whole map culled the moment the camera looks slightly away.
                mesh.bounds = new Bounds(Vector3.zero, new Vector3(1f, 1f, 1f));
                mesh.UploadMeshData(markNoLongerReadable: false);
                _quad = mesh;
                return _quad;
            }
        }

        /// <summary>Builds a material on the stamp shader, or null when the shader is missing.</summary>
        public static Material? Material(Color colour, bool instanced, int renderQueue)
        {
            var shader = StampShader;
            if (shader == null) return null;
            var mat = new Material(shader) { enableInstancing = instanced, renderQueue = renderQueue };
            mat.SetColor(BaseColorId, colour);
            return mat;
        }

        /// <summary>
        /// Destroy that also works in edit mode. Object.Destroy is runtime-only and throws in an
        /// EditMode test, which would make everything that owns a generated texture untestable.
        /// </summary>
        public static void Discard(Object? o)
        {
            if (o == null) return;
            if (Application.isPlaying) Object.Destroy(o);
            else Object.DestroyImmediate(o);
        }

        public static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        public static readonly int InstanceColorId = Shader.PropertyToID("_InstanceColor");

        // ------------------------------------------------------------------ stamps

        /// <summary>
        /// The contact shadow. <paramref name="twoTone"/> steps it into a solid core and one
        /// lighter skirt -- a drawn shape -- instead of a photographic gradient.
        /// </summary>
        public static Texture2D BuildBlob(int size, bool twoTone)
        {
            return Build(size, twoTone ? "BlobTwoTone" : "BlobSmooth", (u, v, noise) =>
            {
                float r = Radius(u, v) * 2f;                 // 0 at centre, 1 at the quad's edge
                if (r >= 1f) return 0f;
                if (!twoTone) return Mathf.Pow(1f - r, 1.6f);
                if (r < 0.52f) return 1f;
                if (r < 0.80f) return 0.46f;
                return 0f;
            });
        }

        /// <summary>
        /// Where a bomb landed: a burnt-out centre, a hard rim, and a few spokes thrown outward.
        /// Three flat tones, because a soot gradient is a photograph and a soot ring is a drawing.
        /// </summary>
        public static Texture2D BuildScorch(int size)
        {
            return Build(size, "DecalScorch", (u, v, noise) =>
            {
                float dx = u - 0.5f, dy = v - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float ang = Mathf.Atan2(dy, dx);
                // Spokes wobble the outer edge so the ring is not a perfect circle stamp.
                float spokes = 0.06f * Mathf.Sin(ang * 7f) + 0.04f * Mathf.Sin(ang * 13f + 1.1f);
                float edge = 0.94f + spokes;

                if (r >= edge) return 0f;
                if (r < 0.34f) return 0.92f;                 // burnt out
                if (r < edge - 0.26f) return 0.62f;          // soot
                return 0.34f;                                // scatter
            });
        }

        /// <summary>Where a body fell. An irregular patch, deliberately not a circle.</summary>
        public static Texture2D BuildStain(int size)
        {
            return Build(size, "DecalStain", (u, v, noise) =>
            {
                float dx = u - 0.5f, dy = v - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                float ang = Mathf.Atan2(dy, dx);
                // A lumpy radius: three harmonics is enough to stop it reading as a circle.
                float lumps = 1f + 0.20f * Mathf.Sin(ang * 3f + 0.7f)
                                 + 0.11f * Mathf.Sin(ang * 5f - 1.9f)
                                 + 0.06f * Mathf.Sin(ang * 9f + 2.4f);
                float edge = 0.88f * lumps;
                if (r >= edge) return 0f;
                return r < edge * 0.62f ? 0.85f : 0.48f;
            });
        }

        /// <summary>
        /// A drag mark: two parallel scuffs down the middle of the quad, tapering at both ends.
        /// The shape is IN the texture rather than in a non-uniform scale, so the caller's API
        /// stays one radius and one yaw.
        /// </summary>
        public static Texture2D BuildDrag(int size)
        {
            return Build(size, "DecalDrag", (u, v, noise) =>
            {
                // v runs along the drag, u across it.
                float along = Mathf.Abs(v - 0.5f) * 2f;
                if (along >= 0.96f) return 0f;
                float taper = 1f - along * along;            // fades out at both ends

                // Two scuffs either side of centre, drifting a little as they travel.
                float drift = 0.035f * Mathf.Sin(v * 11f);
                float a = Mathf.Abs(u - (0.5f - 0.085f + drift));
                float b = Mathf.Abs(u - (0.5f + 0.085f + drift));
                float d = Mathf.Min(a, b);
                if (d > 0.055f) return 0f;
                float core = d < 0.026f ? 0.8f : 0.44f;
                // A little break-up along the length so it reads as scraped, not painted.
                float broken = noise < 0.12f ? 0.55f : 1f;
                return core * taper * broken;
            });
        }

        // ------------------------------------------------------------------ internals

        /// <summary>Per-pixel stamp. <c>noise</c> is a fresh deterministic 0..1 sample.</summary>
        private delegate float Stamp(float u, float v, float noise);

        private static float Radius(float u, float v)
        {
            float dx = u - 0.5f, dy = v - 0.5f;
            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static Texture2D Build(int size, string name, Stamp stamp)
        {
            // RGB stays white and the shader reads .a: the colour comes from the material or the
            // per-instance tint, so one texture serves a blob shadow and a rust-coloured stain.
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true)
            {
                name = name,
                // CLAMP, not repeat. A repeating stamp tiles itself across its own quad the moment
                // a sample lands a hair outside 0..1, which puts a seam through every mark.
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 2,
            };

            var pixels = new Color32[size * size];
            var rng = new Rng(0x5EEDB10BUL ^ (ulong)name.GetHashCode());
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    float a = Mathf.Clamp01(stamp(u, v, rng.NextFloat()));
                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(a * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            return tex;
        }

        /// <summary>Deterministic generator, so a stamp is byte-identical on every run.</summary>
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
        }
    }
}
