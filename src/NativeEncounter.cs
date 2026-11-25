using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using evt;
using m2d;
using nel;
using UnityEngine;

namespace KnightInCradle
{
    internal static class NativeEncounter
    {
        private static readonly EncounterMotion Motion = new EncounterMotion();
        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(M2LpSummon), "run"), transpiler:
                new HarmonyMethod(typeof(NativeEncounter), nameof(UseKnightMotion)));
        }
        private static bool CanUseKnight(M2Mover mover)
        {
            var k = KnightEntity.Instance;
            return KnightInCradlePlugin.KnightModeActive && k != null && k.IsActive &&
                !k.IsDead && !k.IsSitting && !k.NativeTransferOwnsHost && !k.IsRepositioning &&
                !EV.isActive(false) && object.ReferenceEquals(mover, KnightInCradleBehaviour.GetPrPublic()) &&
                mover.Mp != null && mover.Mp == M2DBase.Instance?.curMap;
        }
        internal static void Tick()
        {
            var pr = KnightInCradleBehaviour.GetPrPublic();
            if (pr == null || !CanUseKnight(pr)) { Motion.Reset(); return; }
            var k = KnightEntity.Instance;
            Motion.Sample(pr.Mp, Time.frameCount, k.X, k.Y);
        }
        private static float Vx(M2Mover mover) => CanUseKnight(mover)
            ? Motion.Axis(mover.Mp, Time.frameCount, true) : mover.vx;
        internal static float Vy(M2Mover mover) => CanUseKnight(mover)
            ? Motion.Axis(mover.Mp, Time.frameCount, false) : mover.vy;
        private static IEnumerable<CodeInstruction> UseKnightMotion(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo vx = AccessTools.PropertyGetter(typeof(M2Mover), "vx");
            MethodInfo vy = AccessTools.PropertyGetter(typeof(M2Mover), "vy");
            int xCount = 0, yCount = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(vx))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(NativeEncounter), nameof(Vx));
                    xCount++;
                }
                else if (instruction.Calls(vy))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(NativeEncounter), nameof(Vy));
                    yCount++;
                }
                yield return instruction;
            }
            if (xCount == 0 || yCount == 0) throw new System.InvalidOperationException("Encounter movement queries not found");
            Debug.Log("[KIC][Encounter] native motion queries patched: x=" + xCount + " y=" + yCount);
        }
    }
}
