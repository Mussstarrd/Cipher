#nullable enable
using System.Collections.Generic;
using Cipher.Game;
using Cipher.Sim.Agents;
using Cipher.Sim.Core;
using Cipher.Sim.Grid;
using NUnit.Framework;
using UnityEngine;

namespace Cipher.Game.Tests
{
    /// <summary>
    /// How bodies are handed out once there is more than one kind of body.
    ///
    /// The crowd already had one hard-won rule -- a slot KEEPS its agent, because assigning by
    /// distance rank made characters "scan switching from skin to skin to skin". Typed slots add a
    /// second one that fails in the same family: a body may only be given to an agent of its own
    /// class, or the breacher walks up wearing a delivery walker's chassis.
    /// </summary>
    public sealed class CivilianCrowdTests
    {
        private readonly List<GameObject> _rubbish = new List<GameObject>();

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in _rubbish) if (go != null) Object.DestroyImmediate(go);
            _rubbish.Clear();
        }

        private Transform NewSlot()
        {
            var go = new GameObject("Slot");
            _rubbish.Add(go);
            return go.transform;
        }

        private AgentWorld World(int capacity = 64)
        {
            var map = new GridMap(48, 48);
            var field = new FlowField(map);
            field.Compute(46, 24);
            return new AgentWorld(map, field, new SimConfig(), initialCapacity: capacity);
        }

        private CivilianCrowd Crowd(params (BodyClass cls, int count)[] classes)
        {
            int total = 0;
            foreach (var c in classes) total += c.count;
            var root = new GameObject("CrowdRoot");
            _rubbish.Add(root);
            var crowd = new CivilianCrowd(root.transform, total);
            foreach (var c in classes)
                for (int i = 0; i < c.count; i++) crowd.AddSlot(NewSlot(), c.cls);
            return crowd;
        }

        [Test]
        public void AnAgentOnlyEverGetsABodyOfItsOwnClass()
        {
            var world = World();
            var crowd = Crowd((BodyClass.Signed, 4), (BodyClass.Spitter, 2));

            // Four spitters standing closest, then four ordinary agents behind them. Without typed
            // assignment the nearest four would take the four Signed bodies and two people would
            // walk up wearing herbicide tanks.
            var spitters = new List<int>();
            for (int i = 0; i < 4; i++) spitters.Add(world.SpawnArchetype(new Vec2(10f + i * 0.5f, 10f), Archetype.Spitter));
            for (int i = 0; i < 4; i++) world.Spawn(new Vec2(20f + i * 0.5f, 10f), 50f);

            crowd.Classify = id => CrowdCasting.ClassOf(world.ArchetypeOf(id), id, 1);
            crowd.PromoteRange = 500f;
            crowd.Sync(world, new Vector3(10f, 0f, 10f), 0.016f);

            for (int slot = 0; slot < crowd.SlotCount; slot++)
            {
                int held = crowd.AgentInSlot(slot);
                if (held < 0) continue;
                Assert.That(CrowdCasting.ClassOf(world.ArchetypeOf(held), held, 1),
                            Is.EqualTo(crowd.ClassOfSlot(slot)),
                            $"slot {slot} is wearing the wrong species");
            }
        }

        [Test]
        public void AClassWithNoBodiesLeftStaysInTheFarField()
        {
            // Two spitter bodies and four spitters: the other two must NOT borrow a person's body.
            // A pill that says "green" is better than a silhouette that says "ordinary citizen".
            var world = World();
            var crowd = Crowd((BodyClass.Signed, 8), (BodyClass.Spitter, 2));
            for (int i = 0; i < 4; i++) world.SpawnArchetype(new Vec2(10f + i * 0.5f, 10f), Archetype.Spitter);

            crowd.Classify = id => CrowdCasting.ClassOf(world.ArchetypeOf(id), id, 1);
            crowd.PromoteRange = 500f;
            crowd.Sync(world, new Vector3(10f, 0f, 10f), 0.016f);

            Assert.That(crowd.Promoted.Count, Is.EqualTo(2),
                        "only as many spitters as there are spitter bodies may be promoted");
        }

        [Test]
        public void TheFrameBudgetCapsPromotionsBelowTheNumberOfBodiesBuilt()
        {
            // The pool is deliberately overbuilt so no class starves. The budget is what keeps the
            // overbuild off the frame, and it is the number to argue with if the crowd costs too
            // much -- so it has to actually bind.
            var world = World(256);
            var crowd = Crowd((BodyClass.Signed, 40));
            crowd.ActiveBudget = 12;
            crowd.PromoteRange = 500f;
            for (int i = 0; i < 30; i++) world.Spawn(new Vec2(10f + i * 0.3f, 10f), 50f);

            crowd.Sync(world, new Vector3(10f, 0f, 10f), 0.016f);
            Assert.That(crowd.Promoted.Count, Is.EqualTo(12));
        }

        [Test]
        public void ASlotStillKeepsItsAgent()
        {
            // The original rule, re-asserted because typed slots rewrote the assignment loop that
            // enforces it. The owner found this one; it must not come back.
            var world = World();
            var crowd = Crowd((BodyClass.Signed, 3));
            crowd.PromoteRange = 500f;

            int a = world.Spawn(new Vec2(10f, 10f), 50f);
            int b = world.Spawn(new Vec2(12f, 10f), 50f);
            world.Spawn(new Vec2(14f, 10f), 50f);

            crowd.Sync(world, new Vector3(9f, 0f, 10f), 0.016f);
            var before = new int[crowd.SlotCount];
            for (int i = 0; i < crowd.SlotCount; i++) before[i] = crowd.AgentInSlot(i);

            // Swap who is nearest by moving the camera to the far end.
            crowd.Sync(world, new Vector3(16f, 0f, 10f), 0.016f);
            for (int i = 0; i < crowd.SlotCount; i++)
                Assert.That(crowd.AgentInSlot(i), Is.EqualTo(before[i]),
                            $"slot {i} changed occupant because the distance order changed");
            Assert.That(before, Has.Member(a).And.Member(b));
        }

        [Test]
        public void AnUnclassifiedCrowdBehavesTheWayItDidBeforeThereWereClasses()
        {
            var world = World();
            var crowd = Crowd((BodyClass.Signed, 4));
            crowd.PromoteRange = 500f;
            for (int i = 0; i < 4; i++) world.Spawn(new Vec2(10f + i, 10f), 50f);

            crowd.Classify = null;
            crowd.Sync(world, new Vector3(10f, 0f, 10f), 0.016f);
            Assert.That(crowd.Promoted.Count, Is.EqualTo(4));
        }
    }
}
