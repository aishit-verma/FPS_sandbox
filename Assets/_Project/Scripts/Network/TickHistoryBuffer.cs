using UnityEngine;

namespace PolyFrontlines.Utils.Prediction
{
    // Reusable fixed-size circular buffer for client-side prediction.
    // Indexes by tick % capacity instead of scanning a growing list —
    // O(1) lookup, bounded memory, works for movement, shooting, jumping, etc.
    public class TickHistoryBuffer<TInput>
    {
        private struct Entry
        {
            public int Tick;
            public TInput Input;
            public Vector3 ResultingPosition;
            public bool IsSet;
        }

        private readonly Entry[] _entries;
        private readonly int _capacity;

        public TickHistoryBuffer(int capacity)
        {
            _capacity = capacity;
            _entries = new Entry[capacity];
        }

        public void Record(int tick, TInput input, Vector3 resultingPosition)
        {
            _entries[tick % _capacity] = new Entry
            {
                Tick = tick,
                Input = input,
                ResultingPosition = resultingPosition,
                IsSet = true
            };
        }

        public bool TryGet(int tick, out TInput input, out Vector3 resultingPosition)
        {
            var entry = _entries[tick % _capacity];
            if (entry.IsSet && entry.Tick == tick)
            {
                input = entry.Input;
                resultingPosition = entry.ResultingPosition;
                return true;
            }

            input = default;
            resultingPosition = default;
            return false;
        }

        public void UpdateResultingPosition(int tick, Vector3 newPosition)
        {
            int index = tick % _capacity;
            if (_entries[index].IsSet && _entries[index].Tick == tick)
            {
                _entries[index].ResultingPosition = newPosition;
            }
        }

        public bool TryGetInput(int tick, out TInput input)
        {
            var entry = _entries[tick % _capacity];
            if (entry.IsSet && entry.Tick == tick)
            {
                input = entry.Input;
                return true;
            }
            input = default;
            return false;
        }
    }
}