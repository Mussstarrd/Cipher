#nullable enable
using System.Collections.Generic;
using Cipher.Game;
using Cipher.Sim.Agents;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// The second enemy class, and the distance tiers that have to keep it legible.
    ///
    /// Two owner notes, 2026-09-11: "like 30% of them should be those humanoid robots", and "I can
    /// see the enemies far back on the map but they are still just red pills". What both come down
    /// to is the same property -- CAN YOU TELL WHAT IS COMING AT YOU, AT THE RANGE YOU CAN SEE IT --
    /// and that property is exactly what a later tuning pass erodes without anybody noticing,
    /// because nothing breaks, the game just quietly stops saying which one is the breacher.
    /// </summary>
    public sealed class CrowdClassTests
    {
        // ---- casting ------------------------------------------------------------------------

        [Test]
        public void RoughlyThirtyPercentOfTheWaveIsMachines()
        {
            int machines = 0, n = 4000;
            for (int id = 0; id < n; id++)
                if (CrowdCasting.ClassOf(Archetype.Runner, id, 7) == BodyClass.Humanoid) machines++;

            float share = machines / (float)n;
            Assert.That(share, Is.EqualTo(CrowdCasting.HumanoidShare).Within(0.03f),
                        "the owner asked for about 30% and this is what he will count");
        }

        [Test]
        public void AnAgentIsTheSameSpeciesEveryTimeItIsAsked()
        {
            // The bug this prevents is the one the owner already reported one level down: a body
            // that changes model as it walks. If the CLASS could change, an agent that left
            // promotion range and came back would come back a robot.
            for (int id = 0; id < 500; id++)
            {
                var first = CrowdCasting.ClassOf(Archetype.Runner, id, 99);
                for (int again = 0; again < 3; again++)
                    Assert.That(CrowdCasting.ClassOf(Archetype.Runner, id, 99), Is.EqualTo(first),
                                $"agent {id} changed species between two asks");
            }
        }

        [Test]
        public void MachinesAreNotHandedOutInRuns()
        {
            // The spawn director hands out consecutive ids, so a hash that correlates on adjacent
            // inputs would deliver a wave as blocks of one species. The avalanche exists for this.
            //
            // PINNED TO THE DESIGN SHARE, NOT THE LIVE ONE. The machine class is switched off while
            // it has no art to draw it with, and at a share of zero every body is Signed, every run
            // is 2000 long, and this test fails while testing nothing -- the distribution it exists
            // to guard is not even exercised. What is under test here is the HASH, so the hash is
            // what it sets up.
            float live = CrowdCasting.HumanoidShare;
            CrowdCasting.HumanoidShare = CrowdCasting.DesignShare;
            try
            {
                int longest = 0, run = 0;
                var previous = BodyClass.Signed;
                for (int id = 0; id < 2000; id++)
                {
                    var c = CrowdCasting.ClassOf(Archetype.Runner, id, 3);
                    run = c == previous ? run + 1 : 1;
                    previous = c;
                    longest = Mathf.Max(longest, run);
                }
                Assert.That(longest, Is.LessThan(24), "the wave arrives in species blocks");
            }
            finally { CrowdCasting.HumanoidShare = live; }
        }

        [Test]
        public void Adr003DecidesTheTwoNamedArchetypes()
        {
            // "The Sapper is a person with a toolbox who used to be a contractor. The Spitter
            // becomes a hacked humanoid with an industrial sprayer." Not a judgement call, and not
            // something a later pass should swap around on a whim.
            for (int id = 0; id < 64; id++)
            {
                Assert.That(CrowdCasting.ClassOf(Archetype.Sapper, id, id), Is.EqualTo(BodyClass.Sapper));
                Assert.That(CrowdCasting.ClassOf(Archetype.Spitter, id, id), Is.EqualTo(BodyClass.Spitter));
            }
            Assert.That(CrowdCasting.IsMachine(BodyClass.Spitter), Is.True, "the Spitter is a machine");
            Assert.That(CrowdCasting.IsMachine(BodyClass.Sapper), Is.False, "the Sapper is a person");
        }

        [Test]
        public void ThePoolIsOverbuiltAndTheFrameBudgetIsNot()
        {
            Assert.That(CrowdCasting.TotalBuilt, Is.GreaterThan(CrowdCasting.ActiveBudget),
                        "with four classes a pool sized to the budget starves whichever class a "
                      + "wave happens to be short of");
            Assert.That(CrowdCasting.Built.Length, Is.EqualTo(4));
            foreach (int n in CrowdCasting.Built) Assert.That(n, Is.GreaterThan(0));

            // Sized against the wave table: The Gate peaks at 6% Spitter and 4% Sapper of 150, so
            // a dozen-odd of each covers the worst wave with room, and the rest goes to the crowd.
            Assert.That(CrowdCasting.Built[(int)BodyClass.Signed],
                        Is.GreaterThan(CrowdCasting.Built[(int)BodyClass.Humanoid]),
                        "most of a wave is still ordinary people -- ADR-003's whole point");
        }

        [Test]
        public void AnArchetypeTheCastingHasNeverHeardOfGetsNoCrowdBody()
        {
            // ADR-011's Collector landed while this was being written: it builds its own body, one
            // or two to a field. If an unknown archetype fell through to "ordinary citizen" it would
            // be drawn TWICE -- its own model plus a borrowed coat -- and the next archetype after
            // it would do the same. Own is the default, and the crowd excludes it by construction
            // because no slots are built for it.
            foreach (Archetype a in System.Enum.GetValues(typeof(Archetype)))
            {
                var c = CrowdCasting.ClassOf(a, 5, 5);
                bool known = a == Archetype.Runner || a == Archetype.Sapper || a == Archetype.Spitter;
                if (known) Assert.That(CrowdCasting.HasCrowdBody(c), Is.True, $"{a} lost its body");
                else Assert.That(c, Is.EqualTo(BodyClass.Own), $"{a} would borrow a citizen's body");
            }
            Assert.That(CrowdCasting.HasCrowdBody(BodyClass.Own), Is.False);
            Assert.That(CrowdCasting.Built.Length, Is.EqualTo((int)BodyClass.Own),
                        "a slot count for Own would hand crowd bodies to things that have their own");
        }

        // ---- the silhouette contract ----------------------------------------------------------

        [Test]
        public void AMachineIsNotThesameShapeAsAPerson()
        {
            Assert.That(MachineBody.Height, Is.GreaterThan(CrowdImpostors.PersonHeight + 0.2f),
                        "a machine that is a person's height has lost its best distance tell");
            Assert.That(MachineBody.ShoulderSpan, Is.GreaterThan(0.52f),
                        "narrow shoulders read as a man");
            Assert.That(MachineBody.HipHeight / MachineBody.Height, Is.GreaterThan(0.45f),
                        "a person's hip is well under half their height; a walker's is not");
        }

        [Test]
        public void TheSensorBlockIsAnAntiHeadAndSitsOnTheShoulders()
        {
            // This is the single strongest tell and the one most likely to be "tidied" later.
            Assert.That(MachineBody.HeadWidth, Is.GreaterThan(MachineBody.HeadHeight * 1.5f),
                        "the sensor block has become a head");
            Assert.That(MachineBody.NeckLength, Is.EqualTo(0f),
                        "a neck is what makes a silhouette read as a person");
        }

        [Test]
        public void TheDeclaredHeightIsTheHeightThePartsAddUpTo()
        {
            // It was not: 2.06 was written down beside parts totalling 1.92. A number that is right
            // in the comment and wrong on screen is worse than no number.
            float parts = MachineBody.HipHeight + MachineBody.TorsoHeight
                        + MachineBody.YokeHeight + MachineBody.HeadHeight;
            Assert.That(MachineBody.Height, Is.EqualTo(parts).Within(1e-4f));
        }

        // ---- gait ------------------------------------------------------------------------------

        [Test]
        public void AStandingMachineDoesNotWalk()
        {
            Gait.Pose(1.3f, 0f, out float l, out float r, out float bob, out float roll);
            Assert.That(l, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(r, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(bob, Is.EqualTo(0f).Within(1e-4f));
            Assert.That(roll, Is.EqualTo(0f).Within(1e-4f));
        }

        [Test]
        public void TheLegsAlwaysOpposeEachOther()
        {
            for (float phase = 0f; phase < 7f; phase += 0.31f)
            {
                Gait.Pose(phase, 2f, out float l, out float r, out _, out _);
                Assert.That(l + r, Is.EqualTo(0f).Within(1e-4f), $"legs in phase at {phase}");
            }
        }

        [Test]
        public void TheBodyNeverRisesAboveItsRest()
        {
            // A bob that goes UP lifts the feet off the ground and floats the contact shadow, which
            // is the exact "hovering a hand's width off the ground" the blob shadows exist to fix.
            for (float phase = 0f; phase < 7f; phase += 0.17f)
            {
                Gait.Pose(phase, 3f, out _, out _, out float bob, out _);
                Assert.That(bob, Is.LessThanOrEqualTo(1e-5f), $"the machine hopped at {phase}");
            }
        }

        [Test]
        public void TheStrideSnapsRatherThanSwinging()
        {
            // A pure sine is a pendulum, which is how a person walks. Snap() is what makes it a
            // servo, and "make the gait smoother" is exactly the well-meant change that undoes it.
            Assert.That(Gait.Snap(0.5f), Is.GreaterThan(0.5f),
                        "the stride has gone back to a pendulum");
            Assert.That(Gait.Snap(-0.5f), Is.EqualTo(-Gait.Snap(0.5f)).Within(1e-5f));
            Assert.That(Gait.Snap(1f), Is.EqualTo(1f).Within(1e-5f), "the extremes must not overshoot");
        }

        [Test]
        public void PhaseAdvancesOnlyWhileMoving()
        {
            Assert.That(Gait.AdvancePhase(2f, 0f, 0.016f), Is.EqualTo(2f).Within(1e-6f));
            Assert.That(Gait.AdvancePhase(2f, 3f, 0.016f), Is.GreaterThan(2f));
        }

        // ---- the far field ---------------------------------------------------------------------

        [Test]
        public void EveryClassHasASilhouetteAtEveryTier()
        {
            var material = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
            var impostors = new CrowdImpostors(material);
            try
            {
                for (int c = 0; c < CrowdCasting.Built.Length; c++)
                    for (int lod = 0; lod < CrowdImpostors.LodCount; lod++)
                    {
                        var mesh = impostors.MeshFor((BodyClass)c, lod);
                        Assert.That(mesh, Is.Not.Null, $"{(BodyClass)c} lod {lod}");
                        Assert.That(mesh!.vertexCount, Is.GreaterThan(0));
                    }
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void TheClassReadSurvivesToTheCheapestTier()
        {
            // The point of the middle tier is NOT that something is there -- a capsule did that.
            // It is that a machine still reads as a machine when it is twelve pixels tall. So the
            // height difference has to survive the simplification, at BOTH tiers.
            var material = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
            var impostors = new CrowdImpostors(material);
            try
            {
                for (int lod = 0; lod < CrowdImpostors.LodCount; lod++)
                {
                    float person = impostors.MeshFor(BodyClass.Signed, lod)!.bounds.max.y;
                    float machine = impostors.MeshFor(BodyClass.Humanoid, lod)!.bounds.max.y;
                    Assert.That(machine, Is.GreaterThan(person + 0.15f),
                                $"at lod {lod} a machine is no longer visibly taller than a person");

                    float sapper = impostors.MeshFor(BodyClass.Sapper, lod)!.bounds.max.y;
                    Assert.That(sapper, Is.GreaterThan(person),
                                $"at lod {lod} the Sapper lost his hard hat");
                }
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void AClassWithItsOwnBodyHasNoSilhouetteAndQueuesNothing()
        {
            var material = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
            var impostors = new CrowdImpostors(material);
            try
            {
                Assert.That(impostors.MeshFor(BodyClass.Own, 0), Is.Null);
                impostors.Begin();
                impostors.Add(BodyClass.Own, Vector3.zero, Vector3.forward, 10f, Vector4.one);
                Assert.That(impostors.Queued, Is.Zero, "a Collector was drawn as a crowd silhouette");
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void ImpostorsStandOnTheGroundAndFaceTheRightWay()
        {
            var material = new Material(Shader.Find("Unlit/Color") ?? Shader.Find("Sprites/Default"));
            var impostors = new CrowdImpostors(material);
            try
            {
                for (int c = 0; c < CrowdCasting.Built.Length; c++)
                    for (int lod = 0; lod < CrowdImpostors.LodCount; lod++)
                    {
                        var b = impostors.MeshFor((BodyClass)c, lod)!.bounds;
                        Assert.That(b.min.y, Is.GreaterThanOrEqualTo(-0.02f),
                                    $"{(BodyClass)c} lod {lod} sinks into the ground");
                        Assert.That(b.min.y, Is.LessThan(0.12f),
                                    $"{(BodyClass)c} lod {lod} floats above it");
                    }
            }
            finally { Object.DestroyImmediate(material); }
        }

        [Test]
        public void TheFarFieldIsNotRedAndIsNotUniform()
        {
            // The literal complaint. A single flat colour for the whole crowd was the pre-ADR-003
            // infected flood; ADR-003's crowd is "clean clothes, ordinary faces".
            var seen = new HashSet<Color>();
            for (int id = 0; id < 400; id++)
            {
                var c = CrowdImpostors.DressOf(BodyClass.Signed, id);
                seen.Add(c);
                Assert.That(c.r, Is.LessThan(0.75f).Or.LessThan(c.g + 0.30f),
                            $"agent {id} came out arterial red");
            }
            Assert.That(seen.Count, Is.GreaterThan(4), "the crowd is all wearing the same coat");
        }

        [Test]
        public void TheTwoArchetypesKeepTheColoursThePlayerLearned()
        {
            // The pills were orange and green and the counter-play is completely different. The
            // SHAPE is what is new here; changing the hue at the same time would throw away five
            // waves of the player's training for nothing.
            var sapper = CrowdImpostors.DressOf(BodyClass.Sapper, 11);
            Assert.That(sapper.r, Is.GreaterThan(0.8f));
            Assert.That(sapper.g, Is.InRange(0.25f, 0.6f));
            Assert.That(sapper.b, Is.LessThan(0.2f));

            var spitter = CrowdImpostors.DressOf(BodyClass.Spitter, 11);
            Assert.That(spitter.g, Is.GreaterThan(spitter.r * 1.8f), "the Spitter stopped being green");
            Assert.That(spitter.g, Is.GreaterThan(spitter.b * 1.8f));
        }

        [Test]
        public void DressIsStablePerAgent()
        {
            for (int id = 0; id < 200; id++)
            {
                var first = CrowdImpostors.DressOf(BodyClass.Signed, id);
                Assert.That(CrowdImpostors.DressOf(BodyClass.Signed, id), Is.EqualTo(first),
                            $"agent {id} changed clothes between frames");
            }
        }

        // ---- the Sapper's dressing --------------------------------------------------------------

        [Test]
        public void HiVisKeepsTheDarkPartsDark()
        {
            // A man who is uniformly orange from sole to scalp reads as a bug. Boots and hair stay.
            var boot = CrowdBodies.Tint(new Color(0.06f, 0.05f, 0.05f));
            Assert.That(boot.r, Is.LessThan(0.35f), "his boots turned into traffic cones");

            var shirt = CrowdBodies.Tint(new Color(0.55f, 0.55f, 0.58f));
            Assert.That(shirt.r, Is.GreaterThan(shirt.b * 2f), "the coveralls are not hi-vis");
        }

        [Test]
        public void TheSapperIsTakenOutOfTheOrdinaryCrowd()
        {
            var worker = new GameObject(CrowdBodies.SapperModelName);
            var hero = new GameObject("Adventurer_Civilian");
            var other = new GameObject("Suit_Civilian");
            try
            {
                CrowdBodies.SplitPool(new List<GameObject> { worker, hero, other },
                                      "Adventurer_Civilian", out var signed, out var sapper);
                Assert.That(sapper, Is.SameAs(worker));
                Assert.That(signed, Has.No.Member(worker),
                            "if forty civilians wear the breacher's silhouette it means nothing");
                Assert.That(signed, Has.No.Member(hero), "the player is walking in the horde");
                Assert.That(signed, Has.Member(other));
            }
            finally
            {
                Object.DestroyImmediate(worker);
                Object.DestroyImmediate(hero);
                Object.DestroyImmediate(other);
            }
        }

        [Test]
        public void EverySpitterIsASprayerAndTheServiceKindsAreDealtRound()
        {
            var kinds = new HashSet<MachineKind>();
            for (int i = 0; i < 12; i++)
            {
                Assert.That(CrowdBodies.KindFor(BodyClass.Spitter, i), Is.EqualTo(MachineKind.Sprayer));
                kinds.Add(CrowdBodies.KindFor(BodyClass.Humanoid, i));
            }
            Assert.That(kinds.Count, Is.EqualTo(3), "a crowd of identical delivery walkers");
            Assert.That(kinds, Has.No.Member(MachineKind.Sprayer),
                        "an ordinary humanoid wearing the Spitter's tank is a lie about the threat");
        }
    }
}
