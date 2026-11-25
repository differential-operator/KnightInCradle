using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using m2d;
using nel;

namespace KnightInCradle
{
    internal static class MapRevealDebug
    {
        private static readonly FieldInfo Visited = AccessTools.Field(typeof(WholeMapItem), "Avisitted");
        private static readonly FieldInfo Completion = AccessTools.Field(typeof(WholeMapItem), "completion_ratio_");
        private static readonly FieldInfo Prepared = AccessTools.Field(typeof(WholeMapItem), "prepared_all");

        internal static WholeMapItem Current => KnightInCradleBehaviour.GetPrPublic()?.Mp != null
            ? (M2DBase.Instance as NelM2DBase)?.WM?.CurWM : null;
        private static WholeMapItem pending;
        private static Queue<Map2d> remaining;
        private static string Flag(WholeMapItem map) => "KIC_MAP_REVEAL_ICONS_" + map.text_key;
        private static string FixedFlag(WholeMapItem map) => "KIC_MAP_REVEAL_ICONS_V5_" + map.text_key;
        private static WholeMapItem failedRepair;
        internal static bool Enabled => Current != null && COOK.getSF(Flag(Current)) != 0;
        internal static bool Busy => pending != null;
        internal static bool Ready => Current != null && !Enabled && !Busy;

        internal static void Tick()
        {
            if (pending == null)
            {
                var current = Current;
                if (current == null || current == failedRepair || COOK.getSF(Flag(current)) == 0 || COOK.getSF(FixedFlag(current)) != 0) return;
                QueueIcons(current);
            }
            if (KnightInCradleBehaviour.GetPrPublic()?.Mp == null)
            {
                pending = null;
                remaining = null;
                return;
            }
            try
            {
                if (remaining.Count > 0) RevealIcons(pending, remaining.Dequeue());
                if (remaining.Count == 0)
                {
                    COOK.setSF(Flag(pending), 1);
                    COOK.setSF(FixedFlag(pending), 1);
                    KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][地图全亮] 宝箱、战斗及椅子标记完成：" + pending.text_key);
                    pending = null;
                    remaining = null;
                }
            }
            catch (Exception ex)
            {
                KnightInCradlePlugin.PluginLog?.LogError("[KIC][地图全亮] 标记失败，可重新开启：" + ex);
                failedRepair = pending;
                pending = null;
                remaining = null;
            }
        }

        private static void RevealIcons(WholeMapItem world, Map2d room)
        {
            bool discardLayers = !room.opened;
            try
            {
                room.prepared = true;
                if (!room.loaded) room.load(false); // Build data layers without opening/initializing the room.
                int treasures = 0, battles = 0, benches = 0;
                for (int l = 0; l < room.count_layers; l++)
                {
                    var layer = room.getLayer(l);
                    // Benches are map chips, not label points. Match NelChipBench.fineIcon exactly.
                    for (int c = 0; c < layer.count_chips; c++)
                    {
                        if (layer.getChipByIndex(c) is NelChipBench bench)
                        {
                            new WMIconCreator(bench, WMIcon.TYPE.BENCH, null,
                                room.key + "_" + layer.name + "_bench", 4).notice();
                            benches++;
                        }
                    }
                    var points = layer.remakeLabelPoint(!room.opened);
                    for (int i = 0; i < points.Length; i++)
                    {
                        var point = points.Get(i);
                        if (point is M2LpSummon battle)
                        {
                            new WMIconCreator(point, WMIcon.TYPE.ENEMY, battle.cleared_sf_key).notice();
                            battles++;
                        }
                        else if (point is M2LpPuzzTreasure chest)
                        {
                            // Unvisited chests have not run initAction, so use the map's native cached key.
                            string key = null;
                            var caches = AccessTools.Field(typeof(WholeMapItem), "ATreasure").GetValue(world)
                                as List<WholeMapItem.WMTreasureBoxCache>;
                            if (caches != null)
                                foreach (var cache in caches)
                                    if (cache.map_key == room.key && cache.box_sf_key.EndsWith(point.unique_key, StringComparison.Ordinal))
                                    { key = cache.box_sf_key; break; }
                            key = key ?? M2LpPuzzTreasure.getBoxSfKey(point, new XX.META(point.comment).GetI("flush", 0, 0) != 0);
                            new WMIconCreator(point, WMIcon.TYPE.TREASURE, key).notice();
                            treasures++;
                        }
                    }
                }
                KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][地图标记] " + room.key + " 图层=" + room.count_layers + " 宝箱=" + treasures + " 战斗=" + battles + " 椅子=" + benches);
            }
            finally
            {
                // CLOSED parsing omits normal room setup. Never let native transfer reuse it.
                // Icons contain only coordinates/keys and survive discarding the temporary layers.
                if (discardLayers) room.close(false, true);
            }
        }

        private static void QueueIcons(WholeMapItem map)
        {
            map.PrepareMapData();
            remaining = new Queue<Map2d>();
            foreach (var room in map.getWMItemList())
                if (room.SrcMap != null) remaining.Enqueue(room.SrcMap);
            pending = map;
        }

        internal static void UnlockCurrent()
        {
            var map = Current;
            if (map == null || !Ready) return;
            try
            {
                map.PrepareMapData();
                int added = Reveal(map);
                QueueIcons(map);
                KnightInCradlePlugin.PluginLog?.LogInfo("[KIC][地图全亮] " + map.text_key +
                    " 新增探索房间=" + added + " 探索度=" + map.getCompletionRatio());
            }
            catch (Exception ex)
            {
                KnightInCradlePlugin.PluginLog?.LogError("[KIC][地图全亮] 解锁失败：" + ex);
            }
        }

        // Update both native drawing flags and the list written by WholeMapItem.createBinary.
        // Do not call initS for other rooms: it also changes the player's current map position.
        internal static int Reveal(WholeMapItem map)
        {
            if (Visited == null || Completion == null || Prepared == null)
                throw new MissingFieldException("WholeMapItem exploration fields");
            var rooms = map.getWMItemList();
            var visited = Visited.GetValue(map) as List<Map2d>;
            if (rooms == null || visited == null)
                throw new InvalidOperationException("Current region map is not prepared");
            int added = 0;
            foreach (var room in rooms)
            {
                if (room.SrcMap == null) continue;
                if (!visited.Contains(room.SrcMap))
                {
                    visited.Add(room.SrcMap);
                    added++;
                }
                room.visitted = true;
            }
            Completion.SetValue(map, -1f);
            // Let the native map UI prepare newly visited rooms incrementally.
            if (added > 0) Prepared.SetValue(map, 0);
            return added;
        }
    }
}
