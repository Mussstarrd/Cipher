#nullable enable
using System;
using Cipher.Sim.Core;

namespace Cipher.Sim.Agents
{
    /// <summary>
    /// Which side a body is on, and what it is made of. ADR-010.
    ///
    /// THE SIMULATION HAD NEVER HAD AN AGENT FIGHT ANOTHER AGENT. Every mechanic to date is
    /// crowd-versus-structure or crowd-versus-hero, and the whole targeting layer quietly assumed
    /// there was exactly one side. The turncoat drone breaks that assumption, so the assumption has
    /// to stop being an accident and become a value every query reads.
    ///
    /// Two facts live here:
    ///
    ///   MACHINE   this body is a hacked service humanoid, not a chipped person. ADR-003 named
    ///             three enemy classes and this is the second of them.
    ///   TURNED    the drone has taken it back. It fights for the player until it burns out. It is
    ///             not commanded, it does not path anywhere, it is never a unit the player manages.
    ///
    /// **A CHIPPED PERSON CAN NEVER BE TURNED**, and that limit is the design rather than a
    /// simplification. Taking over a neighbour by hacking the implant in their head is a different
    /// and much darker game than this one. It is also the better rule mechanically: it turns the
    /// machine share of a wave into something the player reads before they buy, and it means a
    /// position that sends mostly people makes the drone a bad purchase. The build bar should never
    /// hold a thing that is correct every time.
    ///
    /// **Machine-ness is a HASH, not stored state.** Derived from the agent id and a per-position
    /// seed, so it costs nothing, needs no serialisation, and is identical every time a position
    /// loads. It lives HERE rather than in the crowd renderer, which had its own copy of the same
    /// hash: conversion is a simulation rule, and two copies of one rule is how the two drift.
    /// </summary>
    public sealed partial class AgentWorld
    {
        private bool[] _turned = Array.Empty<bool>();

        /// <summary>
        /// Seeds which bodies are machines. Set once per position by the game layer, alongside the
        /// spawn director's seed, so a position's crowd is made of the same things every time.
        /// </summary>
        public int MachineSeed { get; set; }

        /// <summary>
        /// The share of ordinary bodies that are hacked service humanoids. The owner's number:
        /// "like 30% of them should be those humanoid robots".
        /// </summary>
        public float MachineShare { get; set; } = 0.30f;

        /// <summary>Bodies currently fighting for the player. Never shot at, and drawn differently.</summary>
        public int TurnedCount { get; private set; }

        /// <summary>
        /// Living bodies still working for HALCYON.
        ///
        /// **This, not <see cref="AliveCount"/>, is what "is the wave clear" must ask.** A convert
        /// is alive and in the sim, so a match reading the raw living count would sit at "wave in
        /// progress" until the player's own machine burned out -- the wave-clear bonus late, the
        /// pack-up window late, and nothing on screen explaining why.
        /// </summary>
        public int HostileCount => AliveCount - TurnedCount;

        /// <summary>
        /// True for a hacked service humanoid rather than a chipped person.
        ///
        /// Spitters are machines unconditionally -- a hacked humanoid carrying an industrial
        /// sprayer is what a Spitter IS. Sappers are unconditionally people: a Sapper is a
        /// contractor with a toolbox who knows which panel is thin. Collectors are people too,
        /// after a fashion (ADR-011) -- engineered, not manufactured, and never turnable. Only
        /// ordinary bodies are rolled, so a wave's true machine share sits a little above
        /// <see cref="MachineShare"/>.
        /// </summary>
        public bool IsMachine(int id)
        {
            if (id < 0 || id >= Count) return false;
            return (Archetype)_archetype[id] switch
            {
                Archetype.Spitter => true,
                Archetype.Sapper => false,
                Archetype.Collector => false,
                _ => MachineFraction(id, MachineSeed) < MachineShare,
            };
        }

        /// <summary>
        /// A stable value in [0,1) from an id and a seed.
        ///
        /// A hash rather than an RNG: nothing is sequenced, nothing is stored, and asking twice
        /// costs nothing -- which matters, because the crowd renderer asks for every visible body
        /// every frame. Murmur3's finaliser specifically, because consecutive ids are exactly how
        /// the spawn director hands them out and a weaker mix returns them in runs: eleven machines
        /// followed by twenty people, instead of a mixed crowd.
        /// </summary>
        public static float MachineFraction(int id, int seed)
        {
            unchecked
            {
                uint h = (uint)id * 2654435761u ^ (uint)seed * 2246822519u;
                h ^= h >> 16;
                h *= 0x85EBCA6Bu;
                h ^= h >> 13;
                h *= 0xC2B2AE35u;
                h ^= h >> 16;
                return (h >> 8) * (1f / 16777216f);   // top 24 bits: exactly representable as float
            }
        }

        /// <summary>True once the drone has taken this one back.</summary>
        public bool IsTurned(int id) => id >= 0 && id < Count && _alive[id] && _turned[id];

        /// <summary>
        /// A body still working for HALCYON: alive, and not one of ours. What area damage, crowd
        /// counts and hero contact pressure all ask.
        /// </summary>
        private bool IsHostile(int id) => _alive[id] && !_turned[id];

        /// <summary>
        /// A body worth spending a round on: hostile, and not already due to fall by itself.
        ///
        /// Every auto-targeting query in the game funnels through this one predicate rather than
        /// testing <c>_alive</c> and hoping. The outside review caught exactly this class of bug
        /// once already: the spoken-for filter was added to the turret query and not to the drone
        /// query, so patrol drones emptied themselves into a body that was going down anyway while
        /// a healthy one walked past. With two sides on the field the same slip shoots your own
        /// converts, which is worse -- it is not a wasted round, it is friendly fire.
        /// </summary>
        private bool IsShootable(int id) => IsHostile(id) && !IsSpokenFor(id);

        /// <summary>
        /// Takes a machine back. Returns false for anything that cannot be turned -- a person, a
        /// Collector, one already turned, one not alive.
        ///
        /// **Conversion starts the convert dying, and that is the balance.** The drone does not
        /// repair anything: it re-points a machine that is still running HALCYON's firmware, and
        /// the network begins taking it back immediately. Mechanically it means two conversions in
        /// a heavy wave are a swing rather than an army, and the player has to keep earning them.
        ///
        /// It is implemented as an ordinary decrypt drain (ADR-008) rather than a separate timer,
        /// which buys three things free: the convert weakens visibly as it goes, the existing
        /// per-tick ageing retires it with no new code, and the health bar over its head means the
        /// same thing it means over everyone else's.
        ///
        /// The drain ADDS to whatever was already eating it, so a machine the player has been
        /// shooting is worth less as a convert than an untouched one. That is a real decision at
        /// the moment of use, and it is the right way round: it rewards converting early.
        /// </summary>
        public bool Turn(int id)
        {
            if (id < 0 || id >= Count || !_alive[id]) return false;
            if (_turned[id] || !IsMachine(id)) return false;

            _turned[id] = true;
            TurnedCount++;

            float burnout = _health[id] / MathF.Max(0.01f, _config.TurnedSeconds);
            if (_drain[id] <= 0f) FailingCount++;
            _drain[id] += burnout;

            // It stops being part of the crowd this instant: whatever errand, plan or grudge it was
            // carrying is HALCYON's, and it is not HALCYON's any more.
            _target[id] = -1;
            _aggro[id] = 0f;
            _timer[id] = 0f;
            _intent[id] = (byte)Intent.Vault;
            _plans.Remove(id);   // only Spitters and runners turn, but never leave a stale plan

            return true;
        }

        /// <summary>
        /// A convert's whole behaviour: find the nearest thing still working for HALCYON, walk at
        /// it, hit it.
        ///
        /// It does not path to the goal, it does not defend anything, and it cannot be given an
        /// order. That simplicity is deliberate -- a convert that needed managing would be a unit,
        /// and ADR-010 is explicit that this is not one. With nothing in range it holds where it
        /// stands rather than falling through to the crowd's movement, which would walk the
        /// player's own machine into the vault.
        /// </summary>
        private void StepTurned(int i, Vec2 pos, float dt, bool gates)
        {
            int prey = NearestHostileTo(pos, _config.TurnedSeekRange, i);
            if (prey < 0) return;

            Vec2 at = new Vec2(_posX[prey], _posY[prey]);
            float distSq = Vec2.DistanceSquared(pos, at);

            if (distSq > _config.TurnedReach * _config.TurnedReach)
            {
                SteerToward(i, pos, at, dt, _config.TurnedSpeed, gates);
                return;
            }

            _timer[i] -= dt;
            if (_timer[i] > 0f) return;
            _timer[i] = _config.TurnedAttackInterval;

            // It hurts the same way the player's weapons do, because it is running the same
            // payload. The drone did not heal this thing, it re-aimed it.
            Infect(prey, _config.TurnedDamage * ThreatScale(i));
        }

        /// <summary>
        /// The nearest living body still on HALCYON's side, or -1. <paramref name="exclude"/> keeps
        /// a convert from picking itself out of the spatial hash.
        ///
        /// Unlike the player's guns this does NOT skip the spoken-for: a convert has seconds to
        /// live and no ammunition to waste, so "hit whatever is closest" is both simpler and, for
        /// something running on a burning-out battery, more honest.
        /// </summary>
        private int NearestHostileTo(Vec2 from, float radius, int exclude)
        {
            RebuildHashIfDirty();
            _queryScratch.Clear();
            _hash.QueryCircle(from, radius, _queryScratch);

            int best = -1;
            float bestSq = radius * radius;
            foreach (int hashId in _queryScratch)
            {
                int id = _hashToAgent[hashId];
                if (id == exclude || !IsHostile(id)) continue;

                float d = Vec2.DistanceSquared(from, new Vec2(_posX[id], _posY[id]));
                // Lowest id on ties, like every other targeting query here. Determinism first.
                if (d < bestSq || (d == bestSq && id < best)) { bestSq = d; best = id; }
            }
            return best;
        }
    }
}
