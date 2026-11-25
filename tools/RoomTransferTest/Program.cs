using System;
using KnightInCradle;

static class Program
{
    static int checks;
    static void Check(bool value, string name)
    {
        if (!value) throw new Exception(name);
        checks++;
        Console.WriteLine("PASS " + name);
    }
    static void Main()
    {
        foreach (int frames in new[] { 13, 50, 180, 1200 })
        {
            var h = new RoomTransferHandoff();
            Check(!h.Tick(true, true, false, true) && h.Active,
                "ownership starts before loading or map change");
            bool premature = false;
            for (int i = 0; i < frames; i++) premature |= h.Tick(true, true, false, true);
            Check(!premature && h.Active, "native exit waits are not cut short " + frames);
            Check(!h.Tick(false, true, true, false) && h.Active,
                "nested event and absent host during map load keep ownership");
            Check(!h.Tick(false, false, true, true) && h.Active,
                "event ending during load cannot release host");
            Check(!h.Tick(false, true, false, true) && h.Active,
                "native walk-in after map load owns movement");
            Check(!h.Tick(false, false, false, true) && h.Active,
                "one quiet frame is not completion");
            Check(!h.Tick(false, true, false, true) && h.Active,
                "next nested event cancels premature completion");
            Check(!h.Tick(false, false, false, false) && h.Active,
                "missing destination host prevents completion");
            h.Tick(false, false, false, true);
            Check(h.Tick(false, false, false, true) && !h.Active,
                "release only after complete script and stable destination");
            Check(!h.Tick(false, false, false, true), "completion emitted once");
            h.Tick(true, true, false, true);
            Check(h.Active, "next room crossing starts a fresh handoff");
            h.Reset();
            Check(!h.Active && !h.Tick(false, false, false, true), "mode switch clears old ownership");
        }
        var ordinary = new RoomTransferHandoff();
        Check(!ordinary.Tick(false, true, false, true) && !ordinary.Active,
            "unrelated dialogue does not start transfer ownership");
        foreach (int direction in new[] { -1, 1 })
        {
            var entry = new RoomArrivalGuard();
            string gate = direction == -1 ? "ExitR_labop" : "ExitL_labop";
            entry.Arm("destination", gate, 46.071f, 14.018f, 46f, direction);
            Check(entry.Blocks("destination", gate), "arrival trigger is disarmed");
            Check(!entry.Blocks("destination", "ExitT_other") && !entry.Blocks("source", gate),
                "other exits and rooms are unaffected");
            Check(!entry.LeaveEntrance("destination", 46f + direction * 5f, 0.43f, true),
                "native walk-in cannot release entry protection early");
            Check(!entry.LeaveEntrance("destination", 46f + direction * 0.3f, 0.43f, false),
                "remaining inside trigger cannot immediately reverse transfer");
            Check(entry.NeedsRestore("destination", 46.071f - direction * 1.04f),
                "observed outward native drift is detected");
            Check(!entry.NeedsRestore("destination", 46.071f + direction * 1.04f),
                "valid inward movement is preserved");
            Check(entry.LeaveEntrance("destination", 46f + direction * 2f, 0.43f, false),
                "walking clear of entrance releases protection");
            Check(!entry.Blocks("destination", gate), "deliberate return is allowed after entry");
            entry.Arm("destination", gate, 0f, 0f, 0f, direction);
            entry.Clear();
            Check(!entry.Blocks("destination", gate), "mode switch clears entry protection");
            entry.Arm("destination", gate, 46.071f, 14.018f, 46f, direction);
            Check(entry.TryWalkInGoal("destination", 46.071f, 0.43f, out float goal, out int walkDir)
                && walkDir == direction, "unfinished entry plans inward auto-walk");
            Check((goal - 46.071f) * direction >= 55f / 28f - 0.001f,
                "entry covers the native 55-map-pixel walk-in distance");
            float walked = 46.071f;
            for (int frame = 0; frame < 100 && (goal - walked) * direction > 0.001f; frame++)
                walked += direction * Math.Min(0.07f, Math.Abs(goal - walked));
            Check(entry.LeaveEntrance("destination", walked, 0.43f, false),
                "auto-walk clears entrance without manual inward movement");
            Check(!entry.Blocks("destination", gate), "immediate intentional return is now allowed");
            entry.Arm("destination", gate, 46.071f, 14.018f, 46f, direction);
            Check(!entry.TryWalkInGoal("destination", goal, 0.43f, out _, out _),
                "successful native walk-in does not receive a second forced walk");
            Check(!entry.TryWalkInGoal("other", 46f, 0.43f, out _, out _),
                "room change cancels obsolete walk-in goal");
            var finishingScript = new RoomTransferHandoff();
            finishingScript.Tick(true, true, false, true);
            entry.Arm("destination", gate, 46.071f, 14.018f, 46f, direction);
            Check(finishingScript.Active && entry.TryWalkInGoal("destination", 46.071f, 0.43f,
                out float earlyGoal, out int earlyDirection), "walk-in goal available before native event ends");
            Check(!entry.LeaveEntrance("destination", goal, 0.43f, true),
                "parallel walk-in keeps reverse-exit guard while script finishes");
            finishingScript.Tick(false, true, false, true);
            Check(finishingScript.Active, "walking early does not terminate native event");
            finishingScript.Tick(false, false, false, true);
            finishingScript.Tick(false, false, false, true);
            Check(entry.LeaveEntrance("destination", goal, 0.43f, finishingScript.Active),
                "parallel walk-in releases reverse gate once native event completes");
        }
        foreach (string gate in new[] { "ExitB_backyard", "ExitT_backyard" })
        {
            var vertical = new RoomArrivalGuard();
            vertical.ArmVertical("console", gate, 10f, 20f, 15f, 24f);
            Check(vertical.Blocks("console", gate), "vertical arrival cannot immediately reverse");
            Check(!vertical.LeaveVerticalEntrance("console", 12f, 20.8f, 13f, 21.8f, false),
                "standing on arrival platform within gate keeps protection");
            Check(!vertical.LeaveVerticalEntrance("console", 16f, 20f, 17f, 22f, true),
                "native script must finish before vertical entrance rearms");
            Check(!vertical.LeaveEntrance("console", 99f, 0.5f, false),
                "horizontal release cannot clear vertical guard");
            Check(!vertical.NeedsRestore("console", 0f) &&
                !vertical.TryWalkInGoal("console", 12f, 0.5f, out _, out _),
                "vertical arrival does not force horizontal movement or restoration");
            Check(!vertical.LeaveVerticalEntrance("console", 14f, 21f, 15.2f, 22f, false),
                "partial overlap cannot rearm gate");
            Check(!vertical.LeaveVerticalEntrance("other", 17f, 21f, 18f, 22f, false),
                "unrelated map cannot rearm vertical gate");
            Check(vertical.LeaveVerticalEntrance("console", 15.5f, 21f, 16f, 22f, false),
                "walking clear of vertical gate permits intentional return");
            Check(!vertical.Blocks("console", gate), "return through same vertical gate is available");
            vertical.ArmVertical("console", gate, 10f, 20f, 15f, 24f);
            Check(!vertical.LeaveVerticalEntrance("console", 12f, 17f, 13f, 19.5f, false, false),
                "native arrival jump cannot rearm gate before landing");
            if (gate.StartsWith("ExitB"))
                Check(!vertical.LeaveVerticalEntrance("console", 12f, 25f, 13f, 26f, false),
                    "falling back down through bottom arrival cannot rearm gate");
            Check(vertical.LeaveVerticalEntrance("console", 12f,
                gate.StartsWith("ExitB") ? 17f : 25f, 13f,
                gate.StartsWith("ExitB") ? 19.5f : 26f, false),
                "landing clear on room interior side rearms gate");
        }
        var nestedLoad = new RoomTransferHandoff();
        nestedLoad.Begin();
        Check(nestedLoad.Active, "transfer callback establishes ownership before knight Update");
        Check(!nestedLoad.Tick(false, true, false, true) && nestedLoad.Active,
            "nested alarm event cannot release arrival ownership");
        Check(!nestedLoad.Tick(false, false, false, true) && nestedLoad.Active,
            "arrival reader gap keeps reverse trigger protected");
        nestedLoad.Begin();
        Check(!nestedLoad.Tick(false, false, false, true), "fresh transfer restarts stability wait");
        Check(nestedLoad.Tick(false, false, false, true) && !nestedLoad.Active,
            "callback-started handoff finishes normally after native event");
        Console.WriteLine(checks + " checks passed");
    }
}
