#nullable enable
using Cipher.Game.Hero;
using Cipher.Game.Match;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    public sealed class DirectorTests
    {
        private static DirectorView View(float t, int sappers = 0, int breaches = 0, int spitters = 0, bool sealedIn = false, int turrets = 1)
            => new DirectorView(t, sappers, breaches, spitters, sealedIn, turrets);

        [Test]
        public void Sapper_FirstAppearanceIsScripted_ThenSpaced_AndCapped()
        {
            var d = new SpawnDirector(new DirectorConfig(), seed: 7);
            for (int i = 0; i < 200; i++) Assert.AreEqual(Archetype.Runner, d.Decide(View(10f, turrets: 0)), "nothing rare before the first-at times");

            Assert.AreEqual(Archetype.Sapper, d.Decide(View(45f, turrets: 0)));
            Assert.AreEqual(1, d.SappersSpawned);
            Assert.AreEqual(Archetype.Runner, d.Decide(View(46f, sappers: 1, turrets: 0)), "cap: one alive");
            for (int i = 0; i < 500; i++) Assert.AreEqual(Archetype.Runner, d.Decide(View(80f, breaches: 1, turrets: 0)), "cap: one active breach");
            for (int i = 0; i < 500; i++) Assert.AreEqual(Archetype.Runner, d.Decide(View(100f, turrets: 0)), "spacing: 60 s since the last");

            int sappers = 0;
            for (int i = 0; i < 4000; i++) if (d.Decide(View(200f, turrets: 0)) == Archetype.Sapper) sappers++;
            Assert.AreEqual(1, sappers, "after spacing, the 0.5% roll fires and then spacing blocks again at the same timestamp");
        }

        [Test]
        public void Sapper_IsForcedWhenASpawnIsSealed()
        {
            var d = new SpawnDirector(new DirectorConfig(), seed: 1);
            Assert.AreEqual(Archetype.Sapper, d.Decide(View(5f, sealedIn: true, turrets: 0)), "sealed at 5 s: forced even before the scripted time");
            Assert.AreEqual(Archetype.Runner, d.Decide(View(10f, sealedIn: true, turrets: 0)), "forced sappers are spaced 20 s");
            Assert.AreEqual(Archetype.Sapper, d.Decide(View(26f, sealedIn: true, turrets: 0)));
        }

        [Test]
        public void Spitter_NeedsATurret_HasPity_AndCap()
        {
            var d2 = new SpawnDirector(new DirectorConfig { SapperFirstAt = 9999f }, seed: 3);
            Assert.AreEqual(Archetype.Runner, d2.Decide(View(50f, turrets: 0)));
            Assert.AreEqual(Archetype.Spitter, d2.Decide(View(50f, turrets: 1)), "first spitter is scripted at 40 s");
            Assert.AreEqual(Archetype.Runner, d2.Decide(View(50f, spitters: 3)), "cap of 3 alive");
            Assert.AreEqual(Archetype.Spitter, d2.Decide(View(96f)), "pity: 45 s without one forces one");
        }

        [Test]
        public void Director_IsReplayable_WithTheSameSeed()
        {
            int Hash(ulong seed)
            {
                // Short spacing so the 0.5% rolls actually get consulted many times.
                var d = new SpawnDirector(new DirectorConfig { SapperFirstAt = 0f, SpitterFirstAt = 0f, SapperSpacing = 1f, SpitterPity = 9999f }, seed);
                int h = 17;
                for (int i = 0; i < 3000; i++) h = h * 31 + (int)d.Decide(View(200f + i * 2f, turrets: 1));
                return h;
            }
            Assert.AreEqual(Hash(42), Hash(42));
            Assert.AreNotEqual(Hash(42), Hash(43));
        }

        [Test]
        public void RepairDrone_RepairsOneStagePer4s_OnlyWithHeroNearby_ThenFinishes()
        {
            var map = new GridMap(20, 5);
            for (int y = 0; y < 5; y++) map.SetWall(10, y, WallKind.Barricade, GridMap.DefaultWallHp);
            var field = new FlowField(map);
            field.Compute(18, 2);
            var world = new AgentWorld(map, field, new SimConfig());
            map.Breach(10, 2); map.Breach(10, 2); // Broken
            var drone = new RepairDrone(10, 2);

            Assert.AreEqual(0, drone.Tick(world, new Vec2(1f, 1f), 10f), "hero far away: no progress");
            Assert.IsFalse(drone.HeroInRange);
            Assert.AreEqual(BreachStage.Broken, map.StageAt(10, 2));

            Assert.AreEqual(0, drone.Tick(world, new Vec2(9f, 2f), 3.9f));
            Assert.AreEqual(1, drone.Tick(world, new Vec2(9f, 2f), 0.2f));
            Assert.AreEqual(BreachStage.Cracked, map.StageAt(10, 2));
            Assert.IsFalse(drone.Done);

            Assert.AreEqual(1, drone.Tick(world, new Vec2(9f, 2f), 4f));
            Assert.AreEqual(BreachStage.Intact, map.StageAt(10, 2));
            Assert.IsTrue(drone.Done);
            Assert.AreEqual(0, drone.Tick(world, new Vec2(9f, 2f), 4f));
        }

        [Test]
        public void GunTiers_ApplyToConfig_AndCratesUpgradeOnPickup()
        {
            var map = new GridMap(64, 48);
            var cfg = new HeroConfig();
            var pickups = new PickupSystem(map, seed: 5, minX: 34, maxX: 60);

            Assert.IsFalse(pickups.CrateActive);
            pickups.Tick(10f, 0.1f, new Vec2(0f, 0f), cfg);
            Assert.IsFalse(pickups.CrateActive, "first crate at 45 s");
            pickups.Tick(45f, 0.1f, new Vec2(0f, 0f), cfg);
            Assert.IsTrue(pickups.CrateActive);
            Assert.GreaterOrEqual(pickups.CratePosition.X, 34f);
            Assert.LessOrEqual(pickups.CratePosition.X, 61f);

            Assert.IsTrue(pickups.Tick(46f, 0.1f, pickups.CratePosition, cfg));
            Assert.AreEqual(1, pickups.GunTier);
            Assert.AreEqual("Decryptor", pickups.GunName);
            Assert.AreEqual(9f, cfg.GunDamage);
            Assert.IsFalse(pickups.CrateActive);

            // Crates expire if ignored.
            pickups.Tick(90f, 0.1f, new Vec2(0f, 0f), cfg);
            Assert.IsTrue(pickups.CrateActive);
            pickups.Tick(120f, 31f, new Vec2(0f, 0f), cfg);
            Assert.IsFalse(pickups.CrateActive);

            GunTiers.Apply(cfg, 3);
            Assert.AreEqual(12f, cfg.GunDamage);
            Assert.AreEqual(1f / 16f, cfg.FireInterval, 1e-6f);
            Assert.AreEqual(0.6f, cfg.GunHitRadius, 1e-6f);
        }
    }
}
