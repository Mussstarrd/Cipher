#nullable enable
using System;
using Cipher.Sim.Core;

namespace Cipher.Sim.Agents
{
    /// <summary>
    /// The Collectors. ADR-011.
    ///
    /// Everything else in this game walks a flow field to the goal and fights whatever is in the
    /// way. A Collector was **sent for the player**: it ignores the objective, it ignores the
    /// emplacements it is not physically obstructed by, and it walks at him. That is the whole
    /// point of it as a boss — **it is the first enemy the player cannot solve by building**, and it
    /// forces them out of the position they spent the entire mission preparing.
    ///
    /// Two more things make it a fight rather than a big number:
    ///
    ///  - **It is HARDENED.** ADR-008's model says one pulse is a death sentence that arrives in
    ///    forty seconds, and hits stack. A Collector's implant is lab hardware and resists, so a hit
    ///    contributes a fraction of the drain it would to a citizen. The passive kill never arrives
    ///    in time, which means every point has to come off by direct fire — the exact inverse of the
    ///    tactic the player has just learned.
    ///  - **It does not come apart until the very end.** No comfortable stretch where it has stopped
    ///    being dangerous but is still standing.
    ///
    /// It is not a damage sponge and must not become one. If the fight is "hold the trigger for
    /// ninety seconds" the design has failed; it should be won by repositioning, by spending the EMP
    /// well, and by choosing what to give up while it walks through the middle of your guns.
    /// </summary>
    public sealed partial class AgentWorld
    {
        /// <summary>True for the one archetype that was built to come for the player personally.</summary>
        public bool IsCollector(int id) =>
            id >= 0 && id < Count && (Archetype)_archetype[id] == Archetype.Collector;

        /// <summary>
        /// How much of a normal payload actually takes hold. ADR-011: the arsenal still works, it
        /// just works badly, which is why the player is not simply disarmed.
        /// </summary>
        private float DrainResistance(int id) =>
            (Archetype)_archetype[id] == Archetype.Collector ? _config.CollectorDrainResistance : 1f;

        /// <summary>
        /// A Collector's step. Straight at the hero, through whatever the wall rules allow, and it
        /// mauls a structure only when one is genuinely in its way rather than seeking them out.
        ///
        /// Returns false when there is no hero to walk at — a dead player leaves it following the
        /// field like everything else, which is the correct fallback and not worth a special case.
        /// </summary>
        private bool StepCollector(int i, Vec2 pos, float dt, bool gates)
        {
            if (!HeroIsPrey) return false;

            float dist = MathF.Sqrt(Vec2.DistanceSquared(pos, HeroPosition));
            if (dist <= _config.HeroContactRange)
            {
                // In reach. The game layer owns hero health and resolves contact, exactly as it
                // does for an ordinary body — a Collector simply counts for a great deal more of it.
                ChasingCount++;
                return true;
            }

            ChasingCount++;

            // Anything solid between it and him gets hit rather than walked around. It does not
            // hunt structures -- it is not interested in the turret, it is interested in getting
            // through the turret -- so this only fires when one is actually in the way.
            if (Structures != null && dist > _config.HeroContactRange)
            {
                int blocking = NearestStructureWithin(pos, _config.CollectorReach);
                if (blocking >= 0)
                {
                    _timer[i] -= dt;
                    if (_timer[i] <= 0f)
                    {
                        _timer[i] = _config.CollectorAttackInterval;
                        _events.Add(new SimEvent(SimEventKind.StructureMauled, i, blocking,
                                                 _config.CollectorStructureDamage * ThreatScale(i)));
                    }
                    return true;
                }
            }

            SteerToward(i, pos, HeroPosition, dt, _config.CollectorSpeed, gates);
            return true;
        }

        /// <summary>
        /// The nearest player structure whose centre is within <paramref name="reach"/>, or -1.
        /// Deliberately a plain scan rather than the spatial hash: structures are counted in tens,
        /// the hash holds agents, and a Collector asks this at most a few times a second.
        /// </summary>
        private int NearestStructureWithin(Vec2 from, float reach)
        {
            if (Structures == null) return -1;

            int best = -1;
            float bestSq = reach * reach;
            int n = Structures.Count;
            for (int s = 0; s < n; s++)
            {
                float d = Vec2.DistanceSquared(from, Structures.PositionAt(s));
                if (d < bestSq) { bestSq = d; best = s; }
            }
            return best;
        }
    }
}
