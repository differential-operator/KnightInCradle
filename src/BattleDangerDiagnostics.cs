using System;
using System.Reflection;
using HarmonyLib;
using m2d;
using nel;

namespace KnightInCradle
{
    // Observe the real settlement without recomputing rewards or modifying native state.
    internal static class BattleDangerDiagnostics
    {
        private static readonly FieldInfo Reserved = AccessTools.Field(typeof(NightController), "reserved_obtainable_grade");
        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(NightController), "SummonerDefeated"),
                prefix: new HarmonyMethod(typeof(BattleDangerDiagnostics), nameof(Before)),
                postfix: new HarmonyMethod(typeof(BattleDangerDiagnostics), nameof(After)));
            harmony.Patch(AccessTools.Method(typeof(M2LpSummon), "closeSummoner"),
                postfix: new HarmonyMethod(typeof(BattleDangerDiagnostics), nameof(Closed)));
        }

        private static void Before(NightController __instance, EnemySummoner Smn, int ob_add, out int __state)
        {
            __state = __instance.getDangerMeterVal(false, true);
            try
            {
                var world = M2DBase.Instance as NelM2DBase;
                KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][危险度结算] 开始 map=" + world?.curMap?.key +
                    " battle=" + Smn?.key + " before=" + __state + " grade=" + Smn?.grade +
                    " override=" + ob_add + " reserved=" + Reserved?.GetValue(__instance) +
                    " debugLock=" + __instance.debug_lock_dangerousness +
                    " debugListener=" + world?.debug_listener_created +
                    " gameOver=" + world?.isGameOverActive() + " battles=" + __instance.getBattleCount());
            }
            catch (Exception ex) { KnightInCradlePlugin.PluginLog?.LogWarning("[KIC][危险度结算] 诊断读取失败 " + ex.Message); }
        }

        private static void After(NightController __instance, int __state, int __result)
        {
            KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][危险度结算] 完成 before=" + __state +
                " after=" + __instance.getDangerMeterVal(false, true) + " award=" + __result);
        }

        private static void Closed(M2LpSummon __instance, bool defeated)
        {
            try
            {
                KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][战斗结束] map=" + __instance.Mp?.key +
                    " area=" + __instance.key + " victory=" + defeated);
            }
            catch (Exception) { }
        }
    }
}
