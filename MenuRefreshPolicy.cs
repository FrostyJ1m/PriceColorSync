namespace SptDiagnostics.PriceColorSync
{
    // Menu navigation alone never schedules another refresh. A raid must have
    // been observed, and then the actual main-menu Show must have completed.
    internal sealed class MenuRefreshPolicy
    {
        private bool startup = true;
        private bool raidSeen;
        private bool menuShown;

        internal void ObserveRaid(bool inRaid)
        {
            if (!inRaid) return;
            raidSeen = true;
            menuShown = false;
        }

        internal void MenuShown() { menuShown = true; }

        internal string PendingReason(bool inRaid, bool sessionReady)
        {
            if (inRaid || !sessionReady || !menuShown) return null;
            return startup ? "client startup" : raidSeen ? "return from raid" : null;
        }

        internal void Requested()
        {
            startup = false;
            raidSeen = false;
        }
    }
}
