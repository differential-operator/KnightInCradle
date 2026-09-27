using System;
using System.Collections.Generic;
using m2d;
using nel;
using UnityEngine;
using XX;

namespace KnightInCradle.Grimm
{
    /// <summary>
    /// 格林之子（护符39）的**与角色无关**的共享实现：状态机 + 火球 + 渲染。
    /// 数值/表现与小骑士那份完全一致（常量见下），角色侧只提供 <see cref="IGrimmHost"/>。
    /// 小骑士与诺艾尔共用这一个类，避免两份实现各自走偏。
    /// </summary>
    public sealed class GrimmController
    {
        // ---- 常量（与 KnightEntity 里那份逐条一致）----
        public const float SitSleepTime = 2f;
        public const float TeleportRange = 6f;
        public const float Scale = 0.22f;
        public const float HoverOffX = 1.2f;          // 诺艾尔口径：后方 1.2 格（骑士侧由宿主覆盖）
        public const float HoverOffY = -1.1f;
        public const float FollowLag = 0.9f;
        public const float MaxSpeedRatio = 2f;
        public const float RespawnDelayTime = 3f;
        public const float SeekRange = 6f;
        public const float AttackInterval = 2f;
        public const float FireballSpeed = 16f;
        public const float FireballRadius = 0.25f;
        public const float FireballLife = 5f;
        public const int FireballDamage = 30;
        public const float FireballSpread = 30f * Mathf.Deg2Rad;
        public const float FireballScale = 0.3f;
        public const float ShootFireTime = 4f / 12f;

        public sealed class Child
        {
            public float X, Y;
            public int Phase;          // 0=出现 1=活跃 2=传送 3=睡眠 4=苏醒 5=攻击
            public float AnimTime;
            public float AttackCd;
            public int SleepStage;
            public float SleepX;
            public float SleepGroundY;
            public NelEnemy Target;
            public bool Fired;
        }

        public sealed class Fireball
        {
            public float X, Y;
            public float DirX, DirY;
            public float Life;
            public readonly HashSet<object> Hits = new HashSet<object>();
        }

        private readonly IGrimmHost _host;
        private Child _child;
        private readonly List<Fireball> _fireballs = new List<Fireball>();
        private float _respawnDelay;
        private float _sitTimer;
        private int _soundState;
        private int _mapRevision = -1;
        private float _fbAnimTime;

        private MeshDrawer _mesh;
        private Material _mat;
        private M2RenderTicket _ticket;
        private MeshDrawer _fbMesh;
        private Material _fbMat;
        private M2RenderTicket _fbTicket;
        private Map2d _map;

        public GrimmController(IGrimmHost host)
        {
            _host = host;
        }

        /// <summary>宿主还没准备好（角色死亡/过图中）时返回 false，共享实现会收尾。</summary>
        private bool Ready => _host != null && _host.Active && _host.GrimmEquipped;

        public void Tick(float dt)
        {
            try
            {
                GrimmAssets.Load();
                if (!Ready)
                {
                    StopIdleLoop();
                    _child = null;
                    _fireballs.Clear();
                    _respawnDelay = 0f;
                    _sitTimer = 0f;
                    Release();
                    return;
                }
                Ensure();
                UpdateFireballs(dt);
                if (_respawnDelay > 0f)
                {
                    _respawnDelay -= dt;
                    if (_respawnDelay > 0f)
                    {
                        StopIdleLoop();
                        return;
                    }
                }
                if (_child == null)
                {
                    _child = new Child
                    {
                        X = HoverX(),
                        Y = _host.Y + HoverOffY,
                        Phase = 0,
                        AnimTime = 0f
                    };
                }
                Child g = _child;
                g.AnimTime += dt;
                if (g.Phase != 5 && _soundState != 1)
                {
                    DashAudio.PlayGrimmIdleLoop();
                    _soundState = 1;
                }
                if (_host.Sitting)
                {
                    _sitTimer += dt;
                    if (_sitTimer >= SitSleepTime && g.Phase == 1)
                    {
                        g.Phase = 3;
                        g.AnimTime = 0f;
                        g.SleepStage = 0;
                        g.SleepX = g.X;
                        g.SleepGroundY = SleepGroundY();
                    }
                }
                else
                {
                    _sitTimer = 0f;
                    if (g.Phase == 3)
                    {
                        g.Phase = 4;
                        g.AnimTime = 0f;
                    }
                }
                switch (g.Phase)
                {
                    case 3:
                        if (g.SleepStage == 0)
                        {
                            float dx = g.SleepX - g.X;
                            float dy = g.SleepGroundY - g.Y;
                            float dist = Mathf.Sqrt(dx * dx + dy * dy);
                            if (dist > 0.02f)
                            {
                                float spd = Mathf.Min(4f, dist * 4f);
                                g.X += dx / dist * spd * dt;
                                g.Y += dy / dist * spd * dt;
                            }
                            else
                            {
                                g.X = g.SleepX;
                                g.Y = g.SleepGroundY;
                                g.SleepStage = 1;
                                g.AnimTime = 0f;
                            }
                        }
                        else if (g.SleepStage == 1 && g.AnimTime >= GrimmAssets.Duration(GrimmAssets.ClipSleep))
                        {
                            g.SleepStage = 2;
                        }
                        return;
                    case 4:
                        if (g.AnimTime >= GrimmAssets.Duration(GrimmAssets.ClipWake))
                        {
                            g.Phase = 1;
                            g.AnimTime = 0f;
                        }
                        return;
                    case 2:
                        if (g.AnimTime >= 0.4f)
                        {
                            _child = null;
                        }
                        return;
                    case 0:
                        if (g.AnimTime >= GrimmAssets.Duration(GrimmAssets.ClipAppear))
                        {
                            g.Phase = 1;
                            g.AnimTime = 0f;
                        }
                        return;
                    case 5:
                        if (!g.Fired && g.AnimTime >= ShootFireTime)
                        {
                            g.Fired = true;
                            SpawnFireballs(g);
                        }
                        if (g.AnimTime >= GrimmAssets.Duration(GrimmAssets.ClipIdle) * 1.5f + ShootFireTime)
                        {
                            g.Phase = 1;
                            g.AnimTime = 0f;
                            g.AttackCd = AttackInterval;
                            g.Target = null;
                            DashAudio.PlayGrimmIdleLoop();
                            _soundState = 1;
                        }
                        return;
                }
                // Phase 1：跟随 + 索敌
                g.AttackCd -= dt;
                float hoverX = HoverX();
                float hoverY = _host.Y + HoverOffY;
                bool moving = Mathf.Abs(_host.Vx) > 0.05f || !_host.Grounded;
                float tdx = hoverX - g.X;
                float tdy = hoverY - g.Y;
                float tdist = Mathf.Sqrt(tdx * tdx + tdy * tdy);
                if (tdist > 0.05f)
                {
                    float spd;
                    if (moving)
                    {
                        float ownerSpd = Mathf.Abs(_host.Vx) * 60f;
                        spd = Mathf.Clamp(tdist * 2.5f, ownerSpd * FollowLag, ownerSpd * MaxSpeedRatio);
                    }
                    else
                    {
                        spd = Mathf.Min(2.5f, tdist * 2.5f);
                    }
                    g.X += tdx / tdist * spd * dt;
                    g.Y += tdy / tdist * spd * dt;
                }
                if (g.AttackCd <= 0f && FindTarget(g))
                {
                    g.Phase = 5;
                    g.AnimTime = 0f;
                    g.Fired = false;
                    if (_soundState == 1)
                    {
                        DashAudio.StopGrimmIdleLoop();
                    }
                    DashAudio.PlayGrimmAttackYelp();
                    _soundState = 2;
                    return;
                }
                float gdx = g.X - _host.X;
                float gdy = g.Y - _host.Y;
                if (gdx * gdx + gdy * gdy > TeleportRange * TeleportRange)
                {
                    g.Phase = 2;
                    g.AnimTime = 0f;
                }
            }
            catch (Exception)
            {
            }
        }

        private void StopIdleLoop()
        {
            if (_soundState == 1)
            {
                DashAudio.StopGrimmIdleLoop();
            }
            _soundState = 0;
        }

        private float HoverX()
        {
            return _host.X + (_host.FacingLeft ? HoverOffX : -HoverOffX);
        }

        private float SleepGroundY()
        {
            return _host.FootY + 0.5f;
        }

        private bool FindTarget(Child g)
        {
            NelEnemy enemy = FindNearestEnemy(g.X, g.Y);
            if (enemy == null)
            {
                return false;
            }
            g.Target = enemy;
            return true;
        }

        private NelEnemy FindNearestEnemy(float sx, float sy)
        {
            try
            {
                Map2d mp = _host.Map;
                int mask = _host.EnemyMask;
                if (mp == null || mp.gameObject == null || mask == 0)
                {
                    return null;
                }
                Vector2 center = mp.gameObject.transform.TransformPoint(
                    new Vector2(mp.pixel2ux(sx * mp.CLEN), mp.pixel2uy(sy * mp.CLEN)));
                Collider2D[] hits = Physics2D.OverlapCircleAll(center, SeekRange, mask);
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

        private void SpawnFireballs(Child g)
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
                tx = g.X + (_host.FacingLeft ? -1f : 1f);
                ty = g.Y - 1f;
            }
            float baseAng = Mathf.Atan2(ty - g.Y, tx - g.X);
            float[] offs = { 0f, FireballSpread, -FireballSpread };
            for (int i = 0; i < offs.Length; i++)
            {
                float a = baseAng + offs[i];
                _fireballs.Add(new Fireball
                {
                    X = g.X,
                    Y = g.Y,
                    DirX = Mathf.Cos(a),
                    DirY = Mathf.Sin(a),
                    Life = FireballLife
                });
            }
        }

        private void UpdateFireballs(float dt)
        {
            if (_fireballs.Count == 0)
            {
                return;
            }
            _fbAnimTime += dt;
            Map2d mp = _host.Map;
            int mask = _host.EnemyMask;
            for (int i = _fireballs.Count - 1; i >= 0; i--)
            {
                Fireball fb = _fireballs[i];
                fb.X += fb.DirX * FireballSpeed * dt;
                fb.Y += fb.DirY * FireballSpeed * dt;
                fb.Life -= dt;
                bool remove = fb.Life <= 0f;
                if (!remove && mp != null && mp.gameObject != null && mask != 0)
                {
                    Vector2 center = mp.gameObject.transform.TransformPoint(
                        new Vector2(mp.pixel2ux(fb.X * mp.CLEN), mp.pixel2uy(fb.Y * mp.CLEN)));
                    Collider2D[] hits = Physics2D.OverlapCircleAll(center, FireballRadius, mask);
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
                        try
                        {
                            _host.ApplyGrimmFireballDamage(enemy, FireballDamage);
                        }
                        catch (Exception)
                        {
                        }
                    }
                }
                if (remove)
                {
                    _fireballs.RemoveAt(i);
                }
            }
        }

        private void Ensure()
        {
            Map2d mp = _host.Map;
            if (mp == null || (_ticket != null && _map == mp && _mapRevision == _host.MapRevision))
            {
                return;
            }
            Release();
            _map = mp;
            _mapRevision = _host.MapRevision;
            _mesh = new MeshDrawer(null, 4, 6);
            _mesh.draw_gl_only = true;
            _mat = MTRX.newMtr(MTRX.ShaderGDT);
            _mat.EnableKeyword("NO_PIXELSNAP");
            _mesh.activate("grimm_body", _mat, false, MTRX.ColWhite, null);
            if (mp.MovRenderer != null)
            {
                _ticket = mp.MovRenderer.assignDrawable(M2Mover.DRAW_ORDER.PR1, null, PrepareMesh, _mesh, null, null);
                _fbMesh = new MeshDrawer(null, 4 * 8, 6 * 8);
                _fbMesh.draw_gl_only = true;
                _fbMat = MTRX.newMtr(MTRX.ShaderGDT);
                _fbMat.EnableKeyword("NO_PIXELSNAP");
                _fbMesh.activate("grimm_fireball", _fbMat, false, MTRX.ColWhite, null);
                _fbTicket = mp.MovRenderer.assignDrawable(M2Mover.DRAW_ORDER.PR1, null, PrepareFireballMesh,
                    _fbMesh, null, null);
            }
        }

        private void Release()
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

        private bool PrepareMesh(Camera Cam, M2RenderTicket Tk, bool need_redraw, int draw_id,
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
            Child g = _child;
            if (g == null || !Ready)
            {
                MdOut = _mesh;
                return true;
            }
            string sprite;
            switch (g.Phase)
            {
                case 0:
                    sprite = GrimmAssets.Frame(GrimmAssets.ClipAppear, g.AnimTime, false);
                    break;
                case 2:
                    sprite = GrimmAssets.Frame(GrimmAssets.ClipIdle, g.AnimTime, true);
                    break;
                case 3:
                    sprite = g.SleepStage >= 1
                        ? GrimmAssets.Frame(GrimmAssets.ClipSleep, g.SleepStage == 2 ? 99f : g.AnimTime, false)
                        : GrimmAssets.Frame(GrimmAssets.ClipFly, g.AnimTime, true);
                    break;
                case 4:
                    sprite = GrimmAssets.Frame(GrimmAssets.ClipWake, g.AnimTime, false);
                    break;
                default:
                    bool moving = Mathf.Abs(g.X - _host.X) > 0.35f ||
                                  Mathf.Abs(g.Y - (_host.Y + HoverOffY)) > 0.35f;
                    sprite = GrimmAssets.Frame(g.Phase == 5 ? GrimmAssets.ClipIdle
                        : (moving ? GrimmAssets.ClipFly : GrimmAssets.ClipIdle), g.AnimTime, true);
                    break;
            }
            Texture2D tex = GrimmAssets.Texture(sprite);
            if (tex == null)
            {
                MdOut = _mesh;
                return true;
            }
            float c = mp.CLEN;
            Tk.Matrix = mp.gameObject.transform.localToWorldMatrix *
                        Matrix4x4.Translate(new Vector3(mp.pixel2ux(_host.X * c), mp.pixel2uy(_host.Y * c), 0f));
            float px = (g.X - _host.X) * c;
            float py = -(g.Y - _host.Y) * c;
            float w = tex.width * Scale;
            float h = tex.height * Scale;
            _mesh.Col = MTRX.ColWhite;
            _mesh.initForImgAndTexture(tex);
            _mesh.uv_top = 0f;
            _mesh.uv_height = 1f;
            if (_host.FacingLeft)
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

        private bool PrepareFireballMesh(Camera Cam, M2RenderTicket Tk, bool need_redraw, int draw_id,
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
            if (_fireballs.Count == 0 || !Ready)
            {
                MdOut = _fbMesh;
                return true;
            }
            Texture2D tex = GrimmAssets.Texture(
                GrimmAssets.Frame(GrimmAssets.ClipFireball, _fbAnimTime, true));
            if (tex == null)
            {
                MdOut = _fbMesh;
                return true;
            }
            float c = mp.CLEN;
            Tk.Matrix = mp.gameObject.transform.localToWorldMatrix *
                        Matrix4x4.Translate(new Vector3(mp.pixel2ux(_host.X * c), mp.pixel2uy(_host.Y * c), 0f));
            _fbMesh.initForImgAndTexture(tex);
            _fbMesh.uv_top = 0f;
            _fbMesh.uv_height = 1f;
            _fbMesh.uv_left = 0f;
            _fbMesh.uv_width = 1f;
            float w = tex.width * FireballScale;
            float h = tex.height * FireballScale;
            for (int i = 0; i < _fireballs.Count; i++)
            {
                Fireball fb = _fireballs[i];
                _fbMesh.Col = MTRX.ColWhite;
                Matrix4x4 saved = _fbMesh.getCurrentMatrix();
                _fbMesh.Translate((fb.X - _host.X) * c * 0.015625f, -(fb.Y - _host.Y) * c * 0.015625f, true);
                _fbMesh.Rect(-w * 0.5f, -h * 0.5f, w, h, false);
                _fbMesh.setCurrentMatrix(saved, false);
            }
            MdOut = _fbMesh;
            return true;
        }
    }
}
