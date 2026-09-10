#nullable enable
using System.Collections.Generic;
using Cipher.Game.Build;
using Cipher.Game.Hero;
using Cipher.Game.Match;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Emplacements;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// Headless replica of the bootstrap's fixed tick: director, world, turrets, events, drones.
    /// Guards the things only a full match shows: a sealed maze gets breached, Spitters hurt turrets.
    /// </summary>
    public sealed class MatchIntegrationTests
    {
        private const float Dt = 1f / 30f;
        private const int W = 64, H = 48, GoalX = 62, GoalY = 24;
        private static readonly (int X, int Y)[] Spawns = { (1, 4), (1, 14), (1, 24), (1, 34), (1, 44) };

        private sealed class Rig
        {
            public GridMap Map = null!;
            public FlowField Field = null!;
            public AgentWorld World = null!;
            public TurretSystem Turrets = null!;
            public MatchState Match = null!;
            public BuildModel Build = null!;
            public SpawnDirector Director = null!;
            public HeroModel Hero = null!;
            public List<SimEvent> Events = new List<SimEvent>();
            public int SappersSeen, SpittersSeen, BreachesOpened, StructureHits;
            public float Seconds;
            public int LastReached;
            public int SpawnCursor;

            public void Tick()
            {
                Seconds += Dt;
                int breached = World.ReachedCount - LastReached;
                int toSpawn = Match.Tick(Dt, World.AliveCount, breached);
                LastReached = World.ReachedCount;
                if (toSpawn > 0)
                {
                    bool sealedIn = false;
                    foreach (var s in Spawns) if (!Field.HasPath(s.X, s.Y)) { sealedIn = true; break; }
                    for (int i = 0; i < toSpawn; i++)
                    {
                        var (sx, sy) = Spawns[SpawnCursor % Spawns.Length];
                        SpawnCursor++;
                        var pos = new Vec2(sx + 0.5f + (SpawnCursor % 3) * 0.3f, sy + 0.5f + (SpawnCursor % 5) * 0.2f);
                        var view = new DirectorView(Seconds, World.ActiveSapperCount, World.ActiveBreachCount,
                                                    World.CountAlive(Archetype.Spitter), sealedIn, Turrets.Turrets.Count);
                        Archetype a = Director.Decide(view);
                        if (a == Archetype.Runner) World.Spawn(pos, 10f);
                        else { World.SpawnArchetype(pos, a); if (a == Archetype.Sapper) SappersSeen++; else SpittersSeen++; }
                    }
                }
                World.Step(Dt);
                Turrets.Step(World, Dt, null);
                Events.Clear();
                World.DrainEvents(Events);
                foreach (var e in Events)
                {
                    if (e.Kind == SimEventKind.BreachStage && e.A >= 0) BreachesOpened++;
                    if (e.Kind == SimEventKind.StructureHit && e.B < Turrets.Turrets.Count)
                    {
                        StructureHits++;
                        Turrets.Damage(Map, e.B, (int)e.F);
                    }
                }
                Build.TickDrones(Hero.Position, Dt);
            }
        }

        private static Rig Make(ulong seed, IReadOnlyList<WaveDef>? waves = null, int vaultHp = 25)
        {
            var r = new Rig { Map = new GridMap(W, H) };
            for (int y = 0; y < H - 10; y++) r.Map.SetWall(16, y, WallKind.Wall, GridMap.DefaultWallHp);
            for (int y = 10; y < H; y++) r.Map.SetWall(32, y, WallKind.Wall, GridMap.DefaultWallHp);
            for (int y = 0; y < H - 10; y++) r.Map.SetWall(48, y, WallKind.Wall, GridMap.DefaultWallHp);
            r.Field = new FlowField(r.Map);
            r.Field.Compute(GoalX, GoalY);
            r.World = new AgentWorld(r.Map, r.Field, new SimConfig(), 4096);
            r.Turrets = new TurretSystem(new TurretConfig());
            r.World.Structures = r.Turrets.AsStructureQuery();
            var eco = new EconomyConfig();
            r.Match = new MatchState(waves ?? WaveTable.Default, eco, vaultHp);
            r.Build = new BuildModel(r.Map, r.World, r.Turrets, r.Match, eco, Spawns, GoalX, GoalY, 50, 24);
            r.Director = new SpawnDirector(new DirectorConfig(), seed);
            r.Hero = new HeroModel(new HeroConfig(), new Vec2(58f, 24f));
            return r;
        }

        [Test]
        public void SealedMaze_GetsBreachedBySapper_AndTheHordeComesThrough()
        {
            var r = Make(seed: 11);
            // Seal the first serpentine gap completely (cells (16, 38..47) are the only way past wall 1).
            for (int y = H - 10; y < H; y++) r.Map.SetWall(16, y, WallKind.Barricade, GridMap.DefaultWallHp);
            r.Match.StartWaveNow();

            for (int t = 0; t < (int)(150f / Dt); t++)
            {
                r.Tick();
                if (r.Match.Phase == MatchPhase.Lost) break;
            }

            Assert.GreaterOrEqual(r.SappersSeen, 1, "a sealed spawn must force a Sapper");
            Assert.GreaterOrEqual(r.BreachesOpened, 1, "the Sapper must open a hole");
            Assert.Greater(r.World.ReachedCount, 0, "runners must eventually reach the vault through the hole");
        }

        [Test]
        public void Turrets_DrawSpitters_WhichDamageThem()
        {
            var r = Make(seed: 5);
            r.Build.SetCursor(30, 8); // just inside the first gap on the second lane
            r.Build.CycleItem(1);
            Assert.IsTrue(r.Build.TryPlace());
            r.Match.StartWaveNow();

            for (int t = 0; t < (int)(140f / Dt); t++)
            {
                r.Tick();
                if (r.Match.Phase == MatchPhase.Lost) break;
            }

            Assert.GreaterOrEqual(r.SpittersSeen, 1, "with a turret up, the director must send Spitters (first at 40 s)");
            Assert.GreaterOrEqual(r.StructureHits, 1, "a Spitter must reach range and hit the turret");
        }

        [Test]
        public void OpenMaze_FirstSapperIsScripted_AtForty5Seconds()
        {
            // One long wave so spawns are still happening at 45 s (wave 1 of the real table is done spawning by 19 s).
            var r = Make(seed: 3, waves: new[] { new WaveDef(2000, 8f, 0f) }, vaultHp: 100000); // nobody is defending; do not lose first
            r.Match.StartWaveNow();
            for (int t = 0; t < (int)(44f / Dt); t++) r.Tick();
            Assert.AreEqual(0, r.SappersSeen, "nothing before 45 s in an open maze");
            for (int t = 0; t < (int)(16f / Dt); t++) r.Tick();
            Assert.AreEqual(1, r.SappersSeen);
        }
    }
}
