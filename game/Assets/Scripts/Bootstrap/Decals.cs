#nullable enable
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// The marks the fight leaves on the ground: scorch rings where the strike landed, dark patches
    /// where bodies fell, drag scuffs.
    ///
    /// Why this is worth a day: a defence game is about a PLACE, and right now the place is
    /// identical at wave twelve to how it looked at wave one. Nothing the player did is written
    /// anywhere except the wall they built. Decals are the cheapest possible memory of a fight --
    /// no simulation, no state, no save -- and they are the single thing that most says someone
    /// played here.
    ///
    /// Instanced projected quads at ground level, on the same shader as the blob shadows, with
    /// generated alpha stamps and no depth write. A per-instance colour carries the fade, so a
    /// whole kind is one draw call no matter how many of them are aging at different rates.
    ///
    /// WIRING. The integrator constructs one and calls it from DrawWorld:
    ///     _decals.Draw();                                             // ages and draws
    /// and gameplay code marks the ground as things happen:
    ///     Decals.Current?.Add(DecalKind.Scorch, impact, 2.2f, yaw);
    /// <see cref="Current"/> exists because the call sites that want to leave a mark -- the blast
    /// handler, the kill handler -- are scattered and none of them should have to be handed a
    /// renderer. It is set by the constructor and cleared by <see cref="Dispose"/>.
    /// </summary>
    public sealed class Decals
    {
        private const int MaxInstancesPerDraw = 1023;
        private const int KindCount = 3;

        /// <summary>
        /// Ground clearance. Under the blob shadows at 0.035 so a body's contact shadow lands on
        /// top of the scorch it is standing in rather than fighting it for the same depth.
        /// </summary>
        public const float GroundOffset = 0.02f;

        /// <summary>The decal system the game is currently running, if any.</summary>
        public static Decals? Current { get; private set; }

        private readonly DecalRing _ring;
        private readonly Material?[] _materials = new Material[KindCount];
        private readonly Texture2D?[] _stamps = new Texture2D[KindCount];
        private readonly Matrix4x4[] _buffer = new Matrix4x4[MaxInstancesPerDraw];
        private readonly Vector4[] _colours = new Vector4[MaxInstancesPerDraw];
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        /// <summary>Ground height, as for the blob shadows: the play area is one flat plane at zero.</summary>
        public float GroundY { get; set; }

        public bool Enabled { get; set; } = true;

        /// <summary>How long each kind lasts, in seconds. Indexed by <see cref="DecalKind"/>.</summary>
        private static readonly float[] Lifetimes = { 95f, 150f, 70f };

        /// <summary>
        /// Base ink per kind. All three are dark and desaturated: a bright decal on an overcast
        /// brown field pulls the eye away from the thing that is about to kill you.
        /// </summary>
        private static readonly Color[] Inks =
        {
            new Color(0.07f, 0.06f, 0.06f, 0.80f),   // Scorch: burnt
            new Color(0.17f, 0.09f, 0.09f, 0.62f),   // Stain: dark rust
            new Color(0.20f, 0.16f, 0.12f, 0.50f),   // Drag: churned earth
        };

        public Decals(int capacity = 256)
        {
            _ring = new DecalRing(capacity);

            _stamps[(int)DecalKind.Scorch] = GroundStamp.BuildScorch(128);
            _stamps[(int)DecalKind.Stain] = GroundStamp.BuildStain(128);
            _stamps[(int)DecalKind.Drag] = GroundStamp.BuildDrag(128);

            for (int k = 0; k < KindCount; k++)
            {
                // 2995: just behind the blob shadows at 3000, still inside the transparent queue.
                var mat = GroundStamp.Material(Inks[k], instanced: true, renderQueue: 2995);
                if (mat != null) mat.mainTexture = _stamps[k];
                _materials[k] = mat;
            }

            Current = this;
        }

        /// <summary>Live marks. Diagnostics and tests.</summary>
        public int Count => _ring.Count;

        public int Capacity => _ring.Capacity;

        /// <summary>How long a kind of mark lasts before it is gone.</summary>
        public static float LifeOf(DecalKind kind) => Lifetimes[(int)kind];

        /// <summary>
        /// Marks the ground. <paramref name="radius"/> is the mark's half-width in metres;
        /// <paramref name="yaw"/> turns it, which matters for the drag scuff and stops a field of
        /// stains from looking like one stamp printed twenty times.
        /// </summary>
        public void Add(DecalKind kind, Vector3 position, float radius, float yaw)
        {
            _ring.Add(kind, position, radius, yaw, Lifetimes[(int)kind]);
        }

        /// <summary>Ages every mark and draws the live ones, one instanced call per kind.</summary>
        public void Draw()
        {
            Draw(Time.deltaTime);
        }

        /// <summary>Aging split out so a test can step it without a frame.</summary>
        public void Draw(float dt)
        {
            _ring.Age(dt);
            if (!Enabled || _ring.Count == 0) return;

            for (int kind = 0; kind < KindCount; kind++)
            {
                var material = _materials[kind];
                if (material == null) continue;

                int n = 0;
                for (int i = 0; i < _ring.Count; i++)
                {
                    var e = _ring[i];
                    if ((int)e.Kind != kind) continue;

                    float fade = DecalRing.Fade(e.Age, e.Life);
                    if (fade <= 0.002f) continue;

                    float d = e.Radius * 2f;
                    _buffer[n] = Matrix4x4.TRS(
                        new Vector3(e.Position.x, GroundY + GroundOffset, e.Position.z),
                        Quaternion.Euler(0f, e.Yaw, 0f),
                        new Vector3(d, 1f, d));

                    var ink = Inks[kind];
                    // Alpha carries the fade AND is the shader's signature for "this instance set
                    // a colour", so it must never reach exactly zero on a drawn instance.
                    _colours[n] = new Vector4(ink.r, ink.g, ink.b, ink.a * fade);
                    n++;

                    if (n == MaxInstancesPerDraw) { Flush(material, n); n = 0; }
                }
                if (n > 0) Flush(material, n);
            }
        }

        private void Flush(Material material, int count)
        {
            _block.Clear();
            _block.SetVectorArray(GroundStamp.InstanceColorId, _colours);
            Graphics.DrawMeshInstanced(GroundStamp.Quad, 0, material, _buffer, count, _block);
        }

        /// <summary>Wipes every mark. Call it when the match falls back to the next position.</summary>
        public void Clear() => _ring.Clear();

        public void Dispose()
        {
            Clear();
            for (int k = 0; k < KindCount; k++)
            {
                GroundStamp.Discard(_materials[k]);
                GroundStamp.Discard(_stamps[k]);
                _materials[k] = null;
                _stamps[k] = null;
            }
            if (ReferenceEquals(Current, this)) Current = null;
        }

        /// <summary>The ring itself, for tests and diagnostics.</summary>
        public DecalRing Ring => _ring;
    }
}
