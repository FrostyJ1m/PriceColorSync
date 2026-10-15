using System;
using SptDiagnostics.PriceColorSync;

internal static class MenuRefreshPolicyTests
{
    private static int assertions;
    private static void Equal(string actual, string expected)
    {
        assertions++;
        if (actual != expected) throw new Exception("Expected " + expected + ", got " + actual);
    }

    private static void Main()
    {
        var policy = new MenuRefreshPolicy();
        Equal(policy.PendingReason(false, true), null); // startup loading
        policy.MenuShown();
        Equal(policy.PendingReason(false, false), null); // session not ready
        Equal(policy.PendingReason(false, true), "client startup");
        Equal(policy.PendingReason(false, true), "client startup"); // retry until requested
        policy.Requested();
        Equal(policy.PendingReason(false, true), null);
        for (int i = 0; i < 5; i++)
        {
            policy.MenuShown(); // stash/trader/settings navigation
            Equal(policy.PendingReason(false, true), null);
        }
        for (int raid = 0; raid < 3; raid++)
        {
            policy.ObserveRaid(true);
            Equal(policy.PendingReason(true, true), null);
            policy.ObserveRaid(false);
            Equal(policy.PendingReason(false, true), null); // teardown/results
            policy.MenuShown();
            Equal(policy.PendingReason(false, false), null);
            Equal(policy.PendingReason(false, true), "return from raid");
            Equal(policy.PendingReason(false, true), "return from raid");
            policy.Requested();
            policy.MenuShown();
            Equal(policy.PendingReason(false, true), null);
        }
        policy.ObserveRaid(true);
        policy.MenuShown(); // defensive: menu shown while raid still active
        Equal(policy.PendingReason(true, true), null);
        policy.ObserveRaid(true);
        Equal(policy.PendingReason(false, true), null);
        policy.MenuShown();
        Equal(policy.PendingReason(false, true), "return from raid");
        Console.WriteLine("Menu refresh policy: " + assertions + " assertions passed.");
    }
}
