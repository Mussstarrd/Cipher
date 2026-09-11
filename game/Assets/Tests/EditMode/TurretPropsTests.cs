#nullable enable
using Cipher.Game;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The emplacements' DESIGN rules, as opposed to their geometry.
    ///
    /// Building one needs a runtime (primitives, a material factory, the AO bake), so what is
    /// tested here is the handful of decisions that a screenshot cannot police and that a later
    /// detailing pass would erode without anybody noticing: that the two families stay opposite
    /// shapes, that tier is visible as hardware rather than only as scale, and that damage takes
    /// pieces off in an order that looks like damage.
    ///
    /// The owner's complaint on 2026-09-11 was "the turrets look very generic and fake". The fix
    /// was silhouette, and silhouette is exactly the property that dies quietly when somebody
    /// later nudges a dish a little wider or a skid a little narrower.
    /// </summary>
    public sealed class TurretPropsTests
    {
        [Test]
        public void TheTwoFamiliesAreOppositeShapes()
        {
            // A player has to tell a single-target gun from an area one across a golf course, and
            // at that distance the ink outline is ALL he gets. Tall-and-narrow against wide-and-low
            // is the contrast; anything subtler is a colour difference and colours grey out in fog.
            Assert.That(TurretProps.SentryHeight, Is.GreaterThan(TurretProps.BrushHogHeight * 1.4f),
                        "the sentry has stopped being the tall one");
            Assert.That(TurretProps.BrushHogSpan, Is.GreaterThan(TurretProps.SentrySpan * 1.4f),
                        "the Brush Hog has stopped being the wide one");
            Assert.That(TurretProps.SentryHeight, Is.GreaterThan(TurretProps.SentrySpan),
                        "a sentry wider than it is tall reads as an area weapon");
            Assert.That(TurretProps.BrushHogSpan, Is.GreaterThan(TurretProps.BrushHogHeight),
                        "a Brush Hog taller than it is wide reads as a single-target weapon");
        }

        [Test]
        public void ATierBuysVisibleHardware()
        {
            // An upgrade the player cannot see is an upgrade he stops buying. The bootstrap's 10%
            // per tier scale is not a cue -- nobody can judge the size of a lone turret.
            Assert.That(TurretProps.PartsForTier(0), Is.EqualTo(TurretProps.BaseHornCount));
            Assert.That(TurretProps.PartsForTier(1), Is.GreaterThan(TurretProps.PartsForTier(0)));
            Assert.That(TurretProps.PartsForTier(2), Is.GreaterThan(TurretProps.PartsForTier(1)));
        }

        [Test]
        public void TierNeverAsksForHardwareThatWasNotBuilt()
        {
            // The parts are built once and shown or hidden. A tier beyond what exists must clamp
            // rather than index off the end of the collar.
            for (int tier = 0; tier < 12; tier++)
            {
                Assert.That(TurretProps.PartsForTier(tier),
                            Is.InRange(TurretProps.BaseHornCount, TurretProps.MaxHornCount));
            }
            Assert.That(TurretProps.PartsForTier(-3), Is.EqualTo(TurretProps.BaseHornCount));
        }

        [Test]
        public void TheCheapestBrushHogIsStillACompleteRing()
        {
            // The bug this prevents: numbering horns 0..5 round a twelve-station collar, which
            // gives a tier-0 emplacement six horns along one side and a bare half-circle facing
            // whichever way it happened to be dropped.
            var slots = new bool[TurretProps.MaxHornCount];
            int previous = -1;
            for (int i = 0; i < TurretProps.BaseHornCount; i++)
            {
                int slot = TurretProps.HornSlot(i);
                Assert.That(slot, Is.InRange(0, TurretProps.MaxHornCount - 1));
                Assert.That(slots[slot], Is.False, $"two horns were built at station {slot}");
                slots[slot] = true;
                if (previous >= 0)
                    Assert.That(slot - previous, Is.EqualTo(TurretProps.MaxHornCount / TurretProps.BaseHornCount),
                                "the base ring is not evenly spaced");
                previous = slot;
            }
        }

        [Test]
        public void EveryHornGetsItsOwnStationOnTheCollar()
        {
            var slots = new bool[TurretProps.MaxHornCount];
            for (int i = 0; i < TurretProps.MaxHornCount; i++)
            {
                int slot = TurretProps.HornSlot(i);
                Assert.That(slot, Is.InRange(0, TurretProps.MaxHornCount - 1));
                Assert.That(slots[slot], Is.False, $"horn {i} collided with an existing horn at {slot}");
                slots[slot] = true;
            }
        }

        [Test]
        public void DamageTakesPiecesOffInSteps_NotOnASlider()
        {
            // A part is either there or it is not. Quantising means the player reads three states
            // instead of squinting at a continuous value he has no reference for.
            Assert.That(TurretProps.MissingParts(1f), Is.EqualTo(0));
            Assert.That(TurretProps.MissingParts(0.9f), Is.EqualTo(0));
            Assert.That(TurretProps.MissingParts(0.6f), Is.EqualTo(1));
            Assert.That(TurretProps.MissingParts(0.35f), Is.EqualTo(2));
            Assert.That(TurretProps.MissingParts(0f), Is.EqualTo(3));
        }

        [Test]
        public void DamageOnlyEverGetsWorse()
        {
            int worst = -1;
            for (float hp = 0f; hp <= 1f; hp += 0.01f)
            {
                int missing = TurretProps.MissingParts(hp);
                if (worst < 0) worst = missing;
                Assert.That(missing, Is.LessThanOrEqualTo(worst), $"damage healed at hp {hp}");
                worst = missing;
            }
            Assert.That(TurretProps.MissingParts(2f), Is.EqualTo(0), "an over-full emplacement is undamaged");
            Assert.That(TurretProps.MissingParts(-1f), Is.EqualTo(3), "a negative emplacement is wrecked");
        }

        [Test]
        public void AHurtEmplacementLeansAndAHealthyOneDoesNot()
        {
            // A LEAN, NOT A SHRINK. The bootstrap already sags a damaged turret vertically; a
            // second size cue on top of it reads as the turret being further away, not hurt.
            Assert.That(TurretProps.LeanDegrees(1f), Is.EqualTo(0f).Within(1e-4f));
            Assert.That(TurretProps.LeanDegrees(0f), Is.GreaterThan(4f));
            Assert.That(TurretProps.LeanDegrees(0f), Is.LessThan(20f), "a lean that steep reads as fallen over");
            Assert.That(TurretProps.LeanDegrees(0.5f), Is.LessThan(TurretProps.LeanDegrees(0.1f)));
        }

        [Test]
        public void TheEmitterGlowsTheColourOfWhatComesOutOfIt()
        {
            // Not a rule about this file so much as a rule about the pair of them: an emplacement
            // lit in a colour its own shots are not reads as a prop with an effect happening near
            // it. The throat colour is taken from SignalFx rather than picked again here.
            Assert.That(SignalFx.Pulse.g, Is.GreaterThanOrEqualTo(SignalFx.WeaponGreenFloor));
        }
    }
}
