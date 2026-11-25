using BepInEx.Configuration;
namespace KnightInCradle
{
    internal static class KnightContactDebug
    {
        private static ConfigEntry<bool> enabled;
        internal static bool Enabled => enabled == null || enabled.Value;
        internal static void Initialize(ConfigFile config) => enabled = config.Bind("Debug", "KnightContactDamage", true,
            "小骑士接触魔物身体时受伤；关闭不影响魔物主动攻击。");
        internal static void Toggle()
        {
            enabled.Value = !Enabled;
            KnightInCradlePlugin.ConfigRef?.Save();
        }
    }
}
