namespace KnightInCradle
{
    internal static class FootBellContact
    {
        // Same 0.25-cell overlap margin as M2FootManager.BccFootCheckDL.
        internal static bool Covers(bool grounded, float left, float right, float top, float feet,
            float bellLeft, float bellRight, float bellTop, float bellBottom)
        {
            return grounded && right >= bellLeft - 0.25f && left <= bellRight + 0.25f &&
                feet >= bellTop - 0.25f && top <= bellBottom + 0.25f;
        }
    }
}
