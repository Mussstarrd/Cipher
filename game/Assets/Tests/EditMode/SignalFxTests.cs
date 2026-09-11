#nullable enable
using Cipher.Game;
using NUnit.Framework;
using UnityEngine;
using Curves = Cipher.Game.SignalFx.Curves;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The maths behind the signal weapons' look.
    ///
    /// A screenshot catches a shell that is the wrong colour. It does NOT catch a shell that stops
    /// expanding halfway, an arc that folds back through its own origin, a strobe that settles into
    /// a rhythm, or a dash train whose spacing depends on how far you shot -- those are all wrong
    /// in a way that reads as "the effect feels a bit off" a month later. So the curves get tests
    /// and the pixels get photographs, and neither substitutes for the other.
    ///
    /// Two of these are DESIGN rules rather than maths, written down as tests on purpose: there is
    /// no orange in the palette (ADR-003 made this a weapon against a device, not a bomb), and the
    /// failing tell desaturates rather than darkening toward red (ADR-003 forbids blood and rot).
    /// Both are the kind of rule that quietly erodes during a tuning pass.
    /// </summary>
    public sealed class SignalFxTests
    {
        // ------------------------------------------------------------------ the palette

        [Test]
        public void PaletteHasNoFireInIt()
        {
            foreach (var c in SignalFx.Palette)
            {
                // Everything is neutral or cold: an orange has red well above blue. This is the
                // rule ADR-003 implies and the one a later "make it pop" pass would break first.
                Assert.That(c.b, Is.GreaterThanOrEqualTo(c.r - 0.001f),
                            $"signal palette colour {c} is warmer than it is cold");
                Assert.That(c.b, Is.GreaterThanOrEqualTo(c.g - 0.001f),
                            $"signal palette colour {c} is warmer than it is cold");
            }
        }

        [Test]
        public void TheImplantIsTheOnlyWarmColourAndItIsADevice()
        {
            // Amber, and the same amber as the cargo drone's underside lamp. It is allowed to be
            // warm because it is a light on a machine, not a fire.
            Assert.That(SignalFx.Implant.r, Is.GreaterThan(SignalFx.Implant.b));
            Assert.That(SignalFx.Implant.g, Is.GreaterThan(SignalFx.Implant.b));
        }

        // ------------------------------------------------------------------ the EMP

        [Test]
        public void EmpShellOnlyEverExpands()
        {
            float previous = -1f;
            for (float t = 0f; t < 0.60f; t += 0.005f)
            {
                var f = Curves.Emp(t);
                Assert.That(f.ShellScale, Is.GreaterThan(previous), $"shell shrank at age {t}");
                previous = f.ShellScale;
            }
        }

        [Test]
        public void EmpRingOutrunsTheShell()
        {
            // The ground ring reaching out past the sphere is what attaches the burst to a place.
            // If a tuning pass ever slows it below the shell the effect becomes a ball in the air.
            for (float t = 0.05f; t < 0.30f; t += 0.01f)
            {
                var f = Curves.Emp(t);
                Assert.That(f.RingRadius, Is.GreaterThan(f.ShellScale), $"ring fell behind at age {t}");
            }
        }

        [Test]
        public void EmpCoreIsGoneAlmostImmediately()
        {
            // The core is an OVEREXPOSURE, not a fireball: it has to be over before the eye has
            // finished moving to it. A core that lingers is the shape a detonation makes.
            Assert.That(Curves.Emp(0f).CoreAlpha, Is.GreaterThan(0.9f));
            Assert.That(Curves.Emp(0.14f).CoreAlpha, Is.EqualTo(0f));
            Assert.That(Curves.Emp(0.5f).CoreAlpha, Is.EqualTo(0f));
        }

        [Test]
        public void ArcsAreDrawnFromOutsideTheKnotAtTheCentre()
        {
            // Every arc shares its innermost segment's airspace, so drawing it puts every arc's
            // brightest, thickest piece in the same two metres -- which makes the centre of the
            // burst the loudest part of it, when the discharge is what comes off the FRONT.
            Assert.That(Curves.ArcFirstDrawnNode, Is.GreaterThan(0));
            Assert.That(Curves.ArcFirstDrawnNode, Is.LessThan(Curves.ArcSegments));

            var start = Curves.ArcNode(5, 2, Curves.ArcFirstDrawnNode);
            Assert.That(new Vector2(start.x, start.z).magnitude, Is.GreaterThan(0f));
        }

        [Test]
        public void EmpEchoStartsLateSoTheTwoFrontsAreDistinct()
        {
            Assert.That(Curves.Emp(0.05f).EchoAlpha, Is.EqualTo(0f));
            Assert.That(Curves.Emp(0.35f).EchoAlpha, Is.GreaterThan(0f));

            // While the first front is still the loud one, the echo is behind it and fainter --
            // two nested shells rather than one thick one.
            var mid = Curves.Emp(0.30f);
            Assert.That(mid.EchoAlpha, Is.LessThan(mid.ShellAlpha));

            // And it ends up outside the first front, which is what makes the burst keep going
            // after the flash instead of simply stopping.
            var late = Curves.Emp(0.55f);
            Assert.That(late.EchoScale, Is.GreaterThan(late.ShellScale));
        }

        [Test]
        public void EmpLeavesNothingDrawnAtTheEndOfItsLife()
        {
            var f = Curves.Emp(1f);
            Assert.That(f.CoreAlpha, Is.EqualTo(0f));
            Assert.That(f.ShellAlpha, Is.EqualTo(0f));
            Assert.That(f.EchoAlpha, Is.EqualTo(0f));
            Assert.That(f.RingAlpha, Is.EqualTo(0f));
            Assert.That(f.ArcAlpha, Is.EqualTo(0f));
            Assert.That(f.StampAlpha, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void EmpStagesAreNeverNegative()
        {
            for (float t = -0.5f; t <= 1.5f; t += 0.01f)
            {
                var f = Curves.Emp(t);
                Assert.That(f.CoreAlpha, Is.GreaterThanOrEqualTo(0f));
                Assert.That(f.ShellAlpha, Is.GreaterThanOrEqualTo(0f));
                Assert.That(f.EchoAlpha, Is.GreaterThanOrEqualTo(0f));
                Assert.That(f.RingAlpha, Is.GreaterThanOrEqualTo(0f));
                Assert.That(f.ArcAlpha, Is.GreaterThanOrEqualTo(0f));
                Assert.That(f.StampAlpha, Is.GreaterThanOrEqualTo(0f));
                Assert.That(f.ShellScale, Is.GreaterThanOrEqualTo(0f));
            }
        }

        // ------------------------------------------------------------------ arcs

        [Test]
        public void ArcsStartAtTheCentreAndOnlyEverTravelOutward()
        {
            // An arc that folds back through its own origin reads as a scribble rather than a
            // discharge, and it is invisible in a still because the fold lands inside the shell.
            for (int arc = 0; arc < Curves.ArcCount; arc++)
            {
                Assert.That(Curves.ArcNode(1234, arc, 0), Is.EqualTo(Vector3.zero));

                float previous = -1f;
                for (int s = 0; s <= Curves.ArcSegments; s++)
                {
                    var node = Curves.ArcNode(1234, arc, s);
                    float radius = new Vector2(node.x, node.z).magnitude;
                    Assert.That(radius, Is.GreaterThan(previous), $"arc {arc} folded back at node {s}");
                    previous = radius;
                }
            }
        }

        [Test]
        public void ArcsReachTheFullReachAtTheirLastNode()
        {
            for (int arc = 0; arc < Curves.ArcCount; arc++)
            {
                var tip = Curves.ArcNode(77, arc, Curves.ArcSegments);
                Assert.That(new Vector2(tip.x, tip.z).magnitude, Is.EqualTo(1f).Within(1e-4f));
            }
        }

        [Test]
        public void ArcsAreDeterministicAndDifferentFromEachOther()
        {
            Assert.That(Curves.ArcNode(9, 3, 2), Is.EqualTo(Curves.ArcNode(9, 3, 2)));
            Assert.That(Curves.ArcNode(9, 3, 2), Is.Not.EqualTo(Curves.ArcNode(9, 4, 2)));
            Assert.That(Curves.ArcNode(9, 3, 2), Is.Not.EqualTo(Curves.ArcNode(10, 3, 2)));
        }

        // ------------------------------------------------------------------ the beam

        [Test]
        public void BeamHeadTravelsFromMuzzleToTargetAndStops()
        {
            Assert.That(Curves.Beam(0f).HeadT, Is.EqualTo(0f));
            Assert.That(Curves.Beam(0.2f).HeadT, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(Curves.Beam(0.5f).HeadT, Is.EqualTo(1f));
            Assert.That(Curves.Beam(1f).HeadT, Is.EqualTo(1f));
        }

        [Test]
        public void BeamTailNeverOvertakesTheHead()
        {
            for (float t = 0f; t <= 1f; t += 0.01f)
            {
                var f = Curves.Beam(t);
                Assert.That(f.TailT, Is.LessThanOrEqualTo(f.HeadT + 1e-4f), $"tail passed the head at {t}");
            }
        }

        [Test]
        public void BeamTerminalBurstOnlyHappensAfterTheHeadArrives()
        {
            Assert.That(Curves.Beam(0.2f).BurstAlpha, Is.EqualTo(0f));
            Assert.That(Curves.Beam(0.5f).BurstAlpha, Is.GreaterThan(0f));
            Assert.That(Curves.Beam(1f).BurstAlpha, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void DashesAreSpacedInMetresSoRangeDoesNotChangeTheirSize()
        {
            // The bug this prevents: spacing dashes as fractions of the length, which makes a long
            // shot's dashes long and reads as the packet slowing down the further you shoot.
            Curves.DashSpan(0, 40f, 0f, 1f, out float nearA, out float nearB);
            Curves.DashSpan(0, 4f, 0f, 1f, out float farA, out float farB);
            Assert.That((nearB - nearA) * 40f, Is.EqualTo((farB - farA) * 4f).Within(1e-3f));
        }

        [Test]
        public void DashesOutsideTheDrawnWindowAreSkipped()
        {
            // Ahead of the head.
            Assert.That(Curves.DashSpan(6, 40f, 0f, 0.05f, out _, out _), Is.False);
            // Behind the tail.
            Assert.That(Curves.DashSpan(0, 40f, 0.9f, 1f, out _, out _), Is.False);
            // Inside it.
            Assert.That(Curves.DashSpan(2, 40f, 0f, 1f, out _, out _), Is.True);
        }

        [Test]
        public void DashCountCoversTheWholeLance()
        {
            int n = Curves.DashCount(40f);
            Assert.That(n * (Curves.DashLength + Curves.DashGap), Is.GreaterThanOrEqualTo(40f));
            Assert.That(Curves.DashCount(0f), Is.EqualTo(0));
        }

        // ------------------------------------------------------------------ the failing tell

        [Test]
        public void FailRingsTightenOntoTheHead()
        {
            // The rings are the only clock the player gets for how long a failing body can still
            // hit him. If they stop tightening the tell stops being readable as a countdown.
            float previous = float.MaxValue;
            for (float fail = 1f; fail >= 0f; fail -= 0.02f)
            {
                var f = Curves.Fail(fail, 0.37f, 3);
                Assert.That(f.RingOuter, Is.LessThan(previous), $"rings widened at fail01 {fail}");
                Assert.That(f.RingInner, Is.LessThan(f.RingOuter));
                previous = f.RingOuter;
            }
        }

        [Test]
        public void FailTintDesaturatesAndGoesCold_NeverTowardBlood()
        {
            var fresh = Curves.FailTint(1f, 0.2f, 5);
            var dying = Curves.FailTint(0f, 0.2f, 5);

            Assert.That(fresh.r, Is.EqualTo(1f).Within(1e-3f), "an untouched chip must not tint the body");

            // Cold, not warm. ADR-003 forbids blood and rot; this is a device losing its grip.
            Assert.That(dying.b, Is.GreaterThan(dying.r), "the failing tint drifted warm");

            // It DRAINS colour rather than adding one. A tint with a wide channel spread is a
            // colour wash, and a colour wash on a body is the first step back toward gore.
            Assert.That(Spread(dying), Is.LessThan(0.3f), "the failing tint became a colour, not a drain");
            Assert.That(dying.r, Is.LessThan(fresh.r), "the failing tint must lose luminance, not gain it");
        }

        [Test]
        public void FailImplantLightGoesOutByTheEnd()
        {
            // The amber going out is the death cue. If it is still burning at fail01 = 0 the body
            // drops with a live light on its head and the player learns nothing from it.
            Assert.That(Brightest(0f, 11), Is.LessThan(0.2f));
            Assert.That(Brightest(1f, 11), Is.GreaterThan(0.5f), "a chip that has just been hit must flare");
        }

        [Test]
        public void FailStrobeIsErratic_NotAMetronome()
        {
            // Counted over a window, a late-stage strobe must be dark more often than a fresh one
            // AND must not land on a clean duty cycle -- the dropouts are what make it read as a
            // device losing the thread rather than one blinking at you.
            Assert.That(LitFraction(1f, 11), Is.GreaterThan(LitFraction(0.1f, 11)));
            Assert.That(LitFraction(0.1f, 11), Is.GreaterThan(0f), "a dying chip must still flash");
            Assert.That(LitFraction(0.1f, 11), Is.LessThan(0.35f));
        }

        [Test]
        public void FailTellIsIdentifiableWithoutAFacingDirection()
        {
            // The renderer falls back to the agent's id when no facing is available. Two agents
            // must not land on the same answer, or a crowd's implants all sit on the same side.
            Assert.That(Curves.Hash01(1000 * 7919), Is.Not.EqualTo(Curves.Hash01(1007 * 7919)));
        }

        // ------------------------------------------------------------------ the jammer field

        [Test]
        public void JammerSweepLoopsOutwardForever()
        {
            float previous = -1f;
            int wraps = 0;
            for (float t = 0f; t < 4f; t += 0.01f)
            {
                var f = Curves.Jammer(t, 0f, 1f);
                if (f.Sweep < previous) wraps++;
                previous = f.Sweep;
                Assert.That(f.Sweep, Is.GreaterThan(0f).And.LessThanOrEqualTo(1.05f));
                Assert.That(f.Alpha, Is.GreaterThan(0f).And.LessThan(1f));
            }
            Assert.That(wraps, Is.GreaterThanOrEqualTo(2), "the sweep must repeat, not run out");
        }

        [Test]
        public void TwoJammersSideBySideDoNotPulseInLockstep()
        {
            var a = Curves.Jammer(1.2f, 3f, 1f);
            var b = Curves.Jammer(1.2f, 11f, 1f);
            Assert.That(a.Sweep, Is.Not.EqualTo(b.Sweep).Within(1e-3f));
        }

        [Test]
        public void JammerGoesQuietWhenTheEmplacementIsNotRunning()
        {
            var off = Curves.Jammer(0.8f, 0f, 0f);
            Assert.That(off.Alpha, Is.EqualTo(0f));
            Assert.That(off.SweepAlpha, Is.EqualTo(0f));
        }

        // ------------------------------------------------------------------ the stamp

        [Test]
        public void InterferenceStampIsACircleWithAHardRim()
        {
            Assert.That(Curves.Interference(0.5f, 0.5f), Is.GreaterThan(0f));
            Assert.That(Curves.Interference(0.02f, 0.02f), Is.EqualTo(0f), "corners must be empty");
            // The rim is the edge the player builds against, so it is the brightest thing in it.
            Assert.That(Curves.Interference(0.5f, 0.97f), Is.GreaterThan(0.8f));
        }

        [Test]
        public void InterferenceStampStaysInRange()
        {
            for (int y = 0; y < 32; y++)
                for (int x = 0; x < 32; x++)
                {
                    float a = Curves.Interference((x + 0.5f) / 32f, (y + 0.5f) / 32f);
                    Assert.That(a, Is.InRange(0f, 1f));
                }
        }

        // ------------------------------------------------------------------ helpers

        private static float Spread(Color c)
            => Mathf.Max(c.r, Mathf.Max(c.g, c.b)) - Mathf.Min(c.r, Mathf.Min(c.g, c.b));

        /// <summary>Brightest the implant gets anywhere in a three-second window.</summary>
        private static float Brightest(float fail01, int id)
        {
            float best = 0f;
            for (float t = 0f; t < 3f; t += 0.004f)
                best = Mathf.Max(best, Curves.Fail(fail01, t, id).LampAlpha);
            return best;
        }

        private static float LitFraction(float fail01, int id)
        {
            int lit = 0, total = 0;
            for (float t = 0f; t < 3f; t += 0.004f)
            {
                if (Curves.Fail(fail01, t, id).Lit) lit++;
                total++;
            }
            return lit / (float)total;
        }
    }
}
