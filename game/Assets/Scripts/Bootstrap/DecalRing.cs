#nullable enable
using UnityEngine;

namespace Cipher.Game
{
    /// <summary>What a mark on the ground is a mark OF.</summary>
    public enum DecalKind
    {
        /// <summary>A bomb or a bursting sapper: burnt centre, hard rim, spokes.</summary>
        Scorch = 0,
        /// <summary>Where a body went down.</summary>
        Stain = 1,
        /// <summary>A scuff, for something that was hauled rather than dropped.</summary>
        Drag = 2,
    }

    /// <summary>
    /// The bookkeeping half of the decal system: a fixed-size ring of marks that age out.
    ///
    /// Split from the drawing on purpose. This is the part with the off-by-one in it -- a ring
    /// buffer that overwrites its oldest entry, retires expired ones from the tail and has to keep
    /// Count honest through both -- and it is the part that can be tested without a GPU, a scene or
    /// a shader. The renderer above it (<see cref="Decals"/>) is then a loop with no decisions in it.
    ///
    /// FIXED SIZE IS THE DESIGN, not a limitation. The battlefield should accumulate a history and
    /// then stop; an unbounded list of marks is an unbounded draw call and, worse, a floor that
    /// eventually reads as solid black where the fighting was heaviest.
    /// </summary>
    public sealed class DecalRing
    {
        public struct Entry
        {
            public Vector3 Position;
            public float Radius;
            public float Yaw;
            public float Age;
            public float Life;
            public DecalKind Kind;
        }

        private readonly Entry[] _entries;
        private int _head;     // index of the OLDEST live entry
        private int _count;

        public DecalRing(int capacity)
        {
            if (capacity < 1) capacity = 1;
            _entries = new Entry[capacity];
        }

        public int Capacity => _entries.Length;

        /// <summary>How many marks are live right now.</summary>
        public int Count => _count;

        /// <summary>
        /// The i-th live mark, oldest first. Indexing rather than an iterator because the renderer
        /// walks this every frame and an enumerator would allocate on a struct every time.
        /// </summary>
        public Entry this[int i] => _entries[(_head + i) % _entries.Length];

        /// <summary>
        /// Records a mark. When the ring is full the OLDEST is overwritten, which is the behaviour
        /// that matters: the newest explosion must always be visible, even in a long fight.
        /// </summary>
        public void Add(DecalKind kind, Vector3 position, float radius, float yaw, float life)
        {
            if (radius <= 0f || life <= 0f) return;

            var entry = new Entry
            {
                Position = position,
                Radius = radius,
                Yaw = yaw,
                Age = 0f,
                Life = life,
                Kind = kind,
            };

            if (_count < _entries.Length)
            {
                _entries[(_head + _count) % _entries.Length] = entry;
                _count++;
            }
            else
            {
                // Full: the slot at the head IS the oldest, so writing there and advancing the head
                // both evicts it and keeps the oldest-first ordering intact.
                _entries[_head] = entry;
                _head = (_head + 1) % _entries.Length;
            }
        }

        /// <summary>
        /// Ages every live mark and retires the ones that have run out.
        ///
        /// Only entries at the TAIL are retired, and that is correct rather than lazy: lifetimes
        /// are per kind and vary, so a young stain can sit behind an expired scorch. Such an entry
        /// stays in the ring at zero alpha until the ones in front of it expire, which costs one
        /// invisible instance and keeps the ring contiguous. Compacting instead would mean moving
        /// entries every frame for a mark nobody can see.
        /// </summary>
        public void Age(float dt)
        {
            if (dt <= 0f || _count == 0) return;
            for (int i = 0; i < _count; i++)
            {
                int idx = (_head + i) % _entries.Length;
                _entries[idx].Age += dt;
            }
            while (_count > 0 && _entries[_head].Age >= _entries[_head].Life)
            {
                _head = (_head + 1) % _entries.Length;
                _count--;
            }
        }

        public void Clear()
        {
            _head = 0;
            _count = 0;
        }

        /// <summary>
        /// How opaque a mark of this age is. Holds full for most of its life then drops away, so a
        /// scorch ring reads as a scorch ring for as long as it is on screen at all instead of
        /// spending its whole existence half-faded.
        /// </summary>
        public static float Fade(float age, float life)
        {
            if (life <= 0f) return 0f;
            float t = Mathf.Clamp01(age / life);
            const float hold = 0.7f;
            if (t <= hold) return 1f;
            return 1f - (t - hold) / (1f - hold);
        }
    }
}
