using System;
using HarmonyLib;
using m2d;
using nel;
using evt;
using UnityEngine;

namespace KnightInCradle
{
    internal static class RoomTransferDiagnostics
    {
        private static readonly RoomArrivalGuard Arrival = new RoomArrivalGuard();
        private static bool _blockedLogged;
        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(M2LpMapTransferBase), "executeTransfer",
                new[] { typeof(Map2d), typeof(string), typeof(string) }),
                prefix: new HarmonyMethod(typeof(RoomTransferDiagnostics), nameof(Before)),
                postfix: new HarmonyMethod(typeof(RoomTransferDiagnostics), nameof(After)));
            harmony.Patch(AccessTools.Method(typeof(M2EventItem), "execute",
                new[] { typeof(M2EventItem.CMD), typeof(IM2EvTrigger) }),
                prefix: new HarmonyMethod(typeof(RoomTransferDiagnostics), nameof(ExitPrefix)));
        }

        private static bool ExitPrefix(M2EventItem __instance, M2EventItem.CMD __0,
            IM2EvTrigger __1, ref M2EventCommand __result)
        {
            if (!KnightInCradlePlugin.KnightModeActive || __0 != M2EventItem.CMD.STAND ||
                !ReferenceEquals(__1, KnightInCradleBehaviour.GetPrPublic()) ||
                !Arrival.Blocks(__instance.Mp?.key, __instance.key)) return true;
            __result = null;
            if (!_blockedLogged)
            {
                _blockedLogged = true;
                KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][入口保护] 阻止到达时反触发 " +
                    __instance.Mp?.key + "/" + __instance.key + Snapshot());
            }
            return false;
        }

        // During save loading the newly created PR exists before Mp is assigned.
        // Native event-size getters dereference Mp, so never evaluate them until this
        // host belongs to the live map and a matching arrival actually needs checking.
        private static bool CanInspectArrival(PRNoel pr)
        {
            return pr != null && pr.Mp != null && pr.Mp == M2DBase.Instance?.curMap &&
                Arrival.Map != null && Arrival.Map == pr.Mp.key;
        }

        internal static void TickArrival(PRNoel pr, bool nativeBusy)
        {
            var knight = KnightEntity.Instance;
            // Callers may finish their own walk before EV has finished the transfer.
            // Never evaluate a new PR map against the knight's previous-map coordinates.
            if (nativeBusy || !CanInspectArrival(pr) ||
                KnightInCradleBehaviour.EventCharmsSuspended || knight == null ||
                !knight.IsInMap(pr.Mp) || knight.NativeTransferOwnsHost) return;
            bool leftVertical = knight != null && knight.IsActive &&
                Arrival.LeaveVerticalEntrance(pr.Mp.key,
                    knight.X + KnightEntity.HurtCenterX - KnightEntity.HurtSizeX,
                    knight.FootY - KnightEntity.HurtSizeY * 2f,
                    knight.X + KnightEntity.HurtCenterX + KnightEntity.HurtSizeX,
                    knight.FootY, false, knight.Grounded);
            if (leftVertical || Arrival.LeaveEntrance(pr.Mp.key, pr.x, pr.event_sizex, false))
                KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][入口保护] 已走离入口，恢复双向通行" + Snapshot() +
                    " knight=" + knight.X.ToString("F3") + "," + knight.FootY.ToString("F3"));
        }

        internal static void FinishArrival(PRNoel pr)
        {
            if (pr == null || !Arrival.NeedsRestore(pr.Mp?.key, pr.x)) return;
            // Finish the native movement first; changing a WAIT_MOVE target mid-script
            // would prevent that script from ever reaching its destination.
            pr.setTo(Arrival.ArrivalX, Arrival.ArrivalFeet - pr.sizey);
            KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][入口保护] 进门位移朝外，交接时恢复安全入口坐标" + Snapshot());
        }

        internal static void ClearArrival() => Arrival.Clear();
        internal static bool TryWalkInGoal(PRNoel pr, out float goal, out int direction)
        {
            goal = 0f; direction = 0;
            return CanInspectArrival(pr) && Arrival.TryWalkInGoal(pr.Mp.key, pr.x,
                pr.event_sizex, out goal, out direction);
        }

        private static string Snapshot()
        {
            var pr = KnightInCradleBehaviour.GetPrPublic();
            return " frame=" + Time.frameCount + " map=" + pr?.Mp?.key +
                " pos=" + pr?.x.ToString("F3") + "," + pr?.y.ToString("F3") +
                " feet=" + pr?.mbottom.ToString("F3") +
                " size=" + pr?.sizex.ToString("F3") + "," + pr?.sizey.ToString("F3") +
                " foot=" + pr?.hasFoot() + " event=" + EV.getCurrentEvent()?.name;
        }

        private static void Before(Map2d __0, string __1, string __2)
        {
            if (!KnightInCradlePlugin.KnightModeActive) return;
            KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][转房命令] 开始 src=" + __0?.key +
                " aim=" + __1 + " exit=" + __2 + Snapshot());
        }

        private static void After(bool __result)
        {
            if (!KnightInCradlePlugin.KnightModeActive) return;
            KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][转房命令] 完成 result=" + __result + Snapshot());
            if (!__result) return;
            KnightEntity.Instance?.AcceptNativeTransferArrival();
            var pr = KnightInCradleBehaviour.GetPrPublic();
            string exit = COOK.last_enter_transfer;
            var lp = pr?.Mp?.getLabelPoint(exit);
            if (lp == null || string.IsNullOrEmpty(exit)) return;
            int inward = exit.StartsWith("ExitR", StringComparison.Ordinal) ? -1 :
                exit.StartsWith("ExitL", StringComparison.Ordinal) ? 1 : 0;
            if (inward != 0)
                Arrival.Arm(pr.Mp.key, exit, pr.x, pr.mbottom,
                    inward > 0 ? lp.mapx + lp.mapw : lp.mapx, inward);
            else if (exit.StartsWith("ExitT", StringComparison.Ordinal) ||
                     exit.StartsWith("ExitB", StringComparison.Ordinal))
                Arrival.ArmVertical(pr.Mp.key, exit, lp.mapx, lp.mapy,
                    lp.mapx + lp.mapw, lp.mapy + lp.maph);
            else { Arrival.Clear(); return; }
            _blockedLogged = false;
            KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][入口保护] 记录 " + pr.Mp.key + "/" + exit);
        }
    }
}
