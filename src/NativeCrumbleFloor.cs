using HarmonyLib;
using evt;
using m2d;
using nel;
using XX;

namespace KnightInCradle
{
    internal static class NativeCrumbleFloor
    {
        private static readonly System.Reflection.FieldInfo Label = AccessTools.Field(typeof(M2BreakableWallMover), "Lp");
        private static readonly System.Reflection.FieldInfo PendingFall = AccessTools.Field(typeof(M2BreakableWallMover), "FallInit");
        private static readonly System.Reflection.MethodInfo Ride = AccessTools.Method(typeof(M2BreakableWallMover), "initRideBy");

        internal static void Apply(Harmony harmony)
        {
            if (Label == null || PendingFall == null || Ride == null)
                throw new System.MissingMemberException("Native crumble-floor contact members");
            harmony.Patch(AccessTools.Method(typeof(M2BreakableWallMover), "runPre"),
                prefix: new HarmonyMethod(typeof(NativeCrumbleFloor), nameof(BeforeRun)));
        }

        private static void BeforeRun(M2BreakableWallMover __instance)
        {
            var k = KnightEntity.Instance;
            if (!KnightInCradlePlugin.KnightModeActive || k == null || !k.IsActive || k.IsDead ||
                !k.Grounded || k.IsRepositioning || k.IsSitting || k.NativeTransferOwnsHost || EV.isActive(false)) return;
            var pr = KnightInCradleBehaviour.GetPrPublic();
            if (pr?.Mp == null || pr.Mp != M2DBase.Instance?.curMap || __instance.Mp != pr.Mp ||
                __instance.destructed || !__instance.is_alive || PendingFall.GetValue(__instance) != null) return;
            var label = Label.GetValue(__instance) as M2LpBreakable;
            if (label == null || !label.fallbreak || label.Lay.unloaded) return;
            var collider = __instance.getBCCCon();
            if (collider == null) return;
            // Same native footedInit query, using the Knight's real feet rather than the paused host foot manager.
            BCCLine floor;
            collider.isFallable(k.X + KnightEntity.HurtCenterX, k.FootY, 0.05f, 0.15f,
                out floor, true, true, -1f, null);
            if (floor == null || floor.foot_aim != AIM.B) return;
            // Native code arms FallInit and tracks the host for under-floor restoration checks.
            // The original runPre below owns timing, shake, destruction and map collision updates.
            Ride.Invoke(__instance, new object[] { pr });
            KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][易碎地板] 踩踏触发 map=" + pr.Mp.key + " floor=" + label.key);
        }
    }
}
