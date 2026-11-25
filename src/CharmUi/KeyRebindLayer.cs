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

        private int _page;
        private GUIStyle _small;
        private GUIStyle _section;
        private GUIStyle _key;
        private Texture2D _buttonNormal, _buttonHover, _buttonDown;
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
            public int Page;
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

        internal static bool CloseForEscape()
        {
            if (!IsOpen) return false;
            _instance._open = false;
            _instance._capturing = false;
            _instance._captureIndex = -1;
            _instance.SetPanelCursor(false);
            return true;
        }

        private void Update()
        {
            try
            {
                if (OverlayEscape.Handle()) return;
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
                new Row { Label = KicPanelText.Taunt, Entry = KnightInCradlePlugin.TauntKey, Fallback = KeyCode.V },
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
                new Row { Page = 1, Label = KicPanelText.SectionPose, Entry = null, Fallback = KeyCode.None },
                new Row { Page = 1, Label = KicPanelText.PoseNext, Entry = KnightInCradlePlugin.PoseBrowserNextKeyConfig, Fallback = KeyCode.F8 },
                new Row { Page = 1, Label = KicPanelText.PosePrev, Entry = KnightInCradlePlugin.PoseBrowserPrevKeyConfig, Fallback = KeyCode.F6 },
                new Row { Page = 1, Label = KicPanelText.PoseOff, Entry = KnightInCradlePlugin.PoseBrowserOffKeyConfig, Fallback = KeyCode.F9 },
                new Row { Page = 1, Label = KicPanelText.SectionPanel, Entry = null, Fallback = KeyCode.None },
                new Row { Page = 1, Label = KicPanelText.PanelToggle, Entry = KnightInCradlePlugin.KeyRebindToggleKey, Fallback = KeyCode.F10 },
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
            _title.normal.textColor = new Color(0.94f, 0.96f, 1f);
            _label = new GUIStyle { font = font, fontSize = 16, alignment = TextAnchor.MiddleLeft };
            _label.normal.textColor = Color.white;
            _hint = new GUIStyle { font = font, fontSize = 14, alignment = TextAnchor.MiddleLeft };
            _hint.normal.textColor = new Color(0.75f, 0.95f, 1f);
            _small = new GUIStyle(_hint) { fontSize = 11, wordWrap = true };
            _small.normal.textColor = new Color(0.58f, 0.66f, 0.75f);
            _section = new GUIStyle(_label) { fontSize = 13, fontStyle = FontStyle.Bold };
            _section.normal.textColor = new Color(0.94f, 0.72f, 0.38f);
            _key = new GUIStyle(_hint) { alignment = TextAnchor.MiddleCenter, fontSize = 13 };
            if (_buttonNormal == null)
            {
                _buttonNormal = Solid(new Color(0.16f, 0.21f, 0.28f));
                _buttonHover = Solid(new Color(0.23f, 0.33f, 0.43f));
                _buttonDown = Solid(new Color(0.12f, 0.42f, 0.51f));
            }
            _button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 13, border = new RectOffset(), padding = new RectOffset(6,6,3,3) };
            _button.normal.background = _buttonNormal;
            _button.hover.background = _buttonHover;
            _button.active.background = _buttonDown;
            _button.normal.textColor = new Color(0.9f,0.94f,1f);
            _button.hover.textColor = Color.white;
            _button.active.textColor = Color.white;
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
                float w = Mathf.Min(640f, vw - 24f);
                float h = Mathf.Min(610f, vh - 24f);
                Rect panel = new Rect((vw - w) * 0.5f, (vh - h) * 0.5f, w, h);
                Color prev = GUI.color;
                GUI.color = new Color(0.035f, 0.05f, 0.075f, 0.97f);
                GUI.DrawTexture(panel, Texture2D.whiteTexture);
                GUI.color = prev;

                Rect titleRect = new Rect(panel.x + 16f, panel.y + 10f, panel.width - 32f, 26f);
                string toggleName = KeyConfig.Parse(
                    KnightInCradlePlugin.KeyRebindToggleKey != null
                        ? KnightInCradlePlugin.KeyRebindToggleKey.Value
                        : null, KeyCode.F10).ToString();
                GUI.Label(titleRect, KicPanelText.Title(toggleName), _title);

                string[] pages = { "小骑士键位", "调试工具" };
                bool enabledBeforeTabs = GUI.enabled;
                GUI.enabled = !_capturing && enabledBeforeTabs;
                for (int t = 0; t < pages.Length; t++)
                {
                    Rect tab = new Rect(panel.x+16f+t*(panel.width-32f)/pages.Length, panel.y+50f, (panel.width-32f)/pages.Length-6f, 30f);
                    if (GUI.Button(tab, pages[t], _button) && _page != t) { _page=t; _scroll=Vector2.zero; }
                    if (_page == t) Fill(new Rect(tab.x,tab.yMax-2f,tab.width,2f), new Color(0.2f,0.8f,0.87f));
                }
                GUI.enabled = enabledBeforeTabs;
                float rowH = 38f;
                bool showFooter = _capturing || _page == 1;
                Rect scrollRect = new Rect(panel.x+12f,panel.y+94f,panel.width-24f,panel.height-94f-(showFooter ? 38f : 12f));
                int visibleRows = _page == 1 ? 9 : 2;
                foreach (Row entry in _rows) if (entry.Page == _page) visibleRows++;
                float contentHeight = visibleRows * rowH;
                Rect inner = new Rect(0f,0f,scrollRect.width-20f,contentHeight);
                _scroll = GUI.BeginScrollView(scrollRect,_scroll,inner);
                if (_page == 1)
                {
                Color oldColor = GUI.color;
                GUI.color = new Color(0.23f, 0.13f, 0.07f, 0.65f);
                GUI.DrawTexture(new Rect(2f,0f,inner.width-4f,rowH*7),Texture2D.whiteTexture);
                GUI.color = new Color(1f,0.72f,0.32f,1f);
                GUI.Label(new Rect(10f, 0f, inner.width-20f, rowH),
                    "高风险调试  /  谨慎选择，易数值崩坏", _section);
                GUI.color = oldColor;
                bool oldEnabled = GUI.enabled;
                GUI.enabled = oldEnabled && DangerDebug.Ready && !_capturing;
                GUI.Label(new Rect(10f, rowH, inner.width-130f, rowH),
                    "危险度 +16 · 当前：" + (DangerDebug.Ready ? DangerDebug.Current.ToString() : "未进入游戏"), _label);
                if (GUI.Button(new Rect(inner.width-110f, rowH, 100f, rowH-8f), "+16", _button)) DangerDebug.Change(16);
                GUI.Label(new Rect(10f, rowH*2, inner.width-130f, rowH), "危险度 −16", _label);
                if (GUI.Button(new Rect(inner.width-110f, rowH*2, 100f, rowH-8f), "−16", _button)) DangerDebug.Change(-16);
                GUI.enabled = oldEnabled && !_capturing;
                GUI.Label(new Rect(10f, rowH*3, inner.width-130f, rowH), "禁用危险度上限", _label);
                if (GUI.Button(new Rect(inner.width-110f, rowH*3, 100f, rowH-8f), DangerDebug.Uncapped ? "是" : "否", _button)) DangerDebug.Toggle();
                GUI.Label(new Rect(10f, rowH*4, inner.width-130f, rowH), "更危险的危险度", _label);
                if (GUI.Button(new Rect(inner.width-110f, rowH*4, 100f, rowH-8f), DangerHealthDebug.Enabled ? "开" : "关", _button)) DangerHealthDebug.Toggle();
                GUI.enabled = oldEnabled && !_capturing && MapRevealDebug.Ready;
                GUI.Label(new Rect(10f, rowH*5, inner.width-130f, rowH), "地图全亮", _label);
                if (GUI.Button(new Rect(inner.width-110f, rowH*5, 100f, rowH-8f), MapRevealDebug.Enabled || MapRevealDebug.Busy ? "开启" : "关闭", _button)) MapRevealDebug.UnlockCurrent();
                GUI.enabled = oldEnabled;
                float mapHintX = 20f + _label.CalcSize(new GUIContent("地图全亮")).x;
                GUI.Label(new Rect(mapHintX, rowH*5, Mathf.Max(0f, inner.width-130f-mapHintX), rowH), "操作无法更改，谨慎开启", _small);
                GUI.enabled = oldEnabled && !_capturing;
                GUI.Label(new Rect(10f, rowH*6, inner.width-130f, rowH), KicPanelText.CritStacks, _label);
                if (GUI.Button(new Rect(inner.width-110f, rowH*6, 100f, rowH-8f), KicPanelText.Confirm, _button))
                    CharmEffects.AddNoelHeavyBlowStacks(100);
                GUI.enabled = oldEnabled;
                }
                if (_page == 1)
                {
                    GUI.Label(new Rect(10f,rowH*7,inner.width-20f,rowH), "显示设置", _section);
                    GUI.Label(new Rect(10f,rowH*8,inner.width-130f,rowH), "立绘显示", _label);
                    bool enabledBeforePortrait = GUI.enabled;
                    GUI.enabled = enabledBeforePortrait && !_capturing;
                    if (GUI.Button(new Rect(inner.width-110f,rowH*8,100f,rowH-8f), PortraitVisibility.Enabled ? "开启" : "关闭", _button)) PortraitVisibility.Toggle();
                    GUI.enabled = enabledBeforePortrait;
                }
                int displayRow = _page == 1 ? 9 : 0;
                for (int i=0; i<_rows.Count; i++)
                {
                    Row r=_rows[i];
                    if (r.Page != _page) continue;
                    Rect row=new Rect(4f,displayRow++*rowH,inner.width-8f,rowH-6f);
                    if (r.Entry == null) GUI.Label(row,r.Label,_section);
                    else DrawBindingRow(i,row,inner.width);
                }
                if (_page == 0)
                {
                    GUI.Label(new Rect(10f,displayRow++*rowH,inner.width-20f,rowH), "普通调试", _section);
                    bool oldEnabled = GUI.enabled;
                    GUI.enabled = oldEnabled && !_capturing;
                    GUI.Label(new Rect(10f,displayRow*rowH,inner.width-130f,rowH), "小骑士：启用碰撞伤害", _label);
                    if (GUI.Button(new Rect(inner.width-110f,displayRow*rowH,100f,rowH-8f), KnightContactDebug.Enabled ? "开" : "关", _button)) KnightContactDebug.Toggle();
                    GUI.enabled = oldEnabled;
                }
                GUI.EndScrollView();
                if (showFooter)
                    GUI.Label(new Rect(panel.x+16f,panel.yMax-30f,panel.width-32f,20f),
                        _capturing ? KicPanelText.HintCapture : "调试会改变游戏状态，请谨慎操作。", _small);
            }
            catch (Exception)
            {
            }
            finally { GUI.matrix = Matrix4x4.identity; }
        }

        private static Texture2D Solid(Color color)
        {
            var texture=new Texture2D(1,1);
            texture.SetPixel(0,0,color);
            texture.Apply();
            return texture;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old=GUI.color;
            GUI.color=color;
            GUI.DrawTexture(rect,Texture2D.whiteTexture);
            GUI.color=old;
        }

        private void DrawBindingRow(int i, Rect row, float innerWidth)
        {
            Row r = _rows[i];
            KeyCode cur = KeyConfig.Parse(r.Entry.Value, r.Fallback);
            string shown = _capturing && _captureIndex == i
                ? KicPanelText.WaitingKey
                : (cur == KeyCode.None ? KicPanelText.NotSet : cur.ToString());
            GUI.Label(new Rect(row.x + 6f, row.y, innerWidth - 220f, row.height), r.Label, _label);
            Fill(new Rect(row.x+row.width-214f,row.y,112f,row.height),new Color(0.08f,0.13f,0.18f));
            GUI.Label(new Rect(row.x + row.width - 210f, row.y, 110f, row.height), shown, _key);
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
