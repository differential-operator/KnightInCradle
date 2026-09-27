using System;
using System.Collections.Generic;
using System.IO;
using m2d;
using nel;
using UnityEngine;
using XX;

namespace KnightInCradle.CharmUi
{
    /// <summary>
    /// 护符39 格林之子（诺艾尔侧）——**照搬小骑士那套完整实现**：
    /// 表现/数值与骑士完全一致（出现 → 待机悬浮 → 索敌飞行 → 喷火球 → 睡眠/苏醒 → 传送、
    /// 待机音效循环、火球与接触伤害 30 点等，全部沿用 `KnightEntity` 的常量与状态机）。
    ///
    /// 本文件先落地"数据 + 素材 + 剪辑"这一层（字段 / GrimmChild / GrimmFireball /
    /// LoadGrimmAssets），状态机与渲染在下一步按 `_grimm_work\*.txt` 里抽取的骑士代码逐块搬进来。
    /// </summary>
    internal static class NoelGrimm
    {
        // ---- 常量（与 KnightEntity 完全一致）----
        public const float GrimmSitSleepTime = 2f;    // 坐椅超过该秒数后入睡
        public const float GrimmTeleportRange = 6f;   // 离诺艾尔超过该距离触发传送
        public const float GrimmScale = 0.22f;        // 渲染缩放（相对贴图原始像素）
        public const float GrimmHoverOffX = 0.8f;     // 待机悬浮相对诺艾尔 X 偏移（格）
        public const float GrimmHoverOffY = -1.1f;    // 待机悬浮相对诺艾尔 Y 偏移（格，向上为负）
        public const float GrimmFollowLag = 0.9f;     // 正常跟随速度比例（略低于诺艾尔）
        public const float GrimmMaxSpeedRatio = 2f;   // 最大速度相对诺艾尔速度的倍数（追赶用）
        public const float GrimmRespawnDelay = 3f;    // 过图后重生等待（秒，同编织者之歌）
        public const float GrimmSeekRange = 6f;       // 攻击范围半径（格，以自身为中心）
        public const float GrimmAttackInterval = 2f;  // 攻击间隔（秒）
        public const float GrimmFireballSpeed = 16f;  // 火球速度（格/秒）
        public const float GrimmFireballRadius = 0.25f; // 火球半径（格）
        public const float GrimmFireballLife = 5f;    // 火球寿命（秒）
        public const int GrimmFireballDamage = 30;    // 火球伤害
        public const float GrimmFireballSpread = 30f * Mathf.Deg2Rad; // 副火球偏转角（±30°）
        public const float GrimmFireballScale = 0.3f; // 火球渲染缩放
        public const float GrimmShootFireTime = 4f / 12f; // 第 0004 帧时刻（12fps）

        /// <summary>跟随中的小格林（字段与骑士的 `GrimmChild` 一一对应）。</summary>
        internal sealed class GrimmChild
        {
            public float X, Y;         // 格坐标
            public int Phase;          // 0=出现 1=活跃 2=传送 3=睡眠 4=苏醒
            public float AnimTime;     // 动画时间
            public float AttackCd;     // 攻击冷却
            public int SleepStage;     // 0=降落中 1=播睡眠动画 2=保持末帧
            public float SleepX;       // 落点 X
            public float SleepGroundY; // 落点 Y（脚底贴地）
            public M2Attackable Target; // 攻击目标（敌人或已仇恨的远端玩家）
            public bool TargetIsWeed;  // 目标是否为魔力草
            public float WeedTargetX, WeedTargetY; // 魔力草目标位置
            public bool Fired;         // 本次攻击是否已发射火球
        }

        /// <summary>小格林火球（字段与骑士的 `GrimmFireball` 一一对应）。</summary>
        internal sealed class GrimmFireball
        {
            public float X, Y;
            public float DirX, DirY;
            public float Life;
            public readonly HashSet<object> Hits = new HashSet<object>(); // 敌人/魔力草去重
        }

        internal static GrimmChild Child;
        internal static readonly List<GrimmFireball> Fireballs = new List<GrimmFireball>();
        internal static float RespawnDelay;
        internal static float SitTimer;
        internal static int SoundState;  // 0=静音 1=待机循环 2=攻击音

        private static readonly Dictionary<string, Texture2D> Tex = new Dictionary<string, Texture2D>();
        private static readonly Dictionary<string, ClipData> Clips = new Dictionary<string, ClipData>();
        private static bool _assetsLoaded;

        private sealed class ClipData
        {
            public float fps;
            public int wrapMode;
            public int loopStart;
            public string[] frames;
        }

        /// <summary>素材（`assets/hk/sheets/grimm/Grimmbat_*` + `grimm_fireball*`）与剪辑表，照抄骑士那份。</summary>
        public static void LoadAssets()
        {
            if (_assetsLoaded)
            {
                return;
            }
            _assetsLoaded = true;
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
                    ["GrimmAppear"] = new[] { "Grimmbat_turn0000", "Grimmbat_turn0001" },
                    ["GrimmIdle"] = new[]
                    {
                        "Grimmbat_idle0000", "Grimmbat_idle0001", "Grimmbat_idle0002",
                        "Grimmbat_idle0003", "Grimmbat_idle0004", "Grimmbat_idle0005"
                    },
                    ["GrimmFly"] = new[]
                    {
                        "Grimmbat_fly_full0000", "Grimmbat_fly_full0001", "Grimmbat_fly_full0002",
                        "Grimmbat_fly_full0003", "Grimmbat_fly_full0004", "Grimmbat_fly_full0005"
                    },
                    ["GrimmSleep"] = new[]
                    {
                        "Grimmbat_sleep0000", "Grimmbat_sleep0001", "Grimmbat_sleep0002"
                    },
                    ["GrimmWake"] = new[]
                    {
                        "Grimmbat_sleep0002", "Grimmbat_sleep0001", "Grimmbat_sleep0000"
                    },
                    ["GrimmFireball"] = new[]
                    {
                        "grimm_fireball0000", "grimm_fireball0001", "grimm_fireball0002",
                        "grimm_fireball0003", "grimm_fireball0004", "grimm_fireball0005",
                        "grimm_fireball0006", "grimm_fireball0007"
                    }
                };
                var fps = new Dictionary<string, float>
                {
                    ["GrimmAppear"] = 20f,
                    ["GrimmIdle"] = 12f,
                    ["GrimmFly"] = 12f,
                    ["GrimmSleep"] = 12f,
                    ["GrimmWake"] = 12f,
                    ["GrimmFireball"] = 16f
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
                    if (frames.Count == 0 || !fps.ContainsKey(kv.Key))
                    {
                        continue;
                    }
                    Clips[kv.Key] = new ClipData
                    {
                        fps = fps[kv.Key],
                        wrapMode = 0,
                        loopStart = 0,
                        frames = frames.ToArray()
                    };
                }
                DashAudio.LoadGrimmSounds();
            }
            catch (Exception ex)
            {
                KnightInCradlePlugin.PluginLog?.LogWarning("[KIC][格林之子] 素材读取失败：" + ex.Message);
            }
        }

        /// <summary>按剪辑名与时间取当前帧贴图名（同骑士的 `GrimmFrame`）。</summary>
        public static string Frame(string clipName, float t, bool loop)
        {
            try
            {
                if (!Clips.TryGetValue(clipName, out ClipData clip) || clip.frames.Length == 0)
                {
                    return null;
                }
                int count = clip.frames.Length;
                int idx = (int)(t * clip.fps);
                if (loop)
                {
                    if (idx < 0)
                    {
                        idx = 0;
                    }
                    idx %= count;
                }
                else
                {
                    if (idx < 0)
                    {
                        idx = 0;
                    }
                    if (idx >= count)
                    {
                        idx = count - 1;
                    }
                }
                return clip.frames[idx];
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static Texture2D Texture(string name)
        {
            return name != null && Tex.TryGetValue(name, out Texture2D tex) ? tex : null;
        }
    }
}
