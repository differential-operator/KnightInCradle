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
        private static Lang? _loggedLang;
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
                    // 等游戏把语言表读进来之后再记录（启动早期那一瞬间还没定语言，记了会误导）
                    bool initted = false;
                    try
                    {
                        initted = TX.isInitted;
                    }
                    catch (Exception)
                    {
                    }
                    if (initted && (!_logged || _loggedLang != _cachedLang))
                    {
                        _logged = true;
                        _loggedLang = _cachedLang;
                        string fam = CurrentFamilyKey();
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
                Lang? byFam = ByFamily(CurrentFamilyKey());
                if (byFam.HasValue)
                {
                    return byFam.Value;
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

        /// <summary>
        /// 当前**实际生效**的语言族。注意：游戏在设置里换语言是调 `TX.changeFamily()`，
        /// 它只改内部的 TxCon，不改 `default_family`，所以要以 `getCurrentFamilyName()` 为准。
        /// </summary>
        private static string CurrentFamilyKey()
        {
            try
            {
                string fam = TX.getCurrentFamilyName();
                if (!string.IsNullOrEmpty(fam))
                {
                    return fam;
                }
            }
            catch (Exception)
            {
            }
            try
            {
                return TX.default_family;
            }
            catch (Exception)
            {
                return "";
            }
        }

        private static Lang? ByFamily(string fam)
        {
            switch (fam)
            {
                case "_":
                case "ja": return Lang.Ja;
                case "en": return Lang.En;
                case "ko-kr": return Lang.Ko;
                case "th": return Lang.Th;
                case "zh-cn":
                case "zh-cnB": return Lang.ZhCn;
                case "zh-tc": return Lang.ZhTc;
                default: return null;
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
