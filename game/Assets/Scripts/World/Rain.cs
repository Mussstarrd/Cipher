#nullable enable
using System;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Where the raindrops are, with no rendering in it.
    ///
    /// The drops live in a box's LOCAL space and the box follows the camera, which is the whole
    /// trick: because the drops never learn about the world, the box moving is free and the only
    /// wrapping that has to happen is the cheap kind. A drop that falls out of the bottom comes
    /// back in at the top; a drop the wind pushes out of the side comes back in the other side.
    /// Nothing is ever allocated, destroyed or sorted after construction.
    ///
    /// Seeded, so the rain in a screenshot is the rain in the next screenshot.
    /// </summary>
    public sealed class RainField
    {
        private readonly Vector3[] _pos;
        private readonly float[] _length;
        private readonly float _radius;
        private readonly float _height;

        /// <summary>Drop positions in box space. Read-only by convention; do not hold the array.</summary>
        public Vector3[] Positions => _pos;
        /// <summary>Per-drop streak length multiplier, so they are not all the same stroke.</summary>
        public float[] Lengths => _length;
        public int Count => _pos.Length;
        public float Radius => _radius;
        public float Height => _height;

        public RainField(int count, float radius, float height, int seed)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            if (radius <= 0f) throw new ArgumentOutOfRangeException(nameof(radius));
            if (height <= 0f) throw new ArgumentOutOfRangeException(nameof(height));

            _radius = radius;
            _height = height;
            _pos = new Vector3[count];
            _length = new float[count];

            // Our own LCG rather than UnityEngine.Random: that one is global mutable state shared
            // with every other system in the frame, so seeding it here would make somebody else's
            // effect depend on how much it rained.
            uint s = (uint)seed | 1u;
            for (int i = 0; i < count; i++)
            {
                _pos[i] = new Vector3(
                    (Next(ref s) * 2f - 1f) * radius,
                    (Next(ref s) - 0.5f) * height,
                    (Next(ref s) * 2f - 1f) * radius);
                _length[i] = 0.65f + Next(ref s) * 0.7f;
            }
        }

        private static float Next(ref uint s)
        {
            s ^= s << 13; s ^= s >> 17; s ^= s << 5;
            return (s & 0xFFFFFF) * (1f / 16777216f);
        }

        /// <summary>
        /// Advances every drop by <paramref name="dt"/> and wraps it back into the box.
        /// </summary>
        /// <param name="fallSpeed">Metres per second downward.</param>
        /// <param name="drift">Horizontal wind, metres per second, world XZ.</param>
        public void Step(float dt, float fallSpeed, Vector2 drift)
        {
            if (dt <= 0f) return;

            float dy = fallSpeed * dt;
            float dx = drift.x * dt;
            float dz = drift.y * dt;
            float halfH = _height * 0.5f;

            for (int i = 0; i < _pos.Length; i++)
            {
                var p = _pos[i];
                p.x += dx;
                p.y -= dy;
                p.z += dz;

                // Repeat rather than a single add: a long frame (a level load, a breakpoint)
                // can move a drop several box-heights, and one add would leave it outside.
                p.y = Wrap(p.y, -halfH, _height);
                p.x = Wrap(p.x, -_radius, _radius * 2f);
                p.z = Wrap(p.z, -_radius, _radius * 2f);

                _pos[i] = p;
            }
        }

        /// <summary>Brings <paramref name="v"/> into [min, min+span) however far outside it is.</summary>
        private static float Wrap(float v, float min, float span)
        {
            float t = (v - min) % span;
            if (t < 0f) t += span;
            return min + t;
        }
    }

    /// <summary>
    /// Draws the rain: one instanced pass of camera-facing streaks, no particle system.
    ///
    /// WHY NOT A PARTICLE SYSTEM. Rain is the one effect on screen at every moment it exists, so
    /// it is the one that has to be free. A ParticleSystem would bring simulation, sorting, a
    /// second material path and (on the URP particle shaders) a variant set larger than the rest
    /// of this game put together. A fixed array of matrices through Graphics.DrawMeshInstanced is
    /// a few hundred microseconds and costs eight variants.
    ///
    /// WHY THE STREAKS ALL POINT THE SAME WAY. They are drawn as ink strokes, not simulated
    /// droplets: in a comic, rain is a set of parallel diagonal lines and the parallelism IS the
    /// read. Billboarding each drop individually would be more correct and look worse.
    /// </summary>
    public sealed class RainFall
    {
        /// <summary>DrawMeshInstanced's hard limit per call.</summary>
        private const int Batch = 1023;
        private const int MaxDrops = 900;

        private readonly RainField _field;
        private readonly Matrix4x4[] _matrices = new Matrix4x4[Batch];
        private readonly Vector4[] _colors = new Vector4[Batch];
        private readonly MaterialPropertyBlock _props = new MaterialPropertyBlock();
        private readonly Mesh _quad;
        private readonly Material? _material;

        private float _intensity;
        private float _wind;

        /// <summary>0 when it is not raining, which is also when nothing is drawn at all.</summary>
        public float Intensity => _intensity;
        /// <summary>False when the shader is missing, so the caller can say so once.</summary>
        public bool Ready => _material != null;

        public RainFall(int seed)
        {
            _field = new RainField(MaxDrops, radius: 22f, height: 26f, seed: seed);
            _quad = BuildStreakMesh();

            var shader = Shader.Find("Exodus/Rain");
            if (shader == null)
            {
                Debug.LogWarning("[Rain] Exodus/Rain not found; the weather will be silent and dry.");
                _material = null;
                return;
            }

            _material = new Material(shader) { enableInstancing = true };
        }

        /// <summary>Sets the condition. Zero intensity stops the draw entirely, not just the alpha.</summary>
        public void SetWeather(WeatherProfile p)
        {
            _intensity = Mathf.Clamp01(p.Rain);
            _wind = Mathf.Clamp01(p.Wind);
        }

        /// <summary>
        /// Steps and draws. Call once a frame with the camera the rain should surround.
        /// </summary>
        public void Update(float dt, Camera? camera)
        {
            if (_material == null || camera == null || _intensity <= 0.001f) return;

            // Wind blows along the sun's westerly, which is also the direction the streaks lean.
            // Speed rises with the weather so heavy rain is visibly faster, not just denser.
            float fall = 18f + 10f * _intensity;
            var drift = new Vector2(-3.5f, 1.2f) * _wind;
            _field.Step(dt, fall, drift);

            // One rotation for every streak (see the class comment). Yaw to face the camera,
            // then lean by the wind so the strokes are diagonal rather than vertical.
            float lean = Mathf.Lerp(4f, 26f, _wind);
            var flat = camera.transform.forward;
            flat.y = 0f;
            if (flat.sqrMagnitude < 1e-4f) flat = Vector3.forward;
            var facing = Quaternion.LookRotation(flat.normalized, Vector3.up);
            var rot = facing * Quaternion.Euler(0f, 0f, lean);

            // Anchored slightly above the camera: the box is 26 m tall and the player looks up
            // into it, so centring it on the eye wastes half the drops behind the near plane.
            var origin = camera.transform.position + Vector3.up * 4f;

            int drops = Mathf.Clamp(Mathf.RoundToInt(MaxDrops * _intensity), 0, _field.Count);
            var positions = _field.Positions;
            var lengths = _field.Lengths;

            int n = 0;
            for (int i = 0; i < drops; i++)
            {
                var scale = new Vector3(0.045f, 1.5f * lengths[i], 1f);
                _matrices[n] = Matrix4x4.TRS(origin + positions[i], rot, scale);

                // Alpha varies per streak so the field has depth without a second draw. Kept low:
                // rain that reads as a grey wash over the frame is rain that has eaten the art.
                float a = 0.20f + 0.28f * lengths[i] * _intensity;
                _colors[n] = new Vector4(0.86f, 0.89f, 0.95f, a);

                if (++n == Batch) { Flush(n); n = 0; }
            }
            if (n > 0) Flush(n);
        }

        private void Flush(int count)
        {
            _props.Clear();
            _props.SetVectorArray("_InstanceColor", _colors);
            Graphics.DrawMeshInstanced(
                _quad, 0, _material, _matrices, count, _props,
                UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows: false);
        }

        /// <summary>A unit quad on the XY plane, pivoted at its centre, facing -Z.</summary>
        private static Mesh BuildStreakMesh()
        {
            var mesh = new Mesh { name = "RainStreak" };
            mesh.SetVertices(new[]
            {
                new Vector3(-0.5f, -0.5f, 0f),
                new Vector3( 0.5f, -0.5f, 0f),
                new Vector3( 0.5f,  0.5f, 0f),
                new Vector3(-0.5f,  0.5f, 0f),
            });
            mesh.SetUVs(0, new[]
            {
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            });
            mesh.SetTriangles(new[] { 0, 2, 1, 0, 3, 2 }, 0);
            mesh.RecalculateNormals();
            // Generous, and fixed: the streaks are repositioned every frame by the instance
            // matrix, so a recalculated local bounds would be wrong the moment it was computed.
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(2f, 2f, 2f));
            return mesh;
        }
    }
}
