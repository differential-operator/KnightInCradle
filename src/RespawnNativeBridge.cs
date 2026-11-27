using m2d;
using nel;

namespace KnightInCradle
{
    internal static class RespawnNativeBridge
    {
        internal static bool Begin(NelM2DBase world, Map2d destination, int x, int y)
        {
            // A same-map changeMap clears camera/event state and is not a teleport.
            if (ReferenceEquals(world.curMap, destination)) return false;
            M2LpMapTransferBase.executeTransferFastTravel(destination, x, y, 10);
            return true;
        }

        internal static void Finish(NelM2DBase world, PRNoel host, bool crossedMap)
        {
            if (crossedMap) host.quitMoveScript(false);
            // Request a chip/gradation redraw without closing actions or resetting the map.
            world.curMap.need_reentry_flag = true;
            world.curMap.need_reentry_gradation_flag = true;
            // Normal event-driven transfers restore this during event close; respawn has no event.
            world.Cam.assignBaseMover(host, 1);
            world.Cam.fineImmediately();
        }
    }
}
