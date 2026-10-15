using System;
using SptDiagnostics.PriceColorSync;

internal static class TierPolicyTests
{
    private static void Equal(object actual, object expected)
    {
        if (!actual.Equals(expected)) throw new Exception("Expected " + expected + ", got " + actual);
    }
    private static void Main()
    {
        Equal(TierPolicy.PriceSource(true, true), "Flea");
        Equal(TierPolicy.PriceSource(true, false), "Trader");
        Equal(TierPolicy.PriceSource(false, true), "Trader");
        Equal(TierPolicy.PriceSource(false, false), "Trader");
        Equal(TierPolicy.PriceSource(true, true, false), "Trader");
        Equal(TierPolicy.PriceSource(true, false, false), "Trader");
        Equal(TierPolicy.PriceSource(true, true, true, true), "Flea");
        Equal(TierPolicy.PriceSource(false, true, true, true), "Flea");
        Equal(TierPolicy.PriceSource(true, false, true, true), "Trader");
        Equal(TierPolicy.PriceSource(false, false, true, true), "Trader");
        Equal(TierPolicy.PriceSource(true, false, true, false, false), "Flea");
        Equal(TierPolicy.PriceSource(false, false, true, false, false), "Trader");
        double[] price = { 5000, 10000, 15000, 25000, 35000 };
        double[] pen = { 15, 26, 35, 44, 54 };
        foreach (double[] boundaries in new double[][] { price, pen })
        {
            Equal(TierPolicy.Tier(0, boundaries), 0);
            for (int i = 0; i < 5; i++)
            {
                Equal(TierPolicy.Tier(boundaries[i] - 0.01, boundaries), i);
                Equal(TierPolicy.Tier(boundaries[i], boundaries), i + 1);
            }
            Equal(TierPolicy.Tier(1000000, boundaries), 5);
            Equal(TierPolicy.Tier(Double.NaN, boundaries), -1);
            Equal(TierPolicy.Tier(Double.PositiveInfinity, boundaries), -1);
            Equal(TierPolicy.Tier(-1, boundaries), -1);
        }
        Console.WriteLine("42 source-selection/tier-boundary/invalid-value assertions passed.");
        Equal(TierPolicy.PreferTrader(10000, 15000), true);
        Equal(TierPolicy.PreferTrader(15000, 10000), false);
        Equal(TierPolicy.PreferTrader(10000, 10000), false);
        Equal(TierPolicy.PreferTrader(null, 10000), true);
        Equal(TierPolicy.PreferTrader(10000, null), false);
        Equal(TierPolicy.PreferTrader(null, null), false);
        Equal(TierPolicy.PreferTrader(Double.NaN, 10000), true);
        Equal(TierPolicy.PreferTrader(Double.PositiveInfinity, 10000), true);
        Equal(TierPolicy.PreferTrader(-1, 10000), true);
        Equal(TierPolicy.PreferTrader(10000, Double.NaN), false);
        Equal(TierPolicy.PreferTrader(10000, Double.PositiveInfinity), false);
        Equal(TierPolicy.PreferTrader(10000, Double.NegativeInfinity), false);
        Equal(TierPolicy.PreferTrader(10000, -1), false);
        Equal(TierPolicy.PreferTrader(null, 0), true);
        Equal(TierPolicy.PreferTrader(0, 0), false);
        Equal(TierPolicy.PreferTrader(9999.5, 10000), true);
        Console.WriteLine("16 best-value assertions passed: flea/trader winners, ties, missing/invalid quotes, zero and fractional values.");
        var queue = new BackgroundWorkQueue<int>();
        for (int i = 0; i < 1000; i++) { queue.Enqueue(i); queue.Enqueue(i); }
        Equal(queue.Count, 1000);
        int processed = 0;
        Equal(queue.Process(2, delegate(int item) { processed++; }), 2);
        Equal(processed, 2);
        Equal(queue.Count, 998);
        Equal(queue.Process(0, delegate(int item) { throw new Exception("Zero budget performed work"); }), 0);
        queue.Clear();
        queue.Enqueue(1);
        Equal(queue.Process(2, delegate(int item) { queue.Enqueue(item); }), 1);
        Equal(queue.Count, 1);
        queue.Clear();
        Equal(queue.Count, 0);
        Console.WriteLine("8 work-queue assertions passed: duplicate coalescing, per-frame bound, no requeue spin.");
        // Simulate elapsed work deterministically: two cheap jobs fit the budget,
        // then the rest must retain FIFO order for subsequent frames.
        for (int i = 0; i < 5; i++) queue.Enqueue(i);
        int elapsed = 0;
        string order = "";
        Equal(queue.Process(12, delegate(int item) { order += item; elapsed++; }, delegate { return elapsed < 2; }), 2);
        Equal(order, "01");
        Equal(queue.Count, 3);
        Equal(queue.Process(12, delegate(int item) { order += item; }, delegate { return false; }), 1);
        Equal(order, "012");
        Equal(queue.Count, 2);
        Equal(queue.Process(1, delegate(int item) { order += item; }, delegate { return true; }), 1);
        Equal(order, "0123");
        Equal(queue.Process(0, delegate(int item) { throw new Exception("Zero budget performed work"); }, delegate { return true; }), 0);
        queue.Clear();
        Equal(queue.Process(12, delegate(int item) { throw new Exception("Empty queue performed work"); }, delegate { return true; }), 0);
        queue.Enqueue(7);
        Equal(queue.Process(12, delegate(int item) { queue.Enqueue(item); }, delegate { return true; }), 1);
        Equal(queue.Count, 1);
        queue.Clear();
        // A cheap backlog now finishes in one frame up to the new item cap.
        for (int i = 0; i < 20; i++) queue.Enqueue(i);
        Equal(queue.Process(12, delegate(int item) { }, delegate { return true; }), 12);
        Equal(queue.Count, 8);
        Equal(queue.Process(12, delegate(int item) { }, delegate { return true; }), 8);
        Equal(queue.Count, 0);
        Console.WriteLine("16 budgeted-queue assertions passed: time cutoff, forward progress, FIFO, item cap, zero/empty work, no requeue spin, faster cheap backlog.");
    }
}
