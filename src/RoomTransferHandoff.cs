namespace KnightInCradle
{
    /// <summary>Keep native ownership through exit, loading, nested events and walk-in.</summary>
    internal sealed class RoomTransferHandoff
    {
        internal bool Active { get; private set; }
        private int quietFrames;

        internal void Begin()
        {
            Active = true;
            quietFrames = 0;
        }

        internal bool Tick(bool exitEvent, bool anyEvent, bool transferring, bool hostReady)
        {
            if (exitEvent) Active = true;
            if (!Active) return false;
            if (anyEvent || exitEvent || transferring || !hostReady)
            {
                quietFrames = 0;
                return false;
            }
            // Avoid releasing ownership during a one-frame gap between nested events.
            if (++quietFrames < 2) return false;
            Reset();
            return true;
        }

        internal void Reset()
        {
            Active = false;
            quietFrames = 0;
        }
    }
}
