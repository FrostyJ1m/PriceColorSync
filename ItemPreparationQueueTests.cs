using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace SptDiagnostics.PriceColorSync
{
    internal static class ItemPreparationQueueTests
    {
        private sealed class Item { public int Id; }
        private static int assertions;
        private static void Check(bool value, string message)
        {
            assertions++;
            if (!value) throw new Exception(message);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference AddTemporary(ItemPreparationQueue<Item> queue)
        {
            var item = new Item();
            queue.Schedule(item, true);
            return new WeakReference(item);
        }

        public static void Main()
        {
            var queue = new ItemPreparationQueue<Item>();
            var item = new Item { Id = 1 };
            var second = new Item { Id = 2 };
            Check(queue.Count == 0, "empty");
            Check(queue.Revision(item) == 0, "unseen revision");
            queue.Schedule(null, true);
            Check(queue.Count == 0, "null ignored");
            queue.Schedule(item, true);
            queue.Schedule(item, false);
            Check(queue.Count == 1, "coalesced");
            Check(queue.Revision(item) == 1, "opening a view preserves exact quote revision");
            queue.Schedule(item, true);
            Check(queue.Revision(item) == 2, "actual mutation invalidates even while queued");
            queue.Schedule(second, true);
            Check(queue.Revision(second) == 1, "separate instances have separate revisions");
            int sum = 0;
            Check(queue.Process(1, delegate(Item i) { sum += i.Id; }, null) == 1, "item cap");
            Check(sum == 1 && queue.Count == 1, "FIFO preparation needs no view");
            Check(queue.Process(0, delegate { throw new Exception(); }, null) == 0, "zero cap");
            Check(queue.Process(10, delegate(Item i) { sum += i.Id; }, null) == 1, "drain remainder");
            Check(sum == 3, "both actual item instances prepared");
            queue.ScheduleAll();
            queue.ScheduleAll();
            Check(queue.Count == 2, "refresh coalesces known items without live views");
            Check(queue.Revision(item) == 2, "snapshot refresh does not fabricate an item mutation");
            Check(queue.Process(10, delegate { }, delegate { return false; }) == 1, "at least one advances under budget");
            Check(queue.Count == 1, "budget stops backlog");
            queue.Clear();
            Check(queue.Count == 0 && queue.Revision(item) == 0, "clear resets session tracking");
            queue.ScheduleAll();
            Check(queue.Count == 0, "cleared items not rescheduled");
            queue.Schedule(item, false);
            int calls = 0;
            queue.Process(10, delegate(Item i) { calls++; queue.Schedule(i, true); }, null);
            Check(calls == 1 && queue.Count == 1, "rescheduling cannot spin in same frame");
            Check(queue.Revision(item) == 1, "mutation during preparation is preserved");
            queue.Clear();
            var threads = new Thread[4];
            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(delegate() { for (int j = 0; j < 1000; j++) queue.Schedule(item, true); });
                threads[i].Start();
            }
            foreach (Thread thread in threads) thread.Join();
            Check(queue.Revision(item) == 4000, "concurrent factory/state notifications retain every revision");
            Check(queue.Count == 1, "concurrent notifications coalesce");
            queue.Clear();
            WeakReference weak = AddTemporary(queue);
            GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
            Check(!weak.IsAlive, "pending work must not retain raid item trees");
            calls = 0;
            Check(queue.Process(10, delegate { calls++; }, null) == 1, "dead entry consumed");
            Check(calls == 0, "dead item never priced");
            queue.ScheduleAll();
            Check(queue.Count == 0, "dead tracked entries pruned");
            bool rejected = false;
            try { queue.Process(-1, delegate { }, null); }
            catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "negative cap rejected");
            Console.WriteLine("Item preparation: " + assertions + " assertions passed (revisions, instance isolation, no-view preparation, budgets, concurrent notifications, weak lifetime).");
            GC.KeepAlive(item); GC.KeepAlive(second);
        }
    }
}
