#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Assembles axis-aligned boxes into one mesh. Twenty-four vertices a box, so every face keeps
    /// its own normal and the cel bands and the inverted-hull outline both behave.
    ///
    /// Boxes rather than an imported low-poly model for the usual reason (see
    /// <see cref="MachineBody"/>), plus one specific to impostors: an impostor has to be the SAME
    /// SHAPE as the body it stands in for, and the bodies are boxes, so anything else would pop.
    /// </summary>
    public sealed class BoxMeshBuilder
    {
        private readonly List<Vector3> _v = new List<Vector3>(256);
        private readonly List<Vector3> _n = new List<Vector3>(256);
        private readonly List<int> _t = new List<int>(384);

        public int BoxCount { get; private set; }

        private static readonly Vector3[] FaceNormals =
        {
            Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back,
        };

        public BoxMeshBuilder Add(Vector3 centre, Vector3 size)
        {
            Vector3 h = size * 0.5f;
            for (int f = 0; f < 6; f++)
            {
                Vector3 n = FaceNormals[f];
                // Two axes spanning the face, picked off the normal.
                Vector3 a = f < 2 ? Vector3.up : Vector3.right;
                Vector3 b = Vector3.Cross(n, a);
                int baseIndex = _v.Count;
                for (int c = 0; c < 4; c++)
                {
                    float sa = (c == 0 || c == 3) ? -1f : 1f;
                    float sb = c < 2 ? -1f : 1f;
                    var corner = centre
                               + Vector3.Scale(n, h)
                               + Vector3.Scale(a, h) * sa
                               + Vector3.Scale(b, h) * sb;
                    _v.Add(corner);
                    _n.Add(n);
                }
                _t.Add(baseIndex); _t.Add(baseIndex + 1); _t.Add(baseIndex + 2);
                _t.Add(baseIndex); _t.Add(baseIndex + 2); _t.Add(baseIndex + 3);
            }
            BoxCount++;
            return this;
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(_v);
            mesh.SetNormals(_n);
            mesh.SetTriangles(_t, 0);
            mesh.RecalculateBounds();
            // Winding comes out inconsistent from the generic face loop above; rather than reason
            // about it per face, let Unity settle it. Backfaces on an impostor are not a cosmetic
            // problem -- the outline pass renders the BACK faces of an expanded hull, so a mesh
            // with mixed winding grows ink on the inside and vanishes on the outside.
            mesh.RecalculateNormals();
            mesh.UploadMeshData(markNoLongerReadable: false);
            return mesh;
        }
    }

    /// <summary>
    /// The middle and far distance tiers of the crowd.
    ///
    /// OWNER, 2026-09-11: "I can see the enemies far back on the map but they are still just red
    /// pills until they get way too close I should be able to see them as their actual character
    /// skin way sooner."
    ///
    /// Two separate faults, and this fixes both.
    ///
    /// FAULT ONE: THE FAR FIELD WAS A CAPSULE. ADR-007 specified three distance tiers and the game
    /// shipped two -- a real body, and then a pill. Promoting more real bodies does not fix that,
    /// because the reason the cutoff is where it is, is that a skinned character is expensive; push
    /// the radius far enough to cover a 128x96 map and the frame goes. So the fix is the tier that
    /// was never built: an INSTANCED SILHOUETTE. It costs almost exactly what the capsule cost --
    /// one <c>DrawMeshInstanced</c> per class per tier, a handful of draws for the entire rest of
    /// the map -- and it carries the one thing that survives to a few pixels, which is shape.
    ///
    /// FAULT TWO: THE CAPSULE WAS RED. Not a hit flash and not a health tint: the crowd's instanced
    /// material was <c>(0.75, 0.15, 0.12)</c>, a flat arterial red, left over from when the enemy
    /// was a flood of infected. ADR-003 retired that premise -- "No rot. No blood. ... Clean
    /// clothes, ordinary faces, normal human eyes" -- and nobody went back for the capsule. A red
    /// tide at distance and ordinary people up close is not a LOD, it is two different games.
    /// So the far field is now dressed: every body carries a per-instance colour, ordinary winter
    /// clothing for the signed, worn polymer for the machines, and the two archetypes keep exactly
    /// the orange and green the player already learned.
    ///
    /// THE CLASS READ IS THE POINT, and it has to hold at the range where it is hardest. A person
    /// impostor has a small head on a neck above narrow shoulders; a machine impostor has a flat
    /// sensor bar sitting straight on a wide yoke, and stands 26 cm taller. Those two outlines are
    /// different at a dozen pixels, which is the entire design.
    /// </summary>
    public sealed class CrowdImpostors
    {
        /// <summary>Middle tier and far tier. Tier 0 is the real body and is not drawn here.</summary>
        public const int LodCount = 2;

        private const int ClassCount = 4;
        private const int MaxPerDraw = 1023;

        private readonly Material _material;
        private readonly Mesh[] _meshes = new Mesh[ClassCount * LodCount];
        private readonly List<Matrix4x4>[] _matrices = new List<Matrix4x4>[ClassCount * LodCount];
        private readonly List<Vector4>[] _colours = new List<Vector4>[ClassCount * LodCount];
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();
        private readonly Matrix4x4[] _chunk = new Matrix4x4[MaxPerDraw];
        private readonly Vector4[] _chunkColours = new Vector4[MaxPerDraw];

        private static readonly int InstanceColorId = Shader.PropertyToID("_InstanceColor");

        /// <summary>
        /// Beyond this, a body drops from the detailed impostor to the cheap one. Chosen so the
        /// swap happens where the detail it drops -- arms, a neck, a cargo box -- is already below
        /// a pixel or two.
        /// </summary>
        public float DetailRange { get; set; } = 46f;

        /// <summary>How many impostors were queued last frame. For the budget readout.</summary>
        public int Queued { get; private set; }

        public CrowdImpostors(Material material)
        {
            _material = material;
            for (int i = 0; i < _matrices.Length; i++)
            {
                _matrices[i] = new List<Matrix4x4>(256);
                _colours[i] = new List<Vector4>(256);
            }
            for (int c = 0; c < ClassCount; c++)
            {
                _meshes[c * LodCount] = BuildMesh((BodyClass)c, detailed: true);
                _meshes[c * LodCount + 1] = BuildMesh((BodyClass)c, detailed: false);
            }
        }

        /// <summary>The silhouette used for a class at a tier. Exposed so tests can measure it.</summary>
        public Mesh MeshFor(BodyClass c, int lod) => _meshes[(int)c * LodCount + Mathf.Clamp(lod, 0, 1)];

        // ---- the silhouettes -------------------------------------------------------------------
        //
        // Proportions track the real bodies these stand in for: a person is 1.8 m (FitToHeight
        // sizes every civilian to exactly that) and a machine is MachineBody.Height. Getting those
        // two numbers right is most of the read, because relative height is legible long after
        // anything else has dissolved.

        /// <summary>A person is this tall. <c>FloodBootstrap</c> fits every civilian to it.</summary>
        public const float PersonHeight = 1.8f;

        private static Mesh BuildMesh(BodyClass c, bool detailed)
        {
            var b = new BoxMeshBuilder();
            bool machine = CrowdCasting.IsMachine(c);
            if (machine) Machine(b, c, detailed); else Person(b, c, detailed);
            return b.Build($"Impostor_{c}_{(detailed ? "mid" : "far")}");
        }

        private static void Person(BoxMeshBuilder b, BodyClass c, bool detailed)
        {
            // 1.8 m: legs 0.84, torso 0.62, shoulders 0.14, neck 0.08, head 0.22.
            b.Add(new Vector3(0f, 0.42f, 0f), new Vector3(0.32f, 0.84f, 0.22f));    // legs
            b.Add(new Vector3(0f, 1.15f, 0f), new Vector3(0.40f, 0.62f, 0.26f));    // torso
            if (detailed)
            {
                b.Add(new Vector3(0f, 1.53f, 0f), new Vector3(0.46f, 0.14f, 0.26f));   // shoulders
                b.Add(new Vector3(-0.26f, 1.19f, 0f), new Vector3(0.09f, 0.54f, 0.11f));
                b.Add(new Vector3(0.26f, 1.19f, 0f), new Vector3(0.09f, 0.54f, 0.11f));
                b.Add(new Vector3(0f, 1.64f, 0f), new Vector3(0.10f, 0.08f, 0.10f));   // NECK
            }
            // THE HEAD. Small, round-ish, and raised clear of the shoulders on a neck. This is the
            // silhouette a machine must not have.
            b.Add(new Vector3(0f, 1.79f, 0f), new Vector3(0.21f, 0.22f, 0.21f));

            // The Sapper's hard hat. A brim wider than the head, which is a shape a bare head is
            // not, and it survives to a very small number of pixels because it breaks the
            // outline's width exactly where a person is narrowest.
            if (c == BodyClass.Sapper)
                b.Add(new Vector3(0f, 1.915f, 0f), new Vector3(0.30f, 0.05f, 0.30f));
        }

        private static void Machine(BoxMeshBuilder b, BodyClass c, bool detailed)
        {
            float hip = MachineBody.HipHeight;
            float torsoTop = hip + MachineBody.TorsoHeight;
            float yokeTop = torsoTop + MachineBody.YokeHeight;

            if (detailed)
            {
                // Two straight legs with daylight between them: a machine stands with its feet
                // apart, which a walking person's silhouette closes.
                b.Add(new Vector3(-0.13f, hip * 0.5f, 0f), new Vector3(0.15f, hip, 0.17f));
                b.Add(new Vector3(0.13f, hip * 0.5f, 0f), new Vector3(0.15f, hip, 0.17f));
                b.Add(new Vector3(-0.34f, torsoTop - 0.32f, 0f), new Vector3(0.11f, 0.64f, 0.13f));
                b.Add(new Vector3(0.34f, torsoTop - 0.32f, 0f), new Vector3(0.11f, 0.64f, 0.13f));
            }
            else
            {
                b.Add(new Vector3(0f, hip * 0.5f, 0f), new Vector3(0.41f, hip, 0.17f));
            }

            b.Add(new Vector3(0f, (hip + torsoTop) * 0.5f, 0f),
                  new Vector3(MachineBody.TorsoWidthOf(MachineKind.DeliveryWalker),
                              MachineBody.TorsoHeight, 0.28f));

            // THE ANTI-HEAD: a wide yoke with a flat sensor block straight on top of it, no neck,
            // nothing round. Kept at both tiers because it is the whole class read.
            b.Add(new Vector3(0f, (torsoTop + yokeTop) * 0.5f, 0f),
                  new Vector3(MachineBody.ShoulderSpan, MachineBody.YokeHeight, 0.30f));
            b.Add(new Vector3(0f, yokeTop + MachineBody.HeadHeight * 0.5f, 0f),
                  new Vector3(MachineBody.HeadWidth, MachineBody.HeadHeight, 0.24f));

            // The Spitter's tank. Green is what the player already reads; this is what he reads it
            // ON, and it is the reason a Spitter is not just a green Humanoid.
            if (c == BodyClass.Spitter)
                b.Add(new Vector3(0f, (hip + torsoTop) * 0.5f, -0.26f), new Vector3(0.30f, 0.54f, 0.24f));
        }

        // ---- colour ---------------------------------------------------------------------------

        private static readonly Color[] Wardrobe =
        {
            new Color(0.23f, 0.26f, 0.33f),   // navy parka
            new Color(0.30f, 0.29f, 0.27f),   // charcoal
            new Color(0.38f, 0.36f, 0.28f),   // olive
            new Color(0.42f, 0.33f, 0.26f),   // brown canvas
            new Color(0.62f, 0.58f, 0.50f),   // oatmeal
            new Color(0.36f, 0.24f, 0.24f),   // maroon
            new Color(0.34f, 0.40f, 0.42f),   // slate
            new Color(0.50f, 0.48f, 0.44f),   // stone
        };

        /// <summary>
        /// What an agent is wearing at distance. Stable per id, so a body does not change clothes
        /// as it walks toward the camera -- the same rule as <see cref="CrowdCasting"/>, and for
        /// the same reason.
        ///
        /// ADR-003's crowd is "clean clothes, ordinary faces". Eight winter colours read as a
        /// street, and one red read as a plague.
        /// </summary>
        public static Color DressOf(BodyClass c, int id)
        {
            switch (c)
            {
                case BodyClass.Sapper: return new Color(0.95f, 0.45f, 0.06f);
                case BodyClass.Spitter: return MachineBody.SprayerGreen * 0.85f;
                case BodyClass.Humanoid:
                    // Worn polymer, three shades of it, so a rank of machines is not one object
                    // repeated.
                    float w = 0.66f + ((id * 2654435761u >> 13) & 3) * 0.05f;
                    return new Color(w, w * 0.99f, w * 0.95f);
                default:
                    return Wardrobe[(int)(((uint)id * 2246822519u >> 11) % (uint)Wardrobe.Length)];
            }
        }

        // ---- the frame --------------------------------------------------------------------------

        public void Begin()
        {
            for (int i = 0; i < _matrices.Length; i++) { _matrices[i].Clear(); _colours[i].Clear(); }
            Queued = 0;
        }

        /// <summary>
        /// Queue one body. <paramref name="travel"/> is how far it moved since the last frame: it
        /// supplies both the facing and whether the body is walking, so the caller does not have to
        /// track a speed it already threw away.
        /// </summary>
        public void Add(BodyClass c, Vector3 position, Vector3 travel, float distance, Vector4 colour)
        {
            int lod = distance > DetailRange ? 1 : 0;
            int bucket = (int)c * LodCount + lod;

            var rotation = travel.sqrMagnitude > 1e-8f
                ? Quaternion.LookRotation(new Vector3(travel.x, 0f, travel.z).normalized, Vector3.up)
                : Quaternion.identity;

            // A crouch on the stride, never a hop: the body dips and comes back to the ground, so
            // the feet stay on it and the contact shadow underneath stays honest.
            float moving = Mathf.Clamp01(travel.magnitude * 30f);
            float bob = -Mathf.Abs(Mathf.Sin(Time.time * 5.4f + position.x * 1.7f + position.z * 2.3f))
                        * 0.035f * moving;

            _matrices[bucket].Add(Matrix4x4.TRS(position + new Vector3(0f, bob, 0f), rotation, Vector3.one));
            _colours[bucket].Add(colour);
            Queued++;
        }

        /// <summary>Draw calls the last <see cref="Draw"/> issued. One per class per tier, plus overflow.</summary>
        public int DrawCalls { get; private set; }

        public void Draw()
        {
            DrawCalls = 0;
            for (int bucket = 0; bucket < _matrices.Length; bucket++)
            {
                var list = _matrices[bucket];
                if (list.Count == 0) continue;
                var colours = _colours[bucket];
                var mesh = _meshes[bucket];
                if (mesh == null) continue;

                for (int start = 0; start < list.Count; start += MaxPerDraw)
                {
                    int n = Mathf.Min(MaxPerDraw, list.Count - start);
                    list.CopyTo(start, _chunk, 0, n);
                    colours.CopyTo(start, _chunkColours, 0, n);
                    _block.SetVectorArray(InstanceColorId, _chunkColours);
                    Graphics.DrawMeshInstanced(mesh, 0, _material, _chunk, n, _block);
                    DrawCalls++;
                }
            }
        }
    }
}
