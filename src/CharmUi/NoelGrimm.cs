using System;
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
                        return Noel != null && CAim._XD(Noel.getAimForCaster(), 1) >= 0;
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

            /// <summary>火球伤害：30 真伤（与骑士一致）。</summary>
            public void ApplyGrimmFireballDamage(NelEnemy enemy, int damage)
            {
                try
                {
                    if (enemy == null || Noel == null)
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
        }
    }
}
