#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Contact shadows: one soft dark stamp on the ground under every body and every prop.
    ///
    /// The problem this fixes is that NOTHING TOUCHES THE FLOOR. There is one directional light and
    /// no ambient occlusion, so a civilian, a jersey barrier and a sentry all read as hovering a
    /// few centimetres above the ground, and at the pitched tactical camera that is most of what
    /// makes the field look like pieces laid on a board rather than a place. Real SSAO needs a
    /// depth-normals pass this shader does not have; a blob shadow needs a quad and an alpha stamp.
    ///
    /// It is also the RIGHT cheat for this direction rather than a tolerated one. Blob shadows read
    /// as a cheap trick in a photoreal game because everything else is physically grounded and they
    /// are not. In an inked comic the mark under a figure IS how ground contact is drawn, which is
    /// why <see cref="GroundStamp.BuildBlob"/> defaults to the hard two-tone stamp: a solid core
    /// and one lighter skirt, not a soft gradient.
    ///
    /// Everything is instanced. A thousand marks is two draw calls and no GameObjects.
    ///
    /// WIRING. The integrator calls, from DrawWorld:
    ///     _blobs.DrawProps();                             // the static registrations, baked once
    ///     _blobs.Draw(_blobPositions, 0.45f);             // whatever moved this frame
    /// and props register themselves as they are built via <see cref="RegisterProp"/>.
    /// </summary>
    public sealed class BlobShadows
    {
        /// <summary>
        /// Unity's hard ceiling for one instanced draw. Same constant the bootstrap uses; repeated
        /// rather than shared because reaching into FloodBootstrap for it would couple this to the
        /// one file the lead engineer is in.
        /// </summary>
        private const int MaxInstancesPerDraw = 1023;

        /// <summary>
        /// How far above the ground the stamp sits. Far enough to beat z-fighting with the ground
        /// plane, under the decals at 0.02 so a blob draws OVER a scorch ring rather than into it.
        /// </summary>
        public const float GroundOffset = 0.035f;

        /// <summary>
        /// Which stamp the shadows use. Two-tone is the shipped choice -- see the class comment --
        /// and the smooth one is kept switchable so the decision can be remade from a screenshot.
        /// Set before the first BlobShadows is constructed; the texture is built in the ctor.
        /// </summary>
        public static bool TwoTone = true;

        // Registered props. Static so SiteProps, TurretProps and the environment dresser can each
        // say "I put a thing here" without any of them holding a renderer reference. Cleared
        // between positions -- see ClearProps, and do not forget it: prop shadows would otherwise
        // accumulate across a fall-back and the next position would be printed on the old one.
        private static readonly List<Matrix4x4> Props = new List<Matrix4x4>(1024);
        private static int _propVersion;

        private readonly Material? _material;
        private readonly Matrix4x4[] _buffer = new Matrix4x4[MaxInstancesPerDraw];
        private readonly List<Matrix4x4> _dynamic = new List<Matrix4x4>(512);
        private Texture2D? _stamp;

        /// <summary>Ground height. The play area is a single flat plane at zero; this is the seam if it stops being.</summary>
        public float GroundY { get; set; }

        public bool Enabled { get; set; } = true;

        public BlobShadows(Color? ink = null)
        {
            // Cold near-black rather than pure black: the shadow tint the lit shader already uses
            // for its dark band, so a contact shadow and a cast shadow agree about what dark is.
            var colour = ink ?? new Color(0.07f, 0.08f, 0.11f, 0.55f);
            _stamp = GroundStamp.BuildBlob(128, TwoTone);
            // 3000 = the front of the transparent queue, so blobs draw over the decals at 2995.
            _material = GroundStamp.Material(colour, instanced: true, renderQueue: 3000);
            if (_material != null) _material.mainTexture = _stamp;
        }

        /// <summary>How many prop shadows are registered. Diagnostics and tests.</summary>
        public static int RegisteredProps => Props.Count;

        /// <summary>
        /// Records a permanent shadow under a built thing. <paramref name="position"/> is where it
        /// stands -- only x and z are used, because the mark goes on the ground, not on the prop.
        /// </summary>
        public static void RegisterProp(Vector3 position, float radius)
        {
            if (radius <= 0f) return;
            Props.Add(Matrix4x4.TRS(
                new Vector3(position.x, GroundOffset, position.z),
                Quaternion.identity,
                new Vector3(radius * 2f, 1f, radius * 2f)));
            _propVersion++;
        }

        /// <summary>
        /// Registers a shadow sized to what the prop actually is, by measuring it.
        ///
        /// MEASURED FROM THE LIVE INSTANCE, through each MeshFilter's localToWorldMatrix -- never
        /// from the prefab asset, which carries an import scale a naive matrix round-trip does not
        /// cancel and which once reported a six-metre road tile as two centimetres (CLAUDE.md).
        /// Renderer.bounds is no good here either: on a freshly instantiated object it has not been
        /// recalculated yet, which is the same trap FitToHeight documents.
        /// </summary>
        public static void RegisterProp(GameObject prop, float min = 0.22f, float max = 2.2f)
        {
            if (prop == null) return;
            float r = MeasureRadius(prop);
            if (r <= 0f) return;
            RegisterProp(prop.transform.position, Mathf.Clamp(r, min, max));
        }

        /// <summary>
        /// Half the prop's larger horizontal dimension, world space, shrunk a little: a shadow the
        /// full width of the object reads as a plinth rather than as contact.
        /// </summary>
        public static float MeasureRadius(GameObject prop)
        {
            if (prop == null) return 0f;
            var filters = prop.GetComponentsInChildren<MeshFilter>(includeInactive: false);
            bool any = false;
            float minX = float.MaxValue, minZ = float.MaxValue;
            float maxX = float.MinValue, maxZ = float.MinValue;

            foreach (var f in filters)
            {
                var mesh = f.sharedMesh;
                if (mesh == null) continue;
                var m = f.transform.localToWorldMatrix;
                Vector3 c = mesh.bounds.center, e = mesh.bounds.extents;
                for (int i = 0; i < 8; i++)
                {
                    var corner = m.MultiplyPoint3x4(c + new Vector3(
                        (i & 1) == 0 ? -e.x : e.x,
                        (i & 2) == 0 ? -e.y : e.y,
                        (i & 4) == 0 ? -e.z : e.z));
                    if (corner.x < minX) minX = corner.x;
                    if (corner.x > maxX) maxX = corner.x;
                    if (corner.z < minZ) minZ = corner.z;
                    if (corner.z > maxZ) maxZ = corner.z;
                    any = true;
                }
            }

            if (!any) return 0f;
            return Mathf.Max(maxX - minX, maxZ - minZ) * 0.5f * 0.85f;
        }

        /// <summary>
        /// Forgets every registered prop. Call this wherever the props themselves are destroyed --
        /// i.e. when the match falls back to the next position.
        /// </summary>
        public static void ClearProps()
        {
            if (Props.Count == 0) return;
            Props.Clear();
            _propVersion++;
        }

        /// <summary>Draws the registered prop shadows. Cheap: the matrices were baked at registration.</summary>
        public void DrawProps()
        {
            if (!Enabled || _material == null || Props.Count == 0) return;
            for (int start = 0; start < Props.Count; start += MaxInstancesPerDraw)
            {
                int n = Mathf.Min(MaxInstancesPerDraw, Props.Count - start);
                Props.CopyTo(start, _buffer, 0, n);
                Graphics.DrawMeshInstanced(GroundStamp.Quad, 0, _material, _buffer, n);
            }
            _ = _propVersion;
        }

        /// <summary>
        /// Draws a shadow under each position, at one shared radius. Built for the crowd: the
        /// caller already walks the agents every frame to place their capsules, so it hands the
        /// same positions here rather than this class walking the sim a second time.
        /// </summary>
        public void Draw(IReadOnlyList<Vector3> positions, float radius)
        {
            if (!Enabled || _material == null || positions == null || positions.Count == 0) return;
            if (radius <= 0f) return;

            float d = radius * 2f;
            var scale = new Vector3(d, 1f, d);
            int n = 0;
            for (int i = 0; i < positions.Count; i++)
            {
                var p = positions[i];
                _buffer[n++] = Matrix4x4.TRS(
                    new Vector3(p.x, GroundY + GroundOffset, p.z), Quaternion.identity, scale);
                if (n == MaxInstancesPerDraw)
                {
                    Graphics.DrawMeshInstanced(GroundStamp.Quad, 0, _material, _buffer, n);
                    n = 0;
                }
            }
            if (n > 0) Graphics.DrawMeshInstanced(GroundStamp.Quad, 0, _material, _buffer, n);
        }

        /// <summary>
        /// Queues one shadow at its own radius, for things that are not all the same size.
        /// Flush with <see cref="FlushQueued"/> in the same frame.
        /// </summary>
        public void Queue(Vector3 position, float radius)
        {
            if (!Enabled || radius <= 0f) return;
            float d = radius * 2f;
            _dynamic.Add(Matrix4x4.TRS(
                new Vector3(position.x, GroundY + GroundOffset, position.z),
                Quaternion.identity, new Vector3(d, 1f, d)));
        }

        /// <summary>Draws and empties whatever <see cref="Queue"/> collected this frame.</summary>
        public void FlushQueued()
        {
            if (_material == null || _dynamic.Count == 0) { _dynamic.Clear(); return; }
            for (int start = 0; start < _dynamic.Count; start += MaxInstancesPerDraw)
            {
                int n = Mathf.Min(MaxInstancesPerDraw, _dynamic.Count - start);
                _dynamic.CopyTo(start, _buffer, 0, n);
                Graphics.DrawMeshInstanced(GroundStamp.Quad, 0, _material, _buffer, n);
            }
            _dynamic.Clear();
        }

        /// <summary>Releases the generated stamp and its material.</summary>
        public void Dispose()
        {
            GroundStamp.Discard(_material);
            GroundStamp.Discard(_stamp);
            _stamp = null;
        }
    }
}
