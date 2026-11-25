namespace KnightInCradle
{
    internal sealed class EventSession
    {
        internal static bool IsRoomTransfer(bool mapCommand, bool standCommand, string key)
        {
            return mapCommand && standCommand && key != null &&
                key.StartsWith("Exit", System.StringComparison.Ordinal);
        }

        internal static bool IsRoomTransferReader(string name)
        {
            // EV.stack clones a suspended reader into plain EvReader when PRE_LOAD
            // interrupts it. The immutable name survives; M2EventCommand type/Ev do not.
            const string prefix = "%M2EVENTCOMMAND__@";
            const string suffix = "__STAND";
            if (name == null || !name.StartsWith(prefix, System.StringComparison.Ordinal) ||
                !name.EndsWith(suffix, System.StringComparison.Ordinal)) return false;
            int split = name.LastIndexOf('~');
            int length = name.Length - suffix.Length - split - 1;
            return split > prefix.Length && length > 4 &&
                name.IndexOf("Exit", split + 1, System.StringComparison.Ordinal) == split + 1;
        }

        internal static bool IsFootBellEnvironment(string readerName, bool standCommand, string map, string key)
        {
            if (readerName == "m2d_house_footbell") return true;
            return standCommand && map == "house_road" &&
                (key == "_LP_decorate_Ev_Reset_L2R" || key == "_LP_decorate_Ev_Reset_R2L");
        }

        internal static bool IsFacilityAlarmEnvironment(string readerName)
        {
            // These PRE_LOAD commands contain only SETMAGIC_NOMANA EV_ELEC_ALERM.
            return readerName == "%M2EVENTCOMMAND__@mount_in_elec_facility_console~facility_error_alerm__PRE_LOAD" ||
                readerName == "%M2EVENTCOMMAND__@mount_in_elec_facility_backyard~facility_error_alerm__PRE_LOAD" ||
                readerName == "%M2EVENTCOMMAND__@mount_in_elec_facility~facility_error_alerm__PRE_LOAD";
        }

        internal static bool ShouldSuspendCharms(bool storySession, bool eventActive, bool requiresNoel)
        {
            // Exit/environment readers also use EV, but are not story takeovers.
            return storySession || (eventActive && requiresNoel);
        }

        internal bool Active { get; private set; }
        internal bool RestoreKnight { get; private set; }
        private int quietFrames;
        internal string LockedRoom { get; private set; }
        internal static bool HoldsUntilNextRoom(string readerName)
        {
            return readerName == "200/71_escape_floor_deadend" ||
                readerName == "200/72_escape_floor_deadend_absorb" ||
                readerName == "200/73_escape_floor_deadend_break" ||
                readerName == "%M2EVENTCOMMAND__@mount_spidert~QA200_EVENT_24__STAND" ||
                readerName == "%M2EVENTCOMMAND__@mount_spidert~QA200_EVENT_25__STAND";
        }
        internal void HoldUntilNextRoom(string map)
        {
            if (RestoreKnight && LockedRoom == null && !string.IsNullOrEmpty(map)) LockedRoom = map;
        }
        internal void Reset()
        {
            Active = RestoreKnight = false;
            LockedRoom = null;
            quietFrames = 0;
        }
        internal void Enter(bool knight)
        {
            Active = true;
            RestoreKnight |= knight;
            quietFrames = 0;
        }
        internal bool Tick(bool busy, bool ready, string currentMap = null)
        {
            if (!Active) return false;
            if (busy || !ready || (LockedRoom != null &&
                (currentMap == null || currentMap == LockedRoom)))
            { quietFrames = 0; return false; }
            if (++quietFrames < 2) return false;
            bool restore = RestoreKnight;
            Reset();
            return restore;
        }
    }
}
