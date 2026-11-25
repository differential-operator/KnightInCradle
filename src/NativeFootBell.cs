using System.Collections.Generic;
using HarmonyLib;
using evt;
using m2d;
using nel;
using XX;

namespace KnightInCradle
{
    internal static class NativeFootBell
    {
        private static readonly System.Reflection.FieldInfo Bells =
            AccessTools.Field(typeof(M2CImgDrawerFootBell), "AOnMap");
        private static readonly DRect Bounds = new DRect("KIC_FootBell");

        internal static void Tick()
        {
            var knight = KnightEntity.Instance;
            if (!KnightInCradlePlugin.KnightModeActive || knight == null || !knight.IsActive ||
                !knight.Grounded || knight.NativeTransferOwnsHost || EV.isActive(false)) return;
            var pr = KnightInCradleBehaviour.GetPrPublic();
            if (pr?.Mp == null || pr.Mp != M2DBase.Instance?.curMap || pr.getFootManager() == null) return;
            if (!(Bells?.GetValue(null) is List<M2CImgDrawerFootBell> bells)) return;
            float cx = knight.X + KnightEntity.HurtCenterX;
            float cy = knight.Y + KnightEntity.HurtCenterY;
            foreach (var bell in bells)
            {
                if (bell == null || bell.Mp != pr.Mp) continue;
                var bounds = bell.getMapBounds(Bounds);
                if (bounds == null || !bounds.active) continue;
                if (FootBellContact.Covers(knight.Grounded, cx - KnightEntity.HurtSizeX,
                    cx + KnightEntity.HurtSizeX, cy - KnightEntity.HurtSizeY, knight.FootY,
                    bounds.left, bounds.right, bounds.top, bounds.bottom))
                    // Native method queues the bell; native runner controls notes, order,
                    // cooldown and lights. It does not dereference its BCC argument.
                    bell.footedInit(null, pr.getFootManager());
            }
        }
    }
}
