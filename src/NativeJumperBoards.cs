using System.Collections.Generic;
using HarmonyLib;
using m2d;
using nel;
using UnityEngine;
using XX;

namespace KnightInCradle
{
    internal static class NativeJumperBoards
    {
        private static readonly HashSet<NelChipJumperBoard> Boards = new HashSet<NelChipJumperBoard>();
        private static readonly System.Reflection.FieldInfo Controller = AccessTools.Field(typeof(NelChipJumperBoard), "CB");
        private static readonly System.Reflection.FieldInfo Power = AccessTools.Field(typeof(NelChipJumperBoard), "pump_velocity");
        private static readonly System.Reflection.MethodInfo Bounce = AccessTools.Method(typeof(NelChipJumperBoard), "DoBounce");
        internal static void Apply(Harmony harmony)
        {
            harmony.Patch(AccessTools.Method(typeof(NelChipJumperBoard), "initAction"),
                postfix: new HarmonyMethod(typeof(NativeJumperBoards), nameof(Opened)));
            harmony.Patch(AccessTools.Method(typeof(NelChipJumperBoard), "closeAction"),
                prefix: new HarmonyMethod(typeof(NativeJumperBoards), nameof(Closed)));
        }
        private static void Opened(NelChipJumperBoard __instance)
        {
            Boards.Add(__instance);
            UnityEngine.Debug.Log("[KIC][Jumper] registered map=" + __instance.Mp.key +
                " moving=" + (__instance.AttachCM != null));
        }
        private static void Closed(NelChipJumperBoard __instance) => Boards.Remove(__instance);

        internal static NelChipJumperBoard Find(Map2d map, float left, float right,
            float previousFeet, float feet, ref float best)
        {
            NelChipJumperBoard selected = null;
            foreach (var board in Boards)
            {
                if (board.Mp != map || board.active_closed || board.Lay.unloaded) continue;
                var cb = Controller.GetValue(board) as BCCLineChipBased;
                if (cb == null || !cb.Bcc.valid || cb.Bcc.foot_aim != AIM.B) continue;
                // Read the live collision line, including mover translation and animation smoothing.
                float top = cb.Bcc.y - cb.BCC.base_shift_y;
                float boardLeft = cb.Bcc.x - cb.BCC.base_shift_x;
                if (!JumperContact.Lands(left, right, previousFeet, feet,
                    boardLeft, boardLeft + cb.width, top)) continue;
                if (float.IsNaN(best) || top <= best + 0.001f)
                {
                    best = top;
                    selected = board;
                }
            }
            return selected;
        }

        internal static Vector2 Pump(NelChipJumperBoard board)
        {
            var receiver = new BounceReceiver();
            // The native path supplies direction, map-specific power and pump animation.
            Bounce.Invoke(board, new object[] { receiver, (float)Power.GetValue(board), true, false });
            return receiver.Velocity;
        }
        private sealed class BounceReceiver : IMapDamageListener
        {
            internal Vector2 Velocity;
            public DRect getMapBounds(DRect rect) => rect;
            public BCCLine get_FootBCC() => null;
            public void remFootListener(IBCCFootListener listener) { }
            public void applyMapDamage(M2MapDamageContainer.M2MapDamageItem damage, BCCLine line) { }
            public void applyVelocity(FOCTYPE type, float x, float y) { Velocity = new Vector2(x, y); }
            public bool isCenterPr() => true;
            public void addOnIce() { }
        }
    }
}
