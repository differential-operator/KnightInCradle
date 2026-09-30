using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace KnightInCradle.CharmUi
{
    /// <summary>
    /// 局内键位设置面板（需求 2026-09-30）：默认 **F9** 开关。
    ///
    /// 面板列出**模组自有**的按键（AIC 自己的魔法/攻击/闪避等键由游戏设置里改，模组会自动跟随）：
    /// 点某一行右侧的「改键」→ 按任意键 / 鼠标即可改；
    /// 改完立刻写入 cfg（`Config.Save()`）并**立即生效** —— 模组绝大多数按键都是每帧读 cfg，
    /// 只有极少数（小骑士的聚焦/法球/梦钉/挑衅/超冲）是启动时缓存的，这里主动刷新一次。
    /// </summary>
    internal sealed class KeyRebindLayer : MonoBehaviour
    {
        private static KeyRebindLayer _instance;

        /// <summary>面板整体缩放（需求 2026-09-30：放大一倍）。</summary>
        private const float PanelScale = 2f;

        private bool _open;
        private bool _capturing;
        private int _captureIndex = -1;
        private Vector2 _scroll;
        private bool _cursorSaved;
        private bool _prevCursorVisible;
        private CursorLockMode _prevCursorLock;
        private GUIStyle _title;
        private GUIStyle _label;
        private GUIStyle _hint;
        private GUIStyle _button;

        private sealed class Row
        {
            public string Label;
            public ConfigEntry<string> Entry;
            public KeyCode Fallback;
            public string Note;
        }

        private List<Row> _rows;

        /// <summary>面板是否打开（打开期间屏蔽模组自己的输入，避免点按钮时顺手放招）。</summary>
        public static bool IsOpen
        {
            get
            {
                try
                {
                    return _instance != null && _instance._open;
                }
                catch (Exception)
                {
                    return false;
                }
            }
        }

        public static void Ensure(GameObject host)
        {
            try
            {
                if (_instance != null || host == null)
                {
                    return;
                }
                _instance = host.AddComponent<KeyRebindLayer>();
            }
            catch (Exception)
            {
            }
        }

        private void Update()
        {
            try
            {
                // 开关用原始输入读取：护符 UI 打开时 `KeyConfig` 会屏蔽输入，这里不能走那个门
                KeyCode toggle = KeyConfig.Parse(
                    KnightInCradlePlugin.KeyRebindToggleKey != null
                        ? KnightInCradlePlugin.KeyRebindToggleKey.Value
                        : null, KeyCode.F9);
                if (toggle != KeyCode.None && !_capturing && UnityEngine.Input.GetKeyDown(toggle))
                {
                    _open = !_open;
                    if (!_open)
                    {
                        _capturing = false;
                        _captureIndex = -1;
                    }
                    SetPanelCursor(_open);
                }
                // 面板打开期间每帧保持"显示并解锁鼠标"（游戏在游玩中会自己把鼠标藏起来/锁住）
                if (_open)
                {
                    SetPanelCursor(true);
                }
            }
            catch (Exception)
            {
            }
        }

        /// <summary>打开面板时呼出鼠标（显示 + 解锁），关闭时还原原来的鼠标状态。</summary>
        private void SetPanelCursor(bool open)
        {
            try
            {
                if (open)
                {
                    if (!_cursorSaved)
                    {
                        _prevCursorVisible = Cursor.visible;
                        _prevCursorLock = Cursor.lockState;
                        _cursorSaved = true;
                    }
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                }
                else if (_cursorSaved)
                {
                    Cursor.visible = _prevCursorVisible;
                    Cursor.lockState = _prevCursorLock;
                    _cursorSaved = false;
                }
            }
            catch (Exception)
            {
            }
        }

        private void BuildRows()
        {
            _rows = new List<Row>
            {
                new Row { Label = "切换 小骑士 / 诺艾尔", Entry = KnightInCradlePlugin.ToggleKey, Fallback = KeyCode.T },
                new Row { Label = "护符界面 开关", Entry = KnightInCradlePlugin.CharmUiKey, Fallback = KeyCode.O },
                new Row { Label = "认真模式 开关", Entry = KnightInCradlePlugin.SeriousModeKey, Fallback = KeyCode.Period },
                new Row { Label = "挑衅", Entry = KnightInCradlePlugin.TauntKey, Fallback = KeyCode.V },
                new Row { Label = "会心 +100（调试）", Entry = KnightInCradlePlugin.CritStackKey, Fallback = KeyCode.H },
                new Row { Label = "── 小骑士：移动/动作 ──", Entry = null, Fallback = KeyCode.None },
                new Row { Label = "左移", Entry = KnightInCradlePlugin.MoveLeftKey, Fallback = KeyCode.A },
                new Row { Label = "右移", Entry = KnightInCradlePlugin.MoveRightKey, Fallback = KeyCode.D },
                new Row { Label = "跳跃", Entry = KnightInCradlePlugin.JumpKey, Fallback = KeyCode.W },
                new Row { Label = "冲刺", Entry = KnightInCradlePlugin.DashKey, Fallback = KeyCode.LeftShift },
                new Row { Label = "攻击 / 蓄力劈砍", Entry = KnightInCradlePlugin.AttackKey, Fallback = KeyCode.Mouse0 },
                new Row { Label = "聚焦 / 回血", Entry = KnightInCradlePlugin.FocusKey, Fallback = KeyCode.C },
                new Row { Label = "法球", Entry = KnightInCradlePlugin.FireballKey, Fallback = KeyCode.S },
                new Row { Label = "梦钉", Entry = KnightInCradlePlugin.DreamNailKey, Fallback = KeyCode.Space },
                new Row { Label = "抬头 / 法术上", Entry = KnightInCradlePlugin.LookUpKey, Fallback = KeyCode.Q },
                new Row { Label = "低头 / 法术下", Entry = KnightInCradlePlugin.LookDownKey, Fallback = KeyCode.Mouse1 },
                new Row { Label = "超级冲刺（额外键）", Entry = KnightInCradlePlugin.SuperDashKey, Fallback = KeyCode.LeftControl },
                new Row { Label = "── 本面板 ──", Entry = null, Fallback = KeyCode.None },
                new Row { Label = "打开 / 关闭键位面板", Entry = KnightInCradlePlugin.KeyRebindToggleKey, Fallback = KeyCode.F9, Note = "改完立即生效" },
            };
        }

        private void EnsureStyles()
        {
            if (_label != null)
            {
                return;
            }
            Font font = Font.CreateDynamicFontFromOSFont(
                new[] { "Microsoft YaHei", "SimHei", "SimSun", "Yu Gothic UI", "Arial" }, 16);
            _title = new GUIStyle { font = font, fontSize = 20, fontStyle = FontStyle.Bold };
            _title.normal.textColor = new Color(1f, 0.85f, 0.3f);
            _label = new GUIStyle { font = font, fontSize = 16, alignment = TextAnchor.MiddleLeft };
            _label.normal.textColor = Color.white;
            _hint = new GUIStyle { font = font, fontSize = 14, alignment = TextAnchor.MiddleLeft };
            _hint.normal.textColor = new Color(0.75f, 0.95f, 1f);
            _button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 14 };
        }

        private void OnGUI()
        {
            try
            {
                if (!_open)
                {
                    return;
                }
                if (_rows == null)
                {
                    BuildRows();
                }
                EnsureStyles();

                HandleCaptureEvent();

                // 需求 2026-09-30：整体放大一倍（用 GUI 矩阵缩放，所有尺寸/字号一起翻倍）
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity,
                    new Vector3(PanelScale, PanelScale, 1f));
                float vw = Screen.width / PanelScale;
                float vh = Screen.height / PanelScale;
                float w = Mathf.Min(560f, vw - 20f);
                float h = Mathf.Min(560f, vh - 20f);
                Rect panel = new Rect((vw - w) * 0.5f, (vh - h) * 0.5f, w, h);
                Color prev = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.82f);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = prev;

                Rect titleRect = new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 26f);
                GUI.Label(titleRect, "键位设置（F9 打开 / 关闭）", _title);

                string hint = _capturing
                    ? "请按下新的按键 / 鼠标键…（Esc 取消）"
                    : "点「改键」后按任意键；改完立即生效并已保存到 cfg。AIC 自身按键请到游戏设置里改。";
                GUI.Label(new Rect(panel.x + 16f, panel.y + 38f, panel.width - 32f, 22f), hint, _hint);

                float rowH = 26f;
                Rect scrollRect = new Rect(panel.x + 8f, panel.y + 64f, panel.width - 16f, panel.height - 74f);
                Rect inner = new Rect(0f, 0f, scrollRect.width - 20f, _rows.Count * rowH + 6f);
                _scroll = GUI.BeginScrollView(scrollRect, _scroll, inner);
                for (int i = 0; i < _rows.Count; i++)
                {
                    Row r = _rows[i];
                    Rect row = new Rect(4f, i * rowH, inner.width - 8f, rowH - 2f);
                    if (r.Entry == null)
                    {
                        GUI.Label(row, r.Label, _hint);
                        continue;
                    }
                    KeyCode cur = KeyConfig.Parse(r.Entry.Value, r.Fallback);
                    string shown = _capturing && _captureIndex == i
                        ? "（等待按键…）"
                        : (cur == KeyCode.None ? "未设置" : cur.ToString());
                    GUI.Label(new Rect(row.x + 6f, row.y, inner.width - 220f, row.height), r.Label, _label);
                    GUI.Label(new Rect(row.x + row.width - 210f, row.y, 110f, row.height), shown, _hint);
                    if (GUI.Button(new Rect(row.x + row.width - 96f, row.y, 44f, row.height), "改键", _button))
                    {
                        _capturing = true;
                        _captureIndex = i;
                    }
                    if (GUI.Button(new Rect(row.x + row.width - 48f, row.y, 44f, row.height), "默认", _button))
                    {
                        ApplyKey(i, r.Fallback);
                    }
                }
                GUI.EndScrollView();
            }
            catch (Exception)
            {
            }
        }

        /// <summary>抓取"新键"：面板打开且处于改键状态时，取下一次按键/鼠标事件。</summary>
        private void HandleCaptureEvent()
        {
            if (!_capturing)
            {
                return;
            }
            Event ev = Event.current;
            if (ev == null)
            {
                return;
            }
            KeyCode got = KeyCode.None;
            if (ev.type == EventType.KeyDown)
            {
                if (ev.keyCode == KeyCode.Escape)
                {
                    _capturing = false;
                    _captureIndex = -1;
                    ev.Use();
                    return;
                }
                if (ev.keyCode != KeyCode.None)
                {
                    got = ev.keyCode;
                }
                ev.Use();
            }
            else if (ev.type == EventType.MouseDown && ev.button >= 0 && ev.button <= 2)
            {
                got = (KeyCode)((int)KeyCode.Mouse0 + ev.button);
                ev.Use();
            }
            if (got != KeyCode.None)
            {
                int idx = _captureIndex;
                _capturing = false;
                _captureIndex = -1;
                ApplyKey(idx, got);
            }
        }

        private void ApplyKey(int index, KeyCode key)
        {
            try
            {
                if (_rows == null || index < 0 || index >= _rows.Count || key == KeyCode.None)
                {
                    return;
                }
                Row r = _rows[index];
                if (r.Entry == null)
                {
                    return;
                }
                r.Entry.Value = key.ToString();
                KnightInCradlePlugin.ConfigRef?.Save();
                // 少数启动时缓存的键（小骑士聚焦/法球/梦钉/挑衅/超冲）立即刷新
                KnightEntity.RefreshConfigKeys();
                KnightInCradlePlugin.PluginLog?.LogInfo(
                    "[KIC][键位] " + r.Label + " → " + key);
            }
            catch (Exception)
            {
            }
        }
    }
}
