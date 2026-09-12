#nullable enable
using Cipher.Game;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The simulation and the renderer must never disagree about which bodies are machines.
    ///
    /// They used to hold two copies of the same avalanche hash, in two assemblies, and the copies
    /// agreed -- which is the dangerous version, because nothing fails until somebody tunes one of
    /// them and then the game shows a robot the drone refuses to convert. ADR-010 turned "which
    /// bodies are machines" from a question about pictures into a rule, so the sim owns it and the
    /// renderer forwards; this asserts the forwarding actually holds, body for body.
    /// </summary>
    public sealed class AllegianceAgreementTests
    {
        private static AgentWorld World(int seed)
        {
            var map = new GridMap(32, 32);
            var field = new FlowField(map);
            field.Compute(30, 16);
            var world = new AgentWorld(map, field, new SimConfig());
            world.MachineSeed = seed;
            world.MachineShare = CrowdCasting.HumanoidShare;
            return world;
        }

        [Test]
        public void TheRendererAndTheSimAgreeBodyForBody()
        {
            const int seed = 20260910;
            var world = World(seed);

            for (int i = 0; i < 500; i++)
            {
                int id = world.Spawn(GridMap.CellCenter(2, 16), 40f);
                var bodyClass = CrowdCasting.ClassOf(world.ArchetypeOf(id), id, seed);
                Assert.That(CrowdCasting.IsMachine(bodyClass), Is.EqualTo(world.IsMachine(id)),
                            $"agent {id} is a machine to one side and not the other");
            }
        }

        [Test]
        public void TheTwoNamedArchetypesAgreeToo()
        {
            // ADR-003 settles both, and it settles them in opposite directions, so this is exactly
            // the pair a drifting copy would get wrong: the Spitter is a hacked humanoid carrying a
            // sprayer, and the Sapper is a contractor with a toolbox.
            var world = World(4242);

            for (int i = 0; i < 40; i++)
            {
                int spitter = world.SpawnArchetype(GridMap.CellCenter(2, 16), Archetype.Spitter);
                int sapper = world.SpawnArchetype(GridMap.CellCenter(2, 16), Archetype.Sapper);

                Assert.That(world.IsMachine(spitter), Is.True, "the Spitter stopped being a machine");
                Assert.That(world.IsMachine(sapper), Is.False, "the Sapper became a machine");

                Assert.That(CrowdCasting.IsMachine(CrowdCasting.ClassOf(Archetype.Spitter, spitter, 4242)),
                            Is.EqualTo(world.IsMachine(spitter)));
                Assert.That(CrowdCasting.IsMachine(CrowdCasting.ClassOf(Archetype.Sapper, sapper, 4242)),
                            Is.EqualTo(world.IsMachine(sapper)));
            }
        }

        [Test]
        public void TheRenderersFractionIsTheSimsFraction()
        {
            // Cheap and direct: if this ever stops being an identity, one of them grew its own copy
            // back.
            for (int id = 0; id < 200; id++)
                for (int seed = 1; seed <= 3; seed++)
                    Assert.That(CrowdCasting.Fraction(id, seed),
                                Is.EqualTo(AgentWorld.MachineFraction(id, seed)));
        }
    }
}
