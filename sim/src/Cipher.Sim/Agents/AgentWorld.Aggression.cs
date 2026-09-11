#nullable enable
using System;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;

namespace Cipher.Sim.Agents
{
    /// <summary>
    /// What the signed do about whatever is in front of them.
    ///
    /// Owner, 2026-09-11, and it is the right read of his own premise:
    ///
    ///   "it seems like when you're close to mobs they should be attracted to you and try to chase
    ///    you down [...] if they walk by a tower they should try to attack the tower to clear their
    ///    path I mean all they're trying to do is run through and Destroy anything that opposes them
    ///    specifically humans so if I'm close to him then they chase and try to kill me and if they
    ///    run by a turret they try to kill the turret and eventually I can't hold it anymore and
    ///    that's when I got to push back"
    ///
    /// Before this, an agent's errand was chosen once at spawn and never revisited: a body assigned
    /// to the vault would walk past the player's boots and past a gun that was shooting it. That is
    /// a crowd following a spline, not a crowd that wants something.
    ///
    /// The rule is OPPORTUNITY, and it layers on top of the spawn intent rather than replacing it:
    ///
    ///   1. A living human inside <see cref="SimConfig.HeroAggroRange"/> outranks everything. They
    ///      are here for people; the vault is only where the people are.
    ///   2. Anything that is shooting at them, inside arm's reach, gets pulled down first.
    ///   3. Otherwise, the errand they spawned with.
    ///
    /// The dedicated hunters keep their long acquire range, so the difference survives: a hunter
    /// crosses a field to reach a gun, everyone else only engages what they nearly walk into.
    ///
    /// Aggression is STICKY for a short while (<see cref="SimConfig.AggroMemory"/>), because a mob
    /// that drops you the instant you step one cell outside its radius reads as a light switch.
    /// </summary>
    public sealed partial class AgentWorld
    {
        /// <summary>Where the player is, and whether they are worth chasing. Set by the game each tick.</summary>
        public Vec2 HeroPosition { get; private set; }

        /// <summary>False while the hero is down or the match is over: nothing chases a corpse.</summary>
        public bool HeroIsPrey { get; private set; }

        /// <summary>
        /// Tells the swarm where the player is. Deterministic: this is an INPUT to the tick, exactly
        /// like dt, so the same inputs still produce the same tick.
        /// </summary>
        public void SetHero(Vec2 position, bool alive)
        {
            HeroPosition = position;
            HeroIsPrey = alive;
        }

        /// <summary>How many living agents are currently chasing the player. For the HUD.</summary>
        public int ChasingCount { get; private set; }

        /// <summary>
        /// True when a body is close enough to the player to be hitting him. A read-only view for
        /// the renderer, so a body standing on the player throws punches instead of idling.
        /// </summary>
        public bool IsInContactWithHero(int id)
        {
            if (!HeroIsPrey || id < 0 || id >= Count || !_alive[id]) return false;
            float reach = _config.HeroContactRange * 1.9f;
            return Vec2.DistanceSquared(new Vec2(_posX[id], _posY[id]), HeroPosition) <= reach * reach;
        }

        /// <summary>
        /// Runs the opportunist rules for one agent. Returns true when it has taken movement for
        /// this tick and the ordinary errand should be skipped.
        /// </summary>
        private bool StepOpportunist(int i, Vec2 pos, float dt, bool gates)
        {
            // ---- 1. People first ----------------------------------------------------------
            if (HeroIsPrey)
            {
                float heroDistSq = Vec2.DistanceSquared(pos, HeroPosition);
                float range = _aggro[i] > 0f ? _config.HeroAggroRange * _config.AggroStickyScale
                                             : _config.HeroAggroRange;

                if (heroDistSq <= range * range)
                {
                    // Sticky: keep coming for a moment after they lose the range, so backing off one
                    // step does not switch the whole crowd off like a light.
                    _aggro[i] = _config.AggroMemory;
                    ChasingCount++;

                    float dist = MathF.Sqrt(heroDistSq);
                    if (dist > _config.HeroContactRange)
                    {
                        // They run at people. The game layer owns hero health and resolves contact.
                        SteerToward(i, pos, HeroPosition, dt, _config.ChaseSpeed * _pace[i], gates);
                        return true;
                    }

                    // In contact: hold position and let the game layer chew. Moving would push them
                    // through him and out the other side.
                    return true;
                }
            }

            if (_aggro[i] > 0f) _aggro[i] = MathF.Max(0f, _aggro[i] - dt);

            // ---- 2. Whatever is shooting at them ------------------------------------------
            var structures = Structures;
            if (structures == null || structures.Count == 0) return false;
            if ((Intent)_intent[i] == Intent.HuntStructure) return false;   // already its whole job

            int best = -1;
            float bestDistSq = _config.OpportunistStructureRange * _config.OpportunistStructureRange;
            for (int s = 0; s < structures.Count; s++)
            {
                float dsq = Vec2.DistanceSquared(pos, structures.PositionAt(s));
                // Strict less-than plus an ascending scan makes ties resolve to the lowest index,
                // which is what keeps this deterministic.
                if (dsq < bestDistSq) { bestDistSq = dsq; best = s; }
            }
            if (best < 0) return false;

            Vec2 target = structures.PositionAt(best);
            float d = MathF.Sqrt(bestDistSq);
            _target[i] = best;

            if (d > _config.HunterContactRange)
            {
                SteerToward(i, pos, target, dt, _config.MoveSpeed * _pace[i], gates);
                return true;
            }

            _timer[i] -= dt;
            if (_timer[i] <= 0f)
            {
                _timer[i] = _config.HunterAttackInterval;
                // Opportunists hit softer than the dedicated hunters: a gun in the way is a nuisance
                // they claw at in passing, not the thing they came for.
                _events.Add(new SimEvent(SimEventKind.StructureMauled, i, best,
                                         _config.HunterStructureDamage * _config.OpportunistDamageScale));
            }
            return true;
        }
    }
}
