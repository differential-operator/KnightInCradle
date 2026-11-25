using System;
using System.Collections.Generic;
using KnightInCradle.CharmUi;

class Program
{
    static int checks;
    static void Check(bool result, string message)
    {
        if (!result) throw new Exception(message);
        checks++;
    }
    static void Main()
    {
        CharmSave.Knight.AddRange(new[] { 40, 42, 1 });
        Check(CharmEquipmentRules.OverchargeCount(CharmOwner.Knight) == 1, "Knight overcharge works before UI creation");
        Check(CharmEquipmentRules.OverchargeCount(CharmOwner.Noel) == 0, "Noel gear is independent");
        CharmUiController.Instance = new CharmUiController { Owner = CharmOwner.Noel };
        Check(CharmEquipmentRules.OverchargeCount(CharmOwner.Knight) == 1, "Opening Noel UI does not clear Knight overcharge");
        CharmUiController.Instance.EquippedIds.Add(1);
        Check(CharmEquipmentRules.OverchargeCount(CharmOwner.Noel) == 2, "Noel uses her own cost and current equipment");
        CharmUiController.Instance.Owner = CharmOwner.Knight;
        CharmUiController.Instance.EquippedIds.Clear();
        CharmUiController.Instance.EquippedIds.AddRange(new[] { 40, 42, 2 });
        Check(CharmEquipmentRules.OverchargeCount(CharmOwner.Knight) == 0, "Zero-cost charms do not cause overcharge at full capacity");
        KnightInCradle.KnightInCradleBehaviour.EventCharmsSuspended = true;
        CharmSave.Noel.Add(1);
        Check(CharmEquipmentRules.OverchargeCount(CharmOwner.Noel) == 0, "Event suspends Noel overcharge from snapshot");
        CharmUiController.Instance.EquippedIds.Add(1);
        Check(CharmEquipmentRules.OverchargeCount(CharmOwner.Knight) == 0, "Event suspends Knight overcharge from UI");
        KnightInCradle.KnightInCradleBehaviour.EventCharmsSuspended = false;
        Check(CharmEquipmentRules.OverchargeCount(CharmOwner.Noel) == 2, "Overcharge returns after Event");
        // Base inventory 30 rows, Greed adds 150 per owner.
        Check(CharmEquipmentRules.CanRemoveGreed(30, 180, 150, true), "One Greed can be removed at base capacity");
        Check(!CharmEquipmentRules.CanRemoveGreed(31, 180, 150, true), "Noel and Knight cannot remove occupied expansion");
        Check(CharmEquipmentRules.CanRemoveGreed(180, 330, 150, true), "Other owner's Greed remains available");
        Check(!CharmEquipmentRules.CanRemoveGreed(181, 330, 150, true), "Removing one of two Greed still checks remaining capacity");
        Check(!CharmEquipmentRules.CanRemoveGreed(31, 180, 150, true), "Second removal cannot use already removed bonus");
        Check(CharmEquipmentRules.CanRemoveGreed(30, 30, 150, false), "Suspended Greed does not subtract bonus twice");
        Check(CharmEquipmentRules.CanRemoveGreed(40, 190, 150, true), "Native inventory upgrades remain available");
        Console.WriteLine($"CharmEquipmentTest: {checks} checks passed");
    }
}

// Minimal dependencies keep the real owner selection and event gating executable outside Unity.
namespace KnightInCradle { static class KnightInCradleBehaviour { public static bool EventCharmsSuspended; } }
namespace KnightInCradle.CharmUi
{
    enum CharmOwner { Knight, Noel }
    class CharmUiController
    {
        public static CharmUiController Instance;
        public CharmOwner Owner;
        public List<int> EquippedIds = new List<int>();
    }
    static class CharmSave
    {
        public static List<int> Knight = new List<int>(), Noel = new List<int>();
        public static IReadOnlyList<int> EquippedSnapshotFor(CharmOwner owner) => owner == CharmOwner.Knight ? Knight : Noel;
    }
    static class CharmDatabase
    {
        public static int Get(int id) => id;
        public static int CostOf(int id, CharmOwner owner) => id == 1 ? (owner == CharmOwner.Knight ? 2 : 5) : id == 2 ? 1 : 0;
        public static int NotchCapacityFor(CharmOwner owner) => owner == CharmOwner.Knight ? 1 : 3;
    }
}
