using HarmonyLib;
using nel;
using UnityEngine;
using XX;

namespace KnightInCradle
{
    internal static class KnightHudFrameFade
    {
        private static readonly System.Reflection.FieldInfo NumberMesh = AccessTools.Field(typeof(UIStatus), "MdGageT");
        private static UIStatus lastUi;
        private static bool lastActive;
        private static float lastAlpha = -1f;
        private static bool Active => KnightInCradlePlugin.KnightModeActive &&
            KnightEntity.Instance != null && KnightEntity.Instance.IsActive;

        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(UIStatus), "DrawGageLay"),
                prefix: new HarmonyMethod(typeof(KnightHudFrameFade), nameof(Before)),
                postfix: new HarmonyMethod(typeof(KnightHudFrameFade), nameof(After)));
            harmony.Patch(AccessTools.Method(typeof(BMListChars), "DrawScaleStringTo"),
                prefix: new HarmonyMethod(typeof(KnightHudFrameFade), nameof(BeforeNumber)),
                postfix: new HarmonyMethod(typeof(KnightHudFrameFade), nameof(After)));
        }

        internal static void Tick()
        {
            var ui = UIStatus.Instance;
            bool active = Active;
            float alpha = active ? KnightHudDeco.RevealProgress : 1f;
            if (ui != null && (active || lastActive) &&
                (ui != lastUi || active != lastActive || alpha != lastAlpha))
            {
                // Both the foreground frame and its background are cached separately.
                ui.redraw_gage = true;
                ui.redraw_gage_back = true;
                ui.redraw_bar_num = true;
            }
            lastUi = ui;
            lastActive = active;
            lastAlpha = alpha;
        }

        private static void Before(MeshDrawer Md, out int __state)
        {
            __state = Active ? Md.getVertexMax() : -1;
        }

        private static void BeforeNumber(MeshDrawer Md, out int __state)
        {
            // The font is shared by the entire game; fade only the status HP / soul number mesh.
            var ui = UIStatus.Instance;
            __state = Active && Md != null && ui != null && NumberMesh != null &&
                ReferenceEquals(NumberMesh.GetValue(ui), Md) ? Md.getVertexMax() : -1;
        }

        private static void After(MeshDrawer Md, int __state)
        {
            if (__state < 0) return;
            float alpha = Mathf.Clamp01(KnightHudDeco.RevealProgress);
            if (alpha >= 1f) return;
            var colors = Md.getColorArray();
            int end = Mathf.Min(Md.getVertexMax(), colors.Length);
            // Only scale newly generated vertices, before native mesh upload; never compound fades.
            for (int i = __state; i < end; i++)
                colors[i].a = (byte)(colors[i].a * alpha);
        }
    }
}
