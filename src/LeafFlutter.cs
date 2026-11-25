using System;
namespace KnightInCradle
{
    internal static class LeafFlutter
    {
        internal const float Duration = 0.35f;
        internal static float Period(int leaf) => 3f + ((leaf * 7) % 11) * 0.2f;
        internal static float Amount(float time, int leaf)
        {
            float period = Period(leaf);
            float delay = 3f + ((leaf * 3) % 11) * 0.2f;
            if (time < delay) return 0f;
            float phase = (time - delay) % period;
            if (phase >= Duration) return 0f;
            double wave = Math.Sin(Math.PI * phase / Duration);
            return (float)(wave * wave);
        }
    }
}
