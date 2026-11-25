using System;
using evt;
using HarmonyLib;
using XX;

namespace KnightInCradle
{
    internal static class NativeF7Console
    {
        internal static void Apply()
        {
            var harmony = new Harmony("dev.KnightInCradle.NativeF7Console");
            harmony.Patch(AccessTools.Method(typeof(EV), "initDebugger"),
                prefix: new HarmonyMethod(typeof(NativeF7Console), nameof(Before)),
                postfix: new HarmonyMethod(typeof(NativeF7Console), nameof(After)),
                finalizer: new HarmonyMethod(typeof(NativeF7Console), nameof(Restore)));
        }

        private static void Before(out bool[] __state)
        {
            __state = new[] { X.DEBUGANNOUNCE, X.DEBUGTIMESTAMP };
            X.DEBUGANNOUNCE = true;
            X.DEBUGTIMESTAMP = true;
        }

        private static void After(bool __result)
        {
            if (__result) KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][原版F7] 事件调试控制台已启用");
        }

        private static Exception Restore(Exception __exception, bool[] __state)
        {
            if (__state != null)
            {
                X.DEBUGANNOUNCE = __state[0];
                X.DEBUGTIMESTAMP = __state[1];
            }
            return __exception;
        }
    }
}
