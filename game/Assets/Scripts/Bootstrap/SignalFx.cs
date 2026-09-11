#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// The visual vocabulary of the SIGNAL weapons: the EMP burst, the microwave pulse that replaces
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
        /// How long one pulse lives. Longer than the 0.06s ballistic tracer it replaces, on
        /// purpose: a bullet is allowed to be a line that was there and is gone, but a wave has to
        /// be SEEN propagating or it is just a line again.
        ///
        /// 0.22 rather than the lance's 0.16. A train of wavefronts needs enough frames for the
        /// eye to catch the SECOND one arriving where the first was, which is the entire
        /// difference between a wave and a bar; at 0.16 a sixty-hertz display got nine frames and
        /// the packet was over before the read landed. It is still short enough that a fast weapon
        /// does not smear into a solid rope.
        /// </summary>
        public const float BeamLife = 0.22f;

        private const int MaxInstancesPerDraw = 1023;

        /// <summary>The effects system the game is currently running, if any.</summary>
        public static SignalFx? Current { get; private set; }

        /// <summary>Ground height. The play area is one flat plane, as it is for the decals.</summary>
        public float GroundY { get; set; }

        public bool Enabled { get; set; } = true;

        // ------------------------------------------------------------------ palette

        /// <summary>
        /// The whole palette, in one place, in THREE families that must never be mistaken for one
        /// another. Colour is the coarsest thing a player reads in a firefight and it is the first
        /// thing that gets muddled when effects are tuned one at a time, so the families are named
        /// here and asserted in the tests rather than left to whoever edits next.
        ///
        ///   COLD (the EMP)      white core, electric blue shell, blue ground stamp. A cargo
        ///                       drone's hard electromagnetic pulse. The owner called this one
        ///                       fantastic on 2026-09-11 and it is deliberately UNCHANGED.
        ///   WARM (the weapons)  amber-gold. The hero's emitter, the turrets', and the Brush Hog's
        ///                       broadcast field. A carrier cooking a chip, not a bolt of anything.
        ///   ORANGE (the chip)   one small deep-orange light at a temple, and the cream flare it
        ///                       throws at the instant it is hit.
        ///
        /// THE WEAPONS AND THE CHIP ARE BOTH WARM AND THAT IS THE TRAP. They are separated on the
        /// GREEN channel, which is what moves a warm colour between gold and orange: every weapon
        /// colour sits at g >= <see cref="WeaponGreenFloor"/> and the implant at
        /// g &lt;= <see cref="ImplantGreenCeiling"/>. Keep any new colour on the right side of its
        /// family's line or "am I shooting or is that one dying" stops being answerable at a
        /// glance. (Hue is only the first of five separators -- see <see cref="Curves.Pulse"/>.)
        /// </summary>
        public static readonly Color Core = new Color(1.00f, 1.00f, 1.00f);
        public static readonly Color Shell = new Color(0.33f, 0.80f, 1.00f);
        public static readonly Color Echo = new Color(0.20f, 0.52f, 0.95f);
        public static readonly Color Ring = new Color(0.62f, 0.94f, 1.00f);
        public static readonly Color Arc = new Color(0.86f, 0.98f, 1.00f);
        public static readonly Color Stamp = new Color(0.30f, 0.68f, 0.95f);

        // ---- the weapons: amber, and on the YELLOW side of amber -------------------------------

        /// <summary>The body of a wavefront. The colour the player will say the gun "is".</summary>
        public static readonly Color Pulse = new Color(1.00f, 0.84f, 0.36f);

        /// <summary>
        /// The leading front and the hit flash. Nearly white, because the front of a pulse is the
        /// hot part -- and because a packet whose head is the same gold as its body is a stick.
        /// </summary>
        public static readonly Color PulseCore = new Color(1.00f, 0.97f, 0.82f);

        /// <summary>The air the carrier is passing through. Only ever drawn at low alpha.</summary>
        public static readonly Color PulseHaze = new Color(1.00f, 0.78f, 0.30f);

        /// <summary>The hero's emitter: the brighter of the two, because it is his.</summary>
        public static readonly Color Emitter = new Color(1.00f, 0.88f, 0.44f);

        /// <summary>An emplacement's. Deeper, so the player can tell his own fire from his guns'.</summary>
        public static readonly Color TurretBeam = new Color(1.00f, 0.80f, 0.32f);

        /// <summary>The Brush Hog's broadcast field on the ground.</summary>
        public static readonly Color Jammer = new Color(0.98f, 0.74f, 0.26f);

        /// <summary>No weapon colour may go below this on green, or it starts reading as an implant.</summary>
        public const float WeaponGreenFloor = 0.72f;

        /// <summary>And the implant may not go above this, or it starts reading as a shot.</summary>
        public const float ImplantGreenCeiling = 0.58f;

        // ---- the chip: deep orange, and only ever the size of a thumbnail ----------------------

        /// <summary>
        /// The implant's own colour. A DEVICE, not fire.
        ///
        /// Pushed from 0.66 green to 0.55 on 2026-09-11 when the weapons went amber. At 0.66 it sat
        /// eight hundredths off the Brush Hog's field and the two warm things in the game were the
        /// same warm thing; at 0.55 the chip is plainly orange beside a plainly gold shot.
        /// </summary>
        public static readonly Color Implant = new Color(1.00f, 0.55f, 0.09f);

        /// <summary>
        /// What the implant flares to at the moment the decrypt lands. Close to
        /// <see cref="PulseCore"/> on purpose: this flare IS the pulse arriving, and the handoff
        /// from the shot to the tell should look like one event rather than two.
        /// </summary>
        public static readonly Color ImplantFlare = new Color(1.00f, 0.93f, 0.72f);

        /// <summary>The EMP's colours. Cold, and staying cold.</summary>
        public static IReadOnlyList<Color> EmpPalette => new[] { Core, Shell, Echo, Ring, Arc, Stamp };

        /// <summary>Everything a weapon puts in the air.</summary>
        public static IReadOnlyList<Color> WeaponPalette => new[]
        {
            Pulse, PulseCore, PulseHaze, Emitter, TurretBeam, Jammer,
        };

        /// <summary>Every colour in the file, for the tests that police the family lines.</summary>
        public static IReadOnlyList<Color> Palette => new[]
        {
            Core, Shell, Echo, Ring, Arc, Stamp,
            Pulse, PulseCore, PulseHaze, Emitter, TurretBeam, Jammer,
            Implant, ImplantFlare,
        };

        // ------------------------------------------------------------------ live effects

        private struct Emp { public Vector3 Centre; public float Radius; public float Age; public int Seed; }
        private struct Beam
        {
            public Vector3 A, B;
            public float Age;
            public Color Ink;
            public bool Landed;

            /// <summary>Which emitter fired it. -1 is an emplacement, which has its own profile.</summary>
            public int Tier;

            /// <summary>Fixes this shot's untidiness. Derived from the shot, never from a counter.</summary>
            public int Seed;
        }

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
        private readonly List<Beam> _heldBeams = new List<Beam>(8);

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
        private readonly Batch _rings = new Batch(1024);
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

        /// <summary>Live bursts and pulses in the air. Diagnostics and tests.</summary>
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
        /// a shot that hit a wall gets the pulse but not the terminal bloom, because nothing in
        /// the wall was decrypted.
        /// </summary>
        /// <param name="tier">
        /// Which rung of the hero's emitter ladder fired it, 0..<see cref="Curves.MaxTier"/>
        /// (Field Jammer, Decryptor, Worm Lance, Cascade Emitter). It selects a
        /// <see cref="Curves.PulseProfile"/>: the shot's wavelength, how far it spreads, how
        /// untidy it is, whether it sheds lobes and what its arrival does. IGNORED for
        /// <see cref="SignalBeam.Turret"/>, which always uses <see cref="Curves.TurretProfile"/> --
        /// an emplacement's tier is its own ladder and reads off its hardware, not off the air.
        ///
        /// The default is 0 on purpose: the hero starts the match holding a Field Jammer, so a
        /// call site that has not been told about tiers yet is still drawing the right weapon.
        /// </param>
        public void AddBeam(Vector3 from, Vector3 to, SignalBeam kind, bool landed, int tier = 0)
        {
            _beams.Add(new Beam
            {
                A = from,
                B = to,
                Age = 0f,
                Ink = kind == SignalBeam.Turret ? TurretBeam : Emitter,
                Landed = landed,
                Tier = kind == SignalBeam.Turret ? -1 : tier,
                Seed = BeamSeed(from, to),
            });
        }

        /// <summary>
        /// Stages ONE pulse at an explicit age, for this frame only, without retaining it. The
        /// <see cref="MarkEmp"/> of the weapon side, and it exists for exactly the same reason:
        /// a pulse lives <see cref="BeamLife"/> = 0.22 seconds, so photographing four tiers at the
        /// same phase by choosing a shutter delay is guesswork, and "how does this tier differ"
        /// is a question about ONE phase seen four times. With this the phase is a parameter.
        /// </summary>
        public void MarkBeam(Vector3 from, Vector3 to, SignalBeam kind, bool landed, int tier, float age01)
        {
            _heldBeams.Add(new Beam
            {
                A = from,
                B = to,
                Age = Mathf.Clamp01(age01) * BeamLife,
                Ink = kind == SignalBeam.Turret ? TurretBeam : Emitter,
                Landed = landed,
                Tier = kind == SignalBeam.Turret ? -1 : tier,
                Seed = BeamSeed(from, to),
            });
        }

        /// <summary>
        /// A shot's own seed, fixing which fronts of an untidy weapon are fat and which are thin.
        /// Derived from where it was fired and where it landed, like <see cref="AddEmp"/>'s, so a
        /// replay of the same match draws the same picture and a held frame is reproducible.
        /// </summary>
        private static int BeamSeed(Vector3 from, Vector3 to)
            => Mathf.RoundToInt(from.x * 37.1f) * 131 + Mathf.RoundToInt(from.z * 53.7f) * 17
               + Mathf.RoundToInt(to.x * 29.3f) * 7 + Mathf.RoundToInt(to.z * 41.9f);

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
            _heldBeams.Clear();
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
            _heldBeams.Clear();
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
                StageBeam(b);
            }

            for (int i = 0; i < _heldBeams.Count; i++) StageBeam(_heldBeams[i]);
        }

        /// <summary>
        /// One pulse, at the age it is already carrying. Split out from the ageing loop so that
        /// <see cref="MarkBeam"/> can stage a frozen one through exactly the same code -- a review
        /// shot of a weapon drawn by a second code path is a review of the second code path.
        /// </summary>
        private void StageBeam(Beam b)
        {
            var p = Curves.ProfileFor(b.Tier);

            Vector3 d = b.B - b.A;
            float len = d.magnitude;
            if (len < 1e-3f) return;
            Vector3 dir = d / len;

            var f = Curves.Pulse(b.Age / BeamLife);
            if (f.Alpha <= 0.002f && f.BloomAlpha <= 0.002f) return;

            float frontD = len * f.FrontT;
            float backD = len * f.BackT;

            // A rotation that points the RING MESH'S NORMAL down the shot. The ring is
            // authored in XZ with a +Y normal, so LookRotation puts +Z on the shot and the
            // extra 90 about X brings +Y round onto it. Every wavefront is therefore a disc
            // standing ACROSS the line of fire, which is the whole read: the player is looking
            // at fronts of a wave, not at a bar someone drew between two points.
            //
            // THIS IS THE PART NO TIER MAY CHANGE. The ladder moves wavelength, spread,
            // untidiness and what the arrival does; it never moves the axis, the lifetime or the
            // hue, because those are what say the weapon is still the same weapon. An upgrade
            // that changed those would read as a different gun in a different pair of hands.
            var across = Quaternion.LookRotation(dir, Vector3.up) * Quaternion.Euler(90f, 0f, 0f);
            Vector3 side = Vector3.Cross(dir, Vector3.up);
            side = side.sqrMagnitude < 1e-5f ? Vector3.right : side.normalized;

            // ---- the air the carrier is passing through ---------------------------------
            // One stretched sphere covering the drawn span, at low alpha, with the rim
            // QUANTISED into four bands. On a smooth ellipsoid that is what produces the
            // shimmer: seen from the side the bands run along the axis like a heat column,
            // and seen from behind the shooter -- the chase camera's normal view, so the
            // common case -- they are concentric rings receding toward the target. It is the
            // heat haze and it costs one instance, because the banding is the shader's.
            //
            // The column is how a tier says how TIDY it is. A Field Jammer spills most of what
            // it makes into the air around the shot and a Decryptor almost none, which is the
            // difference between improvised and purpose-built stated in one number.
            if (f.Alpha > 0.002f && frontD > backD + 0.05f)
            {
                float mid = (frontD + backD) * 0.5f;
                float span = frontD - backD;
                float fat = Curves.PulseRadius(p, len <= 0f ? 0f : mid / len) * 1.8f * p.HazeFat;
                _spheres.Add(Matrix4x4.TRS(b.A + dir * mid,
                                           Quaternion.LookRotation(dir, Vector3.up),
                                           new Vector3(fat, fat, span)),
                             PulseHaze, f.Alpha * 0.09f * p.HazeScale, Shape.Haze);
            }

            // ---- the wavefronts ---------------------------------------------------------
            // A finite TRAIN of discs behind the front, spaced in metres and travelling with
            // it. Finite on purpose: a train that reached all the way back to the muzzle
            // would be a continuous tube, which is the laser the owner rejected. The COUNT and
            // the SPACING are the tier's loudest voice -- five ragged fronts two metres apart
            // against fourteen at seventy centimetres is a difference readable from a still.
            int fronts = Curves.WavefrontCount(p, frontD - backD);
            for (int k = 0; k < fronts; k++)
            {
                if (!Curves.Wavefront(p, k, frontD, backD, len, f.Phase, b.Seed,
                                      out float dist, out float radius, out float amp)) continue;

                Vector3 at = b.A + dir * dist;
                float d2 = radius * 2f;

                // The front itself, hot; and a wider, fainter ghost half a beat outside it, so
                // the edge of each front is soft rather than a hairline. A hard edge on a
                // wavefront is the single thing that makes energy read as a cut.
                // The LEADING front is drawn harder than the rest. With the hard white bead
                // gone something still has to be the head of the packet, and one front at
                // nearly twice the brightness of the one behind it does that without putting a
                // projectile back on the end of the shot.
                float lead = k == 0 ? 0.62f : 0.38f;
                _rings.Add(Matrix4x4.TRS(at, across, new Vector3(d2, 1f, d2)),
                           Curves.FrontColour(b.Ink, amp, p), f.Alpha * amp * lead, Shape.Wave);
                _rings.Add(Matrix4x4.TRS(at, across, new Vector3(d2 * 1.34f, 1f, d2 * 1.34f)),
                           PulseHaze, f.Alpha * amp * 0.12f, Shape.Wave);

                // ---- lobes: the payload leaving the carrier ------------------------------
                // Small fronts of their own, off the axis, walking FURTHER off it the further
                // down the shot they are. This is the Worm Lance's whole tell and it is the
                // one behaviour in the ladder that is about the payload rather than about the
                // beam: something is coming off the carrier on its way and going looking. It
                // is drawn as the same disc at the same angle, so it reads as part of the same
                // shot rather than as a second weapon firing alongside the first.
                for (int l = 0; l < p.Lobes; l++)
                {
                    float u = len <= 0f ? 0f : dist / len;
                    Curves.Lobe(p, k, l, u, out float offset, out float spin, out float lobeAmp);
                    if (lobeAmp <= 0.004f) continue;
                    Vector3 o = Quaternion.AngleAxis(spin, dir) * side * (radius * offset);
                    float ld = d2 * 0.52f;
                    _rings.Add(Matrix4x4.TRS(at + o, across, new Vector3(ld, 1f, ld)),
                               Curves.FrontColour(b.Ink, amp, p), f.Alpha * amp * lobeAmp, Shape.Wave);
                }
            }

            // ---- the leading edge -------------------------------------------------------
            // SOFT, which is the note. The lance had a hard opaque white bead here and that
            // bead was most of why it read as a projectile; a pulse has no nose cone. This is
            // a glow with no occlusion at all, a little bigger than the front it sits on.
            if (f.Alpha > 0.002f && f.FrontT < 1f)
            {
                // ONE soft glow, and no bright core inside it. The first build of this put a
                // near-white sphere here and it photographed as a HEADLIGHT: a round hot thing
                // with a trail behind it is a projectile whatever the trail is made of. The
                // leading wavefront above is the head now, and this is only the air around it.
                float r = Curves.PulseRadius(p, f.FrontT);
                Vector3 head = b.A + dir * frontD;
                _spheres.Add(Matrix4x4.TRS(head, Quaternion.identity, Vector3.one * (r * 2.4f * p.HeadGlow)),
                             PulseHaze, f.Alpha * 0.11f, Shape.Glow);
            }

            // ---- arrival -----------------------------------------------------------------
            // What a landed packet looks like: the carrier collapsing onto one chip. Gold,
            // not blue -- the blue burst belongs to the drone's EMP and nothing else.
            //
            // The tier is read here too, and it is the read that matters most, because the
            // arrival is the frame the player is actually looking at: a Field Jammer barely
            // marks the chip, a Cascade Emitter sets off a chain back down its own line.
            if (b.Landed && f.BloomAlpha > 0.002f)
            {
                for (int s = 0; s < p.ChainStages; s++)
                {
                    Curves.Chain(p, s, f.BloomScale, f.BloomAlpha, len,
                                 out float scale, out float alpha, out float back);
                    if (alpha <= 0.002f || scale <= 0.001f) continue;
                    Vector3 at = b.B - dir * back;
                    _spheres.Add(Matrix4x4.TRS(at, Quaternion.identity, Vector3.one * scale),
                                 Pulse, alpha, Shape.Shell);
                    _spheres.Add(Matrix4x4.TRS(at, Quaternion.identity, Vector3.one * (scale * 0.42f)),
                                 PulseCore, alpha * 0.85f, Shape.Glow);
                }

                // Filaments: the worm looking for the next chip. Short, gold, and thrown from
                // the body it just landed on. They are deliberately NOT the EMP's arcs -- four
                // of them, half a metre, and warm, against eleven cold ones reaching twice a
                // blast radius -- because the one thing this must never be mistaken for is the
                // drone's pulse.
                for (int i = 0; i < p.Filaments; i++)
                {
                    Curves.Filament(b.Seed, i, p, f.BloomAlpha, dir, side,
                                    out Vector3 away, out float reach, out float alpha);
                    if (alpha <= 0.002f) continue;
                    AddSegment(_bars, b.B, b.B + away * reach, 0.05f, PulseCore, alpha, Shape.Solid);
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
        private enum Shape { Solid, Shell, WideShell, Glow, Haze, Wave, Flat }

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
            // The pulse's heat column. Four bands rather than two, because here the banding is the
            // POINT -- on a smooth stretched sphere a quantised rim is a set of concentric
            // wavefronts drawn for free, and two steps is not enough of them to read as a shimmer.
            // Low rim power so the bands reach well in from the silhouette instead of hugging it.
            Shape.Haze => new Vector4(1.35f, 0.62f, 4f, 0f),
            // A wavefront. Flat like a ring, but it barely HIDES anything: twenty fronts stacked
            // along the view direction at the ring's usual 0.85 occlusion paint out the world
            // behind the shot, and a weapon that erases the thing it is aimed at is unusable. Low
            // occlusion is what keeps a packet reading as energy in front of a street rather than
            // as a hole cut in one.
            Shape.Wave => new Vector4(1f, 0f, 1f, 0.18f),
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

            /// <summary>
            /// Metres between one wavefront and the next. In METRES, never in fractions of the
            /// shot: spacing by fraction would make a long shot's wavefronts far apart and a short
            /// one's crowded, so the same weapon would appear to change frequency with range.
            /// A carrier has one wavelength and the player should be able to see that it does.
            /// </summary>
            public const float WaveSpacing = 1.35f;

            /// <summary>
            /// How many wavefronts a packet carries, at most.
            ///
            /// This is a DESIGN cap before it is a cost cap. A train that ran all the way back to
            /// the muzzle would be a continuous tube of light, and a continuous tube of light is
            /// the laser blast the owner rejected. Fourteen fronts is about nineteen metres of
            /// packet: enough that a shot across the lane still fills the frame, short enough that
            /// even the longest shot has a visibly empty end behind it, which is what says the
            /// energy is going that way.
            /// </summary>
            public const int MaxWavefronts = 14;

            /// <summary>Radius of the wavefront as it leaves the horn, and once it has spread.</summary>
            public const float WaveNearRadius = 0.13f;
            public const float WaveFarRadius = 0.46f;

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

            // Pulse stages.
            private const float TravelUntil = 0.46f;
            private const float TailFrom = 0.30f;

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

            /// <summary>One pulse at one instant, in fractions along its own length.</summary>
            public struct PulseFrame
            {
                /// <summary>Where the leading front has reached, 0 at the horn, 1 at the target.</summary>
                public float FrontT;

                /// <summary>Where the packet's trailing end is. It starts late and catches up.</summary>
                public float BackT;

                public float Alpha;

                /// <summary>Drives the shimmer. Advances with age, so the train breathes.</summary>
                public float Phase;

                /// <summary>The carrier collapsing onto the chip it found.</summary>
                public float BloomScale, BloomAlpha;
            }

            /// <summary>
            /// A microwave pulse propagating, rather than a bolt being fired.
            ///
            /// THE OWNER READ THE PREVIOUS VERSION AS A LASER AND HE WAS RIGHT TO. It was a
            /// straight bar with a hard bright bead on the end, which is the drawing of a beam
            /// weapon whatever colour it is painted; the colour was only the half of it he could
            /// name. What replaces it is a train of concentric fronts travelling out along the
            /// axis inside a banded column of haze, with a soft leading edge and an empty tail.
            ///
            /// FIVE THINGS SEPARATE THIS FROM THE DYING-CHIP TELL, which is the other warm thing
            /// on the screen and the one it must never be confused with:
            ///
            ///   1. HUE      gold (g >= <see cref="WeaponGreenFloor"/>) against the implant's
            ///               orange (g &lt;= <see cref="ImplantGreenCeiling"/>).
            ///   2. LIFETIME <see cref="BeamLife"/> is 0.22s. The tell runs the sim's whole 2.5s
            ///               failure. Anything warm that is still there a second later is a body.
            ///   3. MOTION   these fronts EXPAND and travel away; the tell's rings CONTRACT onto a
            ///               head. Opposite directions, which the eye reads before it reads colour.
            ///   4. AXIS     these discs stand ACROSS the line of fire, at any angle the shot
            ///               happens to take. The tell's rings are always flat and horizontal.
            ///   5. PLACE    this lives in the air between a muzzle and a target and touches no
            ///               body; the tell is pinned to a temple at head height, and its rings
            ///               and crackle stayed COLD BLUE precisely so the pair cannot merge.
            /// </summary>
            public static PulseFrame Pulse(float age01)
            {
                float t = Mathf.Clamp01(age01);
                var f = default(PulseFrame);

                // Decelerating, not linear. A front that arrives at constant speed is a bullet;
                // one that leaves fast and settles in is energy spreading into air.
                f.FrontT = Out3(Mathf.Clamp01(t / TravelUntil));
                f.BackT = t <= TailFrom ? 0f : Out3(Mathf.Clamp01((t - TailFrom) / (1f - TailFrom)));
                if (f.BackT > f.FrontT) f.BackT = f.FrontT;

                f.Alpha = t < 0.5f ? 1f : Mathf.Clamp01((1f - t) / 0.5f);
                f.Phase = t * 9f;

                if (t >= TravelUntil)
                {
                    float k = (t - TravelUntil) / (1f - TravelUntil);
                    f.BloomScale = Mathf.Lerp(0.30f, 1.30f, Mathf.Sqrt(k));
                    f.BloomAlpha = Mathf.Pow(1f - k, 1.5f);
                }
                return f;
            }

            /// <summary>
            /// How wide the wavefront is at <paramref name="u"/> along the shot.
            ///
            /// It only ever GROWS. A beam that necks down onto its target is a beam being focused,
            /// which is the laser read again; a broadcast carrier diverges the whole way and the
            /// player should be able to see that the far end of his own shot is the loose end.
            /// Square-rooted so most of the spread happens early and the shape is a horn rather
            /// than a wedge.
            /// </summary>
            public static float PulseRadius(float u)
                => Mathf.Lerp(WaveNearRadius, WaveFarRadius, Mathf.Sqrt(Mathf.Clamp01(u)));

            /// <summary>How many wavefronts are alive in a packet whose drawn span is this long.</summary>
            public static int WavefrontCount(float span)
            {
                if (span <= 0f) return 0;
                return Mathf.Clamp(Mathf.CeilToInt(span / WaveSpacing) + 1, 0, MaxWavefronts);
            }

            /// <summary>
            /// Wavefront <paramref name="index"/> of a packet, counted back from the leading front.
            /// Returns false when it has fallen off the trailing end, which is how the train stays
            /// finite without anything having to remember which fronts it already retired.
            /// </summary>
            /// <param name="frontDist">Distance from the muzzle to the leading front, in metres.</param>
            /// <param name="backDist">Distance to the packet's trailing end, in metres.</param>
            /// <param name="length">The whole shot's length, for the spread envelope.</param>
            /// <param name="phase">From <see cref="PulseFrame.Phase"/>; drives the shimmer.</param>
            /// <param name="dist">Out: where this front is, in metres from the muzzle.</param>
            /// <param name="radius">Out: its radius in metres.</param>
            /// <param name="amp">Out: 0..1 brightness. 1 at the leading front, falling off behind.</param>
            public static bool Wavefront(int index, float frontDist, float backDist, float length,
                                         float phase, out float dist, out float radius, out float amp)
            {
                dist = 0f; radius = 0f; amp = 0f;
                if (index < 0 || index >= MaxWavefronts) return false;

                dist = frontDist - index * WaveSpacing;
                if (dist < backDist || dist < 0f) return false;

                radius = PulseRadius(length <= 0f ? 0f : dist / length);

                // Brightest at the front and dying back through the packet. Squared, so the fall
                // is steep near the head: that gradient is the arrow. Without it a train of equal
                // rings is a ladder and a ladder has no direction.
                float k = index / (float)MaxWavefronts;
                amp = (1f - k) * (1f - k) * (1f - k);

                // The shimmer. A slow travelling wave across the train, never down to nothing --
                // a front that blinks out entirely leaves a hole in the packet and the hole reads
                // as a gap in the geometry rather than as energy breathing.
                amp *= 0.74f + 0.26f * Mathf.Sin(index * 1.9f - phase * 2.2f);

                // And the front fattens a little as it flickers, which is the part that stops the
                // discs looking like a stack of identical washers threaded on a wire.
                radius *= 0.92f + 0.16f * Mathf.Sin(index * 1.1f - phase * 1.7f);
                return amp > 0.004f;
            }


            // ------------------------------------------------------------------ the ladder

            /// <summary>
            /// The top rung of the hero's emitter ladder. Mirrors <c>GunTiers.MaxTier</c>; the
            /// NAMES live there because they are gameplay, and the BEHAVIOUR lives here because it
            /// is drawing, and neither wants to know about the other.
            /// </summary>
            public const int MaxTier = 3;

            /// <summary>
            /// How one rung of the ladder puts its shot in the air.
            ///
            /// OWNER, 2026-09-12: "different gun upgrades should make my weapon skin different and
            /// it should also change the way my projectile goes". This struct is the second half of
            /// that; <c>HeroEmitter</c> is the first.
            ///
            /// THE ESCALATION IS CRUDE -> SOPHISTICATED, NOT SMALL -> BIG (ADR-008). These are
            /// signal weapons carrying malware, built by one veteran out of what a lake community
            /// has in its garages, so an upgrade is a better PAYLOAD and a better aerial, never a
            /// larger calibre. Concretely, across the four rungs:
            ///
            ///   wavelength  2.10m -> 0.70m. The carrier gets finer and the train gets denser,
            ///               which is the single loudest difference between two stills.
            ///   spread      wide and sloppy -> tight -> tight -> wide again, but wide the way a
            ///               broadcast array is wide rather than the way a leak is.
            ///   untidiness  <see cref="Jitter"/> falls off a cliff after the Field Jammer. The
            ///               first weapon is the ONLY one whose fronts are visibly uneven.
            ///   arrival     a weak smudge -> a clean collapse -> a collapse that throws
            ///               filaments looking for the next chip -> a chain back down the line.
            ///
            /// WHAT NO RUNG MAY CHANGE: the hue family, the 0.22s lifetime, the disc-across-the-
            /// axis geometry and the direction of travel. Those four are what say it is the same
            /// weapon, and three of them are also what holds the shot apart from the dying-chip
            /// tell (see <see cref="Pulse"/>). An upgrade must read as "his gun got better", never
            /// as "somebody else is shooting".
            /// </summary>
            public readonly struct PulseProfile
            {
                /// <summary>Metres between one wavefront and the next. The carrier's wavelength.</summary>
                public readonly float Spacing;

                /// <summary>The most fronts this weapon's packet ever carries.</summary>
                public readonly int MaxFronts;

                /// <summary>How fast the packet darkens behind its head, as a front count.</summary>
                public readonly float Falloff;

                /// <summary>Front radius at the horn, and once it has spread to the target.</summary>
                public readonly float NearRadius, FarRadius;

                /// <summary>How uneven the fronts are, 0 = machined, 0.5 = visibly improvised.</summary>
                public readonly float Jitter;

                /// <summary>How far toward <see cref="PulseCore"/> a bright front is pushed.</summary>
                public readonly float Heat;

                /// <summary>Spill into the surrounding air: alpha, and how fat the column is.</summary>
                public readonly float HazeScale, HazeFat;

                /// <summary>Size of the soft glow riding the leading front.</summary>
                public readonly float HeadGlow;

                /// <summary>What the arrival is worth, as a multiple of the shared bloom.</summary>
                public readonly float BloomScale;

                /// <summary>Satellite fronts shed off the axis, and how far out they walk.</summary>
                public readonly int Lobes;
                public readonly float LobeSpread;

                /// <summary>Blooms in the arrival, staggered back down the shot's own line.</summary>
                public readonly int ChainStages;

                /// <summary>Short filaments thrown from a landed packet, hunting.</summary>
                public readonly int Filaments;

                public PulseProfile(float spacing, int maxFronts, float falloff,
                                    float nearRadius, float farRadius, float jitter, float heat,
                                    float hazeScale, float hazeFat, float headGlow, float bloomScale,
                                    int lobes, float lobeSpread, int chainStages, int filaments)
                {
                    Spacing = spacing;
                    MaxFronts = maxFronts;
                    Falloff = falloff;
                    NearRadius = nearRadius;
                    FarRadius = farRadius;
                    Jitter = jitter;
                    Heat = heat;
                    HazeScale = hazeScale;
                    HazeFat = hazeFat;
                    HeadGlow = headGlow;
                    BloomScale = bloomScale;
                    Lobes = lobes;
                    LobeSpread = lobeSpread;
                    ChainStages = chainStages;
                    Filaments = filaments;
                }
            }

            /// <summary>
            /// What an EMPLACEMENT's shot looks like, and it is the file's original tuning to the
            /// last decimal -- <see cref="WaveSpacing"/>, <see cref="MaxWavefronts"/>,
            /// <see cref="WaveNearRadius"/>, <see cref="WaveFarRadius"/> and nothing else.
            ///
            /// It is a separate profile rather than a rung of the ladder because a turret's tier is
            /// its own ladder and it is read off its HARDWARE (aerials, horns, how many throats are
            /// still lit -- see TurretProps). Two objects escalating in the same channel would mean
            /// the player could not tell a tier-3 Sentry's shot from his own tier-3 emitter's, and
            /// "whose fire was that" is the one question a defence game must always answer.
            /// </summary>
            public static readonly PulseProfile TurretProfile = new PulseProfile(
                spacing: WaveSpacing, maxFronts: MaxWavefronts, falloff: MaxWavefronts,
                nearRadius: WaveNearRadius, farRadius: WaveFarRadius,
                jitter: 0f, heat: 0f, hazeScale: 1f, hazeFat: 1f, headGlow: 1f, bloomScale: 1f,
                lobes: 0, lobeSpread: 0f, chainStages: 1, filaments: 0);

            /// <summary>
            /// FIELD JAMMER. Conduit, a drill grip and a bent wire loop. It barely works and it
            /// should barely look like it works: five fat fronts two metres apart, uneven enough
            /// that no two are the same size, spraying loose by the far end and losing a lot of
            /// what it makes into the air. The arrival is a smudge.
            ///
            /// THE SPREAD CAME DOWN FROM 0.78 AFTER THE FIRST CAPTURE. At that width the crudest
            /// weapon in the game threw the BIGGEST thing on the screen -- wider fronts and a
            /// fatter haze cone than the Cascade Emitter -- which inverts the whole ladder in the
            /// one frame a player actually looks at. Loose and untidy has to stay smaller than
            /// broad and deliberate, or "improvised" reads as "powerful".
            /// </summary>
            public static readonly PulseProfile Tier0 = new PulseProfile(
                spacing: 2.10f, maxFronts: 5, falloff: 7f,
                nearRadius: 0.18f, farRadius: 0.52f,
                jitter: 0.42f, heat: 0f, hazeScale: 1.5f, hazeFat: 1.15f, headGlow: 1.25f,
                bloomScale: 0.70f, lobes: 0, lobeSpread: 0f, chainStages: 1, filaments: 0);

            /// <summary>
            /// DECRYPTOR. The first one he BUILT rather than bodged: a waveguide horn on a
            /// machined body. Ten even fronts at 1.3m in a column that barely diverges, almost no
            /// spill, and a clean tight collapse on the chip. Nothing clever -- it simply works,
            /// and the step up from the Jammer is meant to feel like competence, not power.
            /// </summary>
            public static readonly PulseProfile Tier1 = new PulseProfile(
                spacing: 1.30f, maxFronts: 10, falloff: 12f,
                nearRadius: 0.11f, farRadius: 0.30f,
                jitter: 0.05f, heat: 0.25f, hazeScale: 0.75f, hazeFat: 0.85f, headGlow: 0.90f,
                bloomScale: 1.05f, lobes: 0, lobeSpread: 0f, chainStages: 1, filaments: 0);

            /// <summary>
            /// WORM LANCE. The payload self-propagates, so the SHOT has to show something leaving
            /// it: two satellite fronts ride off the axis and walk further out the further down the
            /// shot they get, and the arrival throws short filaments off the body it landed on.
            /// The carrier itself is the tightest of the four -- a lance is a delivery system, and
            /// what spreads is what it is carrying, not the beam.
            /// </summary>
            public static readonly PulseProfile Tier2 = new PulseProfile(
                spacing: 0.95f, maxFronts: 14, falloff: 16f,
                nearRadius: 0.10f, farRadius: 0.34f,
                jitter: 0.12f, heat: 0.35f, hazeScale: 0.60f, hazeFat: 0.80f, headGlow: 0.85f,
                bloomScale: 1.15f, lobes: 2, lobeSpread: 2.2f, chainStages: 1, filaments: 5);

            /// <summary>
            /// CASCADE EMITTER. Named for the event (ADR-003) by the man who refused the chip, and
            /// it is the only rung allowed to be excessive: fourteen fronts at seventy centimetres,
            /// wide and hot, and an arrival that goes off three times, walking back up its own line
            /// toward the shooter. Still gold, still 0.22 seconds, still discs across the axis --
            /// the excess is all in density and in what happens when it lands.
            ///
            /// The spacing is 0.70 and not lower for a reason. Fronts closer together than they are
            /// THICK merge into a continuous tube, and a continuous tube of light is exactly the
            /// laser blast the owner rejected. This is as dense as the family can go.
            /// </summary>
            public static readonly PulseProfile Tier3 = new PulseProfile(
                spacing: 0.70f, maxFronts: 14, falloff: 19f,
                nearRadius: 0.18f, farRadius: 0.62f,
                jitter: 0.08f, heat: 0.60f, hazeScale: 1.50f, hazeFat: 1.15f, headGlow: 1.15f,
                bloomScale: 1.55f, lobes: 1, lobeSpread: 1.1f, chainStages: 3, filaments: 0);

            /// <summary>
            /// The profile for a beam's tier. Anything negative is an emplacement; anything above
            /// the ladder clamps to its top, so a loot table that one day hands out a fifth gun
            /// draws a Cascade Emitter rather than nothing at all.
            /// </summary>
            public static PulseProfile ProfileFor(int tier)
            {
                if (tier < 0) return TurretProfile;
                switch (tier)
                {
                    case 0: return Tier0;
                    case 1: return Tier1;
                    case 2: return Tier2;
                    default: return Tier3;
                }
            }

            /// <summary>As <see cref="PulseRadius(float)"/>, for one rung of the ladder.</summary>
            public static float PulseRadius(in PulseProfile p, float u)
                => Mathf.Lerp(p.NearRadius, p.FarRadius, Mathf.Sqrt(Mathf.Clamp01(u)));

            /// <summary>As <see cref="WavefrontCount(float)"/>, for one rung of the ladder.</summary>
            public static int WavefrontCount(in PulseProfile p, float span)
            {
                if (span <= 0f) return 0;
                return Mathf.Clamp(Mathf.CeilToInt(span / p.Spacing) + 1, 0, p.MaxFronts);
            }

            /// <summary>
            /// The colour of a front at this brightness on this rung.
            ///
            /// ONLY EVER A LERP BETWEEN TWO COLOURS THAT ARE ALREADY IN THE WEAPON FAMILY, which
            /// is what makes the ladder safe. Every weapon colour sits at green >=
            /// <see cref="WeaponGreenFloor"/>, so any mix of two of them does too, and no tier can
            /// drift the shot toward the implant's orange however it is tuned. Tested.
            /// </summary>
            public static Color FrontColour(Color ink, float amp, in PulseProfile p)
                => Color.Lerp(ink, PulseCore, Mathf.Clamp01(amp * amp + p.Heat * amp));

            /// <summary>
            /// Where satellite front <paramref name="lobe"/> of wavefront <paramref name="index"/>
            /// sits, for a weapon that sheds its payload on the way.
            /// </summary>
            /// <param name="u">How far down the shot the parent front is, 0..1.</param>
            /// <param name="offset">Out: how far off the axis, in front radii.</param>
            /// <param name="spin">Out: which way round the axis, in degrees.</param>
            /// <param name="amp">Out: 0..1, relative to the parent front.</param>
            public static void Lobe(in PulseProfile p, int index, int lobe, float u,
                                    out float offset, out float spin, out float amp)
            {
                offset = 0f; spin = 0f; amp = 0f;
                if (p.Lobes <= 0 || lobe < 0 || lobe >= p.Lobes) return;

                // IT MUST GROW WITH u AND NOT WITH AGE. A lobe that widens with the clock is a
                // shockwave; one that widens with DISTANCE ALONG THE SHOT is something that left
                // the carrier and is still going, which is what a self-propagating payload is.
                offset = p.LobeSpread * (0.55f + 1.45f * Mathf.Clamp01(u));

                // Spread evenly round the axis, then walked on by the index so the lobes spiral
                // down the packet rather than lying in one plane -- one plane reads as a mistake
                // in the geometry, a spiral reads as motion.
                spin = lobe * (360f / Mathf.Max(p.Lobes, 1)) + index * 47f;

                // Fainter than the carrier and fading as it leaves. What is peeling off is small.
                amp = 0.42f * (1f - 0.45f * Mathf.Clamp01(u));
            }

            /// <summary>
            /// Bloom <paramref name="stage"/> of an arrival. Stage 0 is the hit itself; later
            /// stages are the cascade walking BACK down the shot's own line toward the shooter,
            /// each one later, smaller and fainter than the one before.
            /// </summary>
            /// <param name="back">Out: metres back along the shot from the impact point.</param>
            public static void Chain(in PulseProfile p, int stage, float bloomScale, float bloomAlpha,
                                     float length, out float scale, out float alpha, out float back)
            {
                scale = 0f; alpha = 0f; back = 0f;
                if (stage < 0 || stage >= p.ChainStages) return;

                scale = bloomScale * p.BloomScale * Mathf.Pow(0.70f, stage);

                // Later stages start later. The bloom alpha is already falling by the time stage
                // two lights, so the delay is applied by simply holding the stage dark until the
                // parent bloom has aged past it -- no second clock, and nothing to keep in sync.
                float begin = stage * 0.22f;
                float k = Mathf.InverseLerp(1f, 0f, bloomAlpha);
                if (k < begin) return;
                alpha = bloomAlpha * Mathf.Pow(0.72f, stage);

                // Spaced in metres but capped against the shot, so a point-blank cascade does not
                // put its last bloom behind the shooter's own head.
                back = Mathf.Min(stage * 1.6f, length * 0.30f);
            }

            /// <summary>
            /// Filament <paramref name="index"/> thrown from a landed packet: the worm looking for
            /// the next chip. Deterministic from the shot's own seed, so a held frame is
            /// reproducible and two shots at the same place scatter the same way.
            /// </summary>
            /// <param name="away">Out: unit direction, always off to the SIDE of the shot.</param>
            public static void Filament(int seed, int index, in PulseProfile p, float bloomAlpha,
                                        Vector3 dir, Vector3 side,
                                        out Vector3 away, out float reach, out float alpha)
            {
                away = side; reach = 0f; alpha = 0f;
                if (p.Filaments <= 0 || index < 0 || index >= p.Filaments) return;

                float a = Hash01(seed, index * 3 + 1) * 360f;
                float lift = Hash01(seed, index * 3 + 2) * 0.9f - 0.2f;
                var perp = Quaternion.AngleAxis(a, dir) * side;
                away = (perp + dir * lift).normalized;

                // SHORT. They are a tell, not a discharge: the EMP's arcs reach nearly twice a
                // blast radius and these reach half a metre, which is most of why the two cannot
                // be confused at a glance.
                reach = 0.38f + 0.34f * Hash01(seed, index * 3 + 3);
                alpha = bloomAlpha * 0.8f;
            }

            /// <summary>
            /// A stable 0..1 from two integers. Not <c>Random</c>: the same shot must draw the
            /// same picture every time it is replayed, and a held review frame must not crawl.
            /// </summary>
            public static float Hash01(int seed, int salt)
            {
                unchecked
                {
                    uint h = (uint)(seed * 73856093) ^ (uint)(salt * 19349663);
                    h ^= h >> 13;
                    h *= 0x85EBCA6Bu;
                    h ^= h >> 16;
                    return (h & 0xFFFFFF) / 16777215f;
                }
            }

            /// <summary>
            /// As <see cref="Wavefront(int,float,float,float,float,out float,out float,out float)"/>,
            /// for one rung of the ladder and one shot's own seed.
            /// </summary>
            public static bool Wavefront(in PulseProfile p, int index, float frontDist, float backDist,
                                         float length, float phase, int seed,
                                         out float dist, out float radius, out float amp)
            {
                dist = 0f; radius = 0f; amp = 0f;
                if (index < 0 || index >= p.MaxFronts) return false;

                dist = frontDist - index * p.Spacing;
                if (dist < backDist || dist < 0f) return false;

                radius = PulseRadius(p, length <= 0f ? 0f : dist / length);

                // Brightest at the front and dying back through the packet. Squared, so the fall
                // is steep near the head: that gradient is the arrow. Without it a train of equal
                // rings is a ladder and a ladder has no direction.
                float k = Mathf.Clamp01(index / Mathf.Max(p.Falloff, 1f));
                amp = (1f - k) * (1f - k) * (1f - k);

                // The shimmer. A slow travelling wave across the train, never down to nothing --
                // a front that blinks out entirely leaves a hole in the packet and the hole reads
                // as a gap in the geometry rather than as energy breathing.
                amp *= 0.74f + 0.26f * Mathf.Sin(index * 1.9f - phase * 2.2f);

                // And the front fattens a little as it flickers, which is the part that stops the
                // discs looking like a stack of identical washers threaded on a wire.
                radius *= 0.92f + 0.16f * Mathf.Sin(index * 1.1f - phase * 1.7f);

                // UNTIDINESS IS A PROPERTY OF THE WEAPON, NOT OF THE MOMENT. It comes off the
                // shot's seed and the front's index, so a Field Jammer's packet is lumpy in a way
                // that holds still while it travels -- which is what makes it read as a badly made
                // aerial rather than as noise somebody added to the effect. At Jitter 0 both terms
                // are exactly 1 and this function is the original, to the bit.
                if (p.Jitter > 0f)
                {
                    amp *= 1f - p.Jitter * 0.55f * Hash01(seed, index);
                    radius *= 1f + p.Jitter * (Hash01(seed, index + 97) - 0.5f) * 1.1f;
                }

                return amp > 0.004f;
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

    /// <summary>Which weapon fired a pulse. Only the colour differs; the shape is one idea.</summary>
    public enum SignalBeam
    {
        /// <summary>The hero's emitter. Cyan-white, the player's own colour.</summary>
        Emitter,

        /// <summary>An emplacement. A deeper blue, so the player can read whose shot that was.</summary>
        Turret,
    }
}
