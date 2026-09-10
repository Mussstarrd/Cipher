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
        public void AimStrike_ClampsToRangeBand_AndSetsAxis()
        {
            var cfg = new HeroConfig();
            var hero = new HeroModel(cfg, new Vec2(10f, 10f));

            hero.AimStrike(new Vec2(11f, 10f)); // 1 cell away: too close
            Assert.AreEqual(10f + cfg.AirstrikeMinRange, hero.StrikeTarget.X, 1e-4f);

            hero.AimStrike(new Vec2(10f, 100f)); // 90 cells away: too far
            Assert.AreEqual(10f + cfg.AirstrikeMaxRange, hero.StrikeTarget.Y, 1e-4f);
            Assert.AreEqual(0f, hero.StrikeAxis.X, 1e-5f);
            Assert.AreEqual(1f, hero.StrikeAxis.Y, 1e-5f);

            hero.AimStrike(new Vec2(22f, 10f)); // inside the band: exact
            Assert.AreEqual(22f, hero.StrikeTarget.X, 1e-4f);
        }

        [Test]
        public void Strike_WaitsForInboundDelay_ThenWalksBombsFarToNear()
        {
            var (_, world) = World(64);
            var cfg = new HeroConfig();
            var hero = new HeroModel(cfg, new Vec2(10f, 30f));
            hero.AimStrike(new Vec2(30f, 30f)); // centre at x=30, line spans x=23..37
            int farAgent = world.Spawn(new Vec2(37f, 30f), 10f);
            int nearAgent = world.Spawn(new Vec2(23f, 30f), 10f);
            int offLine = world.Spawn(new Vec2(30f, 34f), 10f); // 4 cells off-axis, radius is 2

            Assert.IsTrue(hero.TryAirstrike());
            Assert.IsTrue(hero.StrikeInbound);
            Assert.IsFalse(hero.AirstrikeReady, "cooldown starts at the call");

            var impacts = new System.Collections.Generic.List<StrikeImpact>();
            Assert.AreEqual(0, hero.TickStrike(world, cfg.AirstrikeInboundDelay - 0.01f, impacts), "nothing lands before the delay");
            Assert.IsTrue(world.IsAlive(farAgent));

            Assert.AreEqual(1, hero.TickStrike(world, 0.02f, impacts), "first bomb lands at the far end");
            Assert.IsFalse(world.IsAlive(farAgent));
            Assert.IsTrue(world.IsAlive(nearAgent));
            Assert.AreEqual(1, impacts.Count);
            Assert.AreEqual(37f, impacts[0].Center.X, 1e-3f);

            hero.TickStrike(world, cfg.AirstrikeBombInterval * (cfg.AirstrikeBombCount + 1), impacts);
            Assert.IsFalse(hero.StrikeInbound);
            Assert.AreEqual(cfg.AirstrikeBombCount, impacts.Count);
            Assert.IsFalse(world.IsAlive(nearAgent), "last bomb lands at the near end");
            Assert.IsTrue(world.IsAlive(offLine), "the line is only 4 cells wide");
            Assert.AreEqual(2, hero.Kills);
            Assert.AreEqual(23f, impacts[impacts.Count - 1].Center.X, 1e-3f);
        }

        [Test]
        public void Strike_CannotDoubleCall_AndCoolsDown_AndHurtsHeroStandingInIt()
        {
            var (_, world) = World(64);
            var cfg = new HeroConfig();
            var hero = new HeroModel(cfg, new Vec2(10f, 30f));
            hero.AimStrike(new Vec2(16f, 30f)); // min range: line spans x=9..23, hero at 10 is inside

            Assert.IsTrue(hero.TryAirstrike());
            Assert.IsFalse(hero.TryAirstrike(), "no second call while one is inbound");
            hero.TickStrike(world, 10f, null);
            Assert.IsFalse(hero.StrikeInbound);
            Assert.Less(hero.Health, cfg.MaxHealth, "standing in your own strike hurts");
            Assert.IsFalse(hero.AirstrikeReady);

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
            hero.TryAirstrike();
            for (int i = 0; i < 3; i++) world.Spawn(new Vec2(5f, 5f), 10f);
            hero.ApplyContact(world, 1000f);
            Assert.IsTrue(hero.IsDown);

            hero.Respawn(new Vec2(1f, 1f));
            Assert.IsFalse(hero.IsDown);
            Assert.AreEqual(1f, hero.HealthFraction, 1e-5f);
            Assert.AreEqual(1f, hero.Position.X, 1e-5f);
            Assert.IsTrue(hero.AirstrikeReady);
            Assert.IsFalse(hero.StrikeInbound, "respawn cancels an inbound strike");
            Assert.AreEqual(0f, hero.FireCooldown);
        }
    }
}
