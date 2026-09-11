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
    /// Several of these are DESIGN rules rather than maths, written down as tests on purpose: the
    /// EMP stays cold, the weapons stay gold, the implant stays orange and far enough from the
    /// weapons to be told apart, and the failing tell desaturates rather than darkening toward red
    /// (ADR-003 forbids blood and rot). All are the kind of rule that quietly erodes during a
    /// tuning pass and that nobody notices has gone until a playtest.
    /// </summary>
    public sealed class SignalFxTests
    {
        // ------------------------------------------------------------------ the palette

        [Test]
        public void TheEmpStaysCold()
        {
            // The owner called the EMP fantastic on 2026-09-11 while asking for amber WEAPONS.
            // Warming the burst to match them would throw away the one effect that already works
            // AND collapse the two most important signals in the game into one colour.
            foreach (var c in SignalFx.EmpPalette)
            {
                Assert.That(c.b, Is.GreaterThanOrEqualTo(c.r - 0.001f),
                            $"EMP colour {c} is warmer than it is cold");
                Assert.That(c.b, Is.GreaterThanOrEqualTo(c.g - 0.001f),
                            $"EMP colour {c} is warmer than it is cold");
            }
        }

        [Test]
        public void EveryWeaponColourIsAmber()
        {
            // Owner, 2026-09-11: "more like a microwave pulses and I think more yellow or Amber
            // instead of blue". Red above blue is what makes a colour warm at all.
            foreach (var c in SignalFx.WeaponPalette)
            {
                Assert.That(c.r, Is.GreaterThan(c.b + 0.1f), $"weapon colour {c} is not warm");
                Assert.That(c.g, Is.GreaterThan(c.b + 0.1f), $"weapon colour {c} is warm but not amber");
            }
        }

        [Test]
        public void TheWeaponAmberCannotBeMistakenForTheImplantAmber()
        {
            // THE ONE THAT MATTERS. Both are warm, so they are separated on GREEN -- the channel
            // that moves a warm colour between gold and orange. Shooting is gold; a chip dying is
            // orange. Let these two drift together and the player can no longer tell his own fire
            // from the tell it produces, which is the whole reason the tell exists.
            Assert.That(SignalFx.Implant.g, Is.LessThanOrEqualTo(SignalFx.ImplantGreenCeiling));

            foreach (var c in SignalFx.WeaponPalette)
            {
                Assert.That(c.g, Is.GreaterThanOrEqualTo(SignalFx.WeaponGreenFloor),
                            $"weapon colour {c} has drifted toward the implant's orange");
                Assert.That(c.g - SignalFx.Implant.g, Is.GreaterThan(0.12f),
                            $"weapon colour {c} is too close to the implant to be told apart");
            }
        }

        [Test]
        public void TheImplantIsADeviceAndNotAFire()
        {
            // Amber, and the same amber as the cargo drone's underside lamp. It is allowed to be
            // warm because it is a light on a machine, not a fire.
            Assert.That(SignalFx.Implant.r, Is.GreaterThan(SignalFx.Implant.b));
            Assert.That(SignalFx.Implant.g, Is.GreaterThan(SignalFx.Implant.b));
        }

        [Test]
        public void ThePulseOutlivesNothingAndTheTellOutlivesIt()
        {
            // The second separator, after hue: a shot is a flicker and a dying chip is a two and a
            // half second event. Anything warm still on screen a second later is a body.
            Assert.That(SignalFx.BeamLife, Is.LessThan(0.4f), "a shot that lingers becomes a beam");
            Assert.That(SignalFx.BeamLife, Is.GreaterThan(0.12f), "a shot too short to see travel is a tracer");
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

        // ------------------------------------------------------------------ the pulse

        [Test]
        public void PulseFrontTravelsFromHornToTargetAndStops()
        {
            Assert.That(Curves.Pulse(0f).FrontT, Is.EqualTo(0f));
            Assert.That(Curves.Pulse(0.2f).FrontT, Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(Curves.Pulse(0.5f).FrontT, Is.EqualTo(1f));
            Assert.That(Curves.Pulse(1f).FrontT, Is.EqualTo(1f));
        }

        [Test]
        public void PulseFrontDeceleratesRatherThanFlyingLikeARound()
        {
            // Constant speed is a projectile. Energy spreading into air leaves fast and settles,
            // so the first half of the flight must cover more than half the distance.
            Assert.That(Curves.Pulse(0.23f).FrontT, Is.GreaterThan(0.5f));
        }

        [Test]
        public void PulseTailNeverOvertakesTheFront()
        {
            for (float t = 0f; t <= 1f; t += 0.01f)
            {
                var f = Curves.Pulse(t);
                Assert.That(f.BackT, Is.LessThanOrEqualTo(f.FrontT + 1e-4f), $"tail passed the front at {t}");
            }
        }

        [Test]
        public void PulseBloomOnlyHappensAfterTheFrontArrives()
        {
            Assert.That(Curves.Pulse(0.2f).BloomAlpha, Is.EqualTo(0f));
            Assert.That(Curves.Pulse(0.5f).BloomAlpha, Is.GreaterThan(0f));
            Assert.That(Curves.Pulse(1f).BloomAlpha, Is.EqualTo(0f).Within(1e-5f));
        }

        [Test]
        public void WavefrontsAreSpacedInMetresSoRangeDoesNotChangeTheFrequency()
        {
            // The bug this prevents: spacing fronts as fractions of the length, which would make
            // the same weapon appear to change wavelength with how far the player is shooting.
            Curves.Wavefront(0, 40f, 0f, 40f, 0f, out float nearA, out _, out _);
            Curves.Wavefront(1, 40f, 0f, 40f, 0f, out float nearB, out _, out _);
            Curves.Wavefront(0, 4f, 0f, 4f, 0f, out float farA, out _, out _);
            Curves.Wavefront(1, 4f, 0f, 4f, 0f, out float farB, out _, out _);
            Assert.That(nearA - nearB, Is.EqualTo(Curves.WaveSpacing).Within(1e-3f));
            Assert.That(farA - farB, Is.EqualTo(Curves.WaveSpacing).Within(1e-3f));
        }

        [Test]
        public void WavefrontsTravelOutwardWithTheFrontAndNeverBackward()
        {
            // Every front in the train sits between the packet's tail and its leading edge, and
            // they are ordered: front 0 is the leading one. A train that ran the other way would
            // point the shot at the shooter.
            float previous = float.PositiveInfinity;
            for (int k = 0; k < Curves.MaxWavefronts; k++)
            {
                if (!Curves.Wavefront(k, 30f, 4f, 40f, 0.3f, out float d, out _, out _)) continue;
                Assert.That(d, Is.LessThan(previous), $"front {k} did not fall behind front {k - 1}");
                Assert.That(d, Is.GreaterThanOrEqualTo(4f - 1e-4f), $"front {k} escaped out of the tail");
                Assert.That(d, Is.LessThanOrEqualTo(30f + 1e-4f), $"front {k} overtook the leading edge");
                previous = d;
            }
        }

        [Test]
        public void ThePacketIsBrightestAtItsLeadingEdge()
        {
            // THIS IS THE ARROW. The gameplay job of the effect is to say which way the shot went,
            // and with the hard bead deleted the brightness gradient is what says it.
            Curves.Wavefront(0, 30f, 0f, 40f, 0f, out _, out _, out float head);
            Curves.Wavefront(Curves.MaxWavefronts - 1, 30f, 0f, 40f, 0f, out _, out _, out float tail);
            Assert.That(head, Is.GreaterThan(tail * 3f), "the packet has no direction in it");
        }

        [Test]
        public void ThePacketIsFiniteSoItCannotBecomeAContinuousBeam()
        {
            // The owner read the old effect as a laser. A train with no end is a laser whatever
            // colour it is, so the count is capped and the cap is a design rule, not a budget.
            Assert.That(Curves.WavefrontCount(1000f), Is.EqualTo(Curves.MaxWavefronts));
            Assert.That(Curves.WavefrontCount(0f), Is.EqualTo(0));
            Assert.That(Curves.Wavefront(Curves.MaxWavefronts, 500f, 0f, 500f, 0f, out _, out _, out _),
                        Is.False);
        }

        [Test]
        public void WavefrontsOnlyEverSpreadOut()
        {
            // A wave that necks down onto its target is a beam being focused, which is the read
            // this whole pass exists to get rid of.
            float previous = -1f;
            for (float u = 0f; u <= 1f; u += 0.02f)
            {
                float r = Curves.PulseRadius(u);
                Assert.That(r, Is.GreaterThanOrEqualTo(previous), $"the wavefront narrowed at {u}");
                previous = r;
            }
            Assert.That(Curves.PulseRadius(1f), Is.GreaterThan(Curves.PulseRadius(0f) * 3f),
                        "a carrier that does not visibly diverge is a lance");
        }

        [Test]
        public void TheShimmerNeverPutsAHoleInThePacket()
        {
            // A front that blinks to nothing leaves a gap, and the gap reads as missing geometry
            // rather than as energy breathing.
            for (float phase = 0f; phase < 20f; phase += 0.13f)
            {
                for (int k = 0; k < 4; k++)
                {
                    if (!Curves.Wavefront(k, 30f, 0f, 40f, phase, out _, out float r, out float amp)) continue;
                    Assert.That(amp, Is.GreaterThan(0.05f), $"front {k} went dark at phase {phase}");
                    Assert.That(r, Is.GreaterThan(0f), $"front {k} collapsed at phase {phase}");
                }
            }
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
