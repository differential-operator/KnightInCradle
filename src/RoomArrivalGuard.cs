using System;

namespace KnightInCradle
{
    // An arrival is not a deliberate departure through the same trigger.
    internal sealed class RoomArrivalGuard
    {
        internal string Map { get; private set; }
        internal string Exit { get; private set; }
        internal float ArrivalX { get; private set; }
        internal float ArrivalFeet { get; private set; }
        private float interiorEdge;
        private int inward;
        private bool vertical;
        private float left, right, top, bottom;

        internal void ArmVertical(string map, string exit, float x0, float y0, float x1, float y1)
        {
            Map = map; Exit = exit;
            vertical = true;
            inward = 0;
            left = x0; top = y0; right = x1; bottom = y1;
        }
        internal bool LeaveVerticalEntrance(string map, float x0, float y0, float x1, float y1, bool nativeBusy, bool grounded = true)
        {
            if (!vertical || Map == null || Map != map || nativeBusy || !grounded) return false;
            // Clear only after the whole player has left the arrival trigger.
            bool clearX = x1 < left - 0.35f || x0 > right + 0.35f;
            // Falling back out through the arrival side is not a successful entry.
            bool clearY = Exit.StartsWith("ExitB", StringComparison.Ordinal)
                ? y1 < top - 0.35f : y0 > bottom + 0.35f;
            if (!clearX && !clearY) return false;
            Clear();
            return true;
        }

        internal void Arm(string map, string exit, float x, float feet, float edge, int direction)
        {
            vertical = false;
            Map = map; Exit = exit; ArrivalX = x; ArrivalFeet = feet;
            interiorEdge = edge; inward = direction;
        }
        internal bool Blocks(string map, string exit) => Map != null && Map == map && Exit == exit;
        internal bool LeaveEntrance(string map, float x, float eventHalfWidth, bool nativeBusy)
        {
            if (vertical || Map == null || Map != map || nativeBusy) return false;
            if ((x - interiorEdge) * inward <= eventHalfWidth + 0.35f) return false;
            Clear();
            return true;
        }
        internal bool NeedsRestore(string map, float x) => !vertical && Map == map && Map != null &&
            (x - ArrivalX) * inward < -0.05f;
        internal bool TryWalkInGoal(string map, float x, float eventHalfWidth,
            out float goal, out int direction)
        {
            direction = inward;
            goal = x;
            if (vertical || Map == null || Map != map ||
                (x - interiorEdge) * inward > eventHalfWidth + 0.35f) return false;
            float clear = interiorEdge + inward * (eventHalfWidth + 0.65f);
            float nativeDistance = ArrivalX + inward * (55f / 28f);
            goal = inward > 0 ? Math.Max(clear, nativeDistance) : Math.Min(clear, nativeDistance);
            return true;
        }
        internal void Clear() { Map = null; Exit = null; vertical = false; }
    }
}
