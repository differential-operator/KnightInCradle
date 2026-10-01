namespace KnightInCradle.CharmUi
{
    /// <summary>
    /// 键位设置面板的 6 语言文本（需求 2026-10-01）：
    /// 每种语言一条，选词交给 <see cref="KicL10n.Pick"/>（顺序：日/英/韩/泰/简/繁）。
    /// </summary>
    internal static class KicPanelText
    {
        public static string Title(string key)
        {
            return KicL10n.Pick(
                "キー設定（" + key + " で開閉）",
                "Key settings (" + key + " to open/close)",
                "키 설정 (" + key + " 열기/닫기)",
                "ตั้งค่าปุ่ม (" + key + " เปิด/ปิด)",
                "键位设置（" + key + " 打开 / 关闭）",
                "鍵位設定（" + key + " 開啟 / 關閉）");
        }

        public static string HintNormal => KicL10n.Pick(
            "「変更」を押してから任意のキーを押してください。変更はすぐ反映され cfg に保存されます。AIC 本体のキーはゲーム内の設定で変更してください。",
            "Click Rebind, then press any key. Changes take effect immediately and are saved to the cfg. AIC's own keys are changed in the game's settings.",
            "「변경」을 누른 뒤 아무 키나 누르세요. 변경은 즉시 적용되고 cfg에 저장됩니다. AIC 자체 키는 게임 설정에서 변경하세요.",
            "กด \"เปลี่ยน\" แล้วกดปุ่มใดก็ได้ การเปลี่ยนมีผลทันทีและบันทึกลง cfg ส่วนปุ่มของ AIC เองให้แก้ในตั้งค่าของเกม",
            "点「改键」后按任意键；改完立即生效并已保存到 cfg。AIC 自身按键请到游戏设置里改。",
            "點「改鍵」後按任意鍵；改完立即生效並已存到 cfg。AIC 自身按鍵請到遊戲設定裡改。");

        public static string HintCapture => KicL10n.Pick(
            "新しいキー / マウスボタンを押してください…（Esc でキャンセル）",
            "Press the new key / mouse button… (Esc to cancel)",
            "새 키 / 마우스 버튼을 누르세요… (Esc 취소)",
            "กดปุ่มใหม่ / ปุ่มเมาส์… (Esc ยกเลิก)",
            "请按下新的按键 / 鼠标键…（Esc 取消）",
            "請按下新的按鍵 / 滑鼠鍵…（Esc 取消）");

        public static string Rebind => KicL10n.Pick("変更", "Rebind", "변경", "เปลี่ยน", "改键", "改鍵");

        public static string Reset => KicL10n.Pick("既定", "Default", "기본값", "ค่าเริ่มต้น", "默认", "預設");

        public static string WaitingKey => KicL10n.Pick(
            "（キー入力待ち…）", "(waiting for key…)", "(키 입력 대기…)", "(รอปุ่ม…)",
            "（等待按键…）", "（等待按鍵…）");

        public static string NotSet => KicL10n.Pick("未設定", "Not set", "미설정", "ยังไม่ตั้ง", "未设置", "未設定");

        public static string SwitchChar => KicL10n.Pick(
            "ノエル / 騎士 切り替え", "Switch Noel / Knight", "노엘 / 기사 전환",
            "สลับ โนเอล / อัศวิน", "切换 小骑士 / 诺艾尔", "切換 小騎士 / 諾艾爾");

        public static string CharmUi => KicL10n.Pick(
            "護符画面 開閉", "Charm menu", "호부 화면", "เมนูเครื่องราง", "护符界面 开关", "護符介面 開關");

        public static string SeriousMode => KicL10n.Pick(
            "シリアスモード 開閉", "Serious mode", "진지 모드", "โหมดจริงจัง", "认真模式 开关", "認真模式 開關");

        public static string Taunt => KicL10n.Pick("挑発", "Taunt", "도발", "ยั่วยุ", "挑衅", "挑釁");

        public static string CritStacks => KicL10n.Pick(
            "会心 +100（デバッグ）", "Crit +100 (debug)", "회심 +100 (디버그)",
            "คริ +100 (ดีบัก)", "会心 +100（调试）", "會心 +100（除錯）");

        public static string SectionKnight => KicL10n.Pick(
            "── 騎士：移動 / アクション ──", "── Knight: movement / actions ──",
            "── 기사: 이동 / 동작 ──", "── อัศวิน: การเคลื่อนที่ / การกระทำ ──",
            "── 小骑士：移动 / 动作 ──", "── 小騎士：移動 / 動作 ──");

        public static string MoveLeft => KicL10n.Pick("左移動", "Move left", "왼쪽 이동", "เดินซ้าย", "左移", "左移");
        public static string MoveRight => KicL10n.Pick("右移動", "Move right", "오른쪽 이동", "เดินขวา", "右移", "右移");
        public static string Jump => KicL10n.Pick("ジャンプ", "Jump", "점프", "กระโดด", "跳跃", "跳躍");
        public static string Dash => KicL10n.Pick("ダッシュ", "Dash", "대시", "แดช", "冲刺", "衝刺");

        public static string Attack => KicL10n.Pick(
            "攻撃 / 溜め斬り", "Attack / charge slash", "공격 / 차지 베기",
            "โจมตี / ฟันชาร์จ", "攻击 / 蓄力劈砍", "攻擊 / 蓄力劈砍");

        public static string Focus => KicL10n.Pick(
            "集中 / 回復", "Focus / heal", "집중 / 회복", "รวมสมาธิ / ฟื้นฟู", "聚焦 / 回血", "聚焦 / 回血");

        public static string Fireball => KicL10n.Pick("魔弾", "Fireball", "파이어볼", "ลูกไฟ", "法球", "法球");

        public static string DreamNail => KicL10n.Pick("夢の釘", "Dream nail", "꿈의 못", "ตะปูความฝัน", "梦钉", "夢釘");

        public static string LookUp => KicL10n.Pick(
            "上見 / 魔法・上", "Look up / spell up", "위 보기 / 마법 위", "เงยหน้า / เวทขึ้น", "抬头 / 法术上", "抬頭 / 法術上");

        public static string LookDown => KicL10n.Pick(
            "下見 / 魔法・下", "Look down / spell down", "아래 보기 / 마법 아래", "ก้มหน้า / เวทลง", "低头 / 法术下", "低頭 / 法術下");

        public static string SuperDash => KicL10n.Pick(
            "スーパーダッシュ（追加キー）", "Super dash (extra key)", "슈퍼 대시 (추가 키)",
            "ซูเปอร์แดช (ปุ่มเสริม)", "超级冲刺（额外键）", "超級衝刺（額外鍵）");

        public static string SectionPose => KicL10n.Pick(
            "── モーション確認（ポーズブラウザ） ──", "── Pose browser ──",
            "── 포즈 브라우저 ──", "── ดูท่ากิริยา (pose browser) ──",
            "── 查看动作（姿势浏览器） ──", "── 檢視動作（姿勢瀏覽器） ──");

        public static string PoseNext => KicL10n.Pick(
            "モーション確認：次のポーズ", "Pose browser: next pose", "포즈 브라우저: 다음 포즈",
            "ดูกิริยา: ท่าถัดไป", "查看动作：下一个姿势", "檢視動作：下一個姿勢");

        public static string PosePrev => KicL10n.Pick(
            "モーション確認：前のポーズ", "Pose browser: previous pose", "포즈 브라우저: 이전 포즈",
            "ดูกิริยา: ท่าก่อนหน้า", "查看动作：上一个姿势", "檢視動作：上一個姿勢");

        public static string PoseOff => KicL10n.Pick(
            "モーション確認：終了", "Pose browser: close", "포즈 브라우저: 종료",
            "ดูกิริยา: ปิด", "查看动作：关闭浏览", "檢視動作：關閉瀏覽");

        public static string SectionPanel => KicL10n.Pick(
            "── このパネル ──", "── This panel ──", "── 이 패널 ──", "── แผงนี้ ──", "── 本面板 ──", "── 本面板 ──");

        public static string PanelToggle => KicL10n.Pick(
            "キー設定パネル 開閉", "Open / close this panel", "이 패널 열기 / 닫기",
            "เปิด / ปิดแผงนี้", "打开 / 关闭键位面板", "開啟 / 關閉鍵位面板");

        // ---------------- 护符界面里写在 charm_ui/layout.json 中的固定文字 ----------------
        // 这些字是布局文件自带的（不是代码里的），这里按元素路径替换成当前语言。

        public static string Equipped => KicL10n.Pick(
            "装備中", "Equipped", "장착 중", "ติดตั้งอยู่", "已装备", "已裝備");

        public static string GgNail => KicL10n.Pick("骨釘", "Nail", "못", "ตะปู", "骨钉", "骨釘");
        public static string GgShell => KicL10n.Pick("殻", "Shell", "껍질", "เปลือก", "外壳", "外殼");
        public static string GgCharm => KicL10n.Pick("護符", "Charm", "호부", "เครื่องราง", "护符", "護符");
        public static string GgSoul => KicL10n.Pick("魂", "Soul", "영혼", "วิญญาณ", "灵魂", "靈魂");

        /// <summary>把布局文件里的固定文字按元素路径换成当前语言（不认识的原样返回）。</summary>
        public static string LayoutText(string path, string zh)
        {
            switch (path)
            {
                case "CharmUi/text/charm_equipped": return Equipped;
                case "CharmUi/GG_description/nail": return GgNail;
                case "CharmUi/GG_description/mask": return GgShell;
                case "CharmUi/GG_description/charm": return GgCharm;
                case "CharmUi/GG_description/soul": return GgSoul;
                default: return zh;
            }
        }
    }
}
