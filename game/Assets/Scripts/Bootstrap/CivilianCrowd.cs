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

        /// <summary>How long a body stays on the ground before its slot is recycled. The death clip's length.</summary>
        public float DeathSeconds { get; set; } = 1.1f;

        /// <summary>Raised when a slot's occupant has just been killed, so the view plays the fall.</summary>
        public System.Action<int>? OnSlotDied;

        /// <summary>Raised when a slot's occupant took damage and lived, so the view flinches.</summary>
        public System.Action<int>? OnSlotHurt;

        private readonly List<float> _dying = new List<float>();
        private readonly List<float> _lastHealth = new List<float>();

        /// <summary>True while a slot is playing out a death and must not be reassigned.</summary>
        public bool IsDying(int slot) => slot >= 0 && slot < _dying.Count && _dying[slot] > 0f;

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
            _dying.Add(0f);
            _lastHealth.Add(-1f);
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

            int capacity = _slots.Count;
            float rangeSq = PromoteRange * PromoteRange;
            var flatCamera = new Vector3(cameraPosition.x, 0f, cameraPosition.z);

            // Pick the nearest living agents. A bounded selection, not a sort: only the closest
            // _slots.Count of a thousand candidates can ever get a body, so the list never grows
            // past that and the great majority of agents cost one compare against the current worst.
            _nearest.Clear();
            for (int id = 0; id < world.Count; id++)
            {
                if (!world.IsAlive(id)) continue;
                var p = world.PositionOf(id);
                var world3 = new Vector3(p.X, 0f, p.Y);
                float d = (world3 - flatCamera).sqrMagnitude;
                if (d > rangeSq) continue;
                if (_nearest.Count == capacity && d >= _nearest[capacity - 1].dist) continue;

                int lo = 0, hi = _nearest.Count;
                while (lo < hi)
                {
                    int mid = (lo + hi) >> 1;
                    if (_nearest[mid].dist <= d) lo = mid + 1; else hi = mid;
                }
                if (_nearest.Count == capacity) _nearest.RemoveAt(capacity - 1);
                _nearest.Insert(lo, (d, id, world3));
            }

            // ---- Identity, which is the part that matters -------------------------------------
            //
            // The obvious version handed slot 0 to the nearest agent, slot 1 to the next, and so on,
            // every frame. The SET it chose was right and the ASSIGNMENT was garbage: two agents
            // swapping distance order swapped bodies, so the person standing in a given spot changed
            // model several times a second. The owner's words: "the character skins are very quickly
            // like scan switching from skin to skin to skin."
            //
            // So a slot KEEPS its agent for as long as that agent is still in the set, and only a
            // slot whose occupant died or walked out of range is handed to somebody new.
            _chosen.Clear();
            for (int i = 0; i < _nearest.Count; i++) _chosen.Add(_nearest[i].id, _nearest[i].position);

            _free.Clear();
            _holders.Clear();
            for (int i = 0; i < _slots.Count; i++)
            {
                // A body that is DYING keeps its slot until the clip has played out. Releasing it
                // the instant the sim marked the agent dead is why enemies vanished mid-stride --
                // the death animation was on disk and there was never a body left to play it on.
                if (_dying[i] > 0f)
                {
                    _dying[i] -= dt;
                    if (_dying[i] > 0f) continue;
                    _assigned[i] = -1;
                    _free.Add(i);
                    continue;
                }

                int held = _assigned[i];
                if (held >= 0 && _chosen.ContainsKey(held)) { _holders.Add(held); continue; }

                // Alive-but-gone means it walked out of range: hand the body over. Dead means it
                // fell right here: start the death and hold the body where it stood.
                if (held >= 0 && !world.IsAlive(held) && DeathSeconds > 0f)
                {
                    _dying[i] = DeathSeconds;
                    OnSlotDied?.Invoke(i);
                    continue;
                }

                _assigned[i] = -1;
                _free.Add(i);
            }

            // Hand the free slots to the nearest agents that do not have one, nearest first.
            int nextFree = 0;
            for (int i = 0; i < _nearest.Count && nextFree < _free.Count; i++)
            {
                int id = _nearest[i].id;
                if (!_holders.Add(id)) continue;   // already wearing a body
                int slot = _free[nextFree++];
                _assigned[slot] = id;
                // A new occupant is a different person: reset the heading so the body does not spin,
                // and re-roll the stride phase so a freshly promoted group does not march in step.
                _lastPosition[slot] = _chosen[id];
                _lastHealth[slot] = -1f;
                OnSlotReassigned?.Invoke(slot);
            }

            // ---- Move them ---------------------------------------------------------------------
            for (int i = 0; i < _slots.Count; i++)
            {
                int id = _assigned[i];
                var slot = _slots[i];

                // Dying bodies stay exactly where they fell, visible, and are not driven.
                if (_dying[i] > 0f)
                {
                    if (!slot.gameObject.activeSelf) slot.gameObject.SetActive(true);
                    continue;
                }

                if (id < 0 || !_chosen.TryGetValue(id, out var position))
                {
                    if (slot.gameObject.activeSelf) slot.gameObject.SetActive(false);
                    _assigned[i] = -1;
                    continue;
                }

                if (!slot.gameObject.activeSelf) slot.gameObject.SetActive(true);

                Vector3 previous = _lastPosition[i];
                Vector3 delta = position - previous;
                float speed = dt > 0f ? delta.magnitude / dt : 0f;

                if (delta.sqrMagnitude > 1e-6f)
                {
                    var look = Quaternion.LookRotation(delta.normalized, Vector3.up);
                    slot.rotation = Quaternion.Slerp(slot.rotation, look, 1f - Mathf.Exp(-12f * dt));
                }

                slot.position = position;
                _lastPosition[i] = position;
                Promoted.Add(id);

                // A flinch when it is hurt. The sim raises no per-agent damage event, so the body
                // remembers the health it last saw and reacts when the number goes down.
                float hp = world.HealthOf(id);
                if (_lastHealth[i] >= 0f && hp < _lastHealth[i] - 0.01f) OnSlotHurt?.Invoke(i);
                _lastHealth[i] = hp;

                // Walking is for people who are walking. The clip used to run on everybody all the
                // time, including agents standing still against a barricade.
                OnSlotMoved?.Invoke(i, speed);
            }
        }

        /// <summary>Raised when a slot changes occupant, so the view can re-roll that body's stride.</summary>
        public System.Action<int>? OnSlotReassigned;

        /// <summary>Raised each frame with a slot's ground speed, so the view can stop the walk.</summary>
        public System.Action<int, float>? OnSlotMoved;

        private readonly Dictionary<int, Vector3> _chosen = new Dictionary<int, Vector3>(256);
        private readonly HashSet<int> _holders = new HashSet<int>();
        private readonly List<int> _free = new List<int>(256);

        private readonly List<(float dist, int id, Vector3 position)> _nearest =
            new List<(float, int, Vector3)>(512);
    }
}
