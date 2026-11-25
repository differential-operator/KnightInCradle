using System;

namespace KnightInCradle
{
    /// <summary>Horizontal body overlap in map cells (Y points down).</summary>
    internal static class KnightTerrainCollision
    {
        internal const float ContactEpsilon = 0.001f; // Less than 0.03 map pixels.

        internal static bool TryWallContact(float edge, float top, float feet, int direction,
            float reach, Func<int, int, bool> isBlock, out float wallFace)
        {
            wallFace = 0f;
            float start = edge + direction * ContactEpsilon;
            float end = edge + direction * (reach + ContactEpsilon);
            int x0 = (int)Math.Floor(Math.Min(start, end));
            int x1 = (int)Math.Floor(Math.Max(start, end));
            int y0 = (int)Math.Floor(top + ContactEpsilon);
            int y1 = (int)Math.Floor(feet - ContactEpsilon);
            float nearest = float.PositiveInfinity;
            for (int x = x0; x <= x1; x++)
            {
                float face = direction < 0 ? x + 1f : x;
                float distance = (face - edge) * direction;
                if (distance < -ContactEpsilon || distance > reach + ContactEpsilon || distance >= nearest)
                    continue;
                for (int y = y0; y <= y1; y++)
                    if (isBlock(x, y)) { nearest = distance; wallFace = face; break; }
            }
            return !float.IsPositiveInfinity(nearest);
        }

        internal static bool TryCeilingContact(float left, float right, float fromTop, float toTop,
            float lineLeft, float lineRight, float leftY, float rightY, out float contactTop)
        {
            contactTop = 0f;
            float a = Math.Max(left + ContactEpsilon, lineLeft);
            float b = Math.Min(right - ContactEpsilon, lineRight);
            if (a >= b || lineRight <= lineLeft) return false;
            float ya = leftY + (rightY - leftY) * ((a - lineLeft) / (lineRight - lineLeft));
            float yb = leftY + (rightY - leftY) * ((b - lineLeft) / (lineRight - lineLeft));
            // Map Y points down: the lowest ceiling point touches the body first.
            float surface = Math.Max(ya, yb);
            if (fromTop < surface - ContactEpsilon || toTop >= surface - ContactEpsilon)
                return false;
            contactTop = surface + ContactEpsilon;
            return true;
        }

        // Horizontal flight has no BCC ground-following pass: slopes must block
        // only where the body overlaps the solid area below their real surface.
        internal static bool HitsFlightTerrain(float left, float right, float top, float feet,
            Func<int, int, bool> isBlock, Func<int, int, float, float> slopeY)
        {
            int x0 = (int)Math.Floor(left + ContactEpsilon);
            int x1 = (int)Math.Floor(right - ContactEpsilon);
            int y0 = (int)Math.Floor(top + ContactEpsilon);
            int y1 = (int)Math.Floor(feet - ContactEpsilon);
            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                {
                    if (isBlock(x, y)) return true;
                    float a = slopeY(x, y, Math.Max(left + ContactEpsilon, x));
                    float b = slopeY(x, y, Math.Min(right - ContactEpsilon, x + 1f));
                    if (!float.IsNaN(a) && !float.IsNaN(b) &&
                        feet > Math.Min(a, b) + ContactEpsilon) return true;
                }
            return false;
        }

        // Ordinary dash follows a continuous uphill surface before testing body overlap.
        // Super dash deliberately uses SweepHorizontal instead and never climbs terrain.
        internal static bool SweepGroundDash(ref float x, ref float y, float distance,
            float bodyHeight, Func<float, float, float> groundAt,
            Func<float, float, float, bool> riseBlocked, Func<float, float, bool> bodyBlocked)
        {
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(distance) / 0.1f));
            float step = distance / steps;
            for (int i = 0; i < steps; i++)
            {
                float nx = x + step, ny = y;
                float feet = y + bodyHeight;
                float before = groundAt(x, y), after = groundAt(nx, y);
                float rise = feet - after;
                if (!float.IsNaN(before) && !float.IsNaN(after) &&
                    Math.Abs(feet - before) <= 0.12f && rise > 0f &&
                    rise <= Math.Abs(step) * 2f + ContactEpsilon)
                {
                    ny -= rise;
                    if (riseBlocked(nx, y, ny)) return true;
                }
                if (bodyBlocked(nx, ny)) return true;
                x = nx;
                y = ny;
            }
            return false;
        }

        internal static bool SweepHorizontal(ref float x, float distance, Func<float, bool> blocked)
            => SweepAxis(ref x, distance, blocked);

        internal static bool SweepAxis(ref float x, float distance, Func<float, bool> blocked)
        {
            int steps = Math.Max(1, (int)Math.Ceiling(Math.Abs(distance) / 0.1f));
            float start = x;
            for (int i = 1; i <= steps; i++)
            {
                float next = start + distance * ((float)i / steps);
                if (blocked(next))
                {
                    float safe = x, hit = next;
                    for (int j = 0; j < 10; j++)
                    {
                        float mid = (safe + hit) * 0.5f;
                        if (blocked(mid)) hit = mid;
                        else safe = mid;
                    }
                    x = safe;
                    return true;
                }
                x = next;
            }
            return false;
        }

        internal static bool HitsWall(float left, float right, float top, float feet,
            Func<int, int, bool> isBlock, Func<int, int, bool> isSlope)
        {
            int x0 = (int)Math.Floor(left + ContactEpsilon);
            int x1 = (int)Math.Floor(right - ContactEpsilon);
            int y0 = (int)Math.Floor(top + ContactEpsilon);
            int y1 = (int)Math.Floor(feet - ContactEpsilon);
            int footRow = (int)Math.Floor(feet);
            for (int y = y0; y <= y1; y++)
            {
                for (int x = x0; x <= x1; x++)
                {
                    // Keep walkable foot-row ramps in the existing BCC ground solver.
                    if (y == footRow && isSlope(x, y)) continue;
                    if (isBlock(x, y)) return true;
                }
            }
            return false;
        }
    }
}
