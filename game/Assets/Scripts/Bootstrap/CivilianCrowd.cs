#nullable enable
using System;
using System.Collections.Generic;
using Cipher.Sim.Agents;
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>
    /// Draws the nearest slice of the swarm as real bodies. Everything further out is drawn by
    /// <see cref="CrowdImpostors"/> as a silhouette of its own class.
    ///
    /// The honest shape of this: a real body costs far more than an instanced mesh, so a thousand of
    /// them is not affordable. What IS affordable is the nearest hundred or so, which is also the
    /// part of the crowd a player can read in detail. That is ADR-007's tier 0; the two tiers behind
    /// it are what stop the cutoff from being a cliff.
    ///
    /// A body is not necessarily a skinned character. <see cref="BodyClass.Humanoid"/> and
    /// <see cref="BodyClass.Spitter"/> are box-built machines with a procedural gait
    /// (<see cref="MachineBody"/>), which cost rather less than the skinned people they replaced --
    /// so the same budget now buys the same number of bodies for strictly less work.
    ///
    /// This is a bridge, not the destination. The vertex-animation-texture path in ADR-007 is what
    /// eventually makes all thousand real. This exists so the game looks like itself in the meantime,
    /// and because a pool of pre-built characters is exactly what that path will need anyway.
    /// </summary>
    public sealed class CivilianCrowd
    {
        /// <summary>How many bodies were BUILT. <see cref="AddSlot"/> refuses to exceed it.</summary>
        public int Capacity { get; }

        /// <summary>
        /// The most bodies that may be promoted at once, across every class.
        ///
        /// Separate from <see cref="Capacity"/> because slots are typed and a slot cannot change
        /// class: the pool is overbuilt so that no class starves in a wave that is mostly one
        /// thing, and this is the number that keeps the overbuild off the frame. A body with no
        /// occupant is inactive and costs nothing, so building more than this is free at runtime.
        /// </summary>
        public int ActiveBudget { get; set; } = int.MaxValue;

        /// <summary>Agents further than this from the camera are never promoted.</summary>
        public float PromoteRange { get; set; } = 34f;

        /// <summary>
        /// What kind of body an agent wants. An agent may only be given a slot of its own class.
        ///
        /// Null means everything is <see cref="BodyClass.Signed"/>, which is what the crowd did
        /// before there was a second enemy class.
        ///
        /// THIS MUST BE A PURE FUNCTION OF THE AGENT ID (see <see cref="CrowdCasting.ClassOf"/>).
        /// If an agent's answer can change, an agent that walks out of range and back comes back a
        /// different species -- the same class of bug as the body-swapping the owner saw, one level
        /// up.
        /// </summary>
        public Func<int, BodyClass>? Classify;

        private const int ClassCount = 4;

        private readonly List<Transform> _slots = new List<Transform>();
        private readonly List<BodyClass> _slotClass = new List<BodyClass>();
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

        /// <summary>Which agent is wearing slot <paramref name="slot"/>, or -1 for nobody.</summary>
        public int AgentInSlot(int slot) => _assigned[slot];

        /// <summary>The class a slot was built as. A slot can never serve any other.</summary>
        public BodyClass ClassOfSlot(int slot) => _slotClass[slot];

        /// <summary>How many bodies of a class exist in the pool.</summary>
        public int SlotsOfClass(BodyClass c)
        {
            int n = 0;
            for (int i = 0; i < _slotClass.Count; i++) if (_slotClass[i] == c) n++;
            return n;
        }

        public void AddSlot(Transform slot) => AddSlot(slot, BodyClass.Signed);

        public void AddSlot(Transform slot, BodyClass bodyClass)
        {
            // Capacity is the budget the caller sized the pool against; exceeding it silently would
            // put more skinned characters on screen than the frame was costed for. It used to be
            // read by nobody, which made it a comment pretending to be a constraint.
            if (_slots.Count >= Capacity)
                throw new InvalidOperationException(
                    $"CivilianCrowd was built for {Capacity} bodies and AddSlot was called again.");

            _slots.Add(slot);
            _slotClass.Add(bodyClass);
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

            // The nearest slice is chosen ACROSS classes and capped at the frame budget, not at the
            // number of bodies built. Choosing per class would promote the fourteenth-nearest
            // sapper over the second-nearest citizen, which is backwards: what a player can read is
            // what is close to him, whatever species it is.
            int capacity = Mathf.Min(_slots.Count, ActiveBudget);
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

            for (int c = 0; c < ClassCount; c++) _freeByClass[c].Clear();
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
                    _freeByClass[(int)_slotClass[i]].Add(i);
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
                _freeByClass[(int)_slotClass[i]].Add(i);
            }

            // Hand the free slots to the nearest agents that do not have one, nearest first --
            // but only ever a slot of the agent's OWN class. An agent whose class has run out of
            // bodies stays a capsule rather than borrowing somebody else's: a sapper wearing a
            // delivery walker's chassis is worse than a sapper wearing a pill, because the pill at
            // least does not lie about what is coming at you.
            for (int c = 0; c < ClassCount; c++) _nextFree[c] = 0;
            for (int i = 0; i < _nearest.Count; i++)
            {
                int id = _nearest[i].id;
                int c = (int)(Classify != null ? Classify(id) : BodyClass.Signed);
                var free = _freeByClass[c];
                if (_nextFree[c] >= free.Count) continue;
                if (!_holders.Add(id)) continue;   // already wearing a body
                int slot = free[_nextFree[c]++];
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
                OnSlotMoved?.Invoke(i, world.IsInContactWithHero(id) ? -1f : speed);
            }
        }

        /// <summary>Raised when a slot changes occupant, so the view can re-roll that body's stride.</summary>
        public System.Action<int>? OnSlotReassigned;

        /// <summary>
        /// Raised each frame with a slot's ground speed, so the view can stop the walk. A NEGATIVE
        /// speed means the body is in contact with the player and should be swinging, not idling.
        /// </summary>
        public System.Action<int, float>? OnSlotMoved;

        private readonly Dictionary<int, Vector3> _chosen = new Dictionary<int, Vector3>(256);
        private readonly HashSet<int> _holders = new HashSet<int>();

        /// <summary>Free slots, bucketed by the class they were built as.</summary>
        private readonly List<int>[] _freeByClass =
        {
            new List<int>(128), new List<int>(64), new List<int>(32), new List<int>(32),
        };

        private readonly int[] _nextFree = new int[ClassCount];

        private readonly List<(float dist, int id, Vector3 position)> _nearest =
            new List<(float, int, Vector3)>(512);
    }
}
