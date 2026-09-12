#nullable enable
using UnityEngine;

namespace Cipher.Game.Audio
{
    /// <summary>
    /// Decides WHEN a footstep lands and WHICH recording plays, with no AudioSource in it.
    ///
    /// DISTANCE, NOT TIME. A step fires every <see cref="Stride"/> metres travelled rather than
    /// every N seconds. This is the whole design: it means walking backwards, strafing, being
    /// slowed by adrenaline focus and being sped up by a gear roll all produce the right cadence
    /// for free, and -- the failure the owner would have heard first -- a hero shuffling against
    /// a wall at 0.2 m/s does not machine-gun, because they are not covering any ground.
    ///
    /// A hard floor on the gap catches the pathological case anyway (a teleport, a position
    /// change, a frame that ran long), because "it can't happen" is how it ships happening.
    /// </summary>
    public sealed class FootstepCadence
    {
        /// <summary>Metres between steps. A walking adult is about 0.75 m; this is a jog.</summary>
        public const float Stride = 1.85f;

        /// <summary>Below this, the hero is adjusting their feet, not walking.</summary>
        public const float MinSpeed = 0.35f;

        /// <summary>No two steps closer than this, whatever the maths says.</summary>
        public const float MinGap = 0.17f;

        private readonly int _clipCount;
        private float _travelled;
        private float _sinceLast = 999f;
        private int _last = -1;
        private int _lastButOne = -1;
        private uint _state;

        /// <summary>Alternates every step, so the two feet can differ in pitch and level.</summary>
        public bool RightFoot { get; private set; }

        /// <summary>Total steps taken. Handy in tests, and it is the cheapest cadence assertion.</summary>
        public int Steps { get; private set; }

        public FootstepCadence(int clipCount, int seed = 12345)
        {
            _clipCount = Mathf.Max(1, clipCount);
            _state = (uint)seed | 1u;
        }

        /// <summary>
        /// Advances the walk. Returns the index of the clip to play, or -1 for no step this frame.
        /// </summary>
        /// <param name="dt">Seconds since the last call.</param>
        /// <param name="speed">Current ground speed in metres per second.</param>
        public int Tick(float dt, float speed)
        {
            if (dt <= 0f) return -1;
            _sinceLast += dt;

            if (speed < MinSpeed)
            {
                // Bleed the accumulator away while standing so that stopping mid-stride and
                // setting off again does not fire instantly. Bleeding rather than zeroing keeps
                // a brief pause (turning on the spot) from resetting the walk entirely.
                _travelled = Mathf.Max(0f, _travelled - dt * 0.9f);
                return -1;
            }

            _travelled += speed * dt;
            if (_travelled < Stride || _sinceLast < MinGap) return -1;

            // Subtract rather than zero: at a sprint a single long frame can cover more than one
            // stride, and zeroing would silently swallow the extra step.
            _travelled -= Stride;
            _sinceLast = 0f;
            RightFoot = !RightFoot;
            Steps++;
            return Choose();
        }

        /// <summary>
        /// A clip index that is neither of the last two played.
        ///
        /// One-back avoidance is not enough with six clips: an A-B-A-B alternation is audibly a
        /// loop, and it is what "pick anything but the last one" degenerates into surprisingly
        /// often. Two-back is the cheapest rule that sounds like a person walking.
        /// </summary>
        public int Choose()
        {
            if (_clipCount == 1) return 0;

            // Chosen from the allowed set rather than by rejection sampling. Rolling until the
            // pick is acceptable is the obvious way to write this and it is wrong: it needs a
            // bail-out after N tries, and on the try that bails it returns exactly the repeat it
            // was built to avoid. Rare, non-deterministic, and audible. Enumerating instead makes
            // the guarantee total.
            bool avoidTwoBack = _clipCount > 2;

            int allowed = 0;
            for (int i = 0; i < _clipCount; i++)
                if (i != _last && !(avoidTwoBack && i == _lastButOne)) allowed++;

            int target = (int)(NextUnit() * allowed);
            if (target >= allowed) target = allowed - 1;

            int pick = 0;
            for (int i = 0; i < _clipCount; i++)
            {
                if (i == _last || (avoidTwoBack && i == _lastButOne)) continue;
                if (target-- == 0) { pick = i; break; }
            }

            _lastButOne = _last;
            _last = pick;
            return pick;
        }

        /// <summary>Per-step pitch, so six recordings do not sound like six recordings.</summary>
        public float Pitch => (RightFoot ? 1.015f : 0.985f) + (NextUnit() - 0.5f) * 0.07f;

        private float NextUnit()
        {
            _state ^= _state << 13; _state ^= _state >> 17; _state ^= _state << 5;
            return (_state & 0xFFFFFF) * (1f / 16777216f);
        }
    }
}
