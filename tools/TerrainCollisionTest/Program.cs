using System;
using KnightInCradle;

static class Program
{
    static int checks;
    static void Check(bool ok, string name)
    {
        if (!ok) throw new Exception(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }
    static bool Wall(int x, int y) => x <= 4 && y >= 10;
    static bool NoSlope(int x, int y) => false;
    static bool Hit(float x, float feet) => KnightTerrainCollision.HitsWall(
        x - 0.271875f, x + 0.2325f, feet - 0.99f, feet, Wall, NoSlope);

    static void Main()
    {
        // Demonstrate that the reported partial-row corner was missed by the old scan.
        bool oldBlocked = false;
        for (int y = (int)Math.Floor(10.75f - 1.07f); y < (int)Math.Floor(10.75f); y++)
            oldBlocked |= Wall(4, y) || Wall(5, y);
        Check(!oldBlocked && Hit(5.1f, 10.75f), "old corner leak reproduced and blocked");
        // Approach both mirrored upper corners across the complete foot-row depth.
        for (int i = 1; i < 100; i++)
        {
            float feet = 10f + i / 100f;
            Check(Hit(5.1f, feet), "left corner depth " + i);
            Check(KnightTerrainCollision.HitsWall(4.9f, 5.4f, feet - 0.99f, feet,
                (x, y) => x >= 5 && y >= 10, NoSlope), "right corner depth " + i);
        }
        Check(!Hit(4.5f, 10f), "walk on wall top");
        Check(!Hit(5.271875f, 10.8f), "touch wall without overlap");
        Check(!Hit(4.5f, 9.9f), "jump clear of wall top");
        Check(!Hit(4.5f, 10.0001f), "floating-point floor contact");
        Check(KnightTerrainCollision.HitsWall(0.1f, 0.6f, 8.5f, 10.5f,
            (x, y) => y == 9, NoSlope), "wall above feet still blocks");
        Check(!KnightTerrainCollision.HitsWall(0.1f, 0.6f, 9.43f, 10.5f,
            (x, y) => y == 10, (x, y) => y == 10), "foot slope stays with BCC");
        Check(!KnightTerrainCollision.HitsWall(0.1f, 0.6f, 9.43f, 10.5f,
            (x, y) => false, NoSlope), "one-way platform is not a side wall");
        float pos = 6f;
        for (int i = 0; i < 20; i++)
        {
            float next = pos - 0.2f;
            if (!Hit(next, 10.75f)) pos = next;
            Check(pos - 0.271875f >= 5f, "substepped approach stays outside " + i);
        }
        Console.WriteLine(checks + " checks passed");
        TestSuperDash();
        TestCeilings();
        TestWallContacts();
        TestGroundDash();
        Console.WriteLine(checks + " total checks passed");
    }

    static void TestGroundDash()
    {
        foreach (int dir in new[] { -1, 1 })
        foreach (float slope in new[] { 0.2f, 0.5f, 1f, 2f })
        foreach (float distance in new[] { 0.02f, 0.075f, 0.15f, 0.45f, 0.9f, 4f })
        {
            float Surface(float x, float y) => 10f - dir * x * slope;
            bool Body(float x, float y) => y + 1.2f > Surface(x, y) + 0.001f;
            float x = 0f, y = 8.8f;
            Check(Body(dir * distance, y), "old horizontal-first dash overlaps uphill surface");
            bool hit = KnightTerrainCollision.SweepGroundDash(ref x, ref y, dir * distance,
                1.2f, Surface, (xx, from, to) => false, Body);
            Check(!hit && Math.Abs(x - dir * distance) < 0.001f &&
                Math.Abs(y + 1.2f - Surface(x, y)) < 0.001f,
                "ordinary dash climbs slope dir=" + dir + " slope=" + slope + " distance=" + distance);
        }
        // Repeated slow walking across row boundaries and a flat/ramp/flat join.
        foreach (int dir in new[] { -1, 1 })
        foreach (float slope in new[] { 0.25f, 0.5f, 1f, 2f })
        {
            float Surface(float xx, float yy) => 10f - Math.Clamp(dir * xx - 1f, 0f, 3f) * slope;
            float wx = 0f, wy = 8.8f;
            for (int frame = 0; frame < 100; frame++)
            {
                bool blocked = KnightTerrainCollision.SweepGroundDash(ref wx, ref wy,
                    dir * 0.075f, 1.2f, Surface, (xx, a, b) => false,
                    (xx, yy) => yy + 1.2f > Surface(xx, yy) + 0.001f);
                Check(!blocked, "walking across slope row/join dir=" + dir + " frame=" + frame);
            }
            Check(Math.Abs(wx - dir * 7.5f) < 0.001f &&
                Math.Abs(wy + 1.2f - Surface(wx, wy)) < 0.001f, "walk clears complete ramp");
        }
        float px = 0f, py = 8.8f;
        bool stopped = KnightTerrainCollision.SweepGroundDash(ref px, ref py, 4f, 1.2f,
            (x, y) => x >= 1f ? 9f : 10f, (x, a, b) => false,
            (x, y) => x >= 1f && y + 1.2f > 9.001f);
        Check(stopped && px < 1f && py == 8.8f, "vertical step is not an uphill ramp");
        px = 0f; py = 8.8f;
        stopped = KnightTerrainCollision.SweepGroundDash(ref px, ref py, 4f, 1.2f,
            (x, y) => 10f - x, (x, a, b) => b < 8.3f, (x, y) => false);
        Check(stopped && px <= 0.501f && py >= 8.3f, "low ceiling blocks ramp ascent without partial pose change");
        px = 0f; py = 7f;
        KnightTerrainCollision.SweepGroundDash(ref px, ref py, 1f, 1.2f,
            (x, y) => 10f - x, (x, a, b) => false, (x, y) => false);
        Check(py == 7f, "air dash does not snap to distant ground");
        px = 0f; py = 8.8f;
        stopped = KnightTerrainCollision.SweepGroundDash(ref px, ref py, 8f, 1.2f,
            (x, y) => 10f, (x, a, b) => false, (x, y) => x >= 2f && x < 3f);
        Check(stopped && px < 2f, "ordinary dash cannot tunnel through thin wall on long frame");
        px = 0f; py = 8.8f;
        KnightTerrainCollision.SweepGroundDash(ref px, ref py, 2f, 1.2f,
            (x, y) => float.NaN, (x, a, b) => false, (x, y) => false);
        Check(Math.Abs(px - 2f) < 0.001f && py == 8.8f, "dash across gap keeps horizontal flight");
    }

    static void TestWallContacts()
    {
        foreach (int dir in new[] { -1, 1 })
        foreach (float face in new[] { -10f, 0f, 10f })
        foreach (float gap in new[] { -0.0001f, 0f, 0.0001f, 0.1f, 0.24f, 0.25f })
        {
            int cell = (int)face - (dir < 0 ? 1 : 0);
            bool WallCell(int x, int y) => x == cell && y == 5;
            float edge = face - dir * gap;
            Check(KnightTerrainCollision.TryWallContact(edge, 5.01f, 5.99f, dir,
                0.25f, WallCell, out float found) && found == face,
                "wall contact dir=" + dir + " face=" + face + " gap=" + gap);
            // Cancellation snaps to this face; the next ordinary frame must retain it.
            Check(KnightTerrainCollision.TryWallContact(found, 5.5f, 5.99f, dir,
                0.25f, WallCell, out float again) && again == face,
                "snapped wall remains grabbable after cancellation");
        }
        bool RightWall(int x, int y) => x == 10 && y == 5;
        Check(!RightWall((int)Math.Floor(10f) + 1, 5), "old right probe skips a thin wall");
        Check(!KnightTerrainCollision.TryWallContact(9.7f, 5f, 6f, 1, 0.25f,
            RightWall, out _), "wall beyond grip reach is not attached");
        Check(!KnightTerrainCollision.TryWallContact(10.2f, 5f, 6f, 1, 0.25f,
            RightWall, out _), "embedded cell is not a forward wall face");
        Check(!KnightTerrainCollision.TryWallContact(10f, 5f, 6f, 1, 0.25f,
            (x, y) => y == 6, out _), "floor at feet is not a wall");
        Check(!KnightTerrainCollision.TryWallContact(10f, 5.5f, 6.5f, 1, 0.25f,
            (x, y) => false, out _), "missing wall does not restore attachment");
        Check(KnightTerrainCollision.TryWallContact(10f, 4.5f, 6.5f, 1, 0.25f,
            (x, y) => x == 10 && y == 4, out _), "full body sees head contact");
        Check(!KnightTerrainCollision.TryWallContact(10f, 5.5f, 6.5f, 1, 0.25f,
            (x, y) => x == 10 && y == 4, out _), "lower body releases at wall end");
    }

    static void TestSuperDash()
    {
        bool Empty(int x, int y) => false;
        float NoSurface(int x, int y, float atX) => float.NaN;
        foreach (int dir in new[] { -1, 1 })
        foreach (float frameMove in new[] { 0.45f, 0.9f, 1.8f, 9f })
        foreach (float rise in new[] { 1f, 0.5f, 0.33333334f, 0.25f, 0.2f })
        {
            int cell = dir > 0 ? 5 : 4;
            float Surface(int x, int y, float atX) => x == cell && y == 10
                ? 11f - rise * (dir > 0 ? atX - cell : cell + 1f - atX) : float.NaN;
            float feet = 11f - rise * 0.25f;
            bool Blocked(float x) => KnightTerrainCollision.HitsFlightTerrain(
                x - 0.271875f, x + 0.2325f, feet - 0.99f, feet, Empty, Surface);
            float pos = dir > 0 ? 3f : 7f;
            bool stopped = false;
            for (int frame = 0; frame < 30 && !stopped; frame++)
                stopped = KnightTerrainCollision.SweepHorizontal(ref pos, dir * frameMove, Blocked);
            Check(stopped, "superdash stops on slope dir=" + dir + " rise=" + rise + " step=" + frameMove);
            Check(!Blocked(pos), "superdash stops outside solid slope");
            float leadingEdge = pos + (dir > 0 ? 0.2325f : -0.271875f);
            float contact = dir > 0 ? 5.25f : 4.75f;
            Check(Math.Abs(leadingEdge - contact) < 0.02f, "superdash stops at real surface, not empty triangle");
        }
        float above = 3f;
        bool AboveBlocked(float x) => KnightTerrainCollision.HitsFlightTerrain(x - 0.27f,
            x + 0.23f, 8.91f, 9.9f, Empty,
            (cx, cy, atX) => cx == 5 && cy == 10 ? 11f - (atX - 5) : float.NaN);
        Check(!KnightTerrainCollision.SweepHorizontal(ref above, 6f, AboveBlocked) && above == 9f,
            "flight above slope is unobstructed");
        float thinWall = 3f;
        bool WallBlocked(float x) => KnightTerrainCollision.HitsFlightTerrain(x - 0.27f,
            x + 0.23f, 9.51f, 10.5f, (cx, cy) => cx == 5, NoSurface);
        Check(!WallBlocked(12f), "old final-position check misses crossed thin wall");
        Check(KnightTerrainCollision.SweepHorizontal(ref thinWall, 9f, WallBlocked) && thinWall < 5f,
            "long frame cannot tunnel through thin wall");
        Check(!KnightTerrainCollision.HitsFlightTerrain(4.1f, 4.6f, 10.01f, 11f,
            (cx, cy) => cy >= 11, NoSurface), "flat ground contact does not stop flight");
    }

    static void TestCeilings()
    {
        foreach (int direction in new[] { -1, 1 })
        foreach (float slope in new[] { 0f, 0.2f, 0.5f, 1f })
        foreach (float rise in new[] { 0.15f, 0.45f, 1.8f, 8f })
        {
            float ly = direction > 0 ? 10f : 10f + slope;
            float ry = direction > 0 ? 10f + slope : 10f;
            float lowest = 10f + slope * 0.749f;
            float from = lowest + 0.1f;
            Check(KnightTerrainCollision.TryCeilingContact(5.25f, 5.75f, from, from - rise,
                5f, 6f, ly, ry, out float stop), "jump hits flat/sloped ceiling dir=" + direction + " rise=" + rise);
            Check(Math.Abs(stop - lowest - KnightTerrainCollision.ContactEpsilon) < 0.0001f,
                "head stops at lowest point across body width");
            Check(!KnightTerrainCollision.TryCeilingContact(5.25f, 5.75f, stop, stop,
                5f, 6f, ly, ry, out _), "horizontal contact permits sliding along ceiling");
        }
        Check(!KnightTerrainCollision.TryCeilingContact(6.1f, 6.6f, 11f, 1f,
            5f, 6f, 10f, 10.5f, out _), "jump next to ceiling endpoint stays free");
        Check(!KnightTerrainCollision.TryCeilingContact(5.2f, 5.7f, 12f, 11f,
            5f, 6f, 10f, 10f, out _), "jump below ceiling without reaching it");
        Check(KnightTerrainCollision.TryCeilingContact(5.9f, 6.4f, 12f, 9f,
            5f, 6f, 10f, 11f, out float edge) && Math.Abs(edge - 11.001f) < 0.0001f,
            "partial-width contact at inverted slope edge");
        Check(KnightTerrainCollision.TryCeilingContact(5.25f, 5.75f, 11.5f, 10f,
            5f, 6f, 10f, 11f, out _), "sideways head overlap under sloped roof is blocked");
        float y = 12f;
        bool HeadBlocked(float atY) => Math.Floor(atY - 0.99f + 0.001f) == 9;
        Check(!HeadBlocked(4f), "old endpoint check misses overhead slab on long frame");
        Check(KnightTerrainCollision.SweepAxis(ref y, -8f, HeadBlocked) && y > 10.98f,
            "upward sweep cannot cross overhead slab");
    }
}
