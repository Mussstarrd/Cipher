#nullable enable
using System.Collections.Generic;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Drives a crowd body that animates through an ANIMATOR rather than a legacy clip.
    ///
    /// This exists because the bought characters reopened a bug the machines had already taught us.
    /// The crowd raises four events about a slot -- moved, reassigned, died, hurt -- and the
    /// bootstrap answers all four by driving a legacy <c>Animation</c> component. A Synty character
    /// has no <c>Animation</c> component; its motion comes from Humanoid retargeting through an
    /// Animator. So every one of those handlers no-ops on it, exactly as they no-op on a machine,
    /// and the result is exactly the same: a body that walks correctly and then DIES BY VANISHING,
    /// which an outside reviewer once called the single most amateur-reading thing in the build.
    ///
    /// <see cref="MachineGait"/> solved this shape of problem for the boxes and the solution is
    /// copied deliberately, down to the Intercept wiring, because two components answering the same
    /// four events the same way is far easier to reason about than one component with a mode flag.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SkinnedGait : MonoBehaviour
    {
        /// <summary>
        /// The blend-tree parameter, in METRES PER SECOND rather than a 0..1 ratio.
        ///
        /// Feeding a real speed keeps the calibration in one place -- the tree's thresholds, which
        /// were set against SimConfig.MoveSpeed -- instead of splitting it between a normaliser here
        /// and thresholds there, where the two drift apart and nobody can say which is wrong.
        /// </summary>
        private static readonly int SpeedId = Animator.StringToHash("Speed");

        /// <summary>
        /// How quickly the blend follows a change of speed.
        ///
        /// Damped, because the crowd's reported speed is noisy -- the sim steps on a fixed tick and
        /// the view does not, so the raw per-frame figure alternates between a full step and zero.
        /// Feeding that straight in makes a body flicker between walk and run every frame.
        /// </summary>
        private const float BlendSmoothing = 0.12f;

        /// <summary>How long the body takes to go over. Matched to the machines, so a mixed wave falls together.</summary>
        public float FallSeconds { get; set; } = 0.85f;

        private Animator? _animator;
        private Transform? _model;
        private float _fallen;      // 0 upright, 1 flat
        private bool _dying;
        private bool _bound;

        private void Awake() => Bind();

        private void Bind()
        {
            if (_bound) return;
            _bound = true;

            _animator = GetComponentInChildren<Animator>(true);
            // The MODEL's transform, not the slot's. The slot is where the crowd writes position and
            // facing every frame; writing the fall there would be two systems fighting over one
            // transform. An Animator with root motion off never touches its own GameObject, so the
            // model's local rotation is free real estate.
            _model = _animator != null ? _animator.transform : null;
        }

        /// <summary>True when this slot is one we can actually drive.</summary>
        public bool Valid => _animator != null;

        /// <summary>
        /// Scales playback to how fast the body is really travelling, so the feet do not skate.
        ///
        /// A standing body is slowed almost to a stop rather than marching in place: the controller
        /// has a single walk state, and "the characters are always making walking movements" is a
        /// note the owner has already given once.
        /// </summary>
        public void SetSpeed(float metresPerSecond)
        {
            Bind();
            if (_animator == null || _dying) return;

            // A NEGATIVE SPEED MEANS CONTACT, not reverse. The crowd signals "this body is on the
            // hero right now" by passing -1. There is no attack clip in Base Locomotion, so the
            // honest answer is to stand still rather than have someone mauling the player at a jog.
            float target = metresPerSecond < 0f ? 0f : metresPerSecond;

            // SetFloat's own damping, rather than a lerp we keep ourselves: it is frame-rate correct
            // and it is the thing the Animator samples, so there is no second copy of the value to
            // fall out of step.
            _animator.SetFloat(SpeedId, target, BlendSmoothing, Time.deltaTime);
        }

        /// <summary>Go over. The chip finished its decrypt and the body is no longer being driven.</summary>
        public void Die()
        {
            Bind();
            if (_animator == null || _dying) return;

            _dying = true;
            _fallen = 0f;

            // The Animator is switched OFF rather than left playing a walk under a falling body.
            // There is no death clip to crossfade to -- Synty's ANIMATION line has no death set at
            // all, which is a real content gap and is recorded as one.
            _animator.enabled = false;
        }

        /// <summary>A new occupant: stand the body up and re-roll its stride so the crowd is not in step.</summary>
        public void Reset()
        {
            Bind();
            _dying = false;
            _fallen = 0f;

            if (_model != null) _model.localRotation = Quaternion.identity;
            if (_animator == null) return;

            _animator.enabled = true;
            _animator.speed = 1f;
            _animator.SetFloat(SpeedId, 0f);   // a new occupant starts standing, not mid-sprint

            // Desynchronise. Fifty bodies entering on the same frame otherwise march in perfect
            // step, which is the single most obvious tell that a crowd is fake.
            var state = _animator.GetCurrentAnimatorStateInfo(0);
            _animator.Play(state.fullPathHash, 0, Random.value);
        }

        /// <summary>
        /// Lays the body down, after the crowd has finished placing it.
        ///
        /// LateUpdate on purpose: the crowd writes the slot's position and facing in its own update,
        /// and this has to land on top of that rather than underneath it.
        /// </summary>
        private void LateUpdate()
        {
            if (!_dying || _model == null) return;

            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            _fallen = Mathf.Min(1f, _fallen + dt / Mathf.Max(0.05f, FallSeconds));

            // Eased out, so it starts as a stumble and finishes as dead weight rather than a hinge.
            float e = 1f - (1f - _fallen) * (1f - _fallen);
            _model.localRotation = Quaternion.Euler(e * 86f, 0f, 0f);
            _model.localPosition = new Vector3(0f, -e * 0.12f, e * 0.28f);
        }

        // ------------------------------------------------------------------ wiring

        /// <summary>
        /// Wraps a crowd's four slot events so Animator-driven slots are handled here and everyone
        /// else reaches the handler that was already there.
        ///
        /// CALL THIS AFTER the handlers are assigned, and after <see cref="MachineGait.Intercept"/>.
        /// It decorates whatever is on the crowd at the moment it runs; assigning a handler
        /// afterwards would replace the wrapper and these bodies would quietly stop dying -- which
        /// is precisely the failure this class exists to fix, so it would be a cruel one to
        /// reintroduce by ordering.
        /// </summary>
        public static void Intercept(CivilianCrowd crowd, IReadOnlyList<Transform> slots)
        {
            if (crowd == null || slots == null) return;

            var gaits = new SkinnedGait?[slots.Count];
            int found = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i] == null) continue;

                // A CONTROLLERED ANIMATOR IS AUTHORITATIVE and that is the only test. Unity
                // suppresses an Animation component whenever one is present, so asking "does this
                // body also have a legacy component" answers the wrong question -- and asking it
                // was what made this claim zero slots on its first run while every Synty body
                // walked around invincible.
                var animator = slots[i].GetComponentInChildren<Animator>(true);
                if (animator == null || animator.runtimeAnimatorController == null) continue;

                var gait = slots[i].gameObject.AddComponent<SkinnedGait>();
                gaits[i] = gait;
                found++;
            }

            if (found == 0) return;
            Debug.Log($"[SkinnedGait] driving {found} Animator bodies");

            var moved = crowd.OnSlotMoved;
            var reassigned = crowd.OnSlotReassigned;
            var died = crowd.OnSlotDied;
            var hurt = crowd.OnSlotHurt;

            crowd.OnSlotMoved = (slot, speed) =>
            {
                var g = At(gaits, slot);
                if (g != null) g.SetSpeed(speed);
                else moved?.Invoke(slot, speed);
            };

            crowd.OnSlotReassigned = slot =>
            {
                var g = At(gaits, slot);
                if (g != null) g.Reset();
                else reassigned?.Invoke(slot);
            };

            crowd.OnSlotDied = slot =>
            {
                var g = At(gaits, slot);
                if (g != null) g.Die();
                else died?.Invoke(slot);
            };

            // Hurt falls through in every case: the white flash the bootstrap does is the read, and
            // it works on any renderer regardless of how the body is animated.
            crowd.OnSlotHurt = hurt;
        }

        private static SkinnedGait? At(SkinnedGait?[] gaits, int slot)
            => slot >= 0 && slot < gaits.Length ? gaits[slot] : null;
    }
}
