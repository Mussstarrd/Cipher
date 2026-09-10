#nullable enable
using Cipher.Game.Hero;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    public sealed class HeroModelTests
    {
        private static (GridMap map, AgentWorld world) World(int size = 32)
        {
            var map = new GridMap(size, size);
            var field = new FlowField(map);
            field.Compute(size - 1, size / 2);
            return (map, new AgentWorld(map, field, new SimConfig()));
        }

        [Test]
        public void Fire_RespectsCooldown_AndKillsInTwoTaps()
        {
            var (_, world) = World();
            var hero = new HeroModel(new HeroConfig(), new Vec2(5f, 5f));
            int target = world.Spawn(new Vec2(12f, 5f), 10f);

            Assert.IsTrue(hero.TryFire(world, 0f, out var s1));
            Assert.IsTrue(s1.Hit);
            Assert.AreEqual(target, s1.HitId);
            Assert.IsFalse(s1.Killed);
            Assert.IsFalse(hero.TryFire(world, 0f, out _), "second shot in the same instant must be blocked by cooldown");

            hero.Tick(1f);
            Assert.IsTrue(hero.TryFire(world, 0f, out var s2));
            Assert.IsTrue(s2.Killed);
            Assert.AreEqual(1, hero.Kills);
            Assert.AreEqual(2, hero.ShotsFired);
            Assert.IsFalse(world.IsAlive(target));
        }

        [Test]
        public void Fire_MissEndsAtRange_AndSpreadRotatesTheRay()
        {
            var (_, world) = World();
            var cfg = new HeroConfig();
            var hero = new HeroModel(cfg, new Vec2(5f, 5f));
            world.Spawn(new Vec2(12f, 5f), 10f); // dead ahead

            Assert.IsTrue(hero.TryFire(world, 30f, out var shot), "30 degrees of spread must miss a target dead ahead");
            Assert.IsFalse(shot.Hit);
            Assert.AreEqual(cfg.GunRange, (shot.End - shot.Origin).Length, 1e-3f);
            Assert.Greater(shot.End.Y, shot.Origin.Y, "positive spread rotates counter-clockwise");
        }

        [Test]
        public void Aim_IgnoresZeroInput_KeepsLastFacing()
        {
            var hero = new HeroModel(new HeroConfig(), new Vec2(5f, 5f));
            hero.Aim(new Vec2(0f, 2f));
            Assert.AreEqual(0f, hero.Facing.X, 1e-5f);
            Assert.AreEqual(1f, hero.Facing.Y, 1e-5f);
            hero.Aim(Vec2.Zero);
            Assert.AreEqual(1f, hero.Facing.Y, 1e-5f);
        }

        [Test]
        public void Move_UsesTheSwarmWallRule_AndCapsPerCallStep()
        {
            var (map, _) = World();
            map.SetBlocked(8, 5, true);
            var hero = new HeroModel(new HeroConfig { MoveSpeed = 100f }, new Vec2(7.5f, 5.5f));

            hero.Move(map, new Vec2(1f, 0f), 1f);
            Assert.AreEqual(7.5f, hero.Position.X, 1e-4f, "wall directly east must stop the hero");

            hero.Move(map, new Vec2(0f, 1f), 1f);
            Assert.AreEqual(6.4f, hero.Position.Y, 1e-4f, "step is capped at 0.9 cells per call");
        }

        [Test]
        public void Contact_DrainsHealth_CapsAtAgentCap_AndDowns()
        {
            var (_, world) = World();
            var cfg = new HeroConfig { ContactAgentCap = 2, ContactDamagePerAgentPerSecond = 10f };
            var hero = new HeroModel(cfg, new Vec2(5f, 5f));
            for (int i = 0; i < 5; i++) world.Spawn(new Vec2(5.1f + i * 0.05f, 5f), 10f);

            float dmg = hero.ApplyContact(world, 0.5f);
            Assert.AreEqual(10f, dmg, 1e-4f, "2 agents (cap) x 10/s x 0.5s");
            Assert.AreEqual(90f, hero.Health, 1e-4f);

            hero.ApplyContact(world, 100f);
            Assert.IsTrue(hero.IsDown);
            Assert.AreEqual(0f, hero.Health);
            Assert.IsFalse(hero.TryFire(world, 0f, out _), "a downed hero cannot shoot");
            hero.Move(new GridMap(32, 32), new Vec2(1f, 0f), 1f);
            Assert.AreEqual(5f, hero.Position.X, 1e-5f, "a downed hero cannot move");
        }

        [Test]
        public void Airstrike_LandsAheadOnFacing_ThenCoolsDown()
        {
            var (_, world) = World();
            var cfg = new HeroConfig();
            var hero = new HeroModel(cfg, new Vec2(5f, 5f));
            hero.Aim(new Vec2(1f, 0f));
            int inBlast = world.Spawn(new Vec2(5f + cfg.AirstrikeLead, 5f), 10f);
            int outside = world.Spawn(new Vec2(5f + cfg.AirstrikeLead + cfg.AirstrikeRadius + 1f, 5f), 10f);

            Assert.IsTrue(hero.AirstrikeReady);
            Assert.IsTrue(hero.TryAirstrike(world, out var center, out int kills));
            Assert.AreEqual(1, kills);
            Assert.AreEqual(5f + cfg.AirstrikeLead, center.X, 1e-4f);
            Assert.IsFalse(world.IsAlive(inBlast));
            Assert.IsTrue(world.IsAlive(outside));
            Assert.IsFalse(hero.AirstrikeReady);
            Assert.IsFalse(hero.TryAirstrike(world, out _, out _));

            hero.Tick(cfg.AirstrikeCooldown);
            Assert.IsTrue(hero.AirstrikeReady);
            Assert.AreEqual(1f, hero.AirstrikeReadyFraction, 1e-5f);
        }

        [Test]
        public void Respawn_RestoresEverything()
        {
            var (_, world) = World();
            var hero = new HeroModel(new HeroConfig(), new Vec2(5f, 5f));
            hero.TryFire(world, 0f, out _);
            hero.TryAirstrike(world, out _, out _);
            for (int i = 0; i < 3; i++) world.Spawn(new Vec2(5f, 5f), 10f);
            hero.ApplyContact(world, 1000f);
            Assert.IsTrue(hero.IsDown);

            hero.Respawn(new Vec2(1f, 1f));
            Assert.IsFalse(hero.IsDown);
            Assert.AreEqual(1f, hero.HealthFraction, 1e-5f);
            Assert.AreEqual(1f, hero.Position.X, 1e-5f);
            Assert.IsTrue(hero.AirstrikeReady);
            Assert.AreEqual(0f, hero.FireCooldown);
        }
    }
}
