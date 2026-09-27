using System;
using System.Collections.Generic;
using m2d;
using nel;
using UnityEngine;
using XX;
using KnightInCradle.Grimm;

namespace KnightInCradle.CharmUi
{
    /// <summary>
    /// 护符39 格林之子（诺艾尔侧）：只做"宿主"，逻辑全部走共享的
    /// <see cref="GrimmController"/>（与骑士那份同一实现，数值/表现一致）。
    /// </summary>
    internal static class NoelGrimm
    {
        private static GrimmController _ctl;
        private static readonly Host Impl = new Host();

        /// <summary>每帧推进（挂进 `TickNoelCharmEffects`）。</summary>
        public static void Tick(PRNoel pr)
        {
            try
            {
                if (_ctl == null)
                {
                    _ctl = new GrimmController(Impl);
                }
                Impl.Noel = pr;
                _ctl.Tick(Time.deltaTime);
            }
            catch (Exception)
            {
            }
        }

        /// <summary>诺艾尔侧宿主：把角色信息喂给共享实现。</summary>
        private sealed class Host : IGrimmHost
        {
            public PRNoel Noel;
            private int _mask = -1;

            public bool GrimmEquipped =>
                Noel != null && !CharmEffects.IsKnightMode &&
                CharmEffects.IsEquipped(CharmOwner.Noel, CharmEffects.GrimmId);

            public bool Active
            {
                get
                {
                    try
                    {
                        return Noel != null && Noel.is_alive;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            public float X => Noel != null ? Noel.x : 0f;
            public float Y => Noel != null ? Noel.y : 0f;

            public float Vx
            {
                get
                {
                    try
                    {
                        return Noel != null ? Noel.vx : 0f;
                    }
                    catch (Exception)
                    {
                        return 0f;
                    }
                }
            }

            public float Vy
            {
                get
                {
                    try
                    {
                        return Noel != null ? Noel.vy : 0f;
                    }
                    catch (Exception)
                    {
                        return 0f;
                    }
                }
            }

            public bool FacingLeft
            {
                get
                {
                    try
                    {
                        if (Noel == null)
                        {
                            return false;
                        }
                        int aimX = CAim._XD(Noel.getAimForCaster(), 1);
                        if (aimX != 0)
                        {
                            return aimX < 0;
                        }
                        // 正上/正下瞄准时用本体朝向兜底（mpf_is_right>=0 表示面朝右）
                        return Noel.mpf_is_right < 0f;
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            public bool Sitting
            {
                get
                {
                    try
                    {
                        return Noel != null && Noel.isBenchState();
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            public float FootY
            {
                get
                {
                    try
                    {
                        return Noel != null ? Noel.mbottom : 0f;
                    }
                    catch (Exception)
                    {
                        return Y;
                    }
                }
            }

            /// <summary>坐椅睡眠落地基准：诺艾尔脚底再往下 0.5 格。</summary>
            public float SleepGroundBaseY => FootY + 0.5f;

            public bool Grounded
            {
                get
                {
                    try
                    {
                        return Noel != null && Noel.hasFoot();
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                }
            }

            public Map2d Map => Noel != null ? Noel.Mp : null;

            /// <summary>换图/换实例时变化，供共享实现重建票据。</summary>
            public int MapRevision
            {
                get
                {
                    try
                    {
                        // 换图时 `Mp` 实例会变：用 RuntimeHelpers 的哈希代替 Unity 的 GetInstanceID
                        return Noel != null && Noel.Mp != null
                            ? System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(Noel.Mp)
                            : 0;
                    }
                    catch (Exception)
                    {
                        return 0;
                    }
                }
            }

            public int EnemyMask
            {
                get
                {
                    if (_mask >= 0)
                    {
                        return _mask;
                    }
                    int mask = LayerMask.GetMask("EnemySelf", "Enemy", "AttackHitable");
                    foreach (string name in new[] { "Ignore Raycast", "Water", "TransparentFX", "Default" })
                    {
                        int layer = LayerMask.NameToLayer(name);
                        if (layer >= 0)
                        {
                            mask |= 1 << layer;
                        }
                    }
                    _mask = mask;
                    return mask;
                }
            }

            public float HoverOffX => 1.2f;
            public float HoverOffY => -1.1f;

            /// <summary>诺艾尔侧索敌：6 格内最近的魔物（无爱丽丝 / 魔力草层次）。</summary>
            public bool TryAcquireTarget(float x, float y, float range, out float tx, out float ty,
                out object token)
            {
                tx = 0f;
                ty = 0f;
                token = null;
                try
                {
                    Map2d mp = Map;
                    int mask = EnemyMask;
                    if (mp == null || mp.gameObject == null || mask == 0)
                    {
                        return false;
                    }
                    Vector2 center = mp.gameObject.transform.TransformPoint(
                        new Vector2(mp.pixel2ux(x * mp.CLEN), mp.pixel2uy(y * mp.CLEN)));
                    Collider2D[] hits = Physics2D.OverlapCircleAll(center, range, mask);
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
                        float dx = enemy.x - x;
                        float dy = enemy.y - y;
                        float d = dx * dx + dy * dy;
                        if (d < bestD)
                        {
                            bestD = d;
                            best = enemy;
                        }
                    }
                    if (best == null)
                    {
                        return false;
                    }
                    tx = best.x;
                    ty = best.y;
                    token = best;
                    return true;
                }
                catch (Exception)
                {
                    return false;
                }
            }

            /// <summary>火球命中魔物：30 真伤（与骑士一致），同一目标只结算一次。</summary>
            public void OnGrimmFireballCollider(object token, Collider2D col, int damage,
                HashSet<object> hits)
            {
                try
                {
                    if (col == null || Noel == null)
                    {
                        return;
                    }
                    NelEnemy enemy = col.GetComponentInParent<NelEnemy>();
                    if (enemy == null || !enemy.is_alive || !hits.Add(enemy))
                    {
                        return;
                    }
                    var atk = new NelAttackInfo();
                    atk.fix_damage = true;
                    atk.Caster = Noel;
                    atk.AttackFrom = Noel;
                    atk.hpdmg0 = damage;
                    atk.hpdmg_current = damage;
                    atk._apply_knockback_current = true;
                    atk.CenterXy(enemy.x, enemy.y, 0f);
                    enemy.applyDamage(atk, false);
                }
                catch (Exception)
                {
                }
            }

            /// <summary>诺艾尔侧没有额外火球副作用（爱丽丝 / 魔力草只在小骑士侧）。</summary>
            public void OnGrimmFireballTick(object token, float x, float y, float radius,
                HashSet<object> hits, ref bool destroy)
            {
            }
        }
    }
}
