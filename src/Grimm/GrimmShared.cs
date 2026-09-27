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

        /// <summary>宿主能不能"停"（死亡/过图等）：false 时共享实现会收尾。</summary>
        bool Active { get; }

        float X { get; }
        float Y { get; }
        /// <summary>速度（格/帧@60）。</summary>
        float Vx { get; }
        float Vy { get; }
        /// <summary>是否面朝左（决定小格林悬浮在左后方还是右后方）。</summary>
        bool FacingLeft { get; }
        /// <summary>是否正坐在长椅上（坐椅超时会入睡）。</summary>
        bool Sitting { get; }
        /// <summary>坐椅睡眠落地基准 Y（控制器再减去睡眠帧的半高，使贴图底边贴地）。</summary>
        float SleepGroundBaseY { get; }
        bool Grounded { get; }
        Map2d Map { get; }
        /// <summary>宿主版本号：换图/换模式后变化时共享实现会重建票据。返回 0 表示宿主不关心。</summary>
        int MapRevision { get; }

        /// <summary>索敌用的碰撞体掩码（火球命中判定同用）。</summary>
        int EnemyMask { get; }

        /// <summary>待机悬浮相对宿主 X / Y 的偏移（格，Y 向上为负）。</summary>
        float HoverOffX { get; }
        float HoverOffY { get; }

        /// <summary>
        /// 索敌：**由宿主全权决定目标层次**。
        /// 小骑士：PvP 代理目标 &gt; 魔物 &gt; 爱丽丝 &gt; 魔力草；诺艾尔：最近的魔物。
        /// 命中时给出攻击点与 token（token 会原样带回两个火球回调）。
        /// </summary>
        bool TryAcquireTarget(float x, float y, float range, out float tx, out float ty, out object token);

        /// <summary>
        /// 火球碰到一个碰撞体（已按 <see cref="EnemyMask"/> 过滤）。
        /// 宿主自己完成命中判定与伤害落地；<paramref name="hits"/> 用于"同一目标只打一次"的去重。
        /// </summary>
        void OnGrimmFireballCollider(object token, Collider2D col, int damage, HashSet<object> hits);

        /// <summary>
        /// 火球每帧推进后的宿主附加处理（骑士：命中爱丽丝即消失、破坏魔力草）。
        /// 置 <paramref name="destroy"/> 为 true 表示宿主已判定命中并请求销毁火球。
        /// </summary>
        void OnGrimmFireballTick(object token, float x, float y, float radius, HashSet<object> hits,
            ref bool destroy);
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
        public const string ClipTeleport = "GrimmTeleport";
        public const string ClipShoot = "GrimmShoot";
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
                    [ClipTeleport] = new[]
                    {
                        "Grimmbat_teleport0000", "Grimmbat_teleport0001", "Grimmbat_teleport0002",
                        "Grimmbat_teleport0003", "Grimmbat_teleport0004", "Grimmbat_teleport0005",
                        "Grimmbat_teleport0006", "Grimmbat_teleport0007"
                    },
                    [ClipShoot] = new[]
                    {
                        "Grimmbat_shoot0000", "Grimmbat_shoot0001", "Grimmbat_shoot0002",
                        "Grimmbat_shoot0003", "Grimmbat_shoot0004", "Grimmbat_shoot0005"
                    },
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
                    [ClipTeleport] = 20f,
                    [ClipShoot] = 12f,
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

        /// <summary>剪辑最后一帧的贴图（睡眠贴地计算用）。</summary>
        public static Texture2D LastFrameTexture(string clip)
        {
            if (!Clips.TryGetValue(clip, out ClipData c) || c.frames.Length == 0)
            {
                return null;
            }
            return Texture(c.frames[c.frames.Length - 1]);
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
