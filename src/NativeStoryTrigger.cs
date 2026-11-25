using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using m2d;
using nel;

namespace KnightInCradle
{
    // This quest attack is a story trigger, rather than ordinary enemy damage.
    // Keep its native area/progress checks and hand over only when it actually hits.
    internal static class NativeStoryTrigger
    {
        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(NelNPuppyEvent), "runPunch2_200_12_Attack"),
                transpiler: new HarmonyMethod(typeof(NativeStoryTrigger), nameof(BridgeTrigger)));
        }

        private static int StoryHit(PR pr, NelAttackInfo attack, bool force)
        {
            if (KnightInCradlePlugin.KnightModeActive &&
                object.ReferenceEquals(pr, KnightInCradleBehaviour.GetPrPublic()))
            {
                KnightInCradleBehaviour.BeginStoryEvent();
                UnityEngine.Debug.Log("[KIC][Story] cannonroad attack handed to Noel");
            }
            return pr.applyDamage(attack, force);
        }

        private static IEnumerable<CodeInstruction> BridgeTrigger(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo velocity = AccessTools.PropertyGetter(typeof(M2Mover), "vy");
            MethodInfo damage = AccessTools.Method(typeof(PR), "applyDamage",
                new[] { typeof(NelAttackInfo), typeof(bool) });
            int velocities = 0, hits = 0;
            foreach (var instruction in instructions)
            {
                if (instruction.Calls(velocity))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(NativeEncounter), nameof(NativeEncounter.Vy));
                    velocities++;
                }
                else if (instruction.Calls(damage))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = AccessTools.Method(typeof(NativeStoryTrigger), nameof(StoryHit));
                    hits++;
                }
                yield return instruction;
            }
            if (velocities != 1 || hits != 1)
                throw new InvalidOperationException("Quest attack trigger queries changed");
            UnityEngine.Debug.Log("[KIC][Story] cannonroad trigger patched: velocity=1 hit=1");
        }
    }
}
