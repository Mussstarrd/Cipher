using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>The Cartel Gunship: a mobile defence that flies over the maze (arsenal-and-terrain section 1).</summary>
    public class PatrolDroneTests
    {
        private const float Dt = 1f / 30f;

        private static (GridMap map, AgentWorld world) World(int size = 40)
        {
            var map = new GridMap(size, size);
            var field = new FlowField(map);
            field.Compute(size - 2, size / 2);
            return (map, new AgentWorld(map, field, new SimConfig()));
        }

        private static readonly (int X, int Y)[] Square = { (10, 10), (20, 10), (20, 20), (10, 20) };

        [Fact]
        public void Route_MustHaveEnoughDistinctWaypoints()
        {
            var drones = new PatrolDroneSystem();
            Assert.Equal(-1, drones.Add(new[] { (5, 5) }));                                   // too few
            Assert.Equal(-1, drones.Add(new[] { (5, 5), (5, 5) }));                           // no span: it would hover
            Assert.Equal(-1, drones.Add(new[] { (1, 1), (2, 2), (3, 3), (4, 4), (5, 5) }));   // too many
            Assert.Equal(0, drones.Add(new[] { (5, 5), (9, 5) }));
            Assert.Single(drones.Drones);
        }

        [Fact]
        public void Drone_WalksItsLoop_AtTheConfiguredSpeed_AndReturnsToStart()
        {
            var (_, world) = World();
            var cfg = new PatrolDroneConfig();
            var drones = new PatrolDroneSystem(cfg);
            drones.Add(Square);
            var drone = drones.Drones[0];

            Assert.Equal(40f, drone.LoopLength, 2); // a 10x10 square
            Vec2 start = drone.Position;

            // A quarter of the loop puts it on the second corner (within one 0.2-cell step).
            for (int t = 0; t < 50; t++) drones.Step(world, Dt, null);
            Assert.InRange(drone.Position.X, 20.2f, 20.6f);
            Assert.InRange(drone.Position.Y, 10.3f, 10.7f);

            // Three more quarters bring it home.
            for (int t = 0; t < 150; t++) drones.Step(world, Dt, null);
            Assert.InRange(drone.Position.X, start.X - 0.3f, start.X + 0.3f);
            Assert.InRange(drone.Position.Y, start.Y - 0.3f, start.Y + 0.3f);
        }

        [Fact]
        public void Drone_FliesOverWalls()
        {
            var (map, world) = World();
            for (int y = 0; y < 40; y++) map.SetWall(15, y, WallKind.Wall, GridMap.DefaultWallHp);
            var drones = new PatrolDroneSystem();
            drones.Add(Square); // the square straddles the wall at x=15

            for (int t = 0; t < 300; t++) drones.Step(world, Dt, null);
            var d = drones.Drones[0];
            bool crossed = false;
            for (int t = 0; t < 300; t++)
            {
                drones.Step(world, Dt, null);
                if (d.Position.X > 16f) crossed = true;
            }
            Assert.True(crossed, "a gunship is not routed by walls; that is the whole point of it");
        }

        [Fact]
        public void Drone_ShootsTheNearestBody_NotTheOneNearestTheVault()
        {
            var (_, world) = World();
            var drones = new PatrolDroneSystem();
            drones.Add(new[] { (10, 10), (10, 20) });
            var d = drones.Drones[0];

            int nearDrone = world.Spawn(d.Position + new Vec2(1.5f, 0f), 100f);
            int nearVault = world.Spawn(d.Position + new Vec2(4.5f, 0f), 100f); // closer to the exit, further from the drone

            Assert.Equal(nearDrone, world.FindNearestInRange(d.Position, 6f));
            var shots = new List<TurretShot>();
            drones.Step(world, Dt, shots);
            Assert.Single(shots);
            Assert.True(world.HealthOf(nearDrone) < 100f);
            Assert.Equal(100f, world.HealthOf(nearVault), 3);
        }

        [Fact]
        public void Drone_DeliversItsDps_AndCountsKills()
        {
            var (_, world) = World();
            var cfg = new PatrolDroneConfig { Speed = 0.001f }; // effectively parked, so range never lapses
            var drones = new PatrolDroneSystem(cfg);
            drones.Add(new[] { (10, 10), (10, 14) });
            var d = drones.Drones[0];
            world.Spawn(d.Position + new Vec2(1f, 0f), 30f); // 6 shots at 5 damage

            int kills = 0;
            for (int t = 0; t < 30; t++) kills += drones.Step(world, Dt, null); // 1 s = 7 shots
            Assert.Equal(1, kills);
            Assert.Equal(1, drones.Drones[0].Kills);
            Assert.Equal(1, world.TotalKills);
        }

        [Fact]
        public void Drone_TakesDamage_AndIsRemovedWhenDestroyed()
        {
            var drones = new PatrolDroneSystem(new PatrolDroneConfig { MaxHp = 100 });
            drones.Add(Square);
            Assert.False(drones.Damage(0, 60));
            Assert.Equal(40, drones.Drones[0].Hp);
            Assert.True(drones.Damage(0, 60));
            Assert.Empty(drones.Drones);
        }

        [Fact]
        public void Drones_AreDeterministic_AcrossRuns()
        {
            ulong Run()
            {
                var (_, world) = World();
                var drones = new PatrolDroneSystem();
                drones.Add(Square);
                drones.Add(new[] { (25, 12), (33, 12), (29, 24) });
                for (int i = 0; i < 60; i++) world.Spawn(new Vec2(6f + (i % 8) * 0.6f, 8f + (i / 8) * 0.7f), 10f);
                for (int t = 0; t < 400; t++) { world.Step(Dt); drones.Step(world, Dt, null); }
                return world.StateHash() ^ drones.StateHash();
            }
            Assert.Equal(Run(), Run());
        }

        [Fact]
        public void PositionAt_IsPure_AndWrapsBothWays()
        {
            var drones = new PatrolDroneSystem();
            drones.Add(Square);
            var d = drones.Drones[0];
            Assert.Equal(d.PositionAt(0f).X, d.PositionAt(d.LoopLength).X, 3);
            Assert.Equal(d.PositionAt(5f).X, d.PositionAt(5f + d.LoopLength * 3f).X, 3);
            Assert.Equal(d.PositionAt(0f).X, d.PositionAt(-d.LoopLength).X, 3);
        }
    }
}
