using System;

namespace SptDiagnostics.PriceColorSync
{
    // Inventory previews mutate live addresses and then roll back synchronously.
    // A real inner Add/Remove must inherit the outer simulation's suppression.
    internal static class InventorySimulationScope
    {
        [ThreadStatic] private static int depth;

        internal struct State
        {
            internal bool Entered;
            internal int PreviousDepth;
        }

        internal static bool Active { get { return depth != 0; } }

        internal static State Enter(bool simulate)
        {
            if (!simulate) return default(State);
            State state = new State { Entered = true, PreviousDepth = depth };
            depth++;
            return state;
        }

        internal static void Exit(State state)
        {
            // Default state is possible if an earlier Harmony prefix throws.
            if (state.Entered) depth = state.PreviousDepth;
        }
    }
}
