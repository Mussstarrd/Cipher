#nullable enable
using Cipher.Game.UI;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// Phase B: the player can see what is happening to them. The drawing cannot be asserted
    /// headlessly, but the two things that decide WHAT is drawn can: how long a card lives, and
    /// which way a damage wedge points.
    /// </summary>
    public sealed class HudFeedbackTests
    {
        [Test]
        public void ACardLivesForItsSecondsAndThenStops()
        {
            var hud = new HudFeedback();
            hud.ShowCard("WAVE 1", "40 of them", 2f);
            Assert.That(hud.CardVisible, Is.True);

            for (int i = 0; i < 19; i++) hud.Tick(0.1f);
            Assert.That(hud.CardVisible, Is.True, "still up just before its time");

            hud.Tick(0.2f);
            Assert.That(hud.CardVisible, Is.False);
        }

        [Test]
        public void ANewCardReplacesTheOldOneRatherThanQueueing()
        {
            var hud = new HudFeedback();
            hud.ShowCard("WAVE 1", "", 5f);
            hud.ShowCard("WAVE CLEARED", "", 1f);
            Assert.That(hud.CardTitle, Is.EqualTo("WAVE CLEARED"));

            hud.Tick(1.1f);
            Assert.That(hud.CardVisible, Is.False, "the replacement carries its own shorter life");
        }

        [Test]
        public void AHitFromBehindPointsBehindYou()
        {
            var hud = new HudFeedback();
            // Camera looks up +Z (yaw 0); the bite comes from -Z, which is behind the camera.
            hud.NoteDamage(Vector3.zero, new Vector3(0f, 0f, -5f), cameraYawDegrees: 0f);

            Assert.That(hud.WedgeCount, Is.EqualTo(1));
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(hud.WedgeAngle(0), 180f)), Is.LessThan(1f));
        }

        [Test]
        public void TheWedgeIsRelativeToWhereTheCameraLooks()
        {
            var hud = new HudFeedback();
            // The same attacker, with the camera turned to face it, is straight ahead.
            hud.NoteDamage(Vector3.zero, new Vector3(5f, 0f, 0f), cameraYawDegrees: 90f);
            Assert.That(Mathf.Abs(hud.WedgeAngle(0)), Is.LessThan(1f));
        }

        [Test]
        public void TwoBitesFromTheSameSideAreOneWedge()
        {
            var hud = new HudFeedback();
            hud.NoteDamage(Vector3.zero, new Vector3(0f, 0f, -5f), 0f);
            hud.NoteDamage(Vector3.zero, new Vector3(1f, 0f, -5f), 0f);
            Assert.That(hud.WedgeCount, Is.EqualTo(1), "a crowd on one side is one arrow, not a fan of them");

            hud.NoteDamage(Vector3.zero, new Vector3(0f, 0f, 5f), 0f);
            Assert.That(hud.WedgeCount, Is.EqualTo(2), "the other side is its own arrow");
        }

        [Test]
        public void WedgesFadeOutOnTheirOwn()
        {
            var hud = new HudFeedback();
            hud.NoteDamage(Vector3.zero, new Vector3(0f, 0f, -5f), 0f);
            for (int i = 0; i < 20; i++) hud.Tick(0.1f);
            Assert.That(hud.WedgeCount, Is.Zero);
        }

        [Test]
        public void DamageFromExactlyUnderfootDoesNotThrow()
        {
            var hud = new HudFeedback();
            Assert.DoesNotThrow(() => hud.NoteDamage(Vector3.zero, Vector3.zero, 0f));
            Assert.That(hud.WedgeCount, Is.Zero, "no direction to point, so no arrow -- just the hurt flash");
        }
    }
}
