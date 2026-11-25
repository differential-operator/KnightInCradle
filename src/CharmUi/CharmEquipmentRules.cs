using System;
using System.Collections.Generic;

namespace KnightInCradle.CharmUi
{
    internal static class CharmEquipmentRules
    {
        // Equipment belongs to the character, regardless of which UI was last opened.
        public static int OverchargeCount(CharmOwner owner)
        {
            if (KnightInCradleBehaviour.EventCharmsSuspended) return 0;
            CharmUiController controller = CharmUiController.Instance;
            IReadOnlyList<int> equipped = controller != null && controller.Owner == owner
                ? controller.EquippedIds : CharmSave.EquippedSnapshotFor(owner);
            int total = 0;
            if (equipped != null)
            {
                for (int i = 0; i < equipped.Count; i++)
                    total += Math.Max(0, CharmDatabase.CostOf(CharmDatabase.Get(equipped[i]), owner));
            }
            return Math.Max(0, total - CharmDatabase.NotchCapacityFor(owner));
        }

        public static bool CanRemoveGreed(int occupiedRows, int currentCapacity,
            int bonusPerOwner, bool ownerEffectActive)
        {
            int remainingCapacity = currentCapacity - (ownerEffectActive ? bonusPerOwner : 0);
            return occupiedRows <= Math.Max(0, remainingCapacity);
        }
    }
}
