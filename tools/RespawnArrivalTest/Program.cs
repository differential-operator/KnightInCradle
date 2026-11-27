using System;
using KnightInCradle;

class Program
{
    static int count;
    static void Check(bool ok, string description)
    {
        if (!ok) throw new Exception(description);
        count++;
    }
    static void Main()
    {
        var room = new object(); var old = new object();
        var gate = new RespawnArrivalGuard();
        foreach (bool sameRoom in new[] { false, true })
        {
            gate.Begin(room);
            var current = sameRoom ? room : old;
            Check(!gate.Tick(current, old, true, true, 1), "Do not resume on the death map");
            Check(!gate.Tick(room, room, false, false, 2), "Wait for layers");
            Check(!gate.Tick(room, old, true, false, 3), "Wait for native host to arrive");
            Check(!gate.Tick(room, room, true, true, 4), "Wait for native loading");
            Check(!gate.Tick(room, room, true, false, 5), "Opening frame cannot finish respawn");
            Check(!gate.Tick(room, room, true, false, 5), "Multiple GUI/update calls are not multiple frames");
            Check(gate.Tick(room, room, true, false, 6), "Stable destination can finish");
            Check(!gate.Tick(room, room, true, true, 7), "Loading restart revokes readiness");
            Check(!gate.Tick(room, room, true, false, 8), "Stability restarts after reload");
            Check(gate.Tick(room, room, true, false, 9), "Reload can finish normally");
        }
        gate.Begin(null);
        Check(!gate.Tick(null, null, true, false, 10), "No missing-map completion");
        gate.Begin(room);
        Check(!gate.Tick(room, room, true, false, 11), "New death resets prior arrival");
        Console.WriteLine($"RespawnArrivalTest: {count} checks passed");
    }
}
