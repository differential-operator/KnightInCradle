namespace KnightInCradle
{
    // Never hand a freshly opened map back to custom movement on its first frame.
    internal sealed class RespawnArrivalGuard
    {
        private object target;
        private int readyFrame = -1;
        public void Begin(object destination) { target = destination; readyFrame = -1; }
        public bool Tick(object current, object hostMap, bool loaded, bool transferring, int frame)
        {
            if (target == null || !ReferenceEquals(target, current) ||
                !ReferenceEquals(target, hostMap) || !loaded || transferring)
            {
                readyFrame = -1;
                return false;
            }
            if (readyFrame < 0) readyFrame = frame;
            return frame > readyFrame;
        }
    }
}
