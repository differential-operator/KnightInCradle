using System;
using KnightInCradle;
class Program
{
    static int checks;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); checks++; }
    static void Main()
    {
        foreach (string key in new[] { "ExitL_left", "ExitR_right", "ExitT_up", "ExitB_down", "ExitD_door", "ExitWL_warp" })
        {
            Check(EventSession.IsRoomTransfer(true, true, key), "native exit keeps knight: " + key);
            Check(!EventSession.IsRoomTransfer(true, false, key), "exit dialogue still requires Noel");
            Check(!EventSession.IsRoomTransfer(false, true, key), "similarly named story is not an exit command");
        }
        Check(!EventSession.IsRoomTransfer(true, true, "NpcIntro"), "stand-triggered story requires Noel");
        Check(!EventSession.IsRoomTransfer(true, true, null), "unknown command remains protected");
        Check(EventSession.IsFootBellEnvironment("m2d_house_footbell", false, null, null), "bell setup keeps knight");
        foreach (string key in new[] { "_LP_decorate_Ev_Reset_L2R", "_LP_decorate_Ev_Reset_R2L" })
        {
            Check(EventSession.IsFootBellEnvironment("generated", true, "house_road", key), "bell reset keeps knight");
            Check(!EventSession.IsFootBellEnvironment("generated", false, "house_road", key), "dialogue is not a reset");
            Check(!EventSession.IsFootBellEnvironment("generated", true, "other_map", key), "other map remains protected");
        }
        Check(!EventSession.IsFootBellEnvironment("story_intro", true, "house_road", "Npc"), "house road story still requires Noel");
        Check(!EventSession.IsFootBellEnvironment("m2d_house_footbell_story", true, "house_road", "Npc"), "similar story names remain protected");
        foreach (float offset in new[] { -1f, 0f, 1f })
        {
            Check(FootBellContact.Covers(true, 10f + offset, 10.5f + offset, 7f, 8f,
                9f, 11f, 6f, 9f), "grounded knight notifies bell across width");
            Check(!FootBellContact.Covers(false, 10f + offset, 10.5f + offset, 7f, 8f,
                9f, 11f, 6f, 9f), "airborne knight does not notify bell");
        }
        Check(!FootBellContact.Covers(true, 14f, 14.5f, 7f, 8f, 9f, 11f, 6f, 9f), "distant bell stays silent");
        Check(!FootBellContact.Covers(true, 10f, 10.5f, 2f, 3f, 9f, 11f, 6f, 9f), "bell below another platform stays silent");
        Check(!FootBellContact.Covers(true, 10f, 10.5f, 11f, 12f, 9f, 11f, 6f, 9f), "bell above another platform stays silent");
        foreach (string map in new[] { "mount_in_elec_facility_console", "mount_in_elec_facility_backyard", "mount_in_elec_facility" })
        {
            string reader = "%M2EVENTCOMMAND__@" + map + "~facility_error_alerm__PRE_LOAD";
            Check(EventSession.IsFacilityAlarmEnvironment(reader), "alarm PRE_LOAD retains native transfer ownership");
            Check(!EventSession.IsFacilityAlarmEnvironment(reader.Replace("PRE_LOAD", "STAND")), "alarm interaction is not broadly exempted");
            Check(!EventSession.IsFacilityAlarmEnvironment(reader.Replace("facility_error_alerm", "story")), "facility story still requires Noel");
        }
        Check(!EventSession.IsFacilityAlarmEnvironment(null), "null reader is not an alarm");
        Check(!EventSession.IsFacilityAlarmEnvironment("200/45b_elec_facil_alert"), "actual facility plot remains protected");
        foreach (string gate in new[] { "ExitT_backyard", "ExitB_backyard", "ExitR_console", "ExitL_console" })
        {
            string immutableName = "%M2EVENTCOMMAND__@mount_in_elec_facility_console~" + gate + "__STAND";
            Check(EventSession.IsRoomTransferReader(immutableName), "native exit reader recognized by preserved identity");
            string cloneName = new string(immutableName.ToCharArray());
            Check(EventSession.IsRoomTransferReader(cloneName), "plain EvReader continuation without Ev or cmd_index retains transfer ownership");
            Check(!EventSession.IsRoomTransferReader(immutableName.Replace("__STAND", "__TALK")), "same exit dialogue is a story");
            Check(!EventSession.IsRoomTransferReader(immutableName.Replace("~Exit", "~NpcExit")), "NPC name containing Exit remains a story");
        }
        Check(!EventSession.IsRoomTransferReader(null), "null continuation is not a transfer");
        Check(!EventSession.IsRoomTransferReader("story/ExitT_backyard__STAND"), "story filename cannot impersonate map command");
        Check(!EventSession.IsRoomTransferReader("%M2EVENTCOMMAND__@~ExitT_a__STAND"), "missing source map is not a transfer");
        Check(!EventSession.IsRoomTransferReader("%M2EVENTCOMMAND__@map~Exit__STAND"), "missing exit identifier is not a transfer");
        var motion = new EncounterMotion();
        var room = new object();
        motion.Sample(room, 10, 5f, 8f);
        Check(motion.Axis(room, 10, true) == 0f, "arrival is not movement");
        motion.Sample(room, 11, 5.2f, 8f);
        Check(motion.Axis(room, 11, true) > 0f && motion.Axis(room, 11, false) == 0f, "walk activates encounter movement");
        Check(motion.Axis(room, 12, true) > 0f, "native update reads last completed knight frame");
        motion.Sample(room, 12, 5.2f, 7f);
        Check(motion.Axis(room, 12, true) == 0f && motion.Axis(room, 12, false) < 0f, "jump activates encounter movement");
        motion.Sample(room, 13, 9f, 7f);
        Check(motion.Axis(room, 13, true) > 0f, "dash actual displacement works even with zero Vx field");
        motion.Sample(room, 14, 9f, 7f);
        Check(motion.Axis(room, 14, true) == 0f && motion.Axis(room, 14, false) == 0f, "idle or blocked movement remains stationary");
        Check(motion.Axis(room, 16, true) == 0f, "stale sample cannot trigger encounter");
        var otherRoom = new object();
        Check(motion.Axis(otherRoom, 14, true) == 0f, "old room sample cannot leak");
        motion.Sample(otherRoom, 15, 100f, 50f);
        Check(motion.Axis(otherRoom, 15, true) == 0f, "map teleport is not walked distance");
        motion.Sample(otherRoom, 18, 150f, 50f);
        Check(motion.Axis(otherRoom, 18, true) == 0f, "gap during event/loading resets sample");
        motion.Sample(otherRoom, 19, 149f, 50f);
        Check(motion.Axis(otherRoom, 19, true) < 0f, "movement resumes after load");
        motion.Reset();
        Check(motion.Axis(otherRoom, 19, true) == 0f, "mode switch cancels pending movement");
        foreach (bool knight in new[] {false, true})
        {
            var state = new EventSession();
            Check(!state.Active && !state.Tick(false, true), "idle does not switch");
            state.Enter(knight);
            Check(state.Active && state.RestoreKnight == knight, "entry captures original character");
            for (int i=0; i<30; i++) Check(!state.Tick(true, true) && state.Active, "event retains ownership");
            Check(!state.Tick(false, true) && state.Active, "nested event gap is protected");
            state.Enter(false);
            Check(state.RestoreKnight == knight, "nested Noel event preserves original knight intent");
            for (int i=0; i<5; i++) Check(!state.Tick(false, false) && state.Active, "loading or missing host delays restoration");
            Check(!state.Tick(false, true), "first quiet frame");
            Check(!state.Tick(true, true) && state.Active, "new queued event resets quiet frames");
            Check(!state.Tick(false, true) && state.Active, "quiet count restarted");
            Check(state.Tick(false, true) == knight && !state.Active && !state.RestoreKnight, "completion restores exactly original mode");
            Check(!state.Tick(false, true), "restoration is once only");
            state.Enter(!knight);
            Check(state.RestoreKnight == !knight, "next independent event captures new mode");
        }
        var held = new EventSession();
        held.Enter(true);
        held.HoldUntilNextRoom("mount_spidert");
        Check(held.Active && held.RestoreKnight && held.LockedRoom == "mount_spidert", "knight story captures room lock");
        for (int i=0; i<300; i++)
            Check(!held.Tick(false, true, "mount_spidert") && held.Active, "inter-event pause cannot restore knight");
        held.Enter(false);
        held.HoldUntilNextRoom("other");
        Check(held.LockedRoom == "mount_spidert" && held.RestoreKnight, "follow-up event keeps original room and character");
        Check(!held.Tick(true, true, "next"), "destination story must finish before unlock");
        Check(!held.Tick(false, false, "next"), "loading cannot unlock story room");
        Check(!held.Tick(false, true, null), "missing map cannot unlock story room");
        Check(!held.Tick(false, true, "next"), "new room waits stable frame");
        Check(held.Tick(false, true, "next") && !held.Active && held.LockedRoom == null, "restore knight once after entering next room");
        Check(!held.Tick(false, true, "next"), "room restoration is once only");
        var noelStory = new EventSession();
        noelStory.Enter(false); noelStory.HoldUntilNextRoom("mount_spidert");
        Check(noelStory.LockedRoom == null, "Noel-origin story does not gain knight-specific room lock");
        noelStory.Tick(false,true); noelStory.Tick(false,true);
        Check(!noelStory.Active, "ordinary Noel event ends normally");
        held.Enter(true); held.HoldUntilNextRoom("mount_spidert"); held.Reset();
        Check(!held.Active && !held.RestoreKnight && held.LockedRoom == null, "loading clears old room and restore intent");
        foreach (string ev in new[] { "200/71_escape_floor_deadend", "200/72_escape_floor_deadend_absorb", "200/73_escape_floor_deadend_break",
            "%M2EVENTCOMMAND__@mount_spidert~QA200_EVENT_24__STAND", "%M2EVENTCOMMAND__@mount_spidert~QA200_EVENT_25__STAND" })
            Check(EventSession.HoldsUntilNextRoom(ev), "dead-end chain detected: " + ev);
        Check(!EventSession.HoldsUntilNextRoom("200/70_escape_floor"), "previous-room story not locked");
        Check(!EventSession.HoldsUntilNextRoom(null), "unknown event not locked");
        foreach (int pool in new[] { 350, 275, 200, 97, 0 })
        {
            int current = pool;
            foreach (string name in new[] {
                "%M2EVENTCOMMAND__@source~ExitR_gate__STAND",
                "%M2EVENTCOMMAND__@mount_in_elec_facility~facility_error_alerm__PRE_LOAD",
                "%M2EVENTCOMMAND__@source~ExitR_gate__STAND" })
            {
                bool requires = !EventSession.IsRoomTransferReader(name) && !EventSession.IsFacilityAlarmEnvironment(name);
                bool suspended = EventSession.ShouldSuspendCharms(false, true, requires);
                int maximum = suspended ? 200 : 350;
                current = Math.Min(current, maximum);
                Check(current == pool && maximum == 350, "room transfer preserves exact full/partial/empty Joni pool");
            }
        }
        Check(EventSession.ShouldSuspendCharms(false,true,true), "actual Noel story still suspends effects");
        Check(EventSession.ShouldSuspendCharms(true,false,false), "story room lock suspends effects between readers");
        Check(!EventSession.ShouldSuspendCharms(false,false,false), "ordinary gameplay keeps effects");
        Console.WriteLine(checks + " checks passed");
    }
}
