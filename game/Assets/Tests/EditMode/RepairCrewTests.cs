#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Game.Match;
using Cipher.Sim.Core;
using NUnit.Framework;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// ADR-009: between waves the wife and the nine-year-old get out and fix things, and they are
    /// never killed -- the stake is the repair, not the family. These tests pin that line down,
    /// because it is exactly the kind of decision a later change erodes by accident.
    /// </summary>
    public sealed class RepairCrewTests
    {
        private static readonly Vec2 Truck = new Vec2(10f, 10f);

        private static RepairCrew Crew()
        {
            var crew = new RepairCrew();
            crew.Reset(Truck);
            return crew;
        }

        private static List<RepairCrew.Job> OneWreck(float condition = 0.3f, float x = 16f, float y = 10f)
            => new List<RepairCrew.Job> { new RepairCrew.Job(7, new Vec2(x, y), condition) };

        private static Func<Vec2, float, int> Clear => (_, __) => 0;
        private static Func<Vec2, float, int> Swarmed => (_, __) => 4;

        [Test]
        public void TheyStayInTheTruckWhenThereIsNothingToFix()
        {
            var crew = Crew();
            float done = crew.Tick(0.5f, windowOpen: true, OneWreck(condition: 1f), Clear);

            Assert.That(done, Is.Zero);
            Assert.That(crew.Outside, Is.False);
            Assert.That(crew.State, Is.EqualTo(RepairCrew.Stance.Inside));
        }

        [Test]
        public void TheyStayInTheTruckDuringAWave()
        {
            var crew = Crew();
            crew.Tick(0.5f, windowOpen: false, OneWreck(), Clear);
            Assert.That(crew.Outside, Is.False, "nobody gets out while a wave is on the field");
        }

        [Test]
        public void TheyWalkOutAndFixTheWorstThing()
        {
            var crew = Crew();
            var jobs = new List<RepairCrew.Job>
            {
                new RepairCrew.Job(1, new Vec2(13f, 10f), 0.8f),
                new RepairCrew.Job(2, new Vec2(14f, 10f), 0.2f),   // the worst one
            };

            crew.Tick(0.1f, true, jobs, Clear);
            Assert.That(crew.JobIndex, Is.EqualTo(2));
            Assert.That(crew.State, Is.EqualTo(RepairCrew.Stance.Walking));

            float total = 0f;
            for (int i = 0; i < 60; i++) total += crew.Tick(0.1f, true, jobs, Clear);

            Assert.That(crew.State, Is.EqualTo(RepairCrew.Stance.Working));
            Assert.That(total, Is.GreaterThan(0f));
            Assert.That(crew.RepairDelivered, Is.EqualTo(total).Within(1e-3f));
        }

        [Test]
        public void EnemiesNearTheJobSendThemBackAndTheRepairSimplyDoesNotHappen()
        {
            var crew = Crew();
            var jobs = OneWreck();

            // Out and working first, with a clear field.
            for (int i = 0; i < 60; i++) crew.Tick(0.1f, true, jobs, Clear);
            Assert.That(crew.State, Is.EqualTo(RepairCrew.Stance.Working));

            float done = crew.Tick(0.1f, true, jobs, Swarmed);
            Assert.That(done, Is.Zero, "the repair is the thing that is lost");
            Assert.That(crew.AbandonedThisTick, Is.True);
            Assert.That(crew.State, Is.EqualTo(RepairCrew.Stance.Returning));
            Assert.That(crew.JobIndex, Is.EqualTo(-1));
        }

        [Test]
        public void TheyMakeItHomeAndAreNeverKilled()
        {
            var crew = Crew();
            var jobs = OneWreck();
            for (int i = 0; i < 60; i++) crew.Tick(0.1f, true, jobs, Clear);

            for (int i = 0; i < 200; i++) crew.Tick(0.1f, true, jobs, Swarmed);

            Assert.That(crew.State, Is.EqualTo(RepairCrew.Stance.Inside));
            Assert.That(crew.AdultPosition.X, Is.EqualTo(Truck.X).Within(0.01f));
            Assert.That(crew.AdultPosition.Y, Is.EqualTo(Truck.Y).Within(0.01f));
        }

        [Test]
        public void TheyWillNotComeOutForSomethingAlreadySurrounded()
        {
            var crew = Crew();
            crew.Tick(0.5f, true, OneWreck(), Swarmed);
            Assert.That(crew.Outside, Is.False, "they do not walk into it in the first place");
        }

        [Test]
        public void TheyWillNotCrossTheMapForIt()
        {
            var crew = Crew();
            var faraway = OneWreck(condition: 0.1f, x: Truck.X + 90f, y: Truck.Y);
            crew.Tick(0.5f, true, faraway, Clear);
            Assert.That(crew.Outside, Is.False);
        }

        [Test]
        public void AWaveStartingSendsThemHomeMidJob()
        {
            var crew = Crew();
            var jobs = OneWreck();
            for (int i = 0; i < 30; i++) crew.Tick(0.1f, true, jobs, Clear);
            Assert.That(crew.Outside, Is.True);

            crew.Tick(0.1f, windowOpen: false, jobs, Clear);
            Assert.That(crew.State, Is.EqualTo(RepairCrew.Stance.Returning));
        }

        [Test]
        public void AFinishedJobIsLetGoOfRatherThanWorkedForever()
        {
            var crew = Crew();
            var jobs = OneWreck();
            for (int i = 0; i < 60; i++) crew.Tick(0.1f, true, jobs, Clear);
            Assert.That(crew.State, Is.EqualTo(RepairCrew.Stance.Working));

            var fixedUp = new List<RepairCrew.Job> { new RepairCrew.Job(7, new Vec2(16f, 10f), 1f) };
            crew.Tick(0.1f, true, fixedUp, Clear);
            Assert.That(crew.JobIndex, Is.EqualTo(-1));
        }

        [Test]
        public void AJobThatVanishesUnderThemIsLetGoOf()
        {
            var crew = Crew();
            var jobs = OneWreck();
            for (int i = 0; i < 60; i++) crew.Tick(0.1f, true, jobs, Clear);

            // Sold, destroyed, or packed onto the truck: the index is simply not in the list.
            float done = crew.Tick(0.1f, true, new List<RepairCrew.Job>(), Clear);
            Assert.That(done, Is.Zero);
            Assert.That(crew.JobIndex, Is.EqualTo(-1));
        }

        [Test]
        public void TheChildTrailsTheAdultRatherThanStandingInThem()
        {
            var crew = Crew();
            var jobs = OneWreck();
            for (int i = 0; i < 10; i++) crew.Tick(0.1f, true, jobs, Clear);

            float gap = Vec2.DistanceSquared(crew.AdultPosition, crew.ChildPosition);
            Assert.That(gap, Is.GreaterThan(0.2f), "two people, not one");
            Assert.That(gap, Is.LessThan(4f), "a nine-year-old does not wander off");
        }
    }
}
