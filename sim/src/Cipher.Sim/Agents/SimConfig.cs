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
    }
}
