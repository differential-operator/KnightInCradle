using System;
using m2d;
using nel;
using UnityEngine;
using XX;

namespace KnightInCradle.CharmUi
{
    /// <summary>
    /// 护符16 沉重之击的"会心层数"浮动文本框（需求 2026-09-28）：
    /// **跟随诺艾尔（同帧同速、无插值延迟）**，只显示层数数字，文字颜色 #FF41CE。
    /// AIC 裁掉了 uGUI 着色器，这里走 OnGUI（IMGUI）——与护符 UI / 梦语文本框同一条通道。
    /// </summary>
    internal sealed class NoelCritCounter : MonoBehaviour
    {
        /// <summary>文本框锚点相对诺艾尔中心的世界偏移（格；AIC 的 Y 向下为正，负值=向上）。</summary>
        private const float AnchorOffX = 0f;
        private const float AnchorOffY = -1.6f;
        /// <summary>文字大小（1080p 基准，按实际分辨率缩放）。</summary>
        private const float FontSizeAt1080 = 34f;
        /// <summary>需求指定颜色：文字 #FF41CE。</summary>
        private static readonly Color TextColor = new Color32(0xFF, 0x41, 0xCE, 0xFF);
        /// <summary>描边色（让粉字在任何背景上都看得清）。</summary>
        private static readonly Color OutlineColor = new Color32(0x00, 0x00, 0x00, 0xC0);

        private static NoelCritCounter _instance;
        private static Font _font;
        private GUIStyle _style;
        private GUIStyle _outlineStyle;
        private int _builtFontSize = -1;

        /// <summary>首次需要时挂到插件宿主上（幂等）。</summary>
        public static void Ensure(GameObject host)
        {
            try
            {
                if (_instance != null || host == null)
                {
                    return;
                }
                _instance = host.AddComponent<NoelCritCounter>();
            }
            catch (Exception)
            {
            }
        }

        private void OnGUI()
        {
            try
            {
                if (CharmEffects.IsKnightMode ||
                    !CharmEffects.IsEquipped(CharmOwner.Noel, CharmEffects.HeavyBlowId))
                {
                    return; // 只在诺艾尔戴着沉重之击时显示
                }
                NelM2DBase m2d = M2DBase.Instance as NelM2DBase;
                PRNoel pr = m2d != null ? m2d.getPrNoel() : null;
                if (pr == null || !pr.is_alive || pr.Mp == null)
                {
                    return;
                }
                Camera cam = GetMoverCamera(m2d);
                if (cam == null)
                {
                    return;
                }
                Map2d mp = pr.Mp;
                Vector2 world = mp.gameObject.transform.TransformPoint(new Vector2(
                    mp.pixel2ux((pr.x + AnchorOffX) * mp.CLEN),
                    mp.pixel2uy((pr.y + AnchorOffY) * mp.CLEN)));
                Vector3 sp = cam.WorldToScreenPoint(new Vector3(world.x, world.y, 0f));
                if (sp.z < 0f)
                {
                    return; // 在相机背后
                }
                float s = Mathf.Max(0.6f, Screen.height / 1080f);
                float w = 200f * s;
                float h = 60f * s;
                Rect r = new Rect(sp.x - w * 0.5f, Screen.height - sp.y - h * 0.5f, w, h);
                EnsureStyles(s);
                string text = CharmEffects.NoelHeavyBlowStacks.ToString();
                Color prev = GUI.color;
                GUI.color = Color.white;
                GUI.Label(new Rect(r.x + 2f * s, r.y + 2f * s, r.width, r.height), text, _outlineStyle);
                GUI.Label(r, text, _style);
                GUI.color = prev;
            }
            catch (Exception)
            {
            }
        }

        private void EnsureStyles(float s)
        {
            int fontSize = Mathf.Max(10, Mathf.RoundToInt(FontSizeAt1080 * s));
            if (_style != null && _builtFontSize == fontSize)
            {
                return;
            }
            if (_font == null)
            {
                _font = Font.CreateDynamicFontFromOSFont(
                    new[] { "Arial", "Microsoft YaHei", "Yu Gothic UI", "SimSun" }, fontSize);
            }
            _builtFontSize = fontSize;
            _style = new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter,
                font = _font,
                fontSize = fontSize,
                fontStyle = FontStyle.Bold
            };
            _style.normal.textColor = TextColor;
            _outlineStyle = new GUIStyle(_style);
            _outlineStyle.normal.textColor = OutlineColor;
        }

        /// <summary>取"移动体相机"（地图/角色所在的那台相机），用于世界坐标 → 屏幕坐标。</summary>
        private static Camera GetMoverCamera(NelM2DBase m2d)
        {
            try
            {
                M2Camera mc = m2d != null ? m2d.Cam : null;
                if (mc != null)
                {
                    CameraComponentCollecter cc = mc.getMoverCameraCC();
                    if (cc != null && cc.Cam != null)
                    {
                        return cc.Cam;
                    }
                }
            }
            catch (Exception)
            {
            }
            return Camera.main;
        }
    }
}
