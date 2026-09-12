#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// How a machine walks, as arithmetic. No clips, no Animator, no rig.
    ///
    /// The brief asked for the machines to "feel different to fight without becoming a new
    /// archetype ... posture, gait and proportion rather than new mechanics", and gait is the half
    /// of that which is not geometry. Three rules, and each one is a thing a person does NOT do:
    ///
    ///   THE ARMS DO NOT MOVE. A walking man's arms counter-swing his legs. Nothing here swings but
    ///   the legs, which at distance is most of the read.
    ///   THE KNEES DO NOT BEND. The whole leg pivots at the hip, so the foot swings through an arc
    ///   instead of lifting and placing.
    ///   THE STRIDE SNAPS. <see cref="Snap"/> sharpens the sine so the leg spends its time at the
    ///   extremes and crosses the middle quickly -- servo travel, not a pendulum.
    ///
    /// The maths is static and pure so it can be tested without a scene, which matters more here
    /// than usual: a gait that silently does nothing looks exactly like a working import nobody
    /// told to move, and CLAUDE.md is a list of five times that happened.
    /// </summary>
    public static class Gait
    {
        /// <summary>Degrees a leg swings fore and aft at full speed.</summary>
        public const float LegSwingDegrees = 26f;

        /// <summary>How far the chassis drops as the legs open. Small -- a machine rides level.</summary>
        public const float BobMetres = 0.045f;

        /// <summary>Weight shift, in degrees of roll. The lurch.</summary>
        public const float RollDegrees = 3.2f;

        /// <summary>Strides per second per metre/second of ground speed.</summary>
        public const float StridesPerMetre = 0.42f;

        /// <summary>Ground speed at which the gait is running flat out.</summary>
        public const float FullSpeed = 3.0f;

        /// <summary>
        /// Sharpens a sine toward its extremes. 1 would be a pure sine (a pendulum); below 1 the
        /// curve flattens at the top and steepens through the middle, which is what a servo driving
        /// to a stop looks like.
        /// </summary>
        public static float Snap(float s)
        {
            float a = Mathf.Abs(s);
            return Mathf.Sign(s) * Mathf.Pow(a, 0.62f);
        }

        /// <summary>The pose at a stride phase, scaled by how fast the body is actually moving.</summary>
        public static void Pose(float phase, float speed,
                                out float legLeftDegrees, out float legRightDegrees,
                                out float bob, out float rollDegrees)
        {
            float amount = Mathf.Clamp01(speed / FullSpeed);
            float s = Mathf.Sin(phase);
            float swing = Snap(s) * LegSwingDegrees * amount;

            legLeftDegrees = swing;
            legRightDegrees = -swing;

            // Lowest when the legs are furthest apart, level when they pass. Absolute value, so it
            // happens twice per stride, which is correct and is also why it reads as a machine
            // rather than a limp.
            bob = -Mathf.Abs(s) * BobMetres * amount;
            rollDegrees = s * RollDegrees * amount;
        }

        /// <summary>How far the stride phase advances this frame.</summary>
        public static float AdvancePhase(float phase, float speed, float dt)
            => phase + speed * StridesPerMetre * Mathf.PI * 2f * dt;
    }

    /// <summary>
    /// Drives one machine body: finds its parts by name, measures its own ground speed, and poses
    /// it. Deliberately self-sufficient -- it reads its own transform rather than being told, so a
    /// machine works wherever it is parented and nothing has to remember to tick it.
    /// </summary>
    public sealed class MachineGait : MonoBehaviour
    {
        private Transform? _chassis, _legL, _legR;

        /// <summary>
        /// Set when this machine is a real model with its own clips rather than a box rig.
        ///
        /// The two paths are exclusive on purpose. A model carries an authored walk; swinging its
        /// root around procedurally on top of that fights the animation for the same transform and
        /// produces a robot that skates while its legs do something unrelated.
        /// </summary>
        private Animation? _anim;
        private string _clip = string.Empty;
        private bool _bound;

        /// <summary>Above this, in metres per second, the walk becomes the run.</summary>
        private const float RunAbove = 3.1f;

        /// <summary>Metres per second the walk clip was authored for; playback scales off it.</summary>
        private const float WalkReference = 1.5f;

        private Vector3 _chassisRest;
        private float _phase;
        private Vector3 _last;
        private float _speed;
        private float _fallen;      // 0 upright, 1 flat on the ground
        private bool _dying;
        private float _flinch;

        /// <summary>How long the machine takes to go over. Matched to the people's death clip.</summary>
        public float FallSeconds { get; set; } = 0.85f;

        private void Awake() => Bind();

        private void Bind()
        {
            // A flag, not a null check on _chassis: a model machine has no "Chassis" child, so the
            // old guard re-ran Find on three names every frame forever and never succeeded.
            if (_bound) return;
            _bound = true;

            _anim = GetComponentInChildren<Animation>(true);
            _chassis = transform.Find("Chassis");
            _legL = transform.Find("LegL");
            _legR = transform.Find("LegR");
            if (_chassis != null) _chassisRest = _chassis.localPosition;
            _last = transform.position;
            // Desynchronise: fifty machines all starting at phase zero march in step, which is the
            // single most obvious tell that a crowd is fake and is a note the owner has already
            // given once about the people.
            _phase = (GetInstanceID() & 1023) * (Mathf.PI * 2f / 1024f);
        }

        /// <summary>A new occupant. Re-roll the stride and stand the body back up.</summary>
        public void Reset(float phase)
        {
            Bind();
            _phase = phase;
            _dying = false;
            _fallen = 0f;
            _flinch = 0f;
            _speed = 0f;
            _last = transform.position;
            transform.localRotation = Quaternion.identity;
            _clip = string.Empty;   // so the next occupant re-crossfades instead of holding a death pose
        }

        /// <summary>Go over. The machine equivalent of the death clip: it topples, it does not vanish.</summary>
        public void Die()
        {
            Bind();
            _dying = true;
        }

        /// <summary>Took a hit and lived. A short stagger, so damage is visible on a rigid body.</summary>
        public void Flinch() => _flinch = 0.18f;

        private void LateUpdate()
        {
            Bind();
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            var here = transform.position;
            float measured = (new Vector3(here.x, 0f, here.z) - new Vector3(_last.x, 0f, _last.z)).magnitude / dt;
            _last = here;
            // Smoothed, because the sim steps on a fixed tick and the view does not: the raw
            // per-frame delta alternates between a full step and zero, which makes the legs judder.
            _speed = Mathf.Lerp(_speed, measured, 1f - Mathf.Exp(-9f * dt));

            if (_anim != null) { DriveClips(); return; }

            if (_dying)
            {
                _fallen = Mathf.Min(1f, _fallen + dt / Mathf.Max(0.05f, FallSeconds));
                // Face-first, pivoting about the feet, easing out as it hits.
                float t = 1f - (1f - _fallen) * (1f - _fallen);
                transform.localRotation = Quaternion.Euler(t * 88f, 0f, 0f);
                return;
            }

            if (_flinch > 0f) _flinch = Mathf.Max(0f, _flinch - dt);

            _phase = Gait.AdvancePhase(_phase, _speed, dt);
            Gait.Pose(_phase, _speed, out float l, out float r, out float bob, out float roll);

            if (_legL != null) _legL.localRotation = Quaternion.Euler(l, 0f, 0f);
            if (_legR != null) _legR.localRotation = Quaternion.Euler(r, 0f, 0f);
            if (_chassis != null)
            {
                // A flinch is a rock backwards on top of whatever the gait is doing. It reads on a
                // rigid body, which a skinned flinch clip would not have to work for.
                float kick = _flinch > 0f ? -_flinch * 60f : 0f;
                _chassis.localPosition = _chassisRest + new Vector3(0f, bob, 0f);
                _chassis.localRotation = Quaternion.Euler(kick, 0f, roll);
            }
        }

        // ------------------------------------------------------------------ wiring
        //
        // The crowd raises four events about a slot and the bootstrap answers them by driving a
        // legacy Animation component. A machine has no Animation component, so every one of those
        // handlers already no-ops safely on a machine slot -- which is why a machine would walk
        // fine and die by vanishing. Intercept puts the machines in FRONT of those handlers and
        // falls through to them for everyone else, so the whole thing is one line at the call site
        // and the existing handlers are untouched.

        /// <summary>
        /// Wraps a crowd's four slot events so machine slots are driven by their gait and everyone
        /// else reaches the handler that was already there.
        ///
        /// CALL THIS AFTER the handlers are assigned. It decorates whatever is on the crowd at the
        /// moment it runs; assigning a handler afterwards would replace the wrapper and the
        /// machines would quietly stop dying.
        /// </summary>
        public static void Intercept(CivilianCrowd crowd, IReadOnlyList<Transform> slots)
        {
            if (crowd == null) return;

            var gaits = new MachineGait?[slots.Count];
            for (int i = 0; i < slots.Count; i++)
                gaits[i] = slots[i] != null ? slots[i].GetComponentInChildren<MachineGait>(true) : null;

            var moved = crowd.OnSlotMoved;
            var reassigned = crowd.OnSlotReassigned;
            var died = crowd.OnSlotDied;
            var hurt = crowd.OnSlotHurt;

            // Movement needs no machine-side work at all: the gait measures the slot's own travel,
            // so it is already correct. Left wired straight through.
            crowd.OnSlotMoved = moved;

            crowd.OnSlotReassigned = slot =>
            {
                var g = At(gaits, slot);
                if (g != null) g.Reset(Random.value * Mathf.PI * 2f);
                reassigned?.Invoke(slot);
            };
            crowd.OnSlotDied = slot =>
            {
                var g = At(gaits, slot);
                if (g != null) g.Die();
                died?.Invoke(slot);
            };
            crowd.OnSlotHurt = slot =>
            {
                var g = At(gaits, slot);
                if (g != null) g.Flinch();
                hurt?.Invoke(slot);
            };
        }

        private static MachineGait? At(MachineGait?[] gaits, int slot)
            => slot >= 0 && slot < gaits.Length ? gaits[slot] : null;

        /// <summary>
        /// Picks the clip the machine's own speed asks for.
        ///
        /// Speed is MEASURED off the transform rather than asked of the sim, exactly as the box rig
        /// does it, so this needs no wiring back to the crowd and cannot disagree with where the
        /// body actually went. Playback rate scales with speed so the feet do not skate; clamped,
        /// because a machine shoved to twice its speed should look hurried, not comic.
        /// </summary>
        private void DriveClips()
        {
            if (_anim == null) return;

            if (_dying)
            {
                Play("machine_death", 0.12f);
                return;
            }

            string want = _speed < 0.2f ? "machine_idle"
                        : _speed < RunAbove ? "machine_walk"
                        : "machine_run";
            Play(want, 0.18f);

            var state = _anim[want];
            if (state != null && want != "machine_idle")
                state.speed = Mathf.Clamp(_speed / WalkReference, 0.55f, 1.9f);
        }

        private void Play(string clip, float blend)
        {
            if (_anim == null || _clip == clip) return;
            if (_anim[clip] == null) return;   // pack built without this one; keep what is playing
            _anim.CrossFade(clip, blend);
            _clip = clip;
        }

    }
}
