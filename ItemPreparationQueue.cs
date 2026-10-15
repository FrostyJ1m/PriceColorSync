using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace SptDiagnostics.PriceColorSync
{
    // Factory/network hooks can run off-thread. Only enqueue weak references there;
    // the caller runs preparation on Unity's main thread under its frame budget.
    public sealed class ItemPreparationQueue<T> where T : class
    {
        private sealed class Entry
        {
            public WeakReference Item;
            public bool Pending;
            public long Revision;
        }

        private readonly object gate = new object();
        private ConditionalWeakTable<T, Entry> entries = new ConditionalWeakTable<T, Entry>();
        private readonly List<Entry> known = new List<Entry>();
        private readonly Queue<Entry> pending = new Queue<Entry>();
        public int Count { get { lock (gate) return pending.Count; } }

        public void Schedule(T item, bool changed)
        {
            if (item == null) return;
            lock (gate)
            {
                Entry entry;
                if (!entries.TryGetValue(item, out entry))
                {
                    entry = new Entry { Item = new WeakReference(item) };
                    entries.Add(item, entry);
                    known.Add(entry);
                }
                if (changed) entry.Revision++;
                if (!entry.Pending) { entry.Pending = true; pending.Enqueue(entry); }
            }
        }

        public long Revision(T item)
        {
            lock (gate)
            {
                Entry entry;
                return entries.TryGetValue(item, out entry) ? entry.Revision : 0;
            }
        }

        public void ScheduleAll()
        {
            lock (gate)
            {
                int live = 0;
                for (int i = 0; i < known.Count; i++)
                {
                    Entry entry = known[i];
                    if (!entry.Item.IsAlive) continue;
                    known[live++] = entry;
                    if (!entry.Pending) { entry.Pending = true; pending.Enqueue(entry); }
                }
                if (live < known.Count) known.RemoveRange(live, known.Count - live);
            }
        }

        public int Process(int maximum, Action<T> prepare, Func<bool> withinBudget)
        {
            if (maximum < 0) throw new ArgumentOutOfRangeException("maximum");
            int count;
            lock (gate) count = Math.Min(maximum, pending.Count);
            int processed = 0;
            while (processed < count)
            {
                if (processed > 0 && withinBudget != null && !withinBudget()) break;
                T item;
                lock (gate)
                {
                    Entry entry = pending.Dequeue();
                    entry.Pending = false;
                    item = entry.Item.Target as T;
                }
                if (item != null) prepare(item);
                processed++;
            }
            return processed;
        }

        public void Clear()
        {
            lock (gate)
            {
                pending.Clear();
                known.Clear();
                entries = new ConditionalWeakTable<T, Entry>();
            }
        }
    }
}
