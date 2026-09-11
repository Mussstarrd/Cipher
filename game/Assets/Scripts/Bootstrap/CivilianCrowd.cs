#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Agents;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Draws the nearest slice of the swarm as real walking people, and leaves the rest as instanced
    /// capsules.
    ///
    /// The honest shape of this: a skinned character costs far more than an instanced capsule, so a
    /// thousand of them is not affordable. What IS affordable is the couple of hundred closest to the
    /// camera, which is also the only part of the crowd a player can actually read. Everything beyond
    /// that keeps the old cheap representation, and the transition happens far enough out that the
    /// switch is not visible.
    ///
    /// This is a bridge, not the destination. The vertex-animation-texture path in ADR-007 is what
    /// eventually makes all thousand real. This exists so the game looks like itself in the meantime,
    /// and because a pool of pre-built characters is exactly what that path will need anyway.
    /// </summary>
    public sealed class CivilianCrowd
    {
        /// <summary>How many agents get a real body. Beyond this they stay capsules.</summary>
        public int Capacity { get; }

        /// <summary>Agents further than this from the camera are never promoted.</summary>
        public float PromoteRange { get; set; } = 34f;

        private readonly List<Transform> _slots = new List<Transform>();
        private readonly List<Vector3> _lastPosition = new List<Vector3>();
        private readonly List<int> _assigned = new List<int>();
        private readonly Transform _root;

        /// <summary>Agent ids currently wearing a real body, so the capsule pass can skip them.</summary>
        public HashSet<int> Promoted { get; } = new HashSet<int>();

        public CivilianCrowd(Transform root, int capacity)
        {
            _root = root;
            Capacity = Mathf.Max(0, capacity);
        }

        public int SlotCount => _slots.Count;

        public void AddSlot(Transform slot)
        {
            // Capacity is the budget the caller sized the pool against; exceeding it silently would
            // put more skinned characters on screen than the frame was costed for. It used to be
            // read by nobody, which made it a comment pretending to be a constraint.
            if (_slots.Count >= Capacity)
                throw new InvalidOperationException(
                    $"CivilianCrowd was built for {Capacity} bodies and AddSlot was called again.");

            _slots.Add(slot);
            _lastPosition.Add(slot.position);
            _assigned.Add(-1);
            slot.gameObject.SetActive(false);
        }

        /// <summary>
        /// Assigns bodies to the closest agents and moves them. Call once per frame AFTER the sim
        /// has stepped.
        /// </summary>
        public void Sync(AgentWorld world, Vector3 cameraPosition, float dt)
        {
            Promoted.Clear();
            if (_slots.Count == 0 || world == null) return;

            // Pick the nearest living agents. This is a bounded selection, not a sort: only the
            // closest _slots.Count of a thousand candidates can ever get a body, so the list never
            // grows past that and the great majority of agents cost one compare against the current
            // worst. The obvious version sorted the whole in-range set and threw most of that work
            // away every frame; the comment above it claimed otherwise, which is how it survived.
            _nearest.Clear();
            int capacity = _slots.Count;
            float rangeSq = PromoteRange * PromoteRange;
            var flatCamera = new Vector3(cameraPosition.x, 0f, cameraPosition.z);

            for (int id = 0; id < world.Count; id++)
            {
                if (!world.IsAlive(id)) continue;
                var p = world.PositionOf(id);
                var world3 = new Vector3(p.X, 0f, p.Y);
                float d = (world3 - flatCamera).sqrMagnitude;
                if (d > rangeSq) continue;
                if (_nearest.Count == capacity && d >= _nearest[capacity - 1].dist) continue;

                // Binary search for the insertion point, then insert and drop the worst. The list
                // stays sorted, so index 0 is always the nearest.
                int lo = 0, hi = _nearest.Count;
                while (lo < hi)
                {
                    int mid = (lo + hi) >> 1;
                    if (_nearest[mid].dist <= d) lo = mid + 1; else hi = mid;
                }
                if (_nearest.Count == capacity) _nearest.RemoveAt(capacity - 1);
                _nearest.Insert(lo, (d, id, world3));
            }

            int n = Mathf.Min(capacity, _nearest.Count);

            for (int i = 0; i < n; i++)
            {
                var (_, id, position) = _nearest[i];
                var slot = _slots[i];

                if (!slot.gameObject.activeSelf) slot.gameObject.SetActive(true);

                // Face the way it is travelling. Reusing a slot for a different agent would spin the
                // body wildly for one frame, so a change of occupant resets the heading instead.
                Vector3 previous = _assigned[i] == id ? _lastPosition[i] : position;
                Vector3 delta = position - previous;
                if (delta.sqrMagnitude > 1e-6f)
                {
                    var look = Quaternion.LookRotation(delta.normalized, Vector3.up);
                    slot.rotation = Quaternion.Slerp(slot.rotation, look, 1f - Mathf.Exp(-12f * dt));
                }

                slot.position = position;
                _lastPosition[i] = position;
                _assigned[i] = id;
                Promoted.Add(id);
            }

            for (int i = n; i < _slots.Count; i++)
            {
                if (_slots[i].gameObject.activeSelf) _slots[i].gameObject.SetActive(false);
                _assigned[i] = -1;
            }
        }

        private readonly List<(float dist, int id, Vector3 position)> _nearest =
            new List<(float, int, Vector3)>(512);
    }
}
