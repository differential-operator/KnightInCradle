using BepInEx.Configuration;
using HarmonyLib;
using m2d;
using nel;
using UnityEngine;

namespace KnightInCradle
{
    internal static class DangerDebug
    {
        private static ConfigEntry<bool> uncapped;
        internal static bool Uncapped => uncapped != null && uncapped.Value;
        internal static NightController Controller => (M2DBase.Instance as NelM2DBase)?.NightCon;
        internal static bool Ready => Controller != null && KnightInCradleBehaviour.GetPrPublic()?.Mp != null;
        internal static int Current => Controller?.getDangerMeterVal(false, true) ?? 0;
        internal static void Apply(Harmony harmony, ConfigFile config)
        {
            uncapped = config.Bind("Debug", "DisableDangerCap", false, "禁用原版危险度160上限（F10调试开关）");
            harmony.Patch(AccessTools.Method(typeof(NightController), "getDangerLevel"),
                postfix: new HarmonyMethod(typeof(DangerDebug), nameof(Level)));
            harmony.Patch(AccessTools.Method(typeof(NightController), "getDangerMeterVal"),
                postfix: new HarmonyMethod(typeof(DangerDebug), nameof(Meter)));
        }
        internal static void Change(int delta)
        {
            if (!Ready) return;
            var con = Controller;
            int value = DangerDebugMath.Adjust(con.getDangerMeterVal(true, true), delta, Uncapped);
            con.applyDangerousFromEvent(value, true);
            con.showNightLevelAdditionUI(true);
        }
        internal static void Toggle()
        {
            uncapped.Value = !Uncapped;
            KnightInCradlePlugin.ConfigRef?.Save();
            if (!Uncapped && Ready)
                Controller.applyDangerousFromEvent(Mathf.Min(160, Controller.getDangerMeterVal(true, true)), true);
            if (Ready) Controller.UiDg.fineMeterShowLevel();
        }
        private static void Level(NightController __instance, ref float __result)
        {
            if (Uncapped) __result = __instance.getDangerMeterVal(false, true) / 16f;
        }
        private static void Meter(NightController __instance, bool real, bool raw, ref int __result)
        {
            if (Uncapped && !raw) __result = __instance.getDangerMeterVal(real, true);
        }
    }
}
