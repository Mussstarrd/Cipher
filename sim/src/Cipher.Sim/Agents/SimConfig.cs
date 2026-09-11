#nullable enable
namespace Cipher.Sim.Agents
{
    /// <summary>Tunables for agent movement. Immutable per-archetype data; balance lives in data tables, not code.</summary>
    public sealed class SimConfig
    {
        /// <summary>World units per second.</summary>
        public float MoveSpeed { get; set; } = 3f;

        /// <summary>Neighbors inside this radius push agents apart.</summary>
        public float SeparationRadius { get; set; } = 0.6f;

        /// <summary>Blend weight of separation vs. flow-field direction.</summary>
        public float SeparationWeight { get; set; } = 0.8f;

        /// <summary>Agents within this distance of the goal cell center count as arrived.</summary>
        public float GoalRadius { get; set; } = 0.75f;

        public float RunnerHealth { get; set; } = 10f;

        /// <summary>
        /// Neighbours sampled per spatial-hash cell when computing separation. Bounds the swarm's
        /// per-agent cost when a throttled breach piles the whole wave into a few cells; without it
        /// the cost is quadratic in the pile size and the frame loop death-spirals.
        /// </summary>
        public int SeparationNeighborsPerCell { get; set; } = 4;

        // ---- Sapper (docs/design/breach-and-repair.md) ----
        public float SapperHealth { get; set; } = 60f;
        public float SapperSpeed { get; set; } = 2.4f;
        /// <summary>Seconds standing at the wall before the hole opens.</summary>
        public float SapperPlantSeconds { get; set; } = 4f;
        /// <summary>A wall is only worth breaching if it shortens the route by this many cells (ignored when sealed in).</summary>
        public float SapperMinGain { get; set; } = 12f;
        /// <summary>Seconds per breach stage (Cracked → Broken → Collapsed) with no traffic.</summary>
        public float BreachStageSeconds { get; set; } = 20f;
        /// <summary>Each agent admitted through a hole shaves this off the current stage timer.</summary>
        public float TrafficShaveSeconds { get; set; } = 0.5f;

        // ---- Spitter (docs/design/economy-towers-and-aiming.md C) ----
        public float SpitterHealth { get; set; } = 60f;
        public float SpitterSpeed { get; set; } = 2.4f;
        public float SpitterAcquireRange { get; set; } = 12f;
        public float SpitterAttackRange { get; set; } = 9f;
        public float SpitterDamage { get; set; } = 15f;
        public float SpitterAttackInterval { get; set; } = 1.5f;
    
        // --- Intent split (owner, 2026-09-11): not every body runs the same errand. ---

        /// <summary>How far a hunter will look for an emplacement before giving up and rejoining.</summary>
        // ---- Opportunism (owner, 2026-09-11) -------------------------------------------------
        // "if I'm close to him then they chase and try to kill me and if they run by a turret they
        //  try to kill the turret". These are the ranges that turn a crowd following a route into a
        //  crowd that wants something. See AgentWorld.Aggression.cs.

        /// <summary>How close the player has to be before ordinary bodies come for him instead.</summary>
        public float HeroAggroRange { get; set; } = 8f;

        /// <summary>Close enough to be chewing on him. The game layer owns the damage.</summary>
        public float HeroContactRange { get; set; } = 1.0f;

        /// <summary>They move faster at a person than they do at a building.</summary>
        public float ChaseSpeed { get; set; } = 4.2f;

        /// <summary>
        /// Seconds a body keeps coming after the player leaves its range. Without this, stepping one
        /// cell back switches the whole crowd off at once and it reads as a light switch.
        /// </summary>
        public float AggroMemory { get; set; } = 2.5f;

        /// <summary>While that memory lasts, the range they will follow to is this much wider.</summary>
        public float AggroStickyScale { get; set; } = 1.6f;

        /// <summary>Arm's reach. An ordinary body only claws a gun it nearly walks into.</summary>
        public float OpportunistStructureRange { get; set; } = 2.6f;

        /// <summary>An opportunist hits a gun softer than a body that came specifically for guns.</summary>
        public float OpportunistDamageScale { get; set; } = 0.55f;

        public float HunterAcquireRange { get; set; } = 14f;
        /// <summary>Close enough to start tearing at it.</summary>
        public float HunterContactRange { get; set; } = 1.1f;
        /// <summary>Hunters move with purpose. Slightly faster than the crowd, and it reads.</summary>
        public float HunterSpeed { get; set; } = 3.6f;
        public float HunterAttackInterval { get; set; } = 1f;
        public float HunterStructureDamage { get; set; } = 8f;

        /// <summary>Cells a wrecker will search outward for something to pull down.</summary>
        public int WreckerSearchCells { get; set; } = 10;
        public float WreckerAttackInterval { get; set; } = 0.8f;
        public float WreckerWallDamage { get; set; } = 6f;
}
}
