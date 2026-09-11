using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>Queries the hero layer relies on: hitscan, single-target damage, contact counts, shared wall rule.</summary>
    public class HeroQueryTests
    {
        private static AgentWorld MakeWorld(int size = 32)
        {
            var map = new GridMap(size, size);
            var field = new FlowField(map);
            field.Compute(size - 1, size / 2);
            return new AgentWorld(map, field, new SimConfig());
        }

        [Fact]
        public void Raycast_HitsNearestAgentAlongRay_NotTheFirstSpawned()
        {
            var w = MakeWorld();
            int far = w.Spawn(new Vec2(12f, 5f), 10f);
            int near = w.Spawn(new Vec2(8f, 5.2f), 10f);

            Assert.True(w.Raycast(new Vec2(2f, 5f), new Vec2(1f, 0f), 20f, 0.5f, out int hit, out float dist));
            Assert.Equal(near, hit);
            Assert.NotEqual(far, hit);
            Assert.InRange(dist, 5.9f, 6.1f);
        }

        [Fact]
        public void Raycast_IgnoresAgentsBehindOriginOutsideRadiusOrBeyondRange()
        {
            var w = MakeWorld();
            w.Spawn(new Vec2(1f, 5f), 10f);   // behind
            w.Spawn(new Vec2(8f, 6.5f), 10f); // 1.5 off-axis, radius is 0.5
            w.Spawn(new Vec2(28f, 5f), 10f);  // beyond 20 range

            Assert.False(w.Raycast(new Vec2(2f, 5f), new Vec2(1f, 0f), 20f, 0.5f, out int hit, out _));
            Assert.Equal(-1, hit);
        }

        [Fact]
        public void Raycast_SkipsDeadAgents_AndZeroDirectionMisses()
        {
            var w = MakeWorld();
            int a = w.Spawn(new Vec2(6f, 5f), 10f);
            int b = w.Spawn(new Vec2(9f, 5f), 10f);
            w.ApplyDamage(a, 100f);

            Assert.True(w.Raycast(new Vec2(2f, 5f), new Vec2(1f, 0f), 20f, 0.5f, out int hit, out _));
            Assert.Equal(b, hit);
            Assert.False(w.Raycast(new Vec2(2f, 5f), Vec2.Zero, 20f, 0.5f, out _, out _));
        }

        [Fact]
        public void ApplyDamage_WoundsThenKills_AndCountsExactlyOnce()
        {
            var w = MakeWorld();
            int id = w.Spawn(new Vec2(5f, 5f), 10f);

            Assert.False(w.ApplyDamage(id, 4f));
            Assert.True(w.IsAlive(id));
            Assert.Equal(0, w.TotalKills);

            // ADR-008: the fatal hit breaks the chip and pays, but the body keeps coming until
            // its failure window runs out.
            Assert.True(w.ApplyDamage(id, 6f));
            Assert.True(w.IsFailing(id));
            Assert.Equal(1, w.AliveCount);
            Assert.Equal(1, w.TotalKills);

            for (int i = 0; i < 40; i++) w.Step(0.1f);
            Assert.False(w.IsAlive(id));
            Assert.Equal(0, w.AliveCount);
            Assert.Equal(1, w.TotalKills);

            Assert.False(w.ApplyDamage(id, 100f), "dead agents cannot be killed twice");
            Assert.Equal(1, w.TotalKills);
            Assert.False(w.ApplyDamage(999, 1f), "unknown ids are ignored");
        }

        [Fact]
        public void CountWithin_CountsEveryBodyStillStandingInsideRadius()
        {
            var w = MakeWorld();
            w.Spawn(new Vec2(5f, 5f), 10f);
            w.Spawn(new Vec2(5.5f, 5f), 10f);
            int failing = w.Spawn(new Vec2(5f, 5.5f), 10f);
            w.Spawn(new Vec2(9f, 5f), 10f);
            w.ApplyDamage(failing, 100f);

            // A failing body is still standing there, so it is still counted...
            Assert.Equal(3, w.CountWithin(new Vec2(5f, 5f), 1f));
            Assert.Equal(0, w.CountWithin(new Vec2(20f, 20f), 1f));

            // ...but the moment it was hit it stopped counting as a whole attacker, because
            // ThreatWithin sums what is left of each chip rather than heads. ADR-008.
            // (Stepping is deliberately left out: the agents would walk out of the radius and the
            // test would be measuring movement instead of threat. FailingChipTests owns decay.)
            Assert.Equal(3f, w.ThreatWithin(new Vec2(5f, 5f), 1f), 2);
            Assert.Equal(1f, w.ThreatScale(failing), 2);
        }

        [Fact]
        public void Queries_DoNotChangeStateHash()
        {
            var w = MakeWorld();
            for (int i = 0; i < 50; i++) w.Spawn(new Vec2(3f + i * 0.3f, 5f + (i % 3)), 10f);
            w.Step(1f / 30f);
            ulong before = w.StateHash();

            w.Raycast(new Vec2(0f, 5f), new Vec2(1f, 0.1f), 30f, 0.6f, out _, out _);
            w.CountWithin(new Vec2(6f, 6f), 3f);

            Assert.Equal(before, w.StateHash());
        }

        [Fact]
        public void Movement_SharedWallRule_BlocksCornerCuts_AndSlides()
        {
            var map = new GridMap(8, 8);
            map.SetBlocked(3, 2, true); // wall cell
            map.SetBlocked(2, 3, true); // diagonal partner: (2,2)->(3,3) must be sealed

            Assert.False(Movement.CanTravel(map, new Vec2(2.5f, 2.5f), new Vec2(3.5f, 3.5f)));
            Assert.True(Movement.CanTravel(map, new Vec2(2.5f, 2.5f), new Vec2(2.5f, 1.5f)));

            // Walking into a wall slides along it instead of stopping dead.
            Vec2 resolved = Movement.ResolveWalls(map, new Vec2(2.5f, 2.5f), new Vec2(3.5f, 2.2f));
            Assert.Equal(2.5f, resolved.X, 3);
            Assert.Equal(2.2f, resolved.Y, 3);

            Assert.False(Movement.IsOpen(map, new Vec2(-0.1f, 1f)));
            Assert.False(Movement.IsOpen(map, new Vec2(8f, 1f)));
        }
    }
}
