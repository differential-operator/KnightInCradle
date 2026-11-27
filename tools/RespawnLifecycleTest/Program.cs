using System;
using KnightInCradle;
using m2d;
using nel;

class Program
{
    static int count;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; }
    static void Main()
    {
        var room = new Map2d();
        var world = new NelM2DBase { curMap = room };
        Check(!RespawnNativeBridge.Begin(world, room, 12, 15), "Same-room death must not trigger native transfer");
        Check(M2LpMapTransferBase.Calls == 0, "Same-room death preserves map and camera lifetime");
        var host = new PRNoel();
        RespawnNativeBridge.Finish(world, host, false);
        Check(host.Quits == 0, "Same-room redraw does not cancel unrelated scripts");
        Check(room.need_reentry_flag && room.need_reentry_gradation_flag, "Redraw includes chips and gradation");
        Check(ReferenceEquals(world.Cam.Target, host), "Camera gets a concrete target");
        Check(world.Cam.Snap == 1 && world.Cam.Fines == 1, "Camera snaps after binding");
        var destination = new Map2d();
        Check(RespawnNativeBridge.Begin(world, destination, 20, 30), "Cross-map death uses native transfer");
        Check(M2LpMapTransferBase.Calls == 1 && ReferenceEquals(M2LpMapTransferBase.Destination, destination), "Transfer goes to destination once");
        Check(M2LpMapTransferBase.X == 20 && M2LpMapTransferBase.Y == 30 && M2LpMapTransferBase.Delay == 10, "Preserve respawn coordinates");
        world.curMap = destination;
        world.Cam.Target = null;
        RespawnNativeBridge.Finish(world, host, true);
        Check(host.Quits == 1, "Respawn LOAD script ends before player control resumes");
        Check(ReferenceEquals(world.Cam.Target, host), "Cross-map camera target is restored");
        Check(world.Cam.Fines == 2 && destination.need_reentry_flag, "Cross-map arrival is drawn and focused");
        Console.WriteLine($"RespawnLifecycleTest: {count} checks passed");
    }
}
namespace m2d
{
    class Map2d { public bool need_reentry_flag, need_reentry_gradation_flag; }
    class Camera
    {
        public object Target; public int Snap, Fines;
        public void assignBaseMover(object host, int snap) { Target = host; Snap = snap; }
        public void fineImmediately() { if (Target == null) throw new Exception("Camera has no target"); Fines++; }
    }
}
namespace nel
{
    class PRNoel { public int Quits; public void quitMoveScript(bool runOnce) { if (runOnce) throw new Exception("Do not execute stale scripts"); Quits++; } }
    class NelM2DBase { public Map2d curMap; public Camera Cam = new Camera(); }
    static class M2LpMapTransferBase
    {
        public static int Calls, X, Y, Delay; public static Map2d Destination;
        public static void executeTransferFastTravel(Map2d map, int x, int y, int delay)
        { Calls++; Destination = map; X = x; Y = y; Delay = delay; }
    }
}
