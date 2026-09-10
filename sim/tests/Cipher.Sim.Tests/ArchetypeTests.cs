using System.Collections.Generic;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using Xunit;

namespace Cipher.Sim.Tests
{
    /// <summary>Sapper (breacher) and Spitter (structure hunter): docs/design/breach-and-repair.md, economy memo C.</summary>
    public class ArchetypeTests
    {
        private const float Dt = 1f / 30f;

        /// <summary>30x21, wall column at x=15 open only at the top row; goal bottom-right. Breaching at row 19 saves ~22 cells.</summary>
        private static (GridMap map, FlowField field, AgentWorld world) Serpentine(bool sealedTop = false)
        {
            var map = new GridMap(30, 21);
            for (int y = sealedTop ? 0 : 1; y < 21; y++) map.SetWall(15, y, WallKind.Barricade, GridMap.DefaultWallHp);
            var field = new FlowField(map);
            field.Compute(28, 19);
            var world = new AgentWorld(map, field, new SimConfig(), initialCapacity: 64);
            return (map, field, world);
        }

        private static void Run(AgentWorld world, float seconds)
        {
            for (int t = 0; t < (int)(seconds / Dt); t++) world.Step(Dt);
        }

        [Fact]
        public void Sapper_TargetsTheWallThatShortcutsMost_AndReportsIt()
        {
            var (map, _, world) = Serpentine();
            int id = world.SpawnArchetype(new Vec2(2.5f, 19.5f), Archetype.Sapper);

            int target = world.SapperTargetCell(id);
            Assert.Equal(map.CellIndex(15, 19), target);

            var events = new List<SimEvent>();
            world.DrainEvents(events);
            Assert.Contains(events, e => e.Kind == SimEventKind.SapperTargeted && e.A == id && e.B == target);
        }

        [Fact]
        public void Sapper_WalksToWall_Plants_OpensCrackedHole_ThenJoinsTheFlood()
        {
            var (map, _, world) = Serpentine();
            int id = world.SpawnArchetype(new Vec2(2.5f, 19.5f), Archetype.Sapper);

            Run(world, 4f); // 12 cells at 2.4/s = 5 s: still walking
            Assert.Equal(BreachStage.Intact, map.StageAt(15, 19));
            Assert.Equal(Archetype.Sapper, world.ArchetypeOf(id));

            Run(world, 7f); // arrived (~5.5 s) + 4 s plant
            Assert.Equal(BreachStage.Cracked, map.StageAt(15, 19));
            Assert.Equal(1, world.ActiveBreachCount);
            Assert.Equal(Archetype.Runner, world.ArchetypeOf(id));
            Assert.True(world.IsAlive(id));

            var events = new List<SimEvent>();
            world.DrainEvents(events);
            Assert.Contains(events, e => e.Kind == SimEventKind.BreachPlanting && e.B == map.CellIndex(15, 19));
            Assert.Contains(events, e => e.Kind == SimEventKind.BreachStage && (BreachStage)(int)e.F == BreachStage.Cracked);
        }

        [Fact]
        public void Sapper_KilledWhilePlanting_LeavesWallIntact()
        {
            var (map, _, world) = Serpentine();
            int id = world.SpawnArchetype(new Vec2(13.5f, 19.5f), Archetype.Sapper); // right next to the wall
            Run(world, 1.5f); // arrived, planting (4 s)
            Assert.Equal(BreachStage.Intact, map.StageAt(15, 19));
            Assert.True(world.ApplyDamage(id, 1000f));
            Run(world, 10f);
            Assert.Equal(BreachStage.Intact, map.StageAt(15, 19));
            Assert.Equal(0, world.ActiveBreachCount);
        }

        [Fact]
        public void Breach_AdvancesOnTimer_ThenCollapses_AndRepairClosesIt()
        {
            var (map, _, world) = Serpentine();
            var cfg = new SimConfig();
            world.SpawnArchetype(new Vec2(13.5f, 19.5f), Archetype.Sapper);
            Run(world, 6f); // planted → Cracked
            Assert.Equal(BreachStage.Cracked, map.StageAt(15, 19));
            // ~1.6 s elapsed since the hole opened, plus the Sapper itself walking through it shaves TrafficShaveSeconds.
            Assert.InRange(world.BreachTimerAt(15, 19), cfg.BreachStageSeconds - 2.5f, cfg.BreachStageSeconds);

            Run(world, cfg.BreachStageSeconds);
            Assert.Equal(BreachStage.Broken, map.StageAt(15, 19));
            Run(world, cfg.BreachStageSeconds);
            Assert.Equal(BreachStage.Collapsed, map.StageAt(15, 19));
            Assert.Equal(0, world.ActiveBreachCount);
            Assert.False(map.IsBlocked(15, 19));

            Assert.Equal(BreachStage.Broken, world.RepairStage(15, 19));
            Assert.Equal(BreachStage.Cracked, world.RepairStage(15, 19));
            Assert.Equal(BreachStage.Intact, world.RepairStage(15, 19));
            Assert.True(map.IsBlocked(15, 19));
            Assert.Equal(GridMap.DefaultWallHp, map.HpAt(15, 19));
            Assert.Equal(-1f, world.BreachTimerAt(15, 19));

            var events = new List<SimEvent>();
            world.DrainEvents(events);
            Assert.Contains(events, e => e.Kind == SimEventKind.BreachCollapsed);
            Assert.Contains(events, e => e.Kind == SimEventKind.BreachRepaired);
        }

        [Fact]
        public void Traffic_ThroughTheHole_ShortensTheStage()
        {
            var (map, _, world) = Serpentine();
            world.SpawnArchetype(new Vec2(13.5f, 19.5f), Archetype.Sapper);
            Run(world, 6f);
            Assert.Equal(BreachStage.Cracked, map.StageAt(15, 19));
            float before = world.BreachTimerAt(15, 19);
            for (int i = 0; i < 20; i++) world.Spawn(new Vec2(13.2f + (i % 4) * 0.3f, 18.6f + (i / 4) * 0.4f), 10f);
            Run(world, 5f); // ~5 agents admitted at 1/s, each shaving 0.5 s
            float after = world.BreachTimerAt(15, 19);
            Assert.True(after < before - 5f - 1.5f, $"expected traffic to shave extra time: {before} → {after}");
        }

        [Fact]
        public void Sapper_SealedIn_TargetsAWallAnyway()
        {
            var (map, _, world) = Serpentine(sealedTop: true);
            int id = world.SpawnArchetype(new Vec2(2.5f, 19.5f), Archetype.Sapper);
            Assert.NotEqual(-1, world.SapperTargetCell(id));
            Assert.Equal(map.CellIndex(15, 19), world.SapperTargetCell(id));
        }

        [Fact]
        public void Sapper_IgnoresShortcutsBelowMinGain_AndWalksLikeARunner()
        {
            var map = new GridMap(30, 9);
            map.SetWall(15, 7, WallKind.Barricade, GridMap.DefaultWallHp); // a single post: going around costs ~2 cells
            var field = new FlowField(map);
            field.Compute(28, 7);
            var world = new AgentWorld(map, field, new SimConfig());
            int id = world.SpawnArchetype(new Vec2(2.5f, 7.5f), Archetype.Sapper);
            Assert.Equal(-1, world.SapperTargetCell(id));
            float x0 = world.PositionOf(id).X;
            Run(world, 2f);
            Assert.True(world.PositionOf(id).X > x0 + 3f, "no plan: it follows the field toward the goal");
        }

        [Fact]
        public void Sapper_Retargets_WhenItsWallIsRemoved()
        {
            var (map, _, world) = Serpentine();
            int id = world.SpawnArchetype(new Vec2(2.5f, 19.5f), Archetype.Sapper);
            Assert.Equal(map.CellIndex(15, 19), world.SapperTargetCell(id));
            map.Clear(15, 19); // player sells it; the wall next to it is now the best shortcut
            Run(world, Dt);
            int t = world.SapperTargetCell(id);
            Assert.True(t == -1 || t != map.CellIndex(15, 19), "must not keep aiming at an open cell");
            Assert.False(map.IsBlocked(15, 19));
        }

        private sealed class OneTurret : IStructureQuery
        {
            public List<Vec2> Positions = new List<Vec2>();
            public int Count => Positions.Count;
            public Vec2 PositionAt(int index) => Positions[index];
        }

        [Fact]
        public void Spitter_ApproachesToRange_ThenHitsEveryInterval()
        {
            var map = new GridMap(40, 9);
            var field = new FlowField(map);
            field.Compute(38, 4);
            var cfg = new SimConfig();
            var world = new AgentWorld(map, field, cfg);
            var turrets = new OneTurret();
            turrets.Positions.Add(new Vec2(20.5f, 4.5f));
            world.Structures = turrets;

            int id = world.SpawnArchetype(new Vec2(9.5f, 4.5f), Archetype.Spitter); // 11 cells: inside acquire (12), outside attack (9)
            var events = new List<SimEvent>();
            Run(world, 1.5f);
            world.DrainEvents(events);
            Assert.Contains(events, e => e.Kind == SimEventKind.SpitterEngaged && e.B == 0);
            float dist = (world.PositionOf(id) - turrets.Positions[0]).Length;
            Assert.InRange(dist, cfg.SpitterAttackRange - 1.2f, cfg.SpitterAttackRange + 0.1f);

            events.Clear();
            Run(world, 3f);
            world.DrainEvents(events);
            int hits = events.FindAll(e => e.Kind == SimEventKind.StructureHit).Count;
            Assert.InRange(hits, 2, 3);
            Assert.All(events.FindAll(e => e.Kind == SimEventKind.StructureHit), e => Assert.Equal(cfg.SpitterDamage, e.F));
            Assert.InRange((world.PositionOf(id) - turrets.Positions[0]).Length, 0f, cfg.SpitterAttackRange + 0.1f);
        }

        [Fact]
        public void Spitter_FollowsTheField_WhenNothingIsInRange()
        {
            var map = new GridMap(40, 9);
            var field = new FlowField(map);
            field.Compute(38, 4);
            var world = new AgentWorld(map, field, new SimConfig());
            world.Structures = new OneTurret(); // empty
            int id = world.SpawnArchetype(new Vec2(2.5f, 4.5f), Archetype.Spitter);
            Run(world, 2f);
            Assert.True(world.PositionOf(id).X > 5f);
        }

        [Fact]
        public void Archetypes_AreDeterministic_AcrossRuns()
        {
            ulong Run1()
            {
                var (map, _, world) = Serpentine();
                var turrets = new OneTurret();
                turrets.Positions.Add(new Vec2(20.5f, 3.5f));
                world.Structures = turrets;
                world.SpawnArchetype(new Vec2(2.5f, 19.5f), Archetype.Sapper);
                world.SpawnArchetype(new Vec2(2.5f, 2.5f), Archetype.Spitter);
                for (int i = 0; i < 40; i++) world.Spawn(new Vec2(1.5f + (i % 5) * 0.5f, 1f + (i / 5) * 0.8f), 10f);
                Run(world, 30f);
                return world.StateHash();
            }
            Assert.Equal(Run1(), Run1());
        }
    }
}
