#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// The visual vocabulary of the SIGNAL weapons: the EMP burst, the emitter lance that replaces
    /// the bullet tracer, the tell on a body whose chip is dying, and the jammer field an area
    /// turret lays on the ground.
    ///
    /// WHY THIS REPLACES THE EXPLOSIONS AND TRACERS WHOLESALE. ADR-003 says nobody is infected and
    /// nobody is a monster: the enemy are ordinary people who took a neural implant tied to a
    /// stimulus payment, and the AI that administers the implant is driving them. The protagonist
    /// refused the chip. If his rifle still fires a yellow tracer and his airstrike still makes an
    /// orange fireball then the premise is a paragraph in a design document that the player never
    /// meets -- he is just shooting civilians. The weapons have to say, in the frame, that they are
    /// attacking the DEVICE. An expanding blue shell that leaves the body standing says it. A
    /// fireball does not.
    ///
    /// So: no orange anywhere in this file, no smoke, no blood, no gore. The palette is electric
    /// blue and white with one amber note, because amber is the implant's own colour -- the same
    /// amber as the temple light and the cargo drone's underside lamp. When the amber goes out,
    /// that person is finished. That is the only death cue the game needs.
    ///
    /// HOW IT DRAWS. Everything is generated: one icosphere, one annulus ring, the shared ground
    /// quad from <see cref="GroundStamp"/>, and one interference texture. All of it goes through
    /// <c>Exodus/SignalFx</c>, an unlit premultiplied shader whose banded rim term is what makes a
    /// sphere read as a hollow shockwave rather than a balloon. Colour and shape both arrive per
    /// instance, so a whole burst is five instanced draw calls and adding a stage costs none.
    ///
    /// WIRING (everything the integrator needs, in order):
    ///
    ///     // once, next to the other renderers
    ///     _signal = new SignalFx();
    ///     _signal.GroundY = 0f;
    ///
    ///     // instead of adding a Blast for an airstrike impact
    ///     _signal.AddEmp(ToWorld(imp.Center, 0.05f), imp.Radius);
    ///
    ///     // instead of adding a Tracer
    ///     _signal.AddBeam(ToWorld(shot.Origin, 0.75f), ToWorld(shot.End, 0.9f),
    ///                     SignalBeam.Emitter, landed: shot.Hit);
    ///
    ///     // per failing agent, inside the agent loop, before Draw
    ///     _signal.MarkFailing(headWorldPosition, facingWorldDirection, fail01, agentIndex);
    ///
    ///     // per area turret, inside the turret loop, before Draw
    ///     _signal.MarkJammer(ToWorld(turretCell, 0f), turret.Range, strength01: 1f);
    ///
    ///     // once per frame, at the end of DrawWorld
    ///     _signal.Draw(Time.deltaTime);
    ///
    ///     // and on a fall-back to the next position
    ///     _signal.Clear();
    ///
    /// Add/Mark is the whole distinction. ADD is an EVENT the renderer then owns and ages by
    /// itself; MARK is a fact about this frame which the caller re-states every frame and which
    /// this class forgets at the end of it. Failing bodies and jammer fields are facts -- that is
    /// why nothing ever has to tell the renderer that a body stopped failing, and why an effect
    /// can never be left burning on a corpse. <see cref="MarkEmp"/> is the one burst that works
    /// the same way, for anything driving a pulse off its own clock.
    ///
    /// TO LOOK AT IT: <c>-exodus-signalfx-demo</c> stages all four in front of the camera,
    /// <c>-exodus-signalfx-age &lt;0..1&gt;</c> freezes the burst at one stage, and
    /// <c>-exodus-signalfx-tell</c> gives the failing bodies a close-up. See SignalFxDemo.
    ///
    /// <see cref="Current"/> exists for the same reason <see cref="Decals.Current"/> does: the call
    /// sites that want to emit are scattered through gameplay code and none of them should have to
    /// be handed a renderer.
    /// </summary>
    public sealed class SignalFx
    {
        public const string ShaderName = "Exodus/SignalFx";

        /// <summary>How long one EMP burst lives, in seconds.</summary>
        public const float EmpLife = 1.15f;

        /// <summary>
        /// How long an emitter lance lives. Longer than the 0.06s ballistic tracer it replaces, on
        /// purpose: a bullet is allowed to be a line that was there and is gone, but a packet has
        /// to be SEEN travelling or it is just a line again. 0.16s is four frames of flight at
        /// sixty and still short enough that a fast weapon does not smear.
        /// </summary>
        public const float BeamLife = 0.16f;

        private const int MaxInstancesPerDraw = 1023;

        /// <summary>The effects system the game is currently running, if any.</summary>
        public static SignalFx? Current { get; private set; }

        /// <summary>Ground height. The play area is one flat plane, as it is for the decals.</summary>
        public float GroundY { get; set; }

        public bool Enabled { get; set; } = true;

        // ------------------------------------------------------------------ palette

        /// <summary>
        /// The whole palette, in one place, so "no orange" is a thing you can read rather than a
        /// thing you have to trust. <see cref="Palette"/> exposes it to the tests.
        /// </summary>
        public static readonly Color Core = new Color(1.00f, 1.00f, 1.00f);
        public static readonly Color Shell = new Color(0.33f, 0.80f, 1.00f);
        public static readonly Color Echo = new Color(0.20f, 0.52f, 0.95f);
        public static readonly Color Ring = new Color(0.62f, 0.94f, 1.00f);
        public static readonly Color Arc = new Color(0.86f, 0.98f, 1.00f);
        public static readonly Color Stamp = new Color(0.30f, 0.68f, 0.95f);
        public static readonly Color Emitter = new Color(0.62f, 0.95f, 1.00f);
        public static readonly Color TurretBeam = new Color(0.42f, 0.70f, 1.00f);
        public static readonly Color Jammer = new Color(0.26f, 0.62f, 0.92f);

        /// <summary>The implant's own colour. The one warm note in the file, and it is a DEVICE.</summary>
        public static readonly Color Implant = new Color(1.00f, 0.66f, 0.16f);

        /// <summary>What the implant flares to at the moment the decrypt lands.</summary>
        public static readonly Color ImplantFlare = new Color(1.00f, 0.93f, 0.72f);

        /// <summary>Everything the palette contains, for the test that asserts none of it is fire.</summary>
        public static IReadOnlyList<Color> Palette => new[]
        {
            Core, Shell, Echo, Ring, Arc, Stamp, Emitter, TurretBeam, Jammer,
        };

        // ------------------------------------------------------------------ live effects

        private struct Emp { public Vector3 Centre; public float Radius; public float Age; public int Seed; }
        private struct Beam { public Vector3 A, B; public float Age; public Color Ink; public bool Landed; }

        private readonly List<Emp> _emps = new List<Emp>(8);
        private readonly List<Beam> _beams = new List<Beam>(64);

        // Failing bodies and jammer fields are NOT retained. They are facts about the sim's current
        // state, not events, so the owner of that state re-declares them every frame and this class
        // never has to be told when one ends -- which is the difference between an effect that
        // lingers on a corpse and one that does not.
        private struct Failing { public Vector3 Head; public Vector3 Facing; public float Fail01; public int Id; }
        private struct Field { public Vector3 Centre; public float Radius; public float Strength; }

        private readonly List<Failing> _failing = new List<Failing>(64);
        private readonly List<Field> _fields = new List<Field>(16);

        // Bursts somebody else is clocking. Queued rather than drawn where they are declared,
        // because Draw empties the instance batches before it fills them -- a Mark that wrote
        // straight into a batch was silently erased by the next Draw, which cost a build and a
        // capture to find. EVERY per-frame declaration goes through a list for that reason.
        private readonly List<Emp> _held = new List<Emp>(4);

        // ------------------------------------------------------------------ resources

        private readonly Mesh _sphere;
        private readonly Mesh _ring;
        private readonly Mesh _bar;
        private readonly Material? _material;
        private readonly Material? _stampMaterial;
        private readonly Texture2D _interference;

        private readonly Matrix4x4[] _matrices = new Matrix4x4[MaxInstancesPerDraw];
        private readonly Vector4[] _colours = new Vector4[MaxInstancesPerDraw];
        private readonly Vector4[] _params = new Vector4[MaxInstancesPerDraw];
        private readonly MaterialPropertyBlock _block = new MaterialPropertyBlock();

        // Batches, keyed by mesh. Filled every frame and flushed at the end, so ten bursts and two
        // hundred failing bodies still cost one draw call per mesh rather than one per effect.
        private readonly Batch _spheres = new Batch(256);
        private readonly Batch _rings = new Batch(512);
        private readonly Batch _bars = new Batch(1024);
        private readonly Batch _quads = new Batch(128);

        private static readonly int InstanceColorId = Shader.PropertyToID("_InstanceColor");
        private static readonly int InstanceParamsId = Shader.PropertyToID("_InstanceParams");

        public SignalFx()
        {
            _sphere = BuildIcosphere(subdivisions: 2);
            _ring = BuildRing(segments: 48, inner: 0.78f);
            _bar = BuildBar();
            _interference = BuildInterference(192);

            var shader = Shader.Find(ShaderName);
            if (shader == null)
            {
                Debug.LogWarning($"[SignalFx] {ShaderName} not found; signal effects will not draw");
            }
            else
            {
                // 3160: above the ground stamps and the ordinary transparent queue. These are the
                // brightest thing on screen and they belong in front of everything they touch.
                _material = new Material(shader) { enableInstancing = true, renderQueue = 3160 };
            }

            // The jammer field is a FLAT MARK, not a shape in the air, so it goes on the ground
            // stamp path with the blob shadows and the scorch rings rather than on the FX shader.
            // Reusing that machinery also means it sorts correctly against them for free.
            _stampMaterial = GroundStamp.Material(Jammer, instanced: true, renderQueue: 3010);
            if (_stampMaterial != null) _stampMaterial.mainTexture = _interference;

            Current = this;
        }

        /// <summary>Live bursts and lances. Diagnostics and tests.</summary>
        public int EmpCount => _emps.Count;

        public int BeamCount => _beams.Count;

        // ------------------------------------------------------------------ emitting

        /// <summary>
        /// The airstrike's payload, now that the payload is a pulse. <paramref name="radius"/> is
        /// the sim's blast radius in metres; every stage is a multiple of it, so a small emitter
        /// discharge and a cargo drone's whole EMP read as the same event at two scales.
        /// </summary>
        public void AddEmp(Vector3 centre, float radius)
        {
            // The seed makes this burst's arcs its own. Derived from where it landed rather than
            // from a counter, so two bursts in the same place look the same and a replay of the
            // same match draws the same picture.
            int seed = Mathf.RoundToInt(centre.x * 73.3f) * 31 + Mathf.RoundToInt(centre.z * 91.7f);
            _emps.Add(new Emp { Centre = centre, Radius = Mathf.Max(radius, 0.25f), Age = 0f, Seed = seed });
        }

        /// <summary>
        /// One shot from a signal weapon. <paramref name="landed"/> is whether it found a chip --
        /// a shot that hit a wall gets the lance but not the terminal burst, because nothing in
        /// the wall was decrypted.
        /// </summary>
        public void AddBeam(Vector3 from, Vector3 to, SignalBeam kind, bool landed)
        {
            _beams.Add(new Beam
            {
                A = from,
                B = to,
                Age = 0f,
                Ink = kind == SignalBeam.Turret ? TurretBeam : Emitter,
                Landed = landed,
            });
        }

        /// <summary>
        /// Declares that this body's chip is dying THIS FRAME. <paramref name="fail01"/> runs 1 at
        /// the moment the decrypt completes down to 0 when the body drops;
        /// <paramref name="facing"/> may be <see cref="Vector3.zero"/>, in which case the implant
        /// is placed from <paramref name="id"/> so it is at least stable for that agent.
        /// <paramref name="head"/> is the world position of the head, not the feet.
        /// </summary>
        public void MarkFailing(Vector3 head, Vector3 facing, float fail01, int id)
        {
            _failing.Add(new Failing { Head = head, Facing = facing, Fail01 = Mathf.Clamp01(fail01), Id = id });
        }

        /// <summary>
        /// Stages ONE burst at an explicit age, for this frame only, without retaining it.
        ///
        /// This is the review tool and it earned its place immediately: a burst lives 1.15s and its
        /// electrical half is over in the first half of that, so shooting it by choosing a shutter
        /// delay means guessing at a phase and mostly photographing the aftermath. Three rounds of
        /// tuning went into stages that were never in frame. With this, a stage is a parameter.
        /// It is also the shape the integrator wants if it ever drives a burst off its own clock.
        /// </summary>
        public void MarkEmp(Vector3 centre, float radius, float age01, int seed)
        {
            _held.Add(new Emp
            {
                Centre = centre,
                Radius = Mathf.Max(radius, 0.25f),
                Age = Mathf.Clamp01(age01),
                Seed = seed,
            });
        }

        /// <summary>Declares an area turret's interference field for this frame.</summary>
        public void MarkJammer(Vector3 centre, float radius, float strength01)
        {
            _fields.Add(new Field { Centre = centre, Radius = Mathf.Max(radius, 0.5f), Strength = Mathf.Clamp01(strength01) });
        }

        /// <summary>Wipes everything. Call it on a fall-back to the next position.</summary>
        public void Clear()
        {
            _emps.Clear();
            _beams.Clear();
            _failing.Clear();
            _fields.Clear();
            _held.Clear();
        }

        public void Dispose()
        {
            Clear();
            GroundStamp.Discard(_material);
            GroundStamp.Discard(_stampMaterial);
            GroundStamp.Discard(_interference);
            GroundStamp.Discard(_sphere);
            GroundStamp.Discard(_ring);
            GroundStamp.Discard(_bar);
            if (ReferenceEquals(Current, this)) Current = null;
        }

        // ------------------------------------------------------------------ drawing

        /// <summary>
        /// Ages the retained effects, draws everything, and empties the per-frame declarations.
        /// One call, at the end of the frame's world drawing.
        /// </summary>
        public void Draw(float dt)
        {
            _spheres.Clear();
            _rings.Clear();
            _bars.Clear();
            _quads.Clear();

            AgeAndStageEmps(dt);
            for (int i = 0; i < _held.Count; i++) StageEmp(_held[i].Centre, _held[i].Radius, _held[i].Age, _held[i].Seed);
            AgeAndStageBeams(dt);
            StageFailing(Time.time);
            StageFields(Time.time);

            if (Enabled)
            {
                // Ground first, then bars, then shells: the ordering is what keeps the arcs
                // visible THROUGH the shell they are punching out of instead of behind it.
                Flush(GroundStamp.Quad, _stampMaterial, _quads);
                Flush(_ring, _material, _rings);
                Flush(_bar, _material, _bars);
                Flush(_sphere, _material, _spheres);
            }

            // Cleared whether or not we drew: these are declarations about this frame and a
            // disabled renderer must not accumulate a million of them.
            _failing.Clear();
            _fields.Clear();
            _held.Clear();
        }

        private void AgeAndStageEmps(float dt)
        {
            for (int i = _emps.Count - 1; i >= 0; i--)
            {
                var e = _emps[i];
                e.Age += dt;
                if (e.Age >= EmpLife) { _emps.RemoveAt(i); continue; }
                _emps[i] = e;
                StageEmp(e.Centre, e.Radius, e.Age / EmpLife, e.Seed);
            }
        }

        /// <summary>
        /// Draws one burst at one age. Split from the ageing loop so that a burst the renderer owns
        /// and a burst somebody else is clocking go through exactly the same code -- the review
        /// harness must photograph the effect the game ships, not a second implementation of it.
        /// </summary>
        private void StageEmp(Vector3 centre, float radius, float age01, int seed)
        {
            var f = Curves.Emp(age01);
            float r = radius;

            // ---- core ------------------------------------------------------------------
            // A hard white ball that is there for three frames. It is the only OPAQUE thing in
            // a burst, and it is what makes the eye look at the right piece of ground; the
            // rest of the effect is read after it, in the afterimage.
            if (f.CoreAlpha > 0.002f)
                _spheres.Add(Matrix4x4.TRS(centre + Vector3.up * r * 0.30f, Quaternion.identity,
                                           Vector3.one * (r * f.CoreScale)),
                             Core, f.CoreAlpha, Shape.Solid);

            // ---- shell -----------------------------------------------------------------
            if (f.ShellAlpha > 0.002f)
                _spheres.Add(Matrix4x4.TRS(centre + Vector3.up * r * 0.30f,
                                           Quaternion.Euler(0f, seed * 7f, 0f),
                                           Vector3.one * (r * f.ShellScale)),
                             Shell, f.ShellAlpha, Shape.Shell);

            // ---- echo ------------------------------------------------------------------
            // A second, slower, fainter front. One shell is a bubble; two is a discharge that
            // kept going after the first one, which is what an EMP does.
            if (f.EchoAlpha > 0.002f)
                _spheres.Add(Matrix4x4.TRS(centre + Vector3.up * r * 0.30f,
                                           Quaternion.Euler(0f, seed * 13f, 0f),
                                           Vector3.one * (r * f.EchoScale)),
                             Echo, f.EchoAlpha, Shape.WideShell);

            // ---- ground ring -----------------------------------------------------------
            // Outruns the shell on purpose. The pulse reaching the ground before it reaches
            // the top of the sphere is how the burst gets attached to a PLACE.
            if (f.RingAlpha > 0.002f)
            {
                float ring = r * f.RingRadius;
                _rings.Add(Matrix4x4.TRS(new Vector3(centre.x, GroundY + 0.07f, centre.z),
                                         Quaternion.identity, new Vector3(ring, 1f, ring)),
                           Ring, f.RingAlpha, Shape.Flat);
            }

            // ---- arcs ------------------------------------------------------------------
            StageArcs(centre, seed, f, r);

            // ---- ground stamp ----------------------------------------------------------
            // The interference disc lasts the whole life and outlives the light, so the last
            // thing left on screen is a mark on the ground rather than a fade to nothing.
            if (f.StampAlpha > 0.002f)
            {
                float d = r * f.StampRadius * 2f;
                _quads.Add(Matrix4x4.TRS(new Vector3(centre.x, GroundY + 0.03f, centre.z),
                                         Quaternion.Euler(0f, seed * 3f, 0f), new Vector3(d, 1f, d)),
                           Stamp, f.StampAlpha, Shape.Flat);
            }
        }

        private void StageArcs(Vector3 centre, int seed, Curves.EmpFrame f, float r)
        {
            if (f.ArcAlpha <= 0.002f) return;

            for (int a = 0; a < Curves.ArcCount; a++)
            {
                // Each arc flickers on its own schedule. An EMP that draws every arc on every
                // frame is a starburst decal; the ones that blink are what make it electrical.
                float flick = Curves.Hash01(seed * 977 + a * 31 + Mathf.FloorToInt(f.ArcPhase * 9f));
                if (flick < 0.16f) continue;

                // Started one node OUT, not at the centre. Eleven arcs all leaving the same
                // point put eleven lines through the same two metres of air, and that knot is the
                // brightest thing in the burst -- which is wrong, because the discharge is what
                // comes OFF the front. Beginning at the first node leaves the middle to the core
                // and the shell and gives the arcs clean air to travel through.
                var prev = Curves.ArcNode(seed, a, Curves.ArcFirstDrawnNode);
                for (int s = Curves.ArcFirstDrawnNode + 1; s <= Curves.ArcSegments; s++)
                {
                    var next = Curves.ArcNode(seed, a, s);
                    Vector3 p0 = centre + prev * (r * f.ArcReach) + Vector3.up * r * 0.30f;
                    Vector3 p1 = centre + next * (r * f.ArcReach) + Vector3.up * r * 0.30f;
                    prev = next;

                    // Segments thin toward the tip, so an arc tapers the way a spark does rather
                    // than reading as a bent stick of constant width.
                    float thick = r * Mathf.Lerp(0.042f, 0.011f, (s - 1) / (float)Curves.ArcSegments);

                    // A fat dim halo UNDER a thin hot core. Comic lightning is drawn twice -- a
                    // white line inside a coloured one -- and one flat bar on its own reads as a
                    // stick. Two draws is the cheapest possible version of that and it is the
                    // single change that made the burst stop looking like glassware.
                    AddSegment(_bars, p0, p1, thick * 2.4f, Shell, f.ArcAlpha * 0.55f, Shape.Glow);
                    AddSegment(_bars, p0, p1, thick, Arc, f.ArcAlpha, Shape.Solid);
                }
            }
        }

        private void AgeAndStageBeams(float dt)
        {
            for (int i = _beams.Count - 1; i >= 0; i--)
            {
                var b = _beams[i];
                b.Age += dt;
                if (b.Age >= BeamLife) { _beams.RemoveAt(i); continue; }
                _beams[i] = b;

                Vector3 d = b.B - b.A;
                float len = d.magnitude;
                if (len < 1e-3f) continue;
                Vector3 dir = d / len;

                var f = Curves.Beam(b.Age / BeamLife);

                // ---- outer glow -------------------------------------------------------------
                // The full drawn span, fat and faint, drawn first so the dashes sit inside it.
                // This is the thing that makes a one-centimetre line visible at forty metres.
                if (f.Alpha > 0.002f && f.HeadT > f.TailT)
                {
                    Vector3 g0 = b.A + dir * (len * f.TailT);
                    Vector3 g1 = b.A + dir * (len * f.HeadT);
                    AddSegment(_bars, g0, g1, 0.34f, b.Ink, f.Alpha * 0.13f, Shape.Glow);
                }

                // ---- dashed body ------------------------------------------------------------
                // A packet, not a bullet: the lance is BROKEN into equal pieces with gaps, which
                // is the single cheapest way to say "this is data" instead of "this is a round".
                int dashes = Curves.DashCount(len);
                for (int k = 0; k < dashes; k++)
                {
                    if (!Curves.DashSpan(k, len, f.TailT, f.HeadT, out float t0, out float t1)) continue;
                    Vector3 p0 = b.A + dir * (len * t0);
                    Vector3 p1 = b.A + dir * (len * t1);
                    AddSegment(_bars, p0, p1, 0.115f, b.Ink, f.Alpha, Shape.Solid);
                }

                // ---- leading head -----------------------------------------------------------
                if (f.Alpha > 0.002f && f.HeadT < 1f)
                {
                    Vector3 head = b.A + dir * (len * f.HeadT);
                    _spheres.Add(Matrix4x4.TRS(head, Quaternion.identity, Vector3.one * 0.30f),
                                 Core, f.Alpha, Shape.Solid);
                }

                // ---- terminal burst ---------------------------------------------------------
                // What a landed packet looks like: a small version of the EMP, on one person.
                if (b.Landed && f.BurstAlpha > 0.002f)
                {
                    _spheres.Add(Matrix4x4.TRS(b.B, Quaternion.identity, Vector3.one * f.BurstScale),
                                 Shell, f.BurstAlpha, Shape.Shell);
                    _spheres.Add(Matrix4x4.TRS(b.B, Quaternion.identity, Vector3.one * (f.BurstScale * 0.45f)),
                                 Core, f.BurstAlpha * 0.9f, Shape.Solid);
                }
            }
        }

        private void StageFailing(float time)
        {
            for (int i = 0; i < _failing.Count; i++)
            {
                var a = _failing[i];
                var f = Curves.Fail(a.Fail01, time, a.Id);

                // Where the temple is. A facing gives a real temple; without one the implant is
                // placed from the agent's id, which is at least STABLE -- a light that walks round
                // someone's head every frame is worse than a light in the wrong place.
                Vector3 fwd = a.Facing;
                fwd.y = 0f;
                if (fwd.sqrMagnitude < 1e-6f)
                {
                    float ang = Curves.Hash01(a.Id * 7919) * Mathf.PI * 2f;
                    fwd = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                }
                fwd.Normalize();
                Vector3 right = Vector3.Cross(Vector3.up, fwd);
                Vector3 temple = a.Head + right * 0.135f + fwd * 0.05f + Vector3.up * 0.04f;

                // ---- the implant ------------------------------------------------------------
                // The one thing that MUST be legible at any distance and on a capsule as well as
                // on a body, because the capsules are what most of the crowd is. It is a light in
                // the air at head height; it needs no anatomy under it to work.
                if (f.LampAlpha > 0.002f)
                {
                    _spheres.Add(Matrix4x4.TRS(temple, Quaternion.identity, Vector3.one * f.LampSize),
                                 f.LampColour, f.LampAlpha, Shape.Solid);
                    // A soft halo around it, so the flare reads as brightness rather than as the
                    // little cube simply getting bigger.
                    _spheres.Add(Matrix4x4.TRS(temple, Quaternion.identity, Vector3.one * (f.LampSize * 3.0f)),
                                 Implant, f.LampAlpha * 0.30f, Shape.Glow);
                }

                // ---- the closing rings -------------------------------------------------------
                // Two rings at slightly different heights, tightening onto the head as the decrypt
                // finishes. They are the CLOCK: a player who has seen this once knows how long a
                // failing body has left without a health bar anywhere on screen.
                if (f.RingAlpha > 0.002f)
                {
                    float outer = f.RingOuter;
                    _rings.Add(Matrix4x4.TRS(a.Head + Vector3.up * 0.06f,
                                             Quaternion.Euler(0f, time * 210f + a.Id * 37f, 0f),
                                             new Vector3(outer, 1f, outer)),
                               Shell, f.RingAlpha, Shape.Flat);

                    float inner = f.RingInner;
                    _rings.Add(Matrix4x4.TRS(a.Head - Vector3.up * 0.10f,
                                             Quaternion.Euler(0f, -time * 300f - a.Id * 53f, 0f),
                                             new Vector3(inner, 1f, inner)),
                               Ring, f.RingAlpha * 0.75f, Shape.Flat);
                }

                // ---- crackle -----------------------------------------------------------------
                // Three short arcs off the temple, only while the strobe is lit. Deliberately tiny:
                // this is a component failing inside a skull, not a body coming apart. ADR-003
                // forbids rot and blood and this is the whole reason the rule is easy to keep --
                // the interesting thing to look at is the device, so nothing else has to bleed.
                if (f.CrackleAlpha > 0.002f)
                {
                    for (int c = 0; c < 3; c++)
                    {
                        var n0 = Curves.CrackleNode(a.Id, c, 0, time);
                        var n1 = Curves.CrackleNode(a.Id, c, 1, time);
                        AddSegment(_bars, temple + n0 * f.CrackleReach, temple + n1 * f.CrackleReach,
                                   0.022f, Arc, f.CrackleAlpha, Shape.Solid);
                    }
                }
            }
        }

        private void StageFields(float time)
        {
            for (int i = 0; i < _fields.Count; i++)
            {
                var fld = _fields[i];
                var j = Curves.Jammer(time, fld.Centre.x + fld.Centre.z, fld.Strength);

                // ---- the field itself --------------------------------------------------------
                // FLAT AND LOW, which is the design note: an area turret's coverage is a piece of
                // GROUND the player is choosing to own, so the effect has to be the shape of that
                // ground. A dome would say "this volume is protected" and would also hide the
                // thirty people walking through it.
                float d = fld.Radius * 2f * j.Scale;
                _quads.Add(Matrix4x4.TRS(new Vector3(fld.Centre.x, GroundY + 0.045f, fld.Centre.z),
                                         Quaternion.Euler(0f, j.Spin, 0f), new Vector3(d, 1f, d)),
                           Jammer, j.Alpha, Shape.Flat);

                // ---- the sweep ---------------------------------------------------------------
                // One bright ring travelling out on a loop. Without it the disc is a texture that
                // happens to be lying there; with it the emplacement is plainly DOING something,
                // which is what the player is paying for.
                if (j.SweepAlpha > 0.002f)
                {
                    float s = fld.Radius * 2f * j.Sweep;
                    _rings.Add(Matrix4x4.TRS(new Vector3(fld.Centre.x, GroundY + 0.09f, fld.Centre.z),
                                             Quaternion.identity, new Vector3(s, 1f, s)),
                               Ring, j.SweepAlpha, Shape.Flat);
                }
            }
        }

        // ------------------------------------------------------------------ batching

        /// <summary>
        /// The four shape presets, as per-instance rim settings. These are a VOCABULARY, not a
        /// convenience: everything in this file is one of a solid body, a hollow front, a soft
        /// glow or a flat mark, and keeping that to four named cases is what stops the effects
        /// drifting apart from each other as they get tuned.
        /// </summary>
        private enum Shape { Solid, Shell, WideShell, Glow, Flat }

        /// <summary>x = rim power, y = rim strength, z = bands, w = occlusion.</summary>
        private static Vector4 ParamsFor(Shape shape) => shape switch
        {
            // Opaque and flat-shaded. The only thing in a burst that hides the world behind it.
            Shape.Solid => new Vector4(1f, 0f, 1f, 1f),
            // Bright at the silhouette, empty through the middle, in three hard steps.
            //
            // THE RIM POWER IS THE WHOLE EFFECT and 2.6 was too low: a gentle falloff quantises to
            // a non-zero band over most of the sphere, which fills the middle at a third alpha and
            // turns a shockwave into a soap bubble. 4.8 keeps only the grazing ring above the first
            // step, so everything inside it clips away and the shell is genuinely empty.
            // TWO bands, not three. Three steps on a sphere is a smooth-ish falloff wearing a
            // stencil, and it photographed as glassware; two is a hot rim and one flat interior
            // step, which is how a shockwave gets drawn on a page.
            Shape.Shell => new Vector4(3.0f, 1f, 2f, 0.46f),
            // The echo: a fatter, softer band so the two fronts do not look like one shape twice.
            Shape.WideShell => new Vector4(2.2f, 1f, 2f, 0.20f),
            // Pure additive haze. Never occludes; it is light in the air.
            Shape.Glow => new Vector4(1.2f, 0.30f, 2f, 0f),
            // Rings and ground marks: no view dependence at all, they are already the right shape.
            _ => new Vector4(1f, 0f, 1f, 0.85f),
        };

        private sealed class Batch
        {
            public readonly List<Matrix4x4> Matrices;
            public readonly List<Vector4> Colours;
            public readonly List<Vector4> Params;

            public Batch(int capacity)
            {
                Matrices = new List<Matrix4x4>(capacity);
                Colours = new List<Vector4>(capacity);
                Params = new List<Vector4>(capacity);
            }

            public int Count => Matrices.Count;

            public void Clear() { Matrices.Clear(); Colours.Clear(); Params.Clear(); }

            public void Add(Matrix4x4 m, Color ink, float alpha, Shape shape)
                => Add(m, ink, alpha, ParamsFor(shape));

            public void Add(Matrix4x4 m, Color ink, float alpha, Vector4 shape)
            {
                Matrices.Add(m);
                // Alpha doubles as the shader's "this instance was given data" signature, so it
                // must never be exactly zero on an instance we are actually drawing.
                Colours.Add(new Vector4(ink.r, ink.g, ink.b, Mathf.Max(alpha, 0.003f)));
                Params.Add(shape);
            }
        }

        private void AddSegment(Batch batch, Vector3 a, Vector3 b, float thickness, Color ink, float alpha, Shape shape)
        {
            Vector3 d = b - a;
            float len = d.magnitude;
            if (len < 1e-4f) return;
            batch.Add(Matrix4x4.TRS(a + d * 0.5f, Quaternion.LookRotation(d / len, Vector3.up),
                                    new Vector3(thickness, thickness, len)),
                      ink, alpha, ParamsFor(shape));
        }

        private void Flush(Mesh? mesh, Material? material, Batch batch)
        {
            if (mesh == null || material == null || batch.Count == 0) return;

            for (int start = 0; start < batch.Count; start += MaxInstancesPerDraw)
            {
                int n = Mathf.Min(MaxInstancesPerDraw, batch.Count - start);
                batch.Matrices.CopyTo(start, _matrices, 0, n);
                batch.Colours.CopyTo(start, _colours, 0, n);
                batch.Params.CopyTo(start, _params, 0, n);

                _block.Clear();
                _block.SetVectorArray(InstanceColorId, _colours);
                _block.SetVectorArray(InstanceParamsId, _params);
                Graphics.DrawMeshInstanced(mesh, 0, material, _matrices, n, _block);
            }
        }

        // ------------------------------------------------------------------ meshes

        /// <summary>
        /// A low icosphere, 320 triangles at subdivision 2. Not Unity's 515-vertex sphere, which
        /// is heavier than it needs to be and carries UVs and tangents nothing here reads, and not
        /// the 80-triangle version either -- see the note on normals below for why that failed.
        /// </summary>
        private static Mesh BuildIcosphere(int subdivisions)
        {
            float t = (1f + Mathf.Sqrt(5f)) * 0.5f;
            var verts = new List<Vector3>
            {
                new Vector3(-1,  t,  0), new Vector3( 1,  t,  0), new Vector3(-1, -t,  0), new Vector3( 1, -t,  0),
                new Vector3( 0, -1,  t), new Vector3( 0,  1,  t), new Vector3( 0, -1, -t), new Vector3( 0,  1, -t),
                new Vector3( t,  0, -1), new Vector3( t,  0,  1), new Vector3(-t,  0, -1), new Vector3(-t,  0,  1),
            };
            var tris = new List<int>
            {
                0,11,5, 0,5,1, 0,1,7, 0,7,10, 0,10,11,
                1,5,9, 5,11,4, 11,10,2, 10,7,6, 7,1,8,
                3,9,4, 3,4,2, 3,2,6, 3,6,8, 3,8,9,
                4,9,5, 2,4,11, 6,2,10, 8,6,7, 9,8,1,
            };

            for (int s = 0; s < subdivisions; s++)
            {
                var next = new List<int>(tris.Count * 4);
                var cache = new Dictionary<long, int>();
                for (int i = 0; i < tris.Count; i += 3)
                {
                    int a = tris[i], b = tris[i + 1], c = tris[i + 2];
                    int ab = Midpoint(verts, cache, a, b);
                    int bc = Midpoint(verts, cache, b, c);
                    int ca = Midpoint(verts, cache, c, a);
                    next.AddRange(new[] { a, ab, ca, b, bc, ab, c, ca, bc, ab, bc, ca });
                }
                tris = next;
            }

            // SMOOTH NORMALS, and this was the second thing that made the shell invisible. Facets
            // and a narrow rim band do not combine: at eighty triangles almost no face happens to
            // lie within the grazing band, so the shell drew a scatter of lit triangles and then,
            // when the band was narrowed further, nothing at all. The faceting is also redundant
            // -- the BANDING is what makes this look drawn, and a continuous normal is what lets
            // the quantiser lay those bands down as clean concentric rings instead of confetti.
            var outVerts = new Vector3[verts.Count];
            var outNorms = new Vector3[verts.Count];
            for (int i = 0; i < verts.Count; i++)
            {
                outNorms[i] = verts[i].normalized;
                outVerts[i] = outNorms[i] * 0.5f;
            }

            var mesh = new Mesh { name = "SignalFxSphere" };
            mesh.SetVertices(outVerts);
            mesh.SetNormals(outNorms);
            mesh.SetTriangles(tris, 0);
            // Generous, for the same reason the ground quad's are: DrawMeshInstanced culls a whole
            // batch against the MESH bounds, and a tight unit sphere gets a burst on the far side
            // of the map culled the moment the camera looks a little away from it.
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
            mesh.UploadMeshData(markNoLongerReadable: false);
            return mesh;
        }

        private static int Midpoint(List<Vector3> verts, Dictionary<long, int> cache, int a, int b)
        {
            long key = a < b ? ((long)a << 32) | (uint)b : ((long)b << 32) | (uint)a;
            if (cache.TryGetValue(key, out int found)) return found;
            var mid = ((verts[a] + verts[b]) * 0.5f).normalized;
            verts.Add(mid);
            int index = verts.Count - 1;
            cache[key] = index;
            return index;
        }

        /// <summary>
        /// A flat annulus in the XZ plane, one unit across, facing up. A real ring rather than a
        /// squashed cylinder: the ground ring has to be HOLLOW, and a cylinder scaled thin is a
        /// filled disc that hides everything it expands over -- including the bodies the pulse is
        /// supposed to be passing through.
        /// </summary>
        private static Mesh BuildRing(int segments, float inner)
        {
            var verts = new Vector3[segments * 2];
            var norms = new Vector3[segments * 2];
            var tris = new int[segments * 6];

            for (int i = 0; i < segments; i++)
            {
                float a = (i / (float)segments) * Mathf.PI * 2f;
                float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
                verts[i * 2] = new Vector3(cos * inner * 0.5f, 0f, sin * inner * 0.5f);
                verts[i * 2 + 1] = new Vector3(cos * 0.5f, 0f, sin * 0.5f);
                norms[i * 2] = Vector3.up;
                norms[i * 2 + 1] = Vector3.up;

                int n = (i + 1) % segments;
                int o = i * 6;
                tris[o] = i * 2; tris[o + 1] = i * 2 + 1; tris[o + 2] = n * 2 + 1;
                tris[o + 3] = i * 2; tris[o + 4] = n * 2 + 1; tris[o + 5] = n * 2;
            }

            var mesh = new Mesh { name = "SignalFxRing" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
            mesh.UploadMeshData(markNoLongerReadable: false);
            return mesh;
        }

        /// <summary>
        /// A unit box with flat normals, used for every straight piece of light: arc segments,
        /// beam dashes, the outer glow. Built rather than harvested because
        /// <c>GameObject.CreatePrimitive</c> needs a runtime and this type has to be constructible
        /// from an EditMode test.
        /// </summary>
        private static Mesh BuildBar()
        {
            var dirs = new[] { Vector3.forward, Vector3.back, Vector3.up, Vector3.down, Vector3.right, Vector3.left };
            var verts = new Vector3[24];
            var norms = new Vector3[24];
            var tris = new int[36];

            for (int f = 0; f < 6; f++)
            {
                Vector3 n = dirs[f];
                Vector3 u = (Mathf.Abs(n.y) > 0.5f) ? Vector3.forward : Vector3.up;
                Vector3 r = Vector3.Cross(u, n);
                int v = f * 4;
                verts[v] = (n - u - r) * 0.5f;
                verts[v + 1] = (n + u - r) * 0.5f;
                verts[v + 2] = (n + u + r) * 0.5f;
                verts[v + 3] = (n - u + r) * 0.5f;
                for (int k = 0; k < 4; k++) norms[v + k] = n;

                int o = f * 6;
                tris[o] = v; tris[o + 1] = v + 1; tris[o + 2] = v + 2;
                tris[o + 3] = v; tris[o + 4] = v + 2; tris[o + 5] = v + 3;
            }

            var mesh = new Mesh { name = "SignalFxBar" };
            mesh.SetVertices(verts);
            mesh.SetNormals(norms);
            mesh.SetTriangles(tris, 0);
            mesh.bounds = new Bounds(Vector3.zero, Vector3.one);
            mesh.UploadMeshData(markNoLongerReadable: false);
            return mesh;
        }

        /// <summary>
        /// The interference stamp: hard concentric rings, radial ticks, and dropouts where the
        /// signal is not reaching. Three flat tones and no gradient, for the same reason the
        /// scorch ring has none -- a soft radial falloff is a photograph of a light, and what is
        /// wanted is a diagram somebody drew of a jammed volume.
        /// </summary>
        private static Texture2D BuildInterference(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, mipChain: true)
            {
                name = "SignalFxInterference",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                anisoLevel = 2,
            };

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                float v = (y + 0.5f) / size;
                for (int x = 0; x < size; x++)
                {
                    float u = (x + 0.5f) / size;
                    pixels[y * size + x] = new Color32(255, 255, 255,
                        (byte)Mathf.RoundToInt(Curves.Interference(u, v) * 255f));
                }
            }

            tex.SetPixels32(pixels);
            tex.Apply(updateMipmaps: true, makeNoLongerReadable: true);
            return tex;
        }

        // ------------------------------------------------------------------ the maths

        /// <summary>
        /// Every curve, every stage boundary and every piece of pseudo-randomness these effects
        /// use, as pure functions of time.
        ///
        /// Split out from the drawing for two reasons. The first is hard rule 2: this is the only
        /// part of an effect that can be wrong in a way a screenshot will not catch -- a shell
        /// that stops expanding, an arc that flips inside out, a strobe that never goes dark --
        /// and all of it is testable without a renderer. The second is that TUNING an effect is
        /// almost entirely tuning these numbers, and having them in one screen together is what
        /// makes it possible to keep the four effects reading as one family instead of four.
        ///
        /// Nothing in here touches Time, Random or any Unity object, so a test can step an effect
        /// through its whole life in a loop.
        /// </summary>
        public static class Curves
        {
            /// <summary>Arcs thrown out by one burst, and nodes in each arc's jagged polyline.</summary>
            public const int ArcCount = 11;
            public const int ArcSegments = 5;

            /// <summary>
            /// Which node an arc is drawn FROM. Node 0 is still the origin -- that invariant is
            /// what guarantees an arc can never fold back through itself -- but the innermost
            /// segment is not drawn, because every arc shares it.
            /// </summary>
            public const int ArcFirstDrawnNode = 1;

            /// <summary>Metres of lit dash and unlit gap along an emitter lance.</summary>
            public const float DashLength = 0.45f;
            public const float DashGap = 0.55f;

            /// <summary>How long a jammer sweep takes to travel from the emplacement to the rim.</summary>
            public const float JammerPeriod = 1.35f;

            // Stage boundaries, as fractions of an EMP's own life. Fractions rather than seconds
            // so a small emitter discharge and a cargo drone's full pulse are the SAME EVENT at
            // two scales -- the staging is the thing the player learns, not the duration.
            private const float CoreUntil = 0.130f;
            private const float ShellUntil = 0.60f;
            private const float EchoFrom = 0.13f;
            private const float EchoUntil = 0.90f;
            private const float RingUntil = 0.34f;
            private const float ArcUntil = 0.62f;
            private const float StampGrow = 0.32f;

            // Beam stages.
            private const float TravelUntil = 0.42f;
            private const float TailFrom = 0.34f;

            /// <summary>One EMP burst at one instant. All scales are multiples of the blast radius.</summary>
            public struct EmpFrame
            {
                public float CoreScale, CoreAlpha;
                public float ShellScale, ShellAlpha;
                public float EchoScale, EchoAlpha;
                public float RingRadius, RingAlpha;
                public float ArcReach, ArcAlpha, ArcPhase;
                public float StampRadius, StampAlpha;
            }

            /// <summary>
            /// The staging the old fireball had, with the fire taken out. A flash, then a front,
            /// then a mark on the ground -- that order is what an eye expects from a detonation
            /// and abandoning it would make the EMP read as a UI effect rather than as a thing
            /// that happened in the world. What changed is everything the stages are MADE of.
            /// </summary>
            public static EmpFrame Emp(float age01)
            {
                float t = Mathf.Clamp01(age01);
                var f = default(EmpFrame);
                f.ArcPhase = t;

                // The core is opaque white and gone in three frames. It is not an explosion, it is
                // an OVEREXPOSURE -- what a camera does when something far too bright happens in
                // front of it, which is what the eye remembers of a real discharge.
                if (t < CoreUntil)
                {
                    float k = t / CoreUntil;
                    f.CoreScale = Mathf.Lerp(0.35f, 1.25f, Out3(k));
                    f.CoreAlpha = 1f - k * k;
                }

                // The front. Fast out and decelerating, because a pulse loses to the air; a shell
                // that expands linearly reads as an inflating balloon at any speed.
                if (t < ShellUntil)
                {
                    float k = t / ShellUntil;
                    f.ShellScale = Mathf.Lerp(0.22f, 1.55f, Out3(k));
                    f.ShellAlpha = Mathf.Pow(1f - k, 1.1f);
                }

                // The echo: wider, slower, fainter, and it starts LATE. Two fronts is the whole
                // difference between a bubble and a discharge that is still discharging.
                if (t >= EchoFrom && t < EchoUntil)
                {
                    float k = (t - EchoFrom) / (EchoUntil - EchoFrom);
                    f.EchoScale = Mathf.Lerp(0.20f, 2.15f, Out3(k));
                    f.EchoAlpha = Mathf.Pow(1f - k, 2f) * 0.50f;
                }

                // The ground ring OUTRUNS the shell, and that is deliberate. It is the piece that
                // attaches a ball of light in the air to a piece of ground the player owns.
                if (t < RingUntil)
                {
                    float k = t / RingUntil;
                    f.RingRadius = Mathf.Lerp(0.35f, 2.40f, Out3(k));
                    f.RingAlpha = Mathf.Pow(1f - k, 0.85f);
                }

                // ARCS OVERTAKE THE SHELL. At a reach of 1.30 against a shell that grows to
                // 1.55 every spike stayed inside the front, where the shell's own brightness
                // swallowed it -- nine arcs were drawn and not one was visible in the capture.
                // They have to be the thing that BREAKS the silhouette, so they run out past it.
                if (t < ArcUntil)
                {
                    float k = t / ArcUntil;
                    f.ArcReach = Mathf.Lerp(0.35f, 1.70f, Out3(k));
                    f.ArcAlpha = Mathf.Pow(1f - k, 1.1f);
                }

                // The stamp outlives the light, so the last frame of the effect is a mark on the
                // ground rather than a fade to nothing. Where the old blast left soot, this leaves
                // interference: same idea, different physics.
                // Kept close to the blast's OWN radius. The first version ran it out to 2.15x, which
                // on a five-metre strike is a twenty-metre disc of radar rings covering half the
                // screen -- the mark stopped being a mark and became the weather.
                f.StampRadius = Mathf.Lerp(0.70f, 1.25f, Out3(Mathf.Clamp01(t / StampGrow)));
                f.StampAlpha = Mathf.Pow(1f - t, 1.4f) * 0.85f;

                return f;
            }

            /// <summary>One emitter lance at one instant, in fractions along its own length.</summary>
            public struct BeamFrame
            {
                /// <summary>Where the leading head has reached, 0 at the muzzle, 1 at the target.</summary>
                public float HeadT;

                /// <summary>Where the trailing end has reached. It starts late and catches up.</summary>
                public float TailT;

                public float Alpha;
                public float BurstScale, BurstAlpha;
            }

            /// <summary>
            /// A packet leaving and arriving, rather than a line that was briefly there. The head
            /// travels for the first 42% of the life and the tail then chases it in, so the lance
            /// streams INTO the target and vanishes from the muzzle end -- which is the read that
            /// separates a transmission from a bullet.
            /// </summary>
            public static BeamFrame Beam(float age01)
            {
                float t = Mathf.Clamp01(age01);
                var f = default(BeamFrame);
                f.HeadT = Mathf.Clamp01(t / TravelUntil);
                f.TailT = t <= TailFrom ? 0f : Out3(Mathf.Clamp01((t - TailFrom) / (1f - TailFrom)));
                f.Alpha = t < 0.5f ? 1f : Mathf.Clamp01((1f - t) / 0.5f);

                if (t >= TravelUntil)
                {
                    float k = (t - TravelUntil) / (1f - TravelUntil);
                    f.BurstScale = Mathf.Lerp(0.25f, 1.15f, Mathf.Sqrt(k));
                    f.BurstAlpha = Mathf.Pow(1f - k, 1.5f);
                }
                return f;
            }

            /// <summary>How many dashes fit along a lance of this length.</summary>
            public static int DashCount(float length)
            {
                if (length <= 0f) return 0;
                return Mathf.Max(1, Mathf.CeilToInt(length / (DashLength + DashGap)));
            }

            /// <summary>
            /// The span of dash <paramref name="index"/>, as fractions along the lance, clipped to
            /// the drawn window between the tail and the head. False when this dash is entirely
            /// outside the window, which is most of them for most of the flight.
            ///
            /// Dashes are spaced in METRES, not in fractions, so a lance across the map and a lance
            /// across a room have the same dash size. Spacing them in fractions would have made a
            /// long shot's dashes long, which reads as a slower packet the further you shoot.
            /// </summary>
            public static bool DashSpan(int index, float length, float tail01, float head01,
                                        out float t0, out float t1)
            {
                t0 = 0f; t1 = 0f;
                if (length <= 1e-4f) return false;

                float period = DashLength + DashGap;
                float a = index * period;
                float b = a + DashLength;
                t0 = Mathf.Max(Mathf.Clamp01(a / length), Mathf.Clamp01(tail01));
                t1 = Mathf.Min(Mathf.Clamp01(b / length), Mathf.Clamp01(head01));
                return t1 - t0 > 1e-3f;
            }

            /// <summary>One failing chip at one instant.</summary>
            public struct FailFrame
            {
                /// <summary>0 the moment the decrypt lands, 1 when the body drops.</summary>
                public float Decay;

                /// <summary>Whether the implant is showing light this instant.</summary>
                public bool Lit;

                public float LampSize, LampAlpha;
                public Color LampColour;
                public float RingOuter, RingInner, RingAlpha;
                public float CrackleAlpha, CrackleReach;

                /// <summary>
                /// What to multiply the body's own colour by. This is the only part of the tell
                /// that the renderer cannot draw itself -- the crowd owns the bodies.
                /// </summary>
                public Color Tint;
            }

            /// <summary>
            /// The tell on a body whose chip is being decrypted. <paramref name="fail01"/> runs
            /// 1 down to 0 over the failing window.
            ///
            /// ADR-003 SAYS NO ROT AND NO BLOOD, and this is the design that makes that rule easy
            /// rather than a restriction. Nothing here is about a body at all: a device flares,
            /// stutters, loses its rhythm and goes out, and the person is merely standing where it
            /// happens. That is also why it survives on a capsule -- there is no anatomy in the
            /// effect, so the distant instanced crowd gets exactly the same tell as a skinned body.
            /// </summary>
            public static FailFrame Fail(float fail01, float time, int id)
            {
                float decay = 1f - Mathf.Clamp01(fail01);
                var f = default(FailFrame);
                f.Decay = decay;

                // THE PHASE JUMPS WHEN THE FREQUENCY DOES, AND THAT IS THE EFFECT. A strobe driven
                // by time*frequency slips every time the frequency moves; a clean square wave reads
                // as a machine working and a slipping one reads as a machine losing the thread.
                // The dropout on top of it is what stops the eye finding the rhythm at all.
                float freq = Mathf.Lerp(6f, 27f, decay);
                float duty = Mathf.Lerp(0.78f, 0.20f, decay);
                float phase = time * freq;
                int tick = Mathf.FloorToInt(phase);
                bool dropout = Hash01(tick * 31 + id * 17) < Mathf.Lerp(0.04f, 0.44f, decay);
                f.Lit = (phase - tick) < duty && !dropout;

                // One bright flare at the front, so the moment the shot LANDS is unmissable even
                // in a crowd. Everything after it is the chip coming apart.
                // One bright flare, but a SMALL one. At 0.36m across with a 1.3m halo the
                // flare stopped being a light on someone's temple and became a lens flare in
                // front of their head -- the effect has to stay the size of the object it is
                // supposed to be coming out of, or the fiction it carries goes with it.
                float flare = decay < 0.16f ? 1f - decay / 0.16f : 0f;
                f.LampSize = Mathf.Lerp(0.105f, 0.062f, decay) * (1f + 0.85f * flare);
                f.LampColour = Color.Lerp(Implant, ImplantFlare, Mathf.Max(flare, decay * 0.6f));
                // Never fully dark until the end. A strobe that goes to nothing between flashes
                // is invisible in half the frames, and half the bodies in a still then look
                // untouched; an ember that BRIGHTENS is trackable in a crowd and still strobes.
                // Fading toward nothing as the decay completes is the death cue, and it is the
                // only one the game needs.
                f.LampAlpha = (f.Lit ? 1f : 0.30f) * Mathf.Clamp01(1.04f - decay * 0.92f);

                // The rings are the CLOCK. A player who has watched one of these finish knows how
                // much longer this body can still hit him, with no bar anywhere on the screen.
                f.RingOuter = Mathf.Lerp(1.05f, 0.26f, Mathf.Pow(decay, 0.75f));
                f.RingInner = f.RingOuter * Mathf.Lerp(0.62f, 0.90f, decay);
                f.RingAlpha = Mathf.Lerp(0.45f, 1f, decay) * (f.Lit ? 1f : 0.50f);

                f.CrackleAlpha = f.Lit ? Mathf.Lerp(0.35f, 1f, decay) : 0f;
                f.CrackleReach = Mathf.Lerp(0.16f, 0.30f, decay);

                // Cold and DESATURATING, never darker-and-redder. The body is not dying of a
                // wound; the thing driving it is losing its grip, and colour draining out is how
                // that reads without a single drop of anything.
                var cold = new Color(0.58f, 0.66f, 0.80f);
                var tint = Color.Lerp(Color.white, cold, Mathf.Pow(decay, 1.15f));
                if (!f.Lit && decay > 0.25f)
                {
                    float dip = Mathf.Lerp(1f, 0.72f, decay);
                    tint = new Color(tint.r * dip, tint.g * dip, tint.b * dip);
                }
                tint.a = 1f;
                f.Tint = tint;

                return f;
            }

            /// <summary>
            /// What to multiply a failing body's own colour by, for the crowd renderer, which owns
            /// the bodies and is the only thing that can apply it.
            /// </summary>
            public static Color FailTint(float fail01, float time, int id) => Fail(fail01, time, id).Tint;

            /// <summary>One jammer field at one instant.</summary>
            public struct JammerFrame
            {
                public float Scale, Alpha, Spin;
                public float Sweep, SweepAlpha;
            }

            /// <summary>
            /// The interference an area emplacement lays down. <paramref name="phase"/> is any
            /// per-emplacement number -- pass its position -- and exists purely so that two
            /// Brush Hogs side by side do not pulse in lockstep, which reads as one effect
            /// stretched over both rather than two machines working.
            /// </summary>
            public static JammerFrame Jammer(float time, float phase, float strength01)
            {
                float s = Mathf.Clamp01(strength01);
                var f = default(JammerFrame);

                float breathe = Mathf.Sin((time + phase) * 3.1f);
                f.Scale = 1f + 0.035f * breathe;
                f.Alpha = s * (0.34f + 0.10f * breathe);
                f.Spin = Mathf.Repeat(time * 11f + phase * 40f, 360f);

                float k = Mathf.Repeat((time + phase) / JammerPeriod, 1f);
                f.Sweep = Mathf.Lerp(0.18f, 1.02f, k);
                f.SweepAlpha = s * Mathf.Pow(1f - k, 1.3f) * 0.75f;
                return f;
            }

            /// <summary>
            /// The jammer's stamp, per pixel, over a 0..1 quad. Hard rings, radial ticks and wedges
            /// where the signal is not reaching. Three flat tones and no gradient anywhere: a soft
            /// radial falloff is a photograph of a lamp, and what is wanted is a diagram of a
            /// jammed piece of ground.
            /// </summary>
            public static float Interference(float u, float v)
            {
                float dx = u - 0.5f, dy = v - 0.5f;
                float r = Mathf.Sqrt(dx * dx + dy * dy) * 2f;
                if (r >= 0.99f) return 0f;

                // A hard bright rim, because the player builds against this edge and has to be
                // able to see exactly where the emplacement stops covering.
                if (r > 0.93f) return 0.95f;

                float ang = Mathf.Atan2(dy, dx);
                float value = Mathf.Repeat(r * 7f, 1f) < 0.16f ? 0.55f : 0f;

                float ticks = Mathf.Repeat((ang + Mathf.PI) / (Mathf.PI * 2f) * 24f, 1f);
                if (ticks < 0.10f && r > 0.25f) value = Mathf.Max(value, 0.40f);

                // Dead wedges. A perfectly even disc is a stencil; the gaps are what make it read
                // as a signal rather than as paint.
                float wedge = Mathf.Sin(ang * 3f + 0.8f) * Mathf.Sin(ang * 7f - 1.4f);
                if (wedge > 0.55f) value *= 0.25f;

                return Mathf.Clamp01(Mathf.Max(value, 0.13f));
            }

            /// <summary>
            /// Node <paramref name="segment"/> of one arc's jagged polyline, as an offset from the
            /// burst centre in units of the arc's current reach. Node 0 is always the origin and
            /// the radius grows by one step per node, so an arc can kink as much as it likes and
            /// can still never fold back on itself.
            /// </summary>
            public static Vector3 ArcNode(int seed, int arc, int segment)
            {
                float yaw = (arc / (float)ArcCount) * Mathf.PI * 2f
                          + (Hash01(seed * 31 + arc) - 0.5f) * 0.55f;
                float rise = 0f;
                float radius = 0f;

                for (int s = 1; s <= segment; s++)
                {
                    int h = seed * 8191 + arc * 313 + s * 29;
                    yaw += (Hash01(h) - 0.5f) * 1.25f;
                    rise += (Hash01(h + 7) - 0.35f) * 0.26f;
                    radius += 1f / ArcSegments;
                }

                return new Vector3(Mathf.Cos(yaw) * radius, rise, Mathf.Sin(yaw) * radius);
            }

            /// <summary>
            /// One end of one crackle segment beside a failing implant, as a unit-ish offset.
            /// Re-rolled eighteen times a second so the sparks never hold still, which is the
            /// difference between crackle and a little star stuck to someone's head.
            /// </summary>
            public static Vector3 CrackleNode(int id, int arc, int node, float time)
            {
                int tick = Mathf.FloorToInt(time * 18f);
                int h = id * 7919 + arc * 131 + tick * 3;
                float yaw = Hash01(h) * Mathf.PI * 2f;
                if (node != 0) yaw += (Hash01(h + 11) - 0.5f) * 1.2f;
                float pitch = (Hash01(h + 5) - 0.5f) * 1.3f;
                float radius = node == 0 ? 0.30f : 1f;
                return new Vector3(Mathf.Cos(yaw) * Mathf.Cos(pitch), Mathf.Sin(pitch),
                                   Mathf.Sin(yaw) * Mathf.Cos(pitch)) * radius;
            }

            /// <summary>Fast out, decelerating. The shape of anything that is losing to the air.</summary>
            public static float Out3(float k)
            {
                float t = 1f - Mathf.Clamp01(k);
                return 1f - t * t * t;
            }

            /// <summary>
            /// Deterministic 0..1 from an integer. Not UnityEngine.Random: an effect that draws a
            /// different picture on a re-run is an effect nobody can review from a screenshot, and
            /// two screenshots of the same burst have to be comparable.
            /// </summary>
            public static float Hash01(int value)
            {
                uint x = (uint)value * 2654435761u;
                x ^= x >> 15; x *= 2246822519u;
                x ^= x >> 13; x *= 3266489917u;
                x ^= x >> 16;
                return (x >> 8) * (1f / 16777216f);
            }
        }
    }

    /// <summary>Which weapon fired a lance. Only the colour differs; the shape is one idea.</summary>
    public enum SignalBeam
    {
        /// <summary>The hero's emitter. Cyan-white, the player's own colour.</summary>
        Emitter,

        /// <summary>An emplacement. A deeper blue, so the player can read whose shot that was.</summary>
        Turret,
    }
}
