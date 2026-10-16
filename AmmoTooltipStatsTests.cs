using System;
using System.Text;
using SptDiagnostics.PriceColorSync;

internal static class AmmoTooltipStatsTests
{
    private static int assertions;
    private static void Check(string name, int damage, int penetration, string expected)
    {
        var text = new StringBuilder("<size=80%>");
        AmmoTooltipStats.Append(text, name, damage, penetration);
        text.Append("<br>Trader: 123</size>");
        if (text.ToString() != "<size=80%>" + expected + "<br>Trader: 123</size>")
            throw new Exception("Unexpected tooltip: " + text);
        assertions++;
    }
    private static void Main()
    {
        const string stats = "<br>Damage: 58 | Penetration: 43";
        Check("Round", 58, 43, stats);
        Check(null, 58, 43, stats);
        Check("Round [58/43]", 58, 43, "");
        Check("Round <color=#808080>[58/43]</color>", 58, 43, "");
        Check("Round <color=#808080>[ 58 / 43 ]</color> ", 58, 43, "");
        Check("Round [60/43]", 58, 43, stats);
        Check("Round [58/40]", 58, 43, stats);
        Check("Round [58/43] special", 58, 43, stats);
        Check("Round", 0, 0, "<br>Damage: 0 | Penetration: 0");
        Check("Round", -1, 43, "");
        Check("Round", 58, -1, "");
        // SMTM replaces its entire previous price block on each refresh.
        var block = new StringBuilder();
        AmmoTooltipStats.Append(block, "Round", 58, 43);
        block.Append("<br>Trader: 123");
        string tooltip = "Round" + block;
        for (int i = 0; i < 1000; i++)
        {
            var fresh = new StringBuilder();
            AmmoTooltipStats.Append(fresh, "Round", 58, 43);
            fresh.Append("<br>Trader: ").Append(i);
            tooltip = tooltip.Replace(block.ToString(), fresh.ToString());
            block = fresh;
            if (tooltip != "Round" + stats + "<br>Trader: " + i)
                throw new Exception("Refresh accumulated or lost stats");
        }
        assertions++;
        Console.WriteLine(assertions + " ammo tooltip assertions passed (including 1,000 refreshes).");
    }
}
