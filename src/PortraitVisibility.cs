using BepInEx.Configuration;
using HarmonyLib;
using nel;

namespace KnightInCradle
{
    internal static class PortraitVisibility
    {
        private static ConfigEntry<bool> visible;
        private static UIBase appliedUi;
        private static bool appliedKnight;
        private static CFGSP.UIPIC_LR shownPosition = CFGSP.UIPIC_LR.L;
        internal static bool Enabled => visible == null || visible.Value;
        internal static void Initialize(ConfigFile config)
        {
            visible = config.Bind("Visual", "PortraitVisible", true, "立绘显示：关闭大立绘时，诺艾尔显示 HUD 头像，小骑士使用无头像居中血条（F10 调试工具）。");
        }
        internal static void ResetForLoad() { appliedUi = null; }
        internal static void Tick()
        {
            if (UIBase.Instance != null && (UIBase.Instance != appliedUi || appliedKnight != KnightInCradlePlugin.KnightModeActive)) Apply();
        }
        internal static void Toggle()
        {
            visible.Value = !Enabled;
            Apply();
            KnightInCradlePlugin.ConfigRef?.Save();
        }
        internal static void Sync() { Apply(); }

        private static void Apply()
        {
            var ui = UIBase.Instance;
            if (ui == null) return;
            if (CFGSP.uipic_lr == CFGSP.UIPIC_LR.L || CFGSP.uipic_lr == CFGSP.UIPIC_LR.R)
                shownPosition = CFGSP.uipic_lr;
            bool knight = KnightInCradlePlugin.KnightModeActive;
            var position = Enabled ? shownPosition : (knight ? CFGSP.UIPIC_LR.NONE : CFGSP.UIPIC_LR.FACE);
            // Stop rendering the large portrait before any role-switch pose/layout work runs.
            if (!Enabled && knight) ui.FlgNoelAreaDisable.Add("__KIC_KNIGHT_PORTRAIT_HIDDEN");
            else ui.FlgNoelAreaDisable.Rem("__KIC_KNIGHT_PORTRAIT_HIDDEN");
            CFGSP.changeConfigValue("spconfig_uipic_lr", (int)position);
            // Remove the retired mode's extra override; native portrait positioning now owns the layout.
            ui.FlgNoelAreaDisable.Rem("__SERIOUS");
            ui.setDrawBgFlag();
            ui.finePictPos(true);
            var redraw = AccessTools.Field(typeof(UIBase), "redraw_flag_");
            if (redraw != null) redraw.SetValue(ui, (uint)redraw.GetValue(ui) | 32768U | 1048576U);
            if (UIStatus.Instance != null)
            {
                UIStatus.Instance.need_reposit = true;
                UIStatus.Instance.fineActivateFace(position == CFGSP.UIPIC_LR.FACE);
                if (!Enabled && knight)
                {
                    // Clear the old Noel portrait immediately rather than carrying it into Knight mode
                    // during the native face-collapse animation.
                    AccessTools.Field(typeof(UIStatus), "t_faceshift")?.SetValue(UIStatus.Instance, -28f);
                }
                UIStatus.Instance.redraw_gage = true;
                UIStatus.Instance.redraw_gage_back = true;
            }
            appliedUi = ui;
            appliedKnight = knight;
        }
    }
}
