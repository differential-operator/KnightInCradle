using System;
using UnityEngine;
using XX;

namespace KnightInCradle.CharmUi
{
    /// <summary>
    /// 模组文本的多语言支持（需求 2026-10-01）：跟随 AIC 自己的语言设置，
    /// AIC 支持 6 种语言：日本語(_) / English(en) / 한국어(ko-kr) / ไทย(th) / 简体中文(zh-cn) / 繁體中文(zh-tc)。
    ///
    /// 取词约定：**zh-cn 直接用现有中文文本**（避免两处中文不一致），其余语言查翻译表，缺项回落到中文。
    /// 当前语言从游戏自己的字段 `TX.default_family` 读（每秒重读一次，游戏里切语言后会自动跟上）。
    /// </summary>
    internal static class KicL10n
    {
        public enum Lang
        {
            Ja = 0,
            En = 1,
            Ko = 2,
            Th = 3,
            ZhCn = 4,
            ZhTc = 5,
        }

        /// <summary>翻译表里"5 语言"的排列顺序（不含 zh-cn）。</summary>
        public const int FiveCount = 5;

        /// <summary>主动触发一次语言检测（启动时调一次，方便在日志里确认当前语言）。</summary>
        public static void LogOnce()
        {
            try
            {
                Lang _ = Current;
            }
            catch (Exception)
            {
            }
        }

        private static bool _cached;
        private static bool _logged;
        private static Lang _cachedLang;
        private static float _cachedAt = -10f;

        /// <summary>
        /// 按当前语言给出一组系统字体候选（Unity 按顺序取用，第一个可用的优先）。
        /// 目的是让泰语/韩语/日语/繁体中文的界面文字都能找到对应字形，别显示成方块。
        /// </summary>
        public static string[] FontCandidates()
        {
            switch (Current)
            {
                case Lang.Ja:
                    return new[] { "Yu Gothic UI", "Meiryo", "MS Gothic", "Microsoft YaHei", "Arial" };
                case Lang.Ko:
                    return new[] { "Malgun Gothic", "Gulim", "Batang", "Microsoft YaHei", "Arial" };
                case Lang.Th:
                    return new[] { "Leelawadee UI", "Tahoma", "Microsoft YaHei", "Arial" };
                case Lang.ZhTc:
                    return new[] { "Microsoft JhengHei", "MingLiU", "Microsoft YaHei", "Arial" };
                case Lang.En:
                    return new[] { "Segoe UI", "Arial", "Microsoft YaHei" };
                default:
                    return new[] { "Microsoft YaHei", "SimHei", "SimSun", "Segoe UI", "Arial" };
            }
        }

        public static Lang Current
        {
            get
            {
                try
                {
                    if (_cached && Time.unscaledTime - _cachedAt < 1f)
                    {
                        return _cachedLang;
                    }
                    _cachedLang = Detect();
                    _cached = true;
                    _cachedAt = Time.unscaledTime;
                    if (!_logged)
                    {
                        _logged = true;
                        string fam = "";
                        try
                        {
                            fam = TX.default_family;
                        }
                        catch (Exception)
                        {
                        }
                        KnightInCradlePlugin.PluginLog?.LogInfo(
                            "[KIC][语言] AIC 语言族=" + (string.IsNullOrEmpty(fam) ? "(未知)" : fam) +
                            " → 模组文本语言=" + _cachedLang);
                    }
                    return _cachedLang;
                }
                catch (Exception)
                {
                    return Lang.ZhCn;
                }
            }
        }

        private static Lang Detect()
        {
            try
            {
                string fam = TX.default_family;
                // "_" 是"默认族（日语）"，但**启动早期**语言表还没加载，`default_family` 的初始值就是 "_"，
                // 所以只有等语言表加载完之后，才能把 "_" 当成日语。
                bool famLoaded = false;
                try
                {
                    famLoaded = TX.isInitted;
                }
                catch (Exception)
                {
                }
                if (!string.IsNullOrEmpty(fam) && fam != "_")
                {
                    switch (fam)
                    {
                        case "en": return Lang.En;
                        case "ko-kr": return Lang.Ko;
                        case "th": return Lang.Th;
                        case "zh-cn": return Lang.ZhCn;
                        case "zh-tc": return Lang.ZhTc;
                    }
                }
                if (fam == "_" && famLoaded)
                {
                    return Lang.Ja; // 语言表已就绪且仍是默认族 = 游戏按规则选了日语
                }
            }
            catch (Exception)
            {
            }
            // 兜底：跟系统语言走（和游戏自己的 TX.considerCurrentFamilyWithTimezone 同一口径）
            try
            {
                switch (Application.systemLanguage)
                {
                    case SystemLanguage.Japanese: return Lang.Ja;
                    case SystemLanguage.Korean: return Lang.Ko;
                    case SystemLanguage.Thai: return Lang.Th;
                    case SystemLanguage.ChineseSimplified: return Lang.ZhCn;
                    case SystemLanguage.ChineseTraditional:
                    case SystemLanguage.Chinese: return Lang.ZhTc;
                    default: return Lang.En;
                }
            }
            catch (Exception)
            {
                return Lang.ZhCn;
            }
        }

        /// <summary>六语言取词（zh-cn 走传进来的那一条，保证与现有中文一致）。</summary>
        public static string Pick(string ja, string en, string ko, string th, string zhCn, string zhTc)
        {
            switch (Current)
            {
                case Lang.Ja: return Fallback(ja, zhCn);
                case Lang.En: return Fallback(en, zhCn);
                case Lang.Ko: return Fallback(ko, zhCn);
                case Lang.Th: return Fallback(th, zhCn);
                case Lang.ZhTc: return Fallback(zhTc, zhCn);
                default: return zhCn;
            }
        }

        /// <summary>五语言表取词：数组顺序 <see cref="Lang"/> = Ja/En/Ko/Th/ZhTc，zh-cn 直接用传入文本。</summary>
        public static string Pick5(string[] five, string zhCn)
        {
            if (Current == Lang.ZhCn)
            {
                return zhCn;
            }
            if (five == null || five.Length < FiveCount)
            {
                return zhCn;
            }
            int idx = (int)Current;
            if (idx < 0 || idx >= FiveCount)
            {
                return zhCn;
            }
            string s = five[idx];
            return string.IsNullOrEmpty(s) ? zhCn : s;
        }

        private static string Fallback(string s, string zhCn)
        {
            return string.IsNullOrEmpty(s) ? zhCn : s;
        }
    }
}
