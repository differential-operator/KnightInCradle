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
        public const float GrimmHoverOffX = 1.2f;     // 待机悬浮相对诺艾尔 X 偏移（格，需求 2026-09-27：后方 1.2 格）
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

        // ---- 渲染票据（本体 + 火球，身前层 PR1，与梦之盾同一套写法）----
        private static MeshDrawer _mesh;
        private static Material _mat;
        private static M2RenderTicket _ticket;
        private static MeshDrawer _fbMesh;
        private static Material _fbMat;
        private static M2RenderTicket _fbTicket;
        private static Map2d _map;
        private static float _fbAnimTime;

        /// <summary>剪辑时长（秒）= 帧数 / 帧率（同骑士的 `GetClipDuration`）。</summary>
        public static float ClipDuration(string clipName)
        {
            if (!Clips.TryGetValue(clipName, out ClipData clip) || clip.fps <= 0f)
            {
                return 0.1f;
            }
            return clip.frames.Length / clip.fps;
        }

        /// <summary>诺艾尔侧的敌人掩码（与 `CharmEffects` 里那套一致：EnemySelf/Enemy/AttackHitable + 常见层）。</summary>
        private static int EnemyMask()
        {
            int mask = LayerMask.GetMask("EnemySelf", "Enemy", "AttackHitable");
            foreach (string name in new[] { "Ignore Raycast", "Water", "TransparentFX", "Default" })
            {
                int layer = LayerMask.NameToLayer(name);
                if (layer >= 0)
                {
                    mask |= 1 << layer;
                }
            }
            return mask;
        }

        private static bool FaceLeft(PRNoel pr)
        {
            try
            {
                return CAim._XD(pr.getAimForCaster(), 1) >= 0;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static float HoverTargetX(PRNoel pr)
        {
            return pr.x + (FaceLeft(pr) ? GrimmHoverOffX : -GrimmHoverOffX);
        }

        /// <summary>每帧推进（挂进 `TickNoelCharmEffects`）：状态机 + 火球 + 票据维护。</summary>
        public static void Tick(PRNoel pr)
        {
            try
            {
                if (pr == null || CharmEffects.IsKnightMode ||
                    !CharmEffects.IsEquipped(CharmOwner.Noel, CharmEffects.GrimmId))
                {
                    if (SoundState == 1)
                    {
                        DashAudio.StopGrimmIdleLoop();
                    }
                    SoundState = 0;
                    Child = null;
                    Fireballs.Clear();
                    RespawnDelay = 0f;
                    SitTimer = 0f;
                    Release();
                    return;
                }
                LoadAssets();
                Ensure(pr);
                float sdt = Time.deltaTime;
                UpdateFireballs(pr, sdt);
                if (RespawnDelay > 0f)
                {
                    RespawnDelay -= sdt;
                    if (RespawnDelay > 0f)
                    {
                        if (SoundState == 1)
                        {
                            DashAudio.StopGrimmIdleLoop();
                        }
                        SoundState = 0;
                        return;
                    }
                }
                if (Child == null)
                {
                    Child = new GrimmChild
                    {
                        X = HoverTargetX(pr),
                        Y = pr.y + GrimmHoverOffY,
                        Phase = 0,
                        AnimTime = 0f,
                        AttackCd = 0f
                    };
                }
                GrimmChild g = Child;
                g.AnimTime += sdt;
                if (g.Phase != 5 && SoundState != 1)
                {
                    DashAudio.PlayGrimmIdleLoop();
                    SoundState = 1;
                }
                bool sitting = IsSitting(pr);
                if (sitting)
                {
                    SitTimer += sdt;
                    if (SitTimer >= GrimmSitSleepTime && g.Phase == 1)
                    {
                        g.Phase = 3;
                        g.AnimTime = 0f;
                        g.SleepStage = 0;
                        g.SleepX = g.X;
                        g.SleepGroundY = SleepGroundY(pr);
                    }
                }
                else
                {
                    SitTimer = 0f;
                    if (g.Phase == 3)
                    {
                        g.Phase = 4;
                        g.AnimTime = 0f;
                    }
                }
                if (g.Phase == 3)
                {
                    if (g.SleepStage == 0)
                    {
                        float dx = g.SleepX - g.X;
                        float dy = g.SleepGroundY - g.Y;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist > 0.02f)
                        {
                            float spd = Mathf.Min(4f, dist * 4f);
                            g.X += dx / dist * spd * sdt;
                            g.Y += dy / dist * spd * sdt;
                        }
                        else
                        {
                            g.X = g.SleepX;
                            g.Y = g.SleepGroundY;
                            g.SleepStage = 1;
                            g.AnimTime = 0f;
                        }
                    }
                    else if (g.SleepStage == 1 && g.AnimTime >= ClipDuration("GrimmSleep"))
                    {
                        g.SleepStage = 2;
                    }
                    return;
                }
                if (g.Phase == 4)
                {
                    if (g.AnimTime >= ClipDuration("GrimmWake"))
                    {
                        g.Phase = 1;
                        g.AnimTime = 0f;
                    }
                    return;
                }
                if (g.Phase == 2)
                {
                    if (g.AnimTime >= 0.4f)
                    {
                        Child = null; // 传送结束：下一帧在诺艾尔身边以"出现"重生
                    }
                    return;
                }
                if (g.Phase == 0)
                {
                    if (g.AnimTime >= ClipDuration("GrimmAppear"))
                    {
                        g.Phase = 1;
                        g.AnimTime = 0f;
                    }
                    return;
                }
                if (g.Phase == 5)
                {
                    if (!g.Fired && g.AnimTime >= GrimmShootFireTime)
                    {
                        g.Fired = true;
                        SpawnFireballs(pr, g);
                    }
                    if (g.AnimTime >= ClipDuration("GrimmIdle") * 1.5f + GrimmShootFireTime)
                    {
                        g.Phase = 1;
                        g.AnimTime = 0f;
                        g.AttackCd = GrimmAttackInterval;
                        g.Target = null;
                        DashAudio.PlayGrimmIdleLoop();
                        SoundState = 1;
                    }
                    return;
                }
                // Phase 1：跟随 + 索敌
                g.AttackCd -= sdt;
                float hoverX = HoverTargetX(pr);
                float hoverY = pr.y + GrimmHoverOffY;
                float vx, vy;
                SpeedOf(pr, out vx, out vy);
                bool moving = Mathf.Abs(vx) > 0.05f || !pr.hasFoot();
                float tdx = hoverX - g.X;
                float tdy = hoverY - g.Y;
                float tdist = Mathf.Sqrt(tdx * tdx + tdy * tdy);
                if (tdist > 0.05f)
                {
                    float spd;
                    if (moving)
                    {
                        float ownerSpd = Mathf.Abs(vx) * 60f;
                        spd = Mathf.Clamp(tdist * 2.5f, ownerSpd * GrimmFollowLag, ownerSpd * GrimmMaxSpeedRatio);
                    }
                    else
                    {
                        spd = Mathf.Min(2.5f, tdist * 2.5f);
                    }
                    g.X += tdx / tdist * spd * sdt;
                    g.Y += tdy / tdist * spd * sdt;
                }
                if (g.AttackCd <= 0f && FindTarget(pr, g))
                {
                    g.Phase = 5;
                    g.AnimTime = 0f;
                    g.Fired = false;
                    if (SoundState == 1)
                    {
                        DashAudio.StopGrimmIdleLoop();
                    }
                    DashAudio.PlayGrimmAttackYelp();
                    SoundState = 2;
                    return;
                }
                float gdx = g.X - pr.x;
                float gdy = g.Y - pr.y;
                if (gdx * gdx + gdy * gdy > GrimmTeleportRange * GrimmTeleportRange)
                {
                    g.Phase = 2;
                    g.AnimTime = 0f;
                }
            }
            catch (Exception)
            {
            }
        }

        private static bool IsSitting(PRNoel pr)
        {
            try
            {
                return pr.isBenchState();
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void SpeedOf(PRNoel pr, out float vx, out float vy)
        {
            vx = 0f;
            vy = 0f;
            try
            {
                // M2Mover 有 public 的 `vx` / `vy`（格/帧@60，与骑士侧那个 Vx 同单位）——
                // 之前走反射读 M2Phys.walk_xspeed 读不到，导致 vx 恒为 0、跟随速度被压到 2.5 格/秒。
                vx = pr.vx;
                vy = pr.vy;
            }
            catch (Exception)
            {
                vx = 0f;
                vy = 0f;
            }
        }

        private static float SleepGroundY(PRNoel pr)
        {
            try
            {
                return pr.mbottom + 0.5f;
            }
            catch (Exception)
            {
                return pr.y;
            }
        }

        /// <summary>索敌（诺艾尔侧：6 格内最近敌人）。</summary>
        private static bool FindTarget(PRNoel pr, GrimmChild g)
        {
            NelEnemy enemy = FindNearestEnemy(pr, g.X, g.Y);
            if (enemy == null)
            {
                return false;
            }
            g.Target = enemy;
            g.TargetIsWeed = false;
            return true;
        }

        private static NelEnemy FindNearestEnemy(PRNoel pr, float sx, float sy)
        {
            try
            {
                Map2d mp = pr.Mp;
                int mask = EnemyMask();
                if (mp == null || mask == 0)
                {
                    return null;
                }
                Vector2 center = mp.gameObject.transform.TransformPoint(
                    new Vector2(mp.pixel2ux(sx * mp.CLEN), mp.pixel2uy(sy * mp.CLEN)));
                Collider2D[] hits = Physics2D.OverlapCircleAll(center, GrimmSeekRange, mask);
                NelEnemy best = null;
                float bestD = float.MaxValue;
                for (int i = 0; i < hits.Length; i++)
                {
                    Collider2D c = hits[i];
                    if (c == null)
                    {
                        continue;
                    }
                    NelEnemy enemy = c.GetComponentInParent<NelEnemy>();
                    if (enemy == null || !enemy.is_alive)
                    {
                        continue;
                    }
                    float dx = enemy.x - sx;
                    float dy = enemy.y - sy;
                    float d = dx * dx + dy * dy;
                    if (d < bestD)
                    {
                        bestD = d;
                        best = enemy;
                    }
                }
                return best;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>攻击时刻：向目标发射三枚火球（正中 + ±30°），与骑士一致。</summary>
        private static void SpawnFireballs(PRNoel pr, GrimmChild g)
        {
            float tx;
            float ty;
            if (g.Target != null && g.Target.is_alive)
            {
                tx = g.Target.x;
                ty = g.Target.y;
            }
            else
            {
                tx = g.X + (FaceLeft(pr) ? -1f : 1f);
                ty = g.Y - 1f;
            }
            float baseAng = Mathf.Atan2(ty - g.Y, tx - g.X);
            float[] offs = { 0f, GrimmFireballSpread, -GrimmFireballSpread };
            for (int i = 0; i < offs.Length; i++)
            {
                float a = baseAng + offs[i];
                Fireballs.Add(new GrimmFireball
                {
                    X = g.X,
                    Y = g.Y,
                    DirX = Mathf.Cos(a),
                    DirY = Mathf.Sin(a),
                    Life = GrimmFireballLife
                });
            }
        }

        /// <summary>火球推进：直线飞行、穿敌（每敌一次 30 真伤）、5 秒后销毁。</summary>
        private static void UpdateFireballs(PRNoel pr, float sdt)
        {
            if (Fireballs.Count == 0)
            {
                return;
            }
            _fbAnimTime += sdt;
            Map2d mp = pr.Mp;
            int mask = EnemyMask();
            for (int i = Fireballs.Count - 1; i >= 0; i--)
            {
                GrimmFireball fb = Fireballs[i];
                fb.X += fb.DirX * GrimmFireballSpeed * sdt;
                fb.Y += fb.DirY * GrimmFireballSpeed * sdt;
                fb.Life -= sdt;
                bool remove = fb.Life <= 0f;
                if (!remove && mp != null && mask != 0)
                {
                    Vector2 center = mp.gameObject.transform.TransformPoint(
                        new Vector2(mp.pixel2ux(fb.X * mp.CLEN), mp.pixel2uy(fb.Y * mp.CLEN)));
                    Collider2D[] hits = Physics2D.OverlapCircleAll(center, GrimmFireballRadius, mask);
                    for (int j = 0; j < hits.Length; j++)
                    {
                        Collider2D c = hits[j];
                        if (c == null)
                        {
                            continue;
                        }
                        NelEnemy enemy = c.GetComponentInParent<NelEnemy>();
                        if (enemy == null || !enemy.is_alive || !fb.Hits.Add(enemy))
                        {
                            continue;
                        }
                        ApplyFireballDamage(pr, enemy);
                    }
                }
                if (remove)
                {
                    Fireballs.RemoveAt(i);
                }
            }
        }

        /// <summary>火球伤害：30 点真实伤害（与骑士一致）。</summary>
        private static void ApplyFireballDamage(PRNoel pr, NelEnemy enemy)
        {
            try
            {
                var atk = new NelAttackInfo();
                atk.fix_damage = true;
                atk.Caster = pr;
                atk.AttackFrom = pr;
                atk.hpdmg0 = GrimmFireballDamage;
                atk.hpdmg_current = GrimmFireballDamage;
                atk._apply_knockback_current = true;
                atk.CenterXy(enemy.x, enemy.y, 0f);
                enemy.applyDamage(atk, false);
            }
            catch (Exception)
            {
            }
        }

        private static void Ensure(PRNoel pr)
        {
            Map2d mp = pr.Mp;
            if (mp == null || _mesh != null && _map == mp)
            {
                return;
            }
            Release();
            _map = mp;
            _mesh = new MeshDrawer(null, 4, 6);
            _mesh.draw_gl_only = true;
            _mat = MTRX.newMtr(MTRX.ShaderGDT);
            _mat.EnableKeyword("NO_PIXELSNAP");
            _mesh.activate("noel_grimm", _mat, false, MTRX.ColWhite, null);
            _ticket = mp.MovRenderer.assignDrawable(M2Mover.DRAW_ORDER.PR1, null, PrepareMesh, _mesh, null, null);
            _fbMesh = new MeshDrawer(null, 4 * 8, 6 * 8);
            _fbMesh.draw_gl_only = true;
            _fbMat = MTRX.newMtr(MTRX.ShaderGDT);
            _fbMat.EnableKeyword("NO_PIXELSNAP");
            _fbMesh.activate("noel_grimm_fireball", _fbMat, false, MTRX.ColWhite, null);
            _fbTicket = mp.MovRenderer.assignDrawable(M2Mover.DRAW_ORDER.PR1, null, PrepareFireballMesh, _fbMesh, null, null);
        }

        private static void Release()
        {
            try
            {
                if (_ticket != null && _map != null && _map.MovRenderer != null)
                {
                    _map.MovRenderer.deassignDrawable(_ticket, -1);
                }
                if (_fbTicket != null && _map != null && _map.MovRenderer != null)
                {
                    _map.MovRenderer.deassignDrawable(_fbTicket, -1);
                }
            }
            catch (Exception)
            {
            }
            try
            {
                if (_mat != null)
                {
                    IN.DestroyOne(_mat);
                }
                if (_fbMat != null)
                {
                    IN.DestroyOne(_fbMat);
                }
            }
            catch (Exception)
            {
            }
            _ticket = null;
            _mesh = null;
            _mat = null;
            _fbTicket = null;
            _fbMesh = null;
            _fbMat = null;
            _map = null;
        }

        private static bool PrepareMesh(Camera Cam, M2RenderTicket Tk, bool need_redraw, int draw_id,
            out MeshDrawer MdOut, ref bool color_one_overwrite)
        {
            MdOut = null;
            Map2d mp = _map;
            if (mp == null || _mesh == null || draw_id != 0)
            {
                return false;
            }
            _mesh.clearSimple();
            _mesh.Identity();
            PRNoel pr = KnightInCradleBehaviour.GetPrPublic();
            GrimmChild g = Child;
            if (pr == null || g == null || CharmEffects.IsKnightMode ||
                !CharmEffects.IsEquipped(CharmOwner.Noel, CharmEffects.GrimmId))
            {
                MdOut = _mesh;
                return true;
            }
            string sprite;
            if (g.Phase == 0)
            {
                sprite = Frame("GrimmAppear", g.AnimTime, false);
            }
            else if (g.Phase == 2)
            {
                sprite = Frame("GrimmIdle", g.AnimTime, true);
            }
            else if (g.Phase == 3)
            {
                sprite = g.SleepStage == 1
                    ? Frame("GrimmSleep", g.AnimTime, false)
                    : Frame("GrimmFly", g.AnimTime, true);
                if (g.SleepStage == 2)
                {
                    sprite = Frame("GrimmSleep", 99f, false);
                }
            }
            else if (g.Phase == 4)
            {
                sprite = Frame("GrimmWake", g.AnimTime, false);
            }
            else
            {
                bool moving = Mathf.Abs(g.X - pr.x) > 0.35f || Mathf.Abs(g.Y - (pr.y + GrimmHoverOffY)) > 0.35f;
                sprite = Frame(g.Phase == 5 ? "GrimmIdle" : (moving ? "GrimmFly" : "GrimmIdle"), g.AnimTime, true);
            }
            Texture2D tex = Texture(sprite);
            if (tex == null)
            {
                MdOut = _mesh;
                return true;
            }
            float c = mp.CLEN;
            Tk.Matrix = mp.gameObject.transform.localToWorldMatrix *
                        Matrix4x4.Translate(new Vector3(mp.pixel2ux(pr.x * c), mp.pixel2uy(pr.y * c), 0f));
            float px = (g.X - pr.x) * c;
            float py = -(g.Y - pr.y) * c;
            float w = tex.width * GrimmScale;
            float h = tex.height * GrimmScale;
            _mesh.Col = MTRX.ColWhite;
            _mesh.initForImgAndTexture(tex);
            _mesh.uv_top = 0f;
            _mesh.uv_height = 1f;
            if (FaceLeft(pr))
            {
                _mesh.uv_left = 1f;
                _mesh.uv_width = -1f;
            }
            else
            {
                _mesh.uv_left = 0f;
                _mesh.uv_width = 1f;
            }
            Matrix4x4 saved = _mesh.getCurrentMatrix();
            _mesh.Translate(px * 0.015625f, py * 0.015625f, true);
            _mesh.Rect(-w * 0.5f, -h * 0.5f, w, h, false);
            _mesh.setCurrentMatrix(saved, false);
            MdOut = _mesh;
            return true;
        }

        private static bool PrepareFireballMesh(Camera Cam, M2RenderTicket Tk, bool need_redraw, int draw_id,
            out MeshDrawer MdOut, ref bool color_one_overwrite)
        {
            MdOut = null;
            Map2d mp = _map;
            if (mp == null || _fbMesh == null || draw_id != 0)
            {
                return false;
            }
            _fbMesh.clearSimple();
            _fbMesh.Identity();
            PRNoel pr = KnightInCradleBehaviour.GetPrPublic();
            if (pr == null || Fireballs.Count == 0 || CharmEffects.IsKnightMode)
            {
                MdOut = _fbMesh;
                return true;
            }
            string sprite = Frame("GrimmFireball", _fbAnimTime, true);
            Texture2D tex = Texture(sprite);
            if (tex == null)
            {
                MdOut = _fbMesh;
                return true;
            }
            float c = mp.CLEN;
            Tk.Matrix = mp.gameObject.transform.localToWorldMatrix *
                        Matrix4x4.Translate(new Vector3(mp.pixel2ux(pr.x * c), mp.pixel2uy(pr.y * c), 0f));
            _fbMesh.initForImgAndTexture(tex);
            _fbMesh.uv_top = 0f;
            _fbMesh.uv_height = 1f;
            _fbMesh.uv_left = 0f;
            _fbMesh.uv_width = 1f;
            float w = tex.width * GrimmFireballScale;
            float h = tex.height * GrimmFireballScale;
            for (int i = 0; i < Fireballs.Count; i++)
            {
                GrimmFireball fb = Fireballs[i];
                float px = (fb.X - pr.x) * c;
                float py = -(fb.Y - pr.y) * c;
                _fbMesh.Col = MTRX.ColWhite;
                Matrix4x4 saved = _fbMesh.getCurrentMatrix();
                _fbMesh.Translate(px * 0.015625f, py * 0.015625f, true);
                _fbMesh.Rect(-w * 0.5f, -h * 0.5f, w, h, false);
                _fbMesh.setCurrentMatrix(saved, false);
            }
            MdOut = _fbMesh;
            return true;
        }
    }
}
