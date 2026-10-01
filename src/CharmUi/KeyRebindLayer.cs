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
        private bool _rowsLangKnown; // 面板文本已按哪种语言生成（游戏里换语言后重生成一行）
        private KicL10n.Lang _rowsLang;
        private bool _styleLangKnown; // 面板字体/样式已按哪种语言建立
        private KicL10n.Lang _styleLang;

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
                        : null, KeyCode.F10);
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
                new Row { Label = KicPanelText.SwitchChar, Entry = KnightInCradlePlugin.ToggleKey, Fallback = KeyCode.T },
                new Row { Label = KicPanelText.CharmUi, Entry = KnightInCradlePlugin.CharmUiKey, Fallback = KeyCode.O },
                new Row { Label = KicPanelText.SeriousMode, Entry = KnightInCradlePlugin.SeriousModeKey, Fallback = KeyCode.Period },
                new Row { Label = KicPanelText.Taunt, Entry = KnightInCradlePlugin.TauntKey, Fallback = KeyCode.V },
                new Row { Label = KicPanelText.CritStacks, Entry = KnightInCradlePlugin.CritStackKey, Fallback = KeyCode.H },
                new Row { Label = KicPanelText.SectionKnight, Entry = null, Fallback = KeyCode.None },
                new Row { Label = KicPanelText.MoveLeft, Entry = KnightInCradlePlugin.MoveLeftKey, Fallback = KeyCode.A },
                new Row { Label = KicPanelText.MoveRight, Entry = KnightInCradlePlugin.MoveRightKey, Fallback = KeyCode.D },
                new Row { Label = KicPanelText.Jump, Entry = KnightInCradlePlugin.JumpKey, Fallback = KeyCode.W },
                new Row { Label = KicPanelText.Dash, Entry = KnightInCradlePlugin.DashKey, Fallback = KeyCode.LeftShift },
                new Row { Label = KicPanelText.Attack, Entry = KnightInCradlePlugin.AttackKey, Fallback = KeyCode.Mouse0 },
                new Row { Label = KicPanelText.Focus, Entry = KnightInCradlePlugin.FocusKey, Fallback = KeyCode.C },
                new Row { Label = KicPanelText.Fireball, Entry = KnightInCradlePlugin.FireballKey, Fallback = KeyCode.S },
                new Row { Label = KicPanelText.DreamNail, Entry = KnightInCradlePlugin.DreamNailKey, Fallback = KeyCode.Space },
                new Row { Label = KicPanelText.LookUp, Entry = KnightInCradlePlugin.LookUpKey, Fallback = KeyCode.Q },
                new Row { Label = KicPanelText.LookDown, Entry = KnightInCradlePlugin.LookDownKey, Fallback = KeyCode.Mouse1 },
                new Row { Label = KicPanelText.SuperDash, Entry = KnightInCradlePlugin.SuperDashKey, Fallback = KeyCode.LeftControl },
                new Row { Label = KicPanelText.SectionPose, Entry = null, Fallback = KeyCode.None },
                new Row { Label = KicPanelText.PoseNext, Entry = KnightInCradlePlugin.PoseBrowserNextKeyConfig, Fallback = KeyCode.F8 },
                new Row { Label = KicPanelText.PosePrev, Entry = KnightInCradlePlugin.PoseBrowserPrevKeyConfig, Fallback = KeyCode.F7 },
                new Row { Label = KicPanelText.PoseOff, Entry = KnightInCradlePlugin.PoseBrowserOffKeyConfig, Fallback = KeyCode.F9 },
                new Row { Label = KicPanelText.SectionPanel, Entry = null, Fallback = KeyCode.None },
                new Row { Label = KicPanelText.PanelToggle, Entry = KnightInCradlePlugin.KeyRebindToggleKey, Fallback = KeyCode.F10 },
            };
            _rowsLang = KicL10n.Current;
            _rowsLangKnown = true;
        }

        private void EnsureStyles()
        {
            // 需求 2026-10-01：面板文本跟随游戏语言，字体候选也按语言排序
            // （泰语/韩语/日语各自的系统字体放在最前，避免缺字显示成方块）；
            // 游戏内换语言后这里会重建一次字体与样式。
            KicL10n.Lang lang = KicL10n.Current;
            if (_label != null && _styleLangKnown && lang == _styleLang)
            {
                return;
            }
            _styleLang = lang;
            _styleLangKnown = true;
            Font oldFont = _label != null ? _label.font : null;
            Font font = Font.CreateDynamicFontFromOSFont(KicL10n.FontCandidates(), 16);
            if (oldFont != null && oldFont != font)
            {
                try
                {
                    UnityEngine.Object.Destroy(oldFont);
                }
                catch (Exception)
                {
                }
            }
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
                else if (!_capturing)
                {
                    // 游戏里切了语言就重新生成一次行文本（改键进行中不打断）
                    KicL10n.Lang lang = KicL10n.Current;
                    if (!_rowsLangKnown || lang != _rowsLang)
                    {
                        BuildRows();
                    }
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
                string toggleName = KeyConfig.Parse(
                    KnightInCradlePlugin.KeyRebindToggleKey != null
                        ? KnightInCradlePlugin.KeyRebindToggleKey.Value
                        : null, KeyCode.F10).ToString();
                GUI.Label(titleRect, KicPanelText.Title(toggleName), _title);

                string hint = _capturing
                    ? KicPanelText.HintCapture
                    : KicPanelText.HintNormal;
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
                        ? KicPanelText.WaitingKey
                        : (cur == KeyCode.None ? KicPanelText.NotSet : cur.ToString());
                    GUI.Label(new Rect(row.x + 6f, row.y, inner.width - 220f, row.height), r.Label, _label);
                    GUI.Label(new Rect(row.x + row.width - 210f, row.y, 110f, row.height), shown, _hint);
                    if (GUI.Button(new Rect(row.x + row.width - 96f, row.y, 44f, row.height), KicPanelText.Rebind, _button))
                    {
                        _capturing = true;
                        _captureIndex = i;
                    }
                    if (GUI.Button(new Rect(row.x + row.width - 48f, row.y, 44f, row.height), KicPanelText.Reset, _button))
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
