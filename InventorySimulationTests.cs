using System;
using System.Threading;

namespace SptDiagnostics.PriceColorSync
{
    internal static class InventorySimulationTests
    {
        private static int assertions;
        private static void Check(bool condition, string message)
        {
            assertions++;
            if (!condition) throw new Exception(message);
        }

        public static void Main()
        {
            var queue = new ItemPreparationQueue<object>();
            object part = new object(), oldParent = new object(), newParent = new object();
            queue.Schedule(part, true);
            queue.Process(1, delegate { }, null);
            long original = queue.Revision(part);
            // Repeated preview remove/add/rollback must preserve a warm quote, even
            // when nested helpers use simulate=false to mutate the live tree.
            for (int i = 0; i < 1000; i++)
            {
                var preview = InventorySimulationScope.Enter(true);
                try
                {
                    var inner = InventorySimulationScope.Enter(false);
                    try
                    {
                        for (int change = 0; change < 8; change++) queue.Schedule(part, true);
                        queue.Schedule(oldParent, true);
                        queue.Schedule(newParent, true);
                        queue.Schedule(new object(), true); // temporary split/clone
                    }
                    finally { InventorySimulationScope.Exit(inner); }
                }
                finally { InventorySimulationScope.Exit(preview); }
            }
            Check(queue.Revision(part) == original, "preview preserves warm quote revision");
            Check(queue.Count == 0, "preview does not enqueue repricing or temporary instances");
            Check(queue.Revision(oldParent) == 0 && queue.Revision(newParent) == 0, "preview preserves both parent quotes");
            Check(!InventorySimulationScope.Active, "preview suppression restored");

            var outer = InventorySimulationScope.Enter(true);
            var nested = InventorySimulationScope.Enter(true);
            InventorySimulationScope.Exit(nested);
            Check(InventorySimulationScope.Active, "nested simulation retains outer suppression");
            InventorySimulationScope.Exit(default(InventorySimulationScope.State));
            Check(InventorySimulationScope.Active, "unentered finalizer cannot clear outer scope");
            InventorySimulationScope.Exit(outer);

            Exception originalError = new InvalidOperationException("preview failure");
            Exception observed = null;
            try
            {
                var state = InventorySimulationScope.Enter(true);
                try { throw originalError; }
                finally { InventorySimulationScope.Exit(state); }
            }
            catch (Exception error) { observed = error; }
            Check(ReferenceEquals(observed, originalError), "exception remains unchanged");
            Check(!InventorySimulationScope.Active, "exception restores mutation capture");

            outer = InventorySimulationScope.Enter(true);
            bool otherThreadActive = true;
            var thread = new Thread(delegate()
            {
                otherThreadActive = InventorySimulationScope.Active;
                queue.Schedule(newParent, true);
            });
            thread.Start(); thread.Join();
            Check(!otherThreadActive && queue.Revision(newParent) == 1, "other-thread real mutation is captured during preview");
            Check(InventorySimulationScope.Active, "other thread cannot change preview state");
            InventorySimulationScope.Exit(outer);
            queue.Process(10, delegate { }, null);

            var real = InventorySimulationScope.Enter(false);
            try
            {
                queue.Schedule(part, true);
                queue.Schedule(oldParent, true);
                queue.Schedule(newParent, true);
            }
            finally { InventorySimulationScope.Exit(real); }
            Check(queue.Revision(part) == original + 1, "real part mutation invalidates quote");
            Check(queue.Revision(oldParent) == 1 && queue.Revision(newParent) == 2, "real move invalidates both assembly parents");
            Check(queue.Count == 3, "real changes are prepared");
            int prepared = 0;
            queue.Process(10, delegate { prepared++; }, null);
            Check(prepared == 3, "all real changes processed");
            Console.WriteLine("Inventory simulation: " + assertions + " assertions passed.");
        }
    }
}
