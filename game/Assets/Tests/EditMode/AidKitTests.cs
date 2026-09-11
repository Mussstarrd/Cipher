#nullable enable
using Cipher.Game.Match;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>Something to walk to. The roadmap's one permitted exception to the systems freeze.</summary>
    public sealed class AidKitTests
    {
        private static AidKits Kits(params Vec2[] spots) => new AidKits(new GridMap(32, 32), spots, 7UL);

        [Test]
        public void OpeningKitsLandOnTheAuthoredSpots()
        {
            var kits = Kits(new Vec2(5.5f, 5.5f), new Vec2(20.5f, 20.5f), new Vec2(10.5f, 25.5f));
            kits.PlaceOpening();
            Assert.That(kits.Kits.Count, Is.EqualTo(3));
            foreach (var k in kits.Kits)
                Assert.That(k.X == 5.5f || k.X == 20.5f || k.X == 10.5f, Is.True, "kits go where the cars are");
        }

        [Test]
        public void AMissionWithNoSpotsStillGetsKits()
        {
            var kits = Kits();
            kits.PlaceOpening();
            Assert.That(kits.Kits.Count, Is.EqualTo(3), "a mission with no vehicles still has healing in it");
        }

        [Test]
        public void StandingOnAKitHealsAndConsumesIt()
        {
            var kits = Kits(new Vec2(5.5f, 5.5f));
            kits.OpeningKits = 1;
            kits.PlaceOpening();

            float healed = kits.Tick(0.1f, new Vec2(5.5f, 5.5f), health: 40f, maxHealth: 100f, heroDown: false, out bool took);
            Assert.That(took, Is.True);
            Assert.That(healed, Is.EqualTo(45f).Within(0.5f));
            Assert.That(kits.Kits.Count, Is.Zero);
        }

        [Test]
        public void AKitIsNotWastedAtFullHealth()
        {
            var kits = Kits(new Vec2(5.5f, 5.5f));
            kits.OpeningKits = 1;
            kits.PlaceOpening();

            kits.Tick(0.1f, new Vec2(5.5f, 5.5f), 100f, 100f, false, out bool took);
            Assert.That(took, Is.False, "walking over a kit at full health should leave it for later");
            Assert.That(kits.Kits.Count, Is.EqualTo(1));
        }

        [Test]
        public void RegenWaitsForAQuietSpellAndStopsWhenHurt()
        {
            var kits = Kits();
            kits.RegenDelay = 2f;
            kits.RegenPerSecond = 10f;

            float total = 0f;
            for (int i = 0; i < 10; i++) total += kits.Tick(0.1f, Vec2.Zero, 50f, 100f, false, out _);
            Assert.That(total, Is.Zero, "no regen inside the delay");

            for (int i = 0; i < 20; i++) total += kits.Tick(0.1f, Vec2.Zero, 50f, 100f, false, out _);
            Assert.That(total, Is.GreaterThan(0f), "regen once it has been quiet long enough");

            // Getting bitten resets the clock.
            float after = kits.Tick(0.1f, Vec2.Zero, 30f, 100f, false, out _);
            Assert.That(after, Is.Zero);
        }

        [Test]
        public void EachClearedWaveAddsAKit()
        {
            var kits = Kits();
            kits.OpeningKits = 0;
            kits.PlaceOpening();
            kits.OnWaveCleared();
            kits.OnWaveCleared();
            Assert.That(kits.Kits.Count, Is.EqualTo(2));
        }
    }
}
