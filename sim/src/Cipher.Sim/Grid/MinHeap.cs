using System;

namespace Cipher.Sim.Grid
{
    /// <summary>
    /// Array-backed binary min-heap of (priority, value) pairs.
    /// netstandard2.1 has no PriorityQueue; this is the 60 lines that replace it.
    /// Ties break on insertion order via a monotonic sequence, keeping Dijkstra deterministic.
    /// </summary>
    internal sealed class MinHeap
    {
        private (float Priority, long Sequence, int Value)[] _items;
        private int _count;
        private long _nextSequence;

        public MinHeap(int capacity = 64)
        {
            _items = new (float, long, int)[Math.Max(4, capacity)];
        }

        public int Count => _count;

        public void Push(float priority, int value)
        {
            if (_count == _items.Length)
                Array.Resize(ref _items, _items.Length * 2);

            _items[_count] = (priority, _nextSequence++, value);
            SiftUp(_count);
            _count++;
        }

        public (float Priority, int Value) Pop()
        {
            if (_count == 0)
                throw new InvalidOperationException("Heap is empty.");

            var root = _items[0];
            _count--;
            _items[0] = _items[_count];
            if (_count > 0) SiftDown(0);
            return (root.Priority, root.Value);
        }

        private void SiftUp(int i)
        {
            var item = _items[i];
            while (i > 0)
            {
                int parent = (i - 1) / 2;
                if (!Less(item, _items[parent])) break;
                _items[i] = _items[parent];
                i = parent;
            }
            _items[i] = item;
        }

        private void SiftDown(int i)
        {
            var item = _items[i];
            while (true)
            {
                int left = 2 * i + 1;
                if (left >= _count) break;
                int right = left + 1;
                int smallest = right < _count && Less(_items[right], _items[left]) ? right : left;
                if (!Less(_items[smallest], item)) break;
                _items[i] = _items[smallest];
                i = smallest;
            }
            _items[i] = item;
        }

        private static bool Less(in (float Priority, long Sequence, int Value) a, in (float Priority, long Sequence, int Value) b)
            => a.Priority < b.Priority || (a.Priority == b.Priority && a.Sequence < b.Sequence);
    }
}
