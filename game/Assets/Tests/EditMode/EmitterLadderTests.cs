#nullable enable
using Cipher.Game;
using Cipher.Game.Match;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// THE GUN LADDER, BOTH HALVES: the weapon in his hands (<see cref="HeroEmitter"/>) and the
    /// pulse in the air (<see cref="SignalFx.Curves.PulseProfile"/>).
    ///
    /// OWNER, 2026-09-12: "different gun upgrades should make my weapon skin different and it
    /// should also change the way my projectile goes". What is tested here is not that the
    /// geometry is pretty -- a screenshot is the only thing that can say that, and there is one in
    /// art/review -- but the handful of DESIGN rules that a screenshot cannot police and that a
    /// later tuning pass would erode without anybody noticing:
    ///
    ///   * that the four rungs stay four distinct silhouettes rather than one weapon scaled up,
    ///   * that the four shots stay four distinct behaviours in one family,
    ///   * and that no rung, however it is tuned, can drift the weapon's amber toward the
    ///     implant's. That last one is the rule CLAUDE.md calls "separated by RULE, not by habit",
    ///     and adding four tiers of colour maths to a two-colour problem is exactly the change
    ///     that would have broken it quietly.
    /// </summary>
    public sealed class EmitterLadderTests
    {
        private const int Tiers = HeroEmitter.MaxTier + 1;

        // ------------------------------------------------------------------ the weapon in hand

        [Test]
        public void TheLadderIsNotASizeRamp()
        {
            // The trap this exists to stop: making each upgrade a bigger version of the last one.
            // ADR-008 says these are signal weapons carrying malware, so an upgrade is a better
            // aerial, not a larger calibre -- and a ladder that only grows reads as one weapon
            // scaled, which is the same as no visible upgrade at all.
            bool reachEverFalls = false, spanEverFalls = false, spanEverRises = false;
            for (int t = 1; t < Tiers; t++)
            {
                if (HeroEmitter.Reach(t) < HeroEmitter.Reach(t - 1)) reachEverFalls = true;
                if (HeroEmitter.HeadSpan(t) < HeroEmitter.HeadSpan(t - 1)) spanEverFalls = true;
                if (HeroEmitter.HeadSpan(t) > HeroEmitter.HeadSpan(t - 1)) spanEverRises = true;
            }
            Assert.That(reachEverFalls, Is.True, "reach only ever grows: the ladder is a size ramp");
            Assert.That(spanEverFalls, Is.True, "the head only ever grows: the ladder is a size ramp");
            Assert.That(spanEverRises, Is.True, "the head only ever shrinks: the top rung has no presence");
        }

        [Test]
        public void EveryRungHasItsOwnSilhouette()
        {
            // Seen small, over a shoulder, against a busy field, under an ink shader that draws
            // only the outside edge. Two rungs sharing a (reach, span) pair are two rungs the
            // player cannot tell apart at the only distance the weapon is ever seen from.
            for (int a = 0; a < Tiers; a++)
            {
                for (int b = a + 1; b < Tiers; b++)
                {
                    float dReach = Mathf.Abs(HeroEmitter.Reach(a) - HeroEmitter.Reach(b));
                    float dSpan = Mathf.Abs(HeroEmitter.HeadSpan(a) - HeroEmitter.HeadSpan(b));
                    Assert.That(dReach > 0.06f || dSpan > 0.04f, Is.True,
                                $"tiers {a} and {b} are the same shape");
                }
            }
        }

        [Test]
        public void TheLanceIsTheLongestAndTheCascadeIsTheWidest()
        {
            // The two ends of the read. A lance that is not the longest thing he carries is not a
            // lance, and an array of four horns that is not the widest is not an array.
            for (int t = 0; t < Tiers; t++)
            {
                if (t != 2) Assert.That(HeroEmitter.Reach(2), Is.GreaterThan(HeroEmitter.Reach(t)));
                if (t != 3) Assert.That(HeroEmitter.HeadSpan(3), Is.GreaterThan(HeroEmitter.HeadSpan(t)));
            }
            Assert.That(HeroEmitter.Reach(0), Is.LessThan(HeroEmitter.Reach(1)),
                        "the bodged first weapon should be the stubbiest");
        }

        [Test]
        public void ARungBeyondTheLadderClampsRatherThanVanishing()
        {
            // A loot table that one day hands out a fifth gun must draw the top rung, not nothing.
            Assert.That(HeroEmitter.Reach(99), Is.EqualTo(HeroEmitter.Reach(HeroEmitter.MaxTier)));
            Assert.That(HeroEmitter.HeadSpan(99), Is.EqualTo(HeroEmitter.HeadSpan(HeroEmitter.MaxTier)));
            Assert.That(HeroEmitter.Reach(-4), Is.EqualTo(HeroEmitter.Reach(0)));
            Assert.That(HeroEmitter.HeadSpan(-4), Is.EqualTo(HeroEmitter.HeadSpan(0)));
        }

        [Test]
        public void TheWeaponGlowsExactlyWhatItFires()
        {
            // TurretProps takes its emission from SignalFx.Pulse for this reason and the hero's
            // takes it from SignalFx.Emitter: a gun whose lamps are a different amber from its own
            // shot reads as a prop with an effect happening near it.
            Assert.That(HeroEmitter.Emission, Is.EqualTo(SignalFx.Emitter));
            Assert.That(HeroEmitter.Emission.g, Is.GreaterThanOrEqualTo(SignalFx.WeaponGreenFloor));
        }

        [Test]
        public void SwappingTierSwapsTheWeaponAndMovesTheMuzzleWithIt()
        {
            // The one piece of this class that can silently do nothing. If SetTier stops switching
            // assemblies the player carries a Field Jammer through the whole campaign while the
            // HUD tells him he is holding a Cascade Emitter -- which is the bug it exists to fix.
            var mount = new GameObject("Mount");
            try
            {
                mount.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
                var emitter = HeroEmitter.Build(mount.transform, StubMaterial);

                for (int t = 0; t < Tiers; t++)
                {
                    emitter.SetTier(t);
                    Assert.That(emitter.Tier, Is.EqualTo(t));

                    int active = 0;
                    foreach (Transform child in emitter.Root.transform)
                        if (child.gameObject.activeSelf && child.name.StartsWith("Tier")) active++;
                    Assert.That(active, Is.EqualTo(1), $"tier {t} left {active} weapons in his hands");

                    Assert.That(emitter.Muzzle, Is.Not.Null);
                    Assert.That(emitter.Muzzle!.localPosition.z,
                                Is.EqualTo(HeroEmitter.Reach(t)).Within(0.001f));
                }

                // And back down again: a ladder that only climbs is untested in the direction a
                // fall-back to the next position takes it.
                emitter.SetTier(0);
                Assert.That(emitter.Muzzle!.localPosition.z,
                            Is.EqualTo(HeroEmitter.Reach(0)).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(mount);
            }
        }

        [Test]
        public void TheWeaponIsBuiltInMetresWhateverBodyItHangsOn()
        {
            // The hero capsule is scaled (0.8, 0.9, 0.8) and children inherit that. Authored in
            // metres and parented straight on, the weapon comes out squashed on two axes and
            // SHEARED wherever a part is rotated. The root cancels the mount's scale, so every
            // number in HeroEmitter means metres.
            var mount = new GameObject("Mount");
            try
            {
                mount.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
                var emitter = HeroEmitter.Build(mount.transform, StubMaterial);
                Vector3 world = emitter.Root.transform.lossyScale;
                Assert.That(world.x, Is.EqualTo(1f).Within(0.002f));
                Assert.That(world.y, Is.EqualTo(1f).Within(0.002f));
                Assert.That(world.z, Is.EqualTo(1f).Within(0.002f));
            }
            finally
            {
                Object.DestroyImmediate(mount);
            }
        }

        private static Material StubMaterial(Color colour)
        {
            var shader = Shader.Find("Exodus/InstancedLit") ?? Shader.Find("Sprites/Default");
            return new Material(shader) { color = colour };
        }

        // ------------------------------------------------------------------ the pulse in the air

        [Test]
        public void AnEmplacementsShotIsTheOriginalTuningToTheBit()
        {
            // The ladder must not have moved the turrets. Their tier is its own ladder and it is
            // read off their HARDWARE (aerials, horns, lit throats), so if an emplacement's shot
            // started escalating too, "whose fire was that" would stop being answerable.
            var p = SignalFx.Curves.TurretProfile;
            Assert.That(p.Spacing, Is.EqualTo(SignalFx.Curves.WaveSpacing));
            Assert.That(p.MaxFronts, Is.EqualTo(SignalFx.Curves.MaxWavefronts));
            Assert.That(p.NearRadius, Is.EqualTo(SignalFx.Curves.WaveNearRadius));
            Assert.That(p.FarRadius, Is.EqualTo(SignalFx.Curves.WaveFarRadius));
            Assert.That(p.Jitter, Is.EqualTo(0f), "an emplacement's shot must stay machined");

            // And the profile-aware maths must agree with the original function it generalises,
            // for every front of every phase of a shot -- which is the only way to be sure the
            // refactor changed nothing that was already on screen.
            for (int k = 0; k < SignalFx.Curves.MaxWavefronts; k++)
            {
                for (float phase = 0f; phase < 9f; phase += 0.7f)
                {
                    bool oldAlive = SignalFx.Curves.Wavefront(k, 14f, 2f, 20f, phase,
                                                              out float od, out float orr, out float oa);
                    bool newAlive = SignalFx.Curves.Wavefront(p, k, 14f, 2f, 20f, phase, seed: 12345,
                                                              out float nd, out float nr, out float na);
                    Assert.That(newAlive, Is.EqualTo(oldAlive), $"front {k} at phase {phase}");
                    if (!oldAlive) continue;
                    Assert.That(nd, Is.EqualTo(od).Within(1e-5f));
                    Assert.That(nr, Is.EqualTo(orr).Within(1e-5f));
                    Assert.That(na, Is.EqualTo(oa).Within(1e-5f));
                }
            }
        }

        [Test]
        public void TheCarrierGetsFinerEveryTimeTheWeaponGetsBetter()
        {
            // The loudest difference between two stills of two rungs, and the one that says
            // "sophisticated" rather than "bigger": a shorter wavelength and more of it.
            for (int t = 1; t < Tiers; t++)
            {
                Assert.That(SignalFx.Curves.ProfileFor(t).Spacing,
                            Is.LessThan(SignalFx.Curves.ProfileFor(t - 1).Spacing),
                            $"tier {t} did not refine the carrier");
            }
            Assert.That(SignalFx.Curves.Tier3.MaxFronts,
                        Is.GreaterThan(SignalFx.Curves.Tier0.MaxFronts));
        }

        [Test]
        public void NoRungIsAllowedToBecomeAContinuousTubeOfLight()
        {
            // The owner rejected the previous weapon as a laser blast. Fronts packed closer
            // together than they are WIDE merge into one continuous column, which is the laser
            // again wearing the family's colours. This is the line the density may not cross.
            for (int t = 0; t < Tiers; t++)
            {
                var p = SignalFx.Curves.ProfileFor(t);
                Assert.That(p.Spacing, Is.GreaterThan(p.FarRadius),
                            $"tier {t}'s fronts are closer together than they are wide");
                Assert.That(p.MaxFronts, Is.LessThanOrEqualTo(SignalFx.Curves.MaxWavefronts),
                            $"tier {t} carries more fronts than the packet cap allows");
            }
        }

        [Test]
        public void TheCrudestWeaponIsNeverTheBiggestThingOnScreen()
        {
            // FOUND BY TAKING THE PICTURE. The Field Jammer's fronts started at 0.78m, which made
            // the worst weapon in the game throw wider fronts and a fatter haze cone than the
            // Cascade Emitter -- the ladder inverted in the one frame a player actually looks at.
            // Loose and untidy must stay SMALLER than broad and deliberate.
            for (int t = 0; t < Tiers - 1; t++)
            {
                Assert.That(SignalFx.Curves.Tier3.FarRadius,
                            Is.GreaterThan(SignalFx.Curves.ProfileFor(t).FarRadius),
                            $"tier {t} spreads wider than the top of the ladder");
            }

            // It is still the loosest of the three below it, though: that looseness is the whole
            // character of the rung, and tidying it up entirely would leave it with nothing.
            Assert.That(SignalFx.Curves.Tier0.FarRadius,
                        Is.GreaterThan(SignalFx.Curves.Tier1.FarRadius),
                        "the bodged weapon has been tidied into a focused one");
            Assert.That(SignalFx.Curves.Tier0.HazeScale,
                        Is.GreaterThan(SignalFx.Curves.Tier1.HazeScale),
                        "the bodged weapon has stopped leaking into the air");
        }

        [Test]
        public void EveryRungStaysOnTheWeaponSideOfTheAmberLine()
        {
            // THE NON-NEGOTIABLE. Warm colours move between gold and orange on GREEN, so the shot
            // and the dying implant are held apart by WeaponGreenFloor / ImplantGreenCeiling. Four
            // tiers of per-rung colour maths is exactly the change that would breach that quietly,
            // so every rung is swept at every brightness against both weapon inks.
            Color[] inks = { SignalFx.Emitter, SignalFx.TurretBeam, SignalFx.Pulse };
            for (int t = -1; t < Tiers; t++)
            {
                var p = SignalFx.Curves.ProfileFor(t);
                foreach (var ink in inks)
                {
                    for (float amp = 0f; amp <= 1.0001f; amp += 0.05f)
                    {
                        var c = SignalFx.Curves.FrontColour(ink, amp, p);
                        Assert.That(c.g, Is.GreaterThanOrEqualTo(SignalFx.WeaponGreenFloor),
                                    $"tier {t} at amp {amp:0.00} drifted toward the implant's orange");
                        Assert.That(c.g, Is.GreaterThan(SignalFx.ImplantGreenCeiling),
                                    $"tier {t} at amp {amp:0.00} can be mistaken for a dying chip");
                    }
                }
            }
        }

        [Test]
        public void TheHotterRungsAreHotterAndNoneOfThemLeavesTheFamily()
        {
            // Heat is the one colour channel the ladder is allowed to move, and it only ever mixes
            // two colours that are already in the weapon palette -- which is what makes it safe.
            var low = SignalFx.Curves.FrontColour(SignalFx.Emitter, 0.5f, SignalFx.Curves.Tier0);
            var high = SignalFx.Curves.FrontColour(SignalFx.Emitter, 0.5f, SignalFx.Curves.Tier3);
            Assert.That(high.g, Is.GreaterThan(low.g), "the top rung is not hotter than the first");
            Assert.That(high.b, Is.LessThanOrEqualTo(SignalFx.PulseCore.b + 1e-4f),
                        "the top rung has gone past the hottest colour in the family");
        }

        [Test]
        public void OnlyTheWormLanceSpreadsAndOnlyTheCascadeChains()
        {
            // Each rung gets ONE behaviour of its own on top of the shared train. Two rungs sharing
            // a gimmick is two rungs the player reads as the same upgrade twice.
            for (int t = 0; t < Tiers; t++)
            {
                var p = SignalFx.Curves.ProfileFor(t);
                Assert.That(p.Filaments > 0, Is.EqualTo(t == 2),
                            $"filaments belong to the Worm Lance alone, tier {t} disagrees");
                Assert.That(p.ChainStages > 1, Is.EqualTo(t == 3),
                            $"the cascade belongs to the Cascade Emitter alone, tier {t} disagrees");
            }
            Assert.That(SignalFx.Curves.Tier2.Lobes, Is.GreaterThan(0),
                        "the lance no longer sheds its payload on the way");
        }

        [Test]
        public void LobesWalkOffTheAxisWithDISTANCE_NotWithTime()
        {
            // A lobe that widens with the clock is a shockwave. One that widens with distance
            // along the shot is something that LEFT the carrier and is still going, which is what
            // a self-propagating payload looks like.
            var p = SignalFx.Curves.Tier2;
            float previous = -1f;
            for (float u = 0f; u <= 1.0001f; u += 0.1f)
            {
                SignalFx.Curves.Lobe(p, index: 0, lobe: 0, u, out float offset, out _, out float amp);
                Assert.That(offset, Is.GreaterThan(previous), $"lobe stopped spreading at u = {u:0.0}");
                Assert.That(amp, Is.GreaterThan(0f).And.LessThan(1f),
                            "a lobe brighter than its own carrier is a second weapon");
                previous = offset;
            }

            // And a rung with no lobes must draw none, rather than drawing them at zero offset --
            // which would put an invisible instance in the batch for every front of every shot.
            SignalFx.Curves.Lobe(SignalFx.Curves.Tier1, 0, 0, 0.5f, out _, out _, out float none);
            Assert.That(none, Is.EqualTo(0f));
        }

        [Test]
        public void LobesDoNotAllLieInOnePlane()
        {
            // One plane reads as a mistake in the geometry; a spiral down the packet reads as
            // motion. The index walks the spin so consecutive fronts shed in different directions.
            var p = SignalFx.Curves.Tier2;
            SignalFx.Curves.Lobe(p, index: 0, lobe: 0, 0.5f, out _, out float a0, out _);
            SignalFx.Curves.Lobe(p, index: 1, lobe: 0, 0.5f, out _, out float a1, out _);
            Assert.That(Mathf.Abs(Mathf.DeltaAngle(a0, a1)), Is.GreaterThan(10f));
        }

        [Test]
        public void TheCascadeWalksBackDownItsOwnLineAndNeverPastTheShooter()
        {
            // Each stage later, smaller and further back -- and capped against the shot's own
            // length, or a point-blank kill puts the last bloom behind the player's head.
            var p = SignalFx.Curves.Tier3;
            float previousBack = -1f, previousScale = float.MaxValue;
            int lit = 0;
            for (int s = 0; s < p.ChainStages; s++)
            {
                SignalFx.Curves.Chain(p, s, bloomScale: 1f, bloomAlpha: 0.05f, length: 30f,
                                      out float scale, out float alpha, out float back);
                Assert.That(back, Is.GreaterThan(previousBack));
                Assert.That(scale, Is.LessThan(previousScale));
                if (alpha > 0f) lit++;
                previousBack = back;
                previousScale = scale;
            }
            Assert.That(lit, Is.GreaterThan(1), "an aged cascade never lights its later stages");

            // And on a point-blank kill every stage is pulled in against the shot's own length,
            // or the last bloom of a two-metre shot goes off behind the player's head.
            for (int s = 0; s < p.ChainStages; s++)
            {
                SignalFx.Curves.Chain(p, s, 1f, 0.05f, length: 2f, out _, out _, out float back);
                Assert.That(back, Is.LessThanOrEqualTo(2f * 0.30f + 1e-4f),
                            "a cascade stage landed behind the shooter");
            }

            // A stage beyond the profile's own count must draw nothing rather than off the end.
            SignalFx.Curves.Chain(p, p.ChainStages, 1f, 1f, 10f, out float s2, out float a2, out _);
            Assert.That(s2, Is.EqualTo(0f));
            Assert.That(a2, Is.EqualTo(0f));
        }

        [Test]
        public void ACascadeStageIsDarkUntilItsTurn()
        {
            // The delay is applied by holding a stage dark until the parent bloom has aged past
            // it: no second clock, and nothing that can drift out of sync with the shot.
            var p = SignalFx.Curves.Tier3;
            SignalFx.Curves.Chain(p, 2, 1f, bloomAlpha: 1f, length: 10f, out _, out float young, out _);
            SignalFx.Curves.Chain(p, 2, 1f, bloomAlpha: 0.2f, length: 10f, out _, out float older, out _);
            Assert.That(young, Is.EqualTo(0f), "the last stage lit at the instant of impact");
            Assert.That(older, Is.GreaterThan(0f), "the last stage never lights at all");
        }

        [Test]
        public void FilamentsAreTooShortAndTooWarmToBeMistakenForTheEmp()
        {
            // The EMP is the owner's favourite effect and it is COLD. Anything warm thrown out of
            // an impact must stay obviously smaller than its arcs, which reach 1.7 blast radii.
            var p = SignalFx.Curves.Tier2;
            for (int i = 0; i < p.Filaments; i++)
            {
                SignalFx.Curves.Filament(seed: 991, i, p, bloomAlpha: 1f,
                                         Vector3.forward, Vector3.right,
                                         out Vector3 away, out float reach, out float alpha);
                Assert.That(reach, Is.LessThan(0.8f), "a filament has grown into an arc");
                Assert.That(away.magnitude, Is.EqualTo(1f).Within(1e-3f));
                Assert.That(alpha, Is.GreaterThan(0f));
            }

            // A rung with none draws none.
            SignalFx.Curves.Filament(991, 0, SignalFx.Curves.Tier0, 1f, Vector3.forward, Vector3.right,
                                     out _, out float noReach, out _);
            Assert.That(noReach, Is.EqualTo(0f));
        }

        [Test]
        public void UntidinessIsAPropertyOfTheWeaponAndItHoldsStill()
        {
            // A Field Jammer's fronts are uneven because it is a badly made aerial, NOT because
            // somebody added noise to the effect -- so the same shot must be lumpy in the same
            // places every frame it is alive, and on a replay, and in a held review frame.
            var jammer = SignalFx.Curves.Tier0;
            Assert.That(jammer.Jitter, Is.GreaterThan(0.2f), "the first weapon has been tidied up");

            for (int k = 0; k < jammer.MaxFronts; k++)
            {
                SignalFx.Curves.Wavefront(jammer, k, 9f, 0f, 12f, 1.5f, seed: 77,
                                          out _, out float r1, out float a1);
                SignalFx.Curves.Wavefront(jammer, k, 9f, 0f, 12f, 1.5f, seed: 77,
                                          out _, out float r2, out float a2);
                Assert.That(r1, Is.EqualTo(r2));
                Assert.That(a1, Is.EqualTo(a2));
            }

            // Two different shots are lumpy in DIFFERENT places, or the unevenness reads as a
            // pattern in the geometry rather than as a weapon that was never machined.
            bool differs = false;
            for (int k = 0; k < jammer.MaxFronts; k++)
            {
                SignalFx.Curves.Wavefront(jammer, k, 9f, 0f, 12f, 1.5f, 77, out _, out float ra, out _);
                SignalFx.Curves.Wavefront(jammer, k, 9f, 0f, 12f, 1.5f, 4242, out _, out float rb, out _);
                if (Mathf.Abs(ra - rb) > 1e-4f) differs = true;
            }
            Assert.That(differs, Is.True, "every shot is lumpy in exactly the same places");

            // And a machined rung ignores its seed entirely.
            SignalFx.Curves.Wavefront(SignalFx.Curves.TurretProfile, 3, 9f, 0f, 12f, 1.5f, 1,
                                      out _, out float m1, out _);
            SignalFx.Curves.Wavefront(SignalFx.Curves.TurretProfile, 3, 9f, 0f, 12f, 1.5f, 98765,
                                      out _, out float m2, out _);
            Assert.That(m1, Is.EqualTo(m2));
        }

        [Test]
        public void EveryRungsFrontsOnlyEverSpreadOut()
        {
            // A beam that necks down onto its target is a beam being FOCUSED, which is the laser
            // read again. A broadcast carrier diverges the whole way, on every rung.
            for (int t = -1; t < Tiers; t++)
            {
                var p = SignalFx.Curves.ProfileFor(t);
                Assert.That(p.FarRadius, Is.GreaterThan(p.NearRadius), $"tier {t} necks down");
                float previous = -1f;
                for (float u = 0f; u <= 1.0001f; u += 0.05f)
                {
                    float r = SignalFx.Curves.PulseRadius(p, u);
                    Assert.That(r, Is.GreaterThanOrEqualTo(previous - 1e-5f), $"tier {t} at u {u:0.00}");
                    previous = r;
                }
            }
        }

        [Test]
        public void EveryRungsPacketStaysFiniteAndKeepsItsHeadBrightest()
        {
            // The gradient from head to tail is the arrow. Without it a train of equal rings is a
            // ladder, and a ladder has no direction -- which is what made the first version read
            // as a bar somebody drew between two points.
            for (int t = 0; t < Tiers; t++)
            {
                var p = SignalFx.Curves.ProfileFor(t);
                int drawn = SignalFx.Curves.WavefrontCount(p, 400f);
                Assert.That(drawn, Is.LessThanOrEqualTo(p.MaxFronts),
                            $"tier {t}'s packet grew with the range");

                // Phase 0 takes the shimmer out of the comparison, so what is measured is the
                // envelope rather than the breathing on top of it.
                SignalFx.Curves.Wavefront(p, 0, 40f, 0f, 60f, 0f, 0, out _, out _, out float head);
                SignalFx.Curves.Wavefront(p, p.MaxFronts - 1, 40f, 0f, 60f, 0f, 0,
                                          out _, out _, out float tail);
                Assert.That(head, Is.GreaterThan(tail), $"tier {t}'s packet has no direction");
            }
        }

        [Test]
        public void AProfileExistsForAnythingTheGameCanAsk()
        {
            // Negative is an emplacement; above the ladder clamps to the top. A missing profile
            // would draw no shot at all, which is the kind of thing that ships.
            Assert.That(SignalFx.Curves.ProfileFor(-1).Spacing,
                        Is.EqualTo(SignalFx.Curves.TurretProfile.Spacing));
            Assert.That(SignalFx.Curves.ProfileFor(-99).Spacing,
                        Is.EqualTo(SignalFx.Curves.TurretProfile.Spacing));
            Assert.That(SignalFx.Curves.ProfileFor(99).Spacing,
                        Is.EqualTo(SignalFx.Curves.Tier3.Spacing));
            Assert.That(SignalFx.Curves.ProfileFor(HeroEmitter.MaxTier).Spacing,
                        Is.EqualTo(SignalFx.Curves.Tier3.Spacing));
        }

        [Test]
        public void TheLadderInTheHandAndTheLadderInTheAirAreTheSameLength()
        {
            // Two ladders that disagree about how many rungs there are means a tier with a new
            // weapon and the old shot, or the reverse -- and nothing in the game would say so.
            Assert.That(HeroEmitter.MaxTier, Is.EqualTo(SignalFx.Curves.MaxTier));
            Assert.That(HeroEmitter.MaxTier, Is.EqualTo(GunTiers.MaxTier));
            Assert.That(GunTiers.Names.Length, Is.EqualTo(HeroEmitter.MaxTier + 1));
        }
    }
}
