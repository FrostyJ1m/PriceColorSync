using System;

namespace SptDiagnostics.PriceColorSync
{
    public static class TierPolicy
    {
        public static string PriceSource(bool foundInRaid, bool fleaEligible,
            bool fleaForFir = true, bool fleaForNonFir = false, bool traderForBanned = true)
        {
            if (!fleaEligible && traderForBanned) return "Trader";
            return (foundInRaid ? fleaForFir : fleaForNonFir) ? "Flea" : "Trader";
        }

        public static int Tier(double value, double[] boundaries)
        {
            if (boundaries == null || boundaries.Length != 5) throw new ArgumentException("Five boundaries required");
            if (Double.IsNaN(value) || Double.IsInfinity(value) || value < 0) return -1;
            for (int i = 0; i < boundaries.Length; i++) if (value < boundaries[i]) return i;
            return 5;
        }
    }
}
