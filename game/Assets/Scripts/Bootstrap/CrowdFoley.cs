#nullable enable
using System.Collections.Generic;
using Cipher.Game.Audio;
using Cipher.Sim.Agents;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// The sound of the people on the field, and of the place they are in.
    ///
    /// Owner, third play: "there's no birds chirping or forest sounds or feet shuffling or people
    /// grunting or breathing heavily as they run faster or smacking attacking sounds". He was
    /// listing what a crowd IS, acoustically, and none of it existed: the only footsteps in the
    /// game were the hero's own, the only voice was the hero being bitten, and the whole world
    /// under that was three layers of broadband noise -- a procedural wind loop, a wind-in-trees
    /// recording and a woodland bed -- which together is exactly the "ocean breeze" he heard.
    ///
    /// Everything here is procedural (SoundRecipes) and positional (SoundBank.PlayAt), so it costs
    /// no licensed assets and it comes from WHERE the body is. Only bodies near the hero make a
    /// noise, and only a handful at once: foley is for presence, and thirty people's footsteps
    /// mixed together is a rainstorm, not a crowd.
    /// </summary>
    public sealed class CrowdFoley
    {
        /// <summary>Beyond this a body is scenery, acoustically.</summary>
        private const float Radius = 22f;
        /// <summary>At most this many bodies are voiced at once, nearest first as the slots come.</summary>
        private const int MaxVoiced = 10;
        /// <summary>Metres per footfall. Measured stride of the walk clip is ~0.78m; the run is longer, but a
        /// slightly quick cadence on a runner reads as urgency, which is the right error.</summary>
        private const float StrideMetres = 0.78f;
        /// <summary>Faster than this and a body is breathing hard.</summary>
        private const float BreathSpeed = 2.2f;

        private readonly SoundBank _sfx;
        private readonly System.Random _rng = new System.Random(7);

        private Vector3[] _last = System.Array.Empty<Vector3>();
        private float[] _phase = System.Array.Empty<float>();
        private float[] _voiceIn = System.Array.Empty<float>();
        private bool[] _seen = System.Array.Empty<bool>();
        private float _wildlifeIn = 1.5f;

        public CrowdFoley(SoundBank sfx) => _sfx = sfx;

        public void Tick(CivilianCrowd crowd, IReadOnlyList<Transform> slots, AgentWorld world,
                         Vector3 hero, Sky sky, float dt)
        {
            if (dt <= 0f) return;
            Size(slots.Count);
            Wildlife(hero, sky, dt);

            int voiced = 0;
            for (int slot = 0; slot < slots.Count; slot++)
            {
                int id = crowd.AgentInSlot(slot);
                if (id < 0 || !world.IsAlive(id) || slots[slot] == null)
                {
                    _seen[slot] = false;
                    continue;
                }

                Vector3 pos = slots[slot].position;
                if (!_seen[slot])
                {
                    // First sight of this occupant: no step on the frame it appears, and a random
                    // phase so a wave that spawned together does not stamp its feet together.
                    _seen[slot] = true;
                    _last[slot] = pos;
                    _phase[slot] = (float)_rng.NextDouble();
                    _voiceIn[slot] = Range(0.6f, 2.4f);
                    continue;
                }

                Vector3 delta = pos - _last[slot];
                _last[slot] = pos;

                float d = Vector3.Distance(pos, hero);
                if (d > Radius) continue;
                if (++voiced > MaxVoiced) break;

                // Loudness by distance, never to zero inside the radius: a body at the edge is a
                // presence, a body at your shoulder is a fact.
                float near = 1f - d / Radius;
                float travelled = delta.magnitude;
                float speed = travelled / dt;

                if (travelled > 0.002f)
                {
                    _phase[slot] += travelled / StrideMetres;
                    if (_phase[slot] >= 1f)
                    {
                        _phase[slot] -= 1f;
                        _sfx.PlayAt(Sfx.Step, pos, 0.10f + 0.30f * near, pitchJitter: 0.14f);
                    }
                }

                _voiceIn[slot] -= dt;
                if (_voiceIn[slot] > 0f) continue;

                Vector3 mouth = pos + Vector3.up * 1.45f;
                if (world.IsInContactWithHero(id))
                {
                    // On him: effort, not breath. Short interval, because a body tearing at
                    // someone is not quiet about it.
                    _sfx.PlayAt(Sfx.Grunt, mouth, 0.35f + 0.45f * near, pitchJitter: 0.22f);
                    _voiceIn[slot] = Range(0.8f, 1.5f);
                }
                else if (speed > BreathSpeed)
                {
                    _sfx.PlayAt(Sfx.Breath, mouth, 0.12f + 0.32f * near, pitchJitter: 0.20f);
                    _voiceIn[slot] = Range(1.4f, 2.8f);
                }
                else
                {
                    _voiceIn[slot] = 0.4f;   // check again soon; nothing to say yet
                }
            }
        }

        /// <summary>
        /// The place, as one-shots over the bed: birds by day and dusk, crickets by night, nothing
        /// in fog -- fog is the sky whose whole point is that the world has gone away. Placed at a
        /// random spot some way off from the hero, so the woodland has a direction.
        /// </summary>
        private void Wildlife(Vector3 hero, Sky sky, float dt)
        {
            _wildlifeIn -= dt;
            if (_wildlifeIn > 0f) return;

            if (sky == Sky.Fog) { _wildlifeIn = 3f; return; }

            float angle = Range(0f, Mathf.PI * 2f);
            float dist = Range(16f, 34f);
            var at = hero + new Vector3(Mathf.Cos(angle) * dist, Range(3f, 7f), Mathf.Sin(angle) * dist);

            if (sky == Sky.Night)
            {
                _sfx.PlayAt(Sfx.Cricket, at, 0.30f, pitchJitter: 0.08f);
                _wildlifeIn = Range(1.6f, 3.4f);
            }
            else
            {
                _sfx.PlayAt(Sfx.Bird, at, 0.38f, pitchJitter: 0.16f);
                _wildlifeIn = Range(2.2f, 6.5f);
            }
        }

        private void Size(int n)
        {
            if (_last.Length >= n) return;
            System.Array.Resize(ref _last, n);
            System.Array.Resize(ref _phase, n);
            System.Array.Resize(ref _voiceIn, n);
            System.Array.Resize(ref _seen, n);
        }

        private float Range(float a, float b) => a + (float)_rng.NextDouble() * (b - a);
    }
}
