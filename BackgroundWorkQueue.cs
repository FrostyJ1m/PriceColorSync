using System;
using System.Collections.Generic;

namespace SptDiagnostics.PriceColorSync
{
    public sealed class BackgroundWorkQueue<T>
    {
        private readonly Queue<T> pending = new Queue<T>();
        private readonly HashSet<T> queued = new HashSet<T>();
        public int Count { get { return pending.Count; } }
        public void Enqueue(T item) { if (queued.Add(item)) pending.Enqueue(item); }
        public int Process(int maximum, Action<T> action)
        {
            return Process(maximum, action, null);
        }
        public int Process(int maximum, Action<T> action, Func<bool> withinBudget)
        {
            if (maximum < 0) throw new ArgumentOutOfRangeException("maximum");
            // Only work present at entry is eligible; requeueing cannot spin in one frame.
            int count = Math.Min(maximum, pending.Count);
            int processed = 0;
            while (processed < count)
            {
                // Always allow one item so an expensive calculation cannot starve the
                // queue. Check between items; an individual action cannot be interrupted.
                if (processed > 0 && withinBudget != null && !withinBudget()) break;
                T item = pending.Dequeue();
                queued.Remove(item);
                action(item);
                processed++;
            }
            return processed;
        }
        public void Clear() { pending.Clear(); queued.Clear(); }
    }
}
