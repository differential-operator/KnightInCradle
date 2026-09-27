using System;
using System.Collections.Generic;
using System.IO;
using m2d;
using nel;
using UnityEngine;

namespace KnightInCradle.Grimm
{
    /// <summary>
    /// 格林之子（护符39）的**宿主接口**：把"与角色无关"的共享实现
    /// （状态机 / 火球 / 渲染 / 素材）需要的角色侧信息全部抽象出来，
    /// 这样小骑士与诺艾尔**共用同一份实现**（不再各写一份、各偏一点）。
    ///
    /// 约定：
    /// - 坐标一律"格"，Y 向下为正（AIC 网格坐标，与 `M2Mover.x/y` 一致）；
    /// - 速度单位"格/帧@60"（与骑士侧原来的 `Vx/Vy` 同单位）；
    /// - 伤害由宿主自己落地（诺艾尔侧走真伤 + 通用目标通道，骑士侧走它原有那套）。
    /// </summary>
    public interface IGrimmHost
    {
        /// <summary>护符39 是否装备在本地角色身上（且处于该角色的模式）。</summary>
        bool GrimmEquipped { get; }

        float X { get; }
        float Y { get; }
        /// <summary>速度（格/帧@60）。</summary>
        float Vx { get; }
        float Vy { get; }
        /// <summary>是否面朝左（决定小格林悬浮在左后方还是右后方）。</summary>
        bool FacingLeft { get; }
        /// <summary>是否正坐在长椅上（坐椅超时会入睡）。</summary>
        bool Sitting { get; }
        /// <summary>脚底 Y（睡眠落地用）。</summary>
        float FootY { get; }
        bool Grounded { get; }
        Map2d Map { get; }
        /// <summary>敌人掩码（索敌用）。</summary>
        int EnemyMask { get; }
        /// <summary>宿主能不能"停"（死亡/过图等）：false 时共享实现会收尾。</summary>
        bool Active { get; }
        /// <summary>宿主版本号：换图/换模式后变化时共享实现会重建票据。返回 0 表示宿主不关心。</summary>
        int MapRevision { get; }

        /// <summary>把火球伤害落到指定魔物身上（各角色自己的伤害管线）。</summary>
        void ApplyGrimmFireballDamage(NelEnemy enemy, int damage);
    }

    /// <summary>
    /// 格林之子的素材与剪辑表（两个角色共用一份，按需加载一次）。
    /// 帧名与骑士那份完全一致（`Grimmbat_*` / `grimm_fireball*`）。
    /// </summary>
    public static class GrimmAssets
    {
        public const string ClipAppear = "GrimmAppear";
        public const string ClipIdle = "GrimmIdle";
        public const string ClipFly = "GrimmFly";
        public const string ClipSleep = "GrimmSleep";
        public const string ClipWake = "GrimmWake";
        public const string ClipFireball = "GrimmFireball";

        private sealed class ClipData
        {
            public float fps;
            public string[] frames;
        }

        private static readonly Dictionary<string, Texture2D> Tex = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, ClipData> Clips = new Dictionary<string, ClipData>();
        private static bool _loaded;

        public static bool Loaded => _loaded;

        public static void Load()
        {
            if (_loaded)
            {
                return;
            }
            _loaded = true;
            try
            {
                string dir = Path.Combine(BepInEx.Paths.PluginPath, "KnightInCradle",
                    "assets", "hk", "sheets", "grimm");
                if (!Directory.Exists(dir))
                {
                    return;
                }
                var wanted = new Dictionary<string, string[]>
                {
                    [ClipAppear] = new[] { "Grimmbat_turn0000", "Grimmbat_turn0001" },
                    [ClipIdle] = new[]
                    {
                        "Grimmbat_idle0000", "Grimmbat_idle0001", "Grimmbat_idle0002",
                        "Grimmbat_idle0003", "Grimmbat_idle0004", "Grimmbat_idle0005"
                    },
                    [ClipFly] = new[]
                    {
                        "Grimmbat_fly_full0000", "Grimmbat_fly_full0001", "Grimmbat_fly_full0002",
                        "Grimmbat_fly_full0003", "Grimmbat_fly_full0004", "Grimmbat_fly_full0005"
                    },
                    [ClipSleep] = new[] { "Grimmbat_sleep0000", "Grimmbat_sleep0001", "Grimmbat_sleep0002" },
                    [ClipWake] = new[] { "Grimmbat_sleep0002", "Grimmbat_sleep0001", "Grimmbat_sleep0000" },
                    [ClipFireball] = new[]
                    {
                        "grimm_fireball0000", "grimm_fireball0001", "grimm_fireball0002",
                        "grimm_fireball0003", "grimm_fireball0004", "grimm_fireball0005",
                        "grimm_fireball0006", "grimm_fireball0007"
                    }
                };
                var fps = new Dictionary<string, float>
                {
                    [ClipAppear] = 20f,
                    [ClipIdle] = 12f,
                    [ClipFly] = 12f,
                    [ClipSleep] = 12f,
                    [ClipWake] = 12f,
                    [ClipFireball] = 16f
                };
                foreach (KeyValuePair<string, string[]> kv in wanted)
                {
                    foreach (string n in kv.Value)
                    {
                        if (Tex.ContainsKey(n))
                        {
                            continue;
                        }
                        string png = Path.Combine(dir, n + ".png");
                        if (!File.Exists(png))
                        {
                            continue;
                        }
                        var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                        if (!ImageConversion.LoadImage(tex, File.ReadAllBytes(png)))
                        {
                            continue;
                        }
                        tex.filterMode = FilterMode.Point;
                        tex.wrapMode = TextureWrapMode.Clamp;
                        Tex[n] = tex;
                    }
                }
                foreach (KeyValuePair<string, string[]> kv in wanted)
                {
                    var frames = new List<string>();
                    foreach (string n in kv.Value)
                    {
                        if (Tex.ContainsKey(n))
                        {
                            frames.Add(n);
                        }
                    }
                    if (frames.Count > 0 && fps.ContainsKey(kv.Key))
                    {
                        Clips[kv.Key] = new ClipData { fps = fps[kv.Key], frames = frames.ToArray() };
                    }
                }
            }
            catch (Exception ex)
            {
                KnightInCradlePlugin.PluginLog?.LogWarning("[KIC][格林之子] 素材读取失败：" + ex.Message);
            }
        }

        public static Texture2D Texture(string name)
        {
            return name != null && Tex.TryGetValue(name, out Texture2D tex) ? tex : null;
        }

        public static string Frame(string clip, float t, bool loop)
        {
            if (!Clips.TryGetValue(clip, out ClipData c) || c.frames.Length == 0)
            {
                return null;
            }
            int idx = loop
                ? (int)(t * c.fps) % c.frames.Length
                : Mathf.Clamp((int)(t * c.fps), 0, c.frames.Length - 1);
            return c.frames[idx];
        }

        public static float Duration(string clip)
        {
            if (!Clips.TryGetValue(clip, out ClipData c) || c.fps <= 0f)
            {
                return 0.1f;
            }
            return c.frames.Length / c.fps;
        }
    }
}
