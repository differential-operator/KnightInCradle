using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using m2d;
using nel;

namespace KnightInCradle
{
    internal static class NativeInteraction
    {
        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(M2EventItem), "execute",
                new[] { typeof(M2EventItem.CMD), typeof(IM2EvTrigger) }),
                prefix: new HarmonyMethod(typeof(NativeInteraction), nameof(BenchTalkPrefix)));
            var replacement = new HarmonyMethod(typeof(NativeInteraction), nameof(UseKnightGround));
            harmony.Patch(AccessTools.Method(typeof(M2MoverPr), "canOpenCheckEvent"), transpiler: replacement);
            harmony.Patch(AccessTools.Method(typeof(PR), "canOpenCheckEvent"), transpiler: replacement);
            harmony.Patch(AccessTools.Method(typeof(UILpSummon), "run"), transpiler: replacement);
        }

        // Consume only the actual bench chip's TALK command, before it enters EV.
        private static bool BenchTalkPrefix(M2EventItem __instance, M2EventItem.CMD __0,
            IM2EvTrigger __1, ref M2EventCommand __result)
        {
            var knight = KnightEntity.Instance;
            if (__0 != M2EventItem.CMD.TALK || !KnightInCradlePlugin.KnightModeActive ||
                knight == null || !knight.IsActive ||
                !object.ReferenceEquals(__1, KnightInCradleBehaviour.GetPrPublic()) ||
                !knight.TryNativeBenchInteraction(__instance)) return true;
            __result = __instance.Get(__0);
            return false;
        }

        // Only replace the eligibility queries in these three interaction methods.
        // Do not change hasFoot globally: native physics and room-transfer scripts need their own foot state.
        private static IEnumerable<CodeInstruction> UseKnightGround(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            var native = AccessTools.Method(typeof(M2Mover), "hasFoot");
            var bridge = AccessTools.Method(typeof(NativeInteraction), nameof(HasInteractionFoot));
            int count = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(native))
                {
                    instruction.opcode = System.Reflection.Emit.OpCodes.Call;
                    instruction.operand = bridge;
                    count++;
                }
                yield return instruction;
            }
            UnityEngine.Debug.Log("[KIC][交互] " + original.DeclaringType.Name + "." + original.Name + " ground checks=" + count);
            if (count == 0) throw new System.InvalidOperationException("Native interaction hasFoot query not found");
        }

        private static bool HasInteractionFoot(M2Mover mover)
        {
            var knight = KnightEntity.Instance;
            if (KnightInCradlePlugin.KnightModeActive && knight != null && knight.IsActive &&
                !knight.NativeTransferOwnsHost && !KnightInCradleBehaviour.EventCharmsSuspended &&
                object.ReferenceEquals(mover, KnightInCradleBehaviour.GetPrPublic()))
                return knight.Grounded;
            return mover.hasFoot();
        }
    }
}
