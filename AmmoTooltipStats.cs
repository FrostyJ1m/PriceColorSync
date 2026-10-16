using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace SptDiagnostics.PriceColorSync
{
    internal static class AmmoTooltipStats
    {
        private static readonly Regex Tags = new Regex("<[^>]*>");
        private static readonly Regex NameStats = new Regex(@"\[\s*(\d+)\s*/\s*(\d+)\s*\]\s*$");

        internal static void Append(StringBuilder text, string localizedName, int damage, int penetration)
        {
            if (text == null || damage < 0 || penetration < 0) return;
            string damageText = damage.ToString(CultureInfo.InvariantCulture);
            string penetrationText = penetration.ToString(CultureInfo.InvariantCulture);
            // Item Valuation used a rich-text [damage/penetration] name suffix.
            // Only suppress a matching pair; stale names must not hide current data.
            Match existing = NameStats.Match(Tags.Replace(localizedName ?? "", ""));
            if (existing.Success && existing.Groups[1].Value == damageText
                && existing.Groups[2].Value == penetrationText) return;
            text.Append("<br>Damage: ").Append(damageText)
                .Append(" | Penetration: ").Append(penetrationText);
        }
    }
}
