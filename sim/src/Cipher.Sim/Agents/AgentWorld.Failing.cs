#nullable enable
using System;

namespace Cipher.Sim.Agents
{
    /// <summary>
    /// Chips fail; they do not switch off. ADR-008.
    ///
    /// Weapons in this game carry malware, not kinetic energy: they decrypt and kill the implant.
    /// So an agent whose integrity reaches zero does NOT leave the simulation. It spends
    /// <see cref="SimConfig.FailSeconds"/> in a failing state — still walking, at a speed decaying
    /// toward a stumble, still attacking, at strength decaying to nothing — and only then drops.
    ///
    /// This is the one mechanical addition ADR-008 spends, and it is load-bearing for three
    /// separate playtest complaints that were all the same complaint: turrets killed too easily,
    /// a full line of people could not take one turret, and a crowd that reached the player was
    /// not frightening. All three are symptoms of instant death. An agent that vanishes on its
    /// last point of integrity can never finish the swing it started.
    ///
    /// The accounting deliberately splits the two moments:
    ///   - the chip BREAKS -> the player is paid (TotalKills, the archetype counters)
    ///   - the body DROPS   -> it leaves AliveCount and the spatial hash
    /// Paying at the break keeps cash and experience on the player's action rather than two and a
    /// half seconds behind it, which would read as a bug.
    ///
    /// No RNG here, as everywhere in the core: the decay is a linear function of dt.
    /// </summary>
    public sealed partial class AgentWorld
    {
        private float[] _failing = Array.Empty<float>();
        private long _brokenSappers;
        private long _brokenSpitters;

        /// <summary>
        /// Chips broken per archetype, cumulative.
        ///
        /// The game layer used to infer Sapper and Spitter deaths by watching the living count
        /// fall, because the sim raises no per-archetype death event. That inference is wrong now
        /// in two ways: the count falls a full failure window after the player earned it, and a
        /// body that reaches the vault leaves the count without anyone having broken its chip.
        /// Counting at the break is exact, so the inference can be deleted rather than patched.
        /// </summary>
        public long BrokenOf(Archetype archetype) => archetype switch
        {
            Archetype.Sapper => _brokenSappers,
            Archetype.Spitter => _brokenSpitters,
            _ => TotalBroken - _brokenSappers - _brokenSpitters,
        };

        private void NoteBroken(Archetype archetype)
        {
            if (archetype == Archetype.Sapper) _brokenSappers++;
            else if (archetype == Archetype.Spitter) _brokenSpitters++;
        }

        /// <summary>Chips broken since the world was created. Not the same as bodies dropped.</summary>
        public long TotalBroken { get; private set; }

        /// <summary>
        /// Bodies still upright with a dead chip. A wave is not clear while any of these remain,
        /// which is correct: they are the crowd finishing its swing.
        /// </summary>
        public int FailingCount { get; private set; }

        /// <summary>True while this agent's chip is dying but its body is still on its feet.</summary>
        public bool IsFailing(int id) => id >= 0 && id < Count && _alive[id] && _failing[id] > 0f;

        /// <summary>
        /// 1 while the chip is intact, falling to 0 as it fails. Everything the agent does — how
        /// fast it walks, how hard it hits — is scaled by this, which is what "gradually decreasing
        /// strength" means in practice.
        /// </summary>
        public float Integrity01(int id)
        {
            if (id < 0 || id >= Count || !_alive[id]) return 0f;
            float left = _failing[id];
            if (left <= 0f) return 1f;
            float total = MathF.Max(0.0001f, _config.FailSeconds);
            return Math.Clamp(left / total, 0f, 1f);
        }

        /// <summary>
        /// What this agent's attacks are worth right now. A healthy chip is 1; a failing one decays
        /// to zero over the window. Callers multiply their damage by it rather than checking the
        /// state, so nothing has to know the state exists.
        /// </summary>
        public float ThreatScale(int id) => Integrity01(id);

        /// <summary>
        /// What this agent's legs are worth right now. Floored above zero on purpose: a body that
        /// stops dead the instant its chip breaks reads as a freeze, and the whole point of the
        /// window is that it keeps coming at you.
        /// </summary>
        public float SpeedScale(int id)
        {
            float integrity = Integrity01(id);
            return integrity >= 1f ? 1f : _config.FailSpeedFloor
                                        + (1f - _config.FailSpeedFloor) * integrity;
        }

        /// <summary>
        /// Breaks a chip: the agent stays upright and dangerous for the failure window, and the
        /// player is paid now. Returns false if it was already broken, so a second hit on a
        /// failing body cannot be paid twice.
        ///
        /// This is the ONLY place a chip is retired. Every damage path funnels through it.
        /// </summary>
        private bool BreakChip(int id)
        {
            if (_failing[id] > 0f) return false;

            _failing[id] = MathF.Max(0.0001f, _config.FailSeconds);
            FailingCount++;
            TotalBroken++;
            TotalKills++;
            NoteBroken((Archetype)_archetype[id]);
            return true;
        }

        /// <summary>
        /// Ages every failing body and drops the ones that are done. Called once per tick AFTER
        /// movement, so a body gets its last step before it falls rather than freezing a tick early.
        /// </summary>
        private void StepFailing(float dt)
        {
            if (FailingCount == 0) return;

            for (int i = 0; i < Count; i++)
            {
                if (!_alive[i] || _failing[i] <= 0f) continue;

                _failing[i] -= dt;
                if (_failing[i] > 0f) continue;

                _failing[i] = 0f;
                _alive[i] = false;
                AliveCount--;
                FailingCount--;
                _hashDirty = true;
            }
        }
    }
}
