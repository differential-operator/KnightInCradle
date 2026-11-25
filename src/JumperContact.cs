namespace KnightInCradle
{
    internal static class JumperContact
    {
        internal static bool Lands(float left, float right, float oldFeet, float feet,
            float boardLeft, float boardRight, float top)
        {
            // Reject rising through the underside; catch downward sweeps even at low frame rates.
            return right > boardLeft && left < boardRight && feet >= oldFeet &&
                oldFeet <= top + 0.30f && feet >= top - 0.35f;
        }
    }
}
