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
    }
}
