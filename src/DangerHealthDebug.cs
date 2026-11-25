using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using BepInEx.Configuration;
using HarmonyLib;
using m2d;
using nel;

namespace KnightInCradle
{
    internal static class DangerHealthDebug
    {
        private sealed class Health
        {
            internal int Basis, LastMax, LastHp;
            internal double Fraction;
        }
        private static readonly ConditionalWeakTable<NelEnemy, Health> entries = new ConditionalWeakTable<NelEnemy, Health>();
        private static readonly FieldInfo maxField = AccessTools.Field(typeof(M2Attackable), "maxhp");
        private static readonly FieldInfo hpField = AccessTools.Field(typeof(M2Attackable), "hp");
        private static ConfigEntry<bool> enabled;
        internal static bool Enabled => enabled != null && enabled.Value;
        internal static void Apply(Harmony harmony, ConfigFile config)
        {
            enabled = config.Bind("Debug", "MoreDangerousDanger", false, "危险度每1点，魔物基础最大生命增加1%。");
            foreach (string method in new[] { "overdriveStatusFine", "quitOverDrive" })
                harmony.Patch(AccessTools.Method(typeof(NelEnemy), method),
                    prefix: new HarmonyMethod(typeof(DangerHealthDebug), nameof(BeforeNativeResize)),
                    postfix: new HarmonyMethod(typeof(DangerHealthDebug), nameof(AfterNativeResize)));
        }
        internal static void Toggle()
        {
            enabled.Value = !Enabled;
            KnightInCradlePlugin.ConfigRef?.Save();
            Tick();
        }
        internal static void Tick()
        {
            var map = M2DBase.Instance?.curMap;
            if (enabled == null || map == null) return;
            int danger = DangerDebug.Controller?.getDangerMeterVal() ?? 0;
            for (int i = map.count_movers-1; i >= 0; i--)
                if (map.getMv(i) is NelEnemy enemy) Update(enemy,danger);
        }
        private static void Update(NelEnemy enemy, int danger)
        {
            if (enemy == null || !enemy.is_alive) return;
            int maximum = (int)maxField.GetValue(enemy), hp = (int)hpField.GetValue(enemy);
            if (maximum <= 0 || hp <= 0) return;
            Health state;
            if (!entries.TryGetValue(enemy,out state))
            {
                if (!Enabled) return;
                state = new Health { Basis=maximum,LastMax=maximum,LastHp=hp,Fraction=(double)hp/maximum };
                entries.Add(enemy,state);
            }
            // Native reinitialization supplies a fresh unscaled maximum.
            if (maximum != state.LastMax) {state.Basis=maximum;state.Fraction=(double)hp/maximum;}
            else if (hp != state.LastHp) state.Fraction=(double)hp/maximum;
            int target = Enabled ? DangerHealthMath.Maximum(state.Basis,danger) : state.Basis;
            if (target != maximum)
            {
                hp = Math.Max(1, Math.Min(target,(int)Math.Round(target*state.Fraction,MidpointRounding.AwayFromZero)));
                maxField.SetValue(enemy,target); hpField.SetValue(enemy,hp);
            }
            state.LastMax=target;state.LastHp=hp;
        }
        private static void BeforeNativeResize(NelEnemy __instance)
        {
            Health state;
            if (__instance == null || !entries.TryGetValue(__instance,out state)) return;
            int hp=(int)hpField.GetValue(__instance), maximum=(int)maxField.GetValue(__instance);
            if (maximum == state.LastMax && maximum > 0)
            {
                maxField.SetValue(__instance,state.Basis);
                hpField.SetValue(__instance,hp<=0 ? 0 : Math.Max(1,(int)Math.Round((double)hp*state.Basis/maximum)));
            }
            entries.Remove(__instance);
        }
        private static void AfterNativeResize(NelEnemy __instance) => Update(__instance,DangerDebug.Controller?.getDangerMeterVal() ?? 0);
    }
}
