namespace KnightInCradle
{
    internal sealed class EncounterMotion
    {
        private object map;
        private float x, y;
        private int frame = -100;
        private float dx, dy;
        internal void Reset() { map = null; frame = -100; dx = dy = 0f; }
        internal void Sample(object currentMap, int currentFrame, float currentX, float currentY)
        {
            bool continuous = currentMap != null && object.ReferenceEquals(map, currentMap) && currentFrame == frame + 1;
            dx = continuous ? currentX - x : 0f;
            dy = continuous ? currentY - y : 0f;
            map = currentMap; frame = currentFrame; x = currentX; y = currentY;
        }
        internal float Axis(object currentMap, int currentFrame, bool horizontal)
        {
            if (map == null || !object.ReferenceEquals(map, currentMap) ||
                currentFrame < frame || currentFrame > frame + 1) return 0f;
            return horizontal ? dx : dy;
        }
    }
}
