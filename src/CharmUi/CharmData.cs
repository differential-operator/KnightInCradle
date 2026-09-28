using System;
using nel;
using UnityEngine;

namespace KnightInCradle.CharmUi
{
    /// <summary>单个护符数据（由 护符.txt 生成）。</summary>
    public sealed class CharmData
    {
        public int Id;
        public string IconFile;
        public string Name;
        /// <summary>解锁后"解锁：…"那一行的正文（含 `\n\n` 分隔的风味文本）。</summary>
        public string Desc;
        /// <summary>费用显示。&lt; 0 = 显示"？"（37〜41 这些"童话里的传说护符"）。</summary>
        public int Cost;
        /// <summary>小骑士（空洞骑士）那一套的名称/描述/费用；诺艾尔侧留空表示沿用 Name/Desc/Cost。</summary>
        public string KName;
        public string KDesc;
        public int KCost;
        /// <summary>true = 可选中但**不能佩戴**（需求 2026-09-27：37/38/39/41）。</summary>
        public bool NoEquip;
    }

    /// <summary>41 个护符静态数据（40 虚空之心为固定护符，cost=0 不可卸下）。</summary>
    public static class CharmDatabase
    {
        public const int FixedCharmId = 40;
        /// <summary>
        /// 护符槽上限：11 = 初始 3 + 开宝箱最多 8（这两部分等解锁/宝箱系统上线后再做成动态），
        /// 再**加上**炼金做出来的护符槽（三种各 +1，见 `CharmSlotCrafting.CraftedSlotBonus`）。
        /// </summary>
        /// <summary>护符槽上限（含基础 3、开箱最多 +8、预留 +3 = 14）。</summary>
        public const int MaxNotchCapacity = 14;
        public static int NotchCapacity => 3 + Mathf.Min(MaxNotchCapacity - 3, OpenedChestCount / 4);
        /// <summary>小骑士那一套的护符槽上限（需求 2026-09-27：固定 12）。</summary>
        public const int KnightNotchCapacity = 12;

        /// <summary>按角色取护符槽上限：小骑士 = 12；诺艾尔 = 3 + min(11, 开箱数/4)。</summary>
        public static int NotchCapacityFor(CharmOwner owner)
        {
            return owner == CharmOwner.Knight ? KnightNotchCapacity : NotchCapacity;
        }

        /// <summary>按角色取名称/描述/费用（小骑士用 K* 那一份）。</summary>
        public static string NameOf(CharmData cd, CharmOwner owner)
        {
            if (cd == null) return "";
            return owner == CharmOwner.Knight && !string.IsNullOrEmpty(cd.KName) ? cd.KName : cd.Name;
        }

        public static string DescOf(CharmData cd, CharmOwner owner)
        {
            if (cd == null) return "";
            return owner == CharmOwner.Knight && !string.IsNullOrEmpty(cd.KDesc) ? cd.KDesc : cd.Desc;
        }

        public static int CostOf(CharmData cd, CharmOwner owner)
        {
            if (cd == null) return 0;
            return owner == CharmOwner.Knight && cd.KName != null ? cd.KCost : cd.Cost;
        }


        /// <summary>
        /// 已开启的宝箱总数（游戏自己的成就计数 `ACHIVE.MENT.treasure_total_obtain`，
        /// 就是"所有地图上开过的宝箱数"）。
        /// </summary>
        public static int OpenedChestCount
        {
            get
            {
                try
                {
                    return (int)COOK.CurAchive.Get(ACHIVE.MENT.treasure_total_obtain);
                }
                catch (Exception)
                {
                    return 0;
                }
            }
        }

        /// <summary>
        /// 已解锁的护符数量（"坚固护符槽"的制作条件用它）。
        /// ⚠ 真正的解锁机制还没做：现在按"全部已解锁（36 个真正可用的护符）"计，
        /// 等解锁系统落地后改成读 `CharmSave` 里的解锁集合即可。
        /// </summary>
        public static int UnlockedCharmCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < All.Length; i++)
                {
                    if (All[i].Id >= 1 && All[i].Id <= 36 && !IsLocked(All[i].Id))
                    {
                        n++;
                    }
                }
                return n;
            }
        }
        /// <summary>寻神者模式选择器（自限机制，不占护符费用；仅能通过顶部 sign 装配）。</summary>
        public const int GgSelectorId = 42;
        public static readonly CharmData[] All = new CharmData[]
        {
            // 诺艾尔侧的文本与费用按 `docs/护符效果描述.md` 重写：
            //   LockedText = 面板上"未解锁：…"那一行；Desc = "解锁：…"那一行（含 \n\n 后的风味文本）。
            new CharmData { Id = 1, KName = "任性的指南针", KDesc = "允许持有者在地图上自由传送。", KCost = 1, IconFile = "1_compass", Name = "任性的指南针", Cost = 1,
                Desc = "允许漫游者在地图上随意移动。" },
            new CharmData { Id = 2, KName = "蜂群集结", KDesc = "小蜂群会为持有者收集魔力草中的所有灵魂。\n\n蜂群也会帮助持有者捡起周围的物品。\n\n适合那些无论多细小的东西都不愿意丢下的人。", KCost = 1, IconFile = "2_collector", Name = "蜂群集结", Cost = 1,
                Desc = "自动收集周围散落的物品。\n\n适合那些无论多细小的东西都不愿意丢下的人。" },
            new CharmData { Id = 3, KName = "坚硬外壳", KDesc = "增加受伤后的无敌时间。\n\n使其更容易在危险情形下逃脱。", KCost = 2, IconFile = "3_sturdy", Name = "坚硬外壳", Cost = 3,
                Desc = "使持有者具有更强的韧性，使持有者获得一定不受伤害的时间。\n\n使其更容易在危险情形下逃脱。" },
            new CharmData { Id = 4, KName = "灵魂捕手", KDesc = "萨满曾用它来从周围的世界中吸取更多灵魂。\n\n用骨钉劈砍敌人能获得更多灵魂", KCost = 2, IconFile = "4_soul_catcher", Name = "灵魂捕手", Cost = 2,
                Desc = "传说蜗牛萨满曾用它来从周围的世界中吸取灵魂\n\n用法术攻击敌人时获得额外魔力。" },
            new CharmData { Id = 5, KName = "萨满之石", KDesc = "据说包含前代萨满的学识。\n\n使造成的法术伤害提高。", KCost = 3, IconFile = "5_shaman", Name = "萨满之石", Cost = 4,
                Desc = "传说包含了萨满学识的神秘护符\n\n提高法术的威力，对敌人造成更多伤害。" },
            new CharmData { Id = 6, KName = "噬魂者", KDesc = "被遗忘的萨满神器，用来从活着的生物身上吸取灵魂。\n\n大大增加用骨钉劈砍敌人获得的灵魂。", KCost = 4, IconFile = "6_soul_eater", Name = "噬魂者", Cost = 4,
                Desc = "被遗忘的萨满神器，用来从活着的生物身上吸取灵魂。\n\n大大增加用法术攻击敌人时获得的魔力数量。" },
            new CharmData { Id = 7, KName = "冲刺大师", KDesc = "有着被称为“冲刺大师”的古怪虫子形象。\n\n持有者将能够更频繁地冲刺，也能向下冲刺。", KCost = 2, IconFile = "7_rush_master", Name = "冲刺大师", Cost = 2,
                Desc = "在童话里被称为“冲刺大师”的古怪虫子形象。\n\n使持有者能够持续冲刺，但冲刺的速度略微降低。" },
            new CharmData { Id = 8, KName = "飞毛腿", KDesc = "有着被称为“飞毛腿”的古怪虫子形象。\n\n提高持有者的移动速度。", KCost = 1, IconFile = "8_runner", Name = "飞毛腿", Cost = 1,
                Desc = "在童话里被称为“飞毛腿”的古怪虫子形象。\n\n增加持有者的移动速度，使其能避开危险或追上敌人。" },
            new CharmData { Id = 9, KName = "幼虫之歌", KDesc = "包含被解救的幼虫的感激。\n\n受到伤害时，获得灵魂。", KCost = 1, IconFile = "9_little_bug", Name = "幼虫之歌", Cost = 1,
                Desc = "在童话里由幼虫的感激凝聚而成的护符。\n\n受到伤害时获得灵魂。" },
            new CharmData { Id = 10, KName = "蜕变挽歌", KDesc = "包含将要步入生命的下一个阶段的幼虫的感激。\n\n当持有者处于满血状态时，能从骨钉中射出白热能量束，对前方一定距离的敌人造成伤害。", KCost = 3, IconFile = "10_big_bug", Name = "蜕变挽歌", Cost = 4,
                Desc = "由直面恐惧的勇气凝聚而成，为武器灌输神圣的力量。\n\n使持有者的攻击能够向前发射白热能量束。" },
            new CharmData { Id = 11, KName = "坚固心脏", KDesc = "提高持有者的血量上限。\n\n这个护符很坚固，不会破碎。", KCost = 2, IconFile = "11_hard_heart", Name = "坚固心脏", Cost = 2,
                Desc = "增加持有者的生命值，承受更多伤害。\n\n因为怕疼就全点生命了。" },
            new CharmData { Id = 12, KName = "坚固贪婪", KDesc = "大幅提升背包上限，每持有4个物品，持有者造成的伤害降低0.75%。击杀敌人会掉落丰厚的资源。\n\n这个护符很坚固，不会破碎。", KCost = 2, IconFile = "12_hard_geo", Name = "坚固贪婪", Cost = 2,
                Desc = "大大提高持有者的背包上限，击杀敌人时能获取更加丰富的物资\n\n用未知的神秘技术改造了背包，目前没发现风险......应该吧。" },
            new CharmData { Id = 13, KName = "坚固力量", KDesc = "使骨钉的伤害提升。\n\n这个护符很坚固，不会破碎。", KCost = 3, IconFile = "13_hard_power", Name = "坚固力量", Cost = 4,
                Desc = "使持有者的近战攻击造成更多伤害。\n\n从一场又一场战斗中积累起来的经验。" },
            new CharmData { Id = 14, KName = "法术扭曲者", KDesc = "反映了灵魂圣所掌握灵魂力量的欲望。\n\n法术的灵魂消耗降低。", KCost = 2, IconFile = "14_spell_twister", Name = "法术扭曲者", Cost = 3,
                Desc = "从周围的空气中凝聚魔力，从而减少法术的消耗。\n\n十分可怕的护符，当然，对于被攻击的魔物来说。" },
            new CharmData { Id = 15, KName = "稳定之体", KDesc = "攻击时不会产生后坐力。\n\n使持有者保持稳定，持续攻击。", KCost = 1, IconFile = "15_stable", Name = "稳定之体", Cost = 1,
                Desc = "使持有者碰到魔物不会摔倒。\n\n能够保持稳定，持续攻击。" },
            new CharmData { Id = 16, KName = "沉重之击", KDesc = "阵亡战士的骨钉形成，渴望再次被拾起。\n\n普通攻击有8%的概率直接击杀目标。", KCost = 2, IconFile = "16_heavy", Name = "沉重之击", Cost = 2,
                Desc = "连续对魔物造成伤害时，攻击力得到提升。\n\n真的很重。" },
            new CharmData { Id = 17, KName = "快速劈砍", KDesc = "诞生于那些被融合的不完美的废弃骨钉。\n\n允许持有者更快的挥动骨钉。", KCost = 3, IconFile = "17_fast_slash", Name = "快速劈砍", Cost = 3,
                Desc = "允许持有者更快地挥动法杖。\n\n熟能生巧。" },
            new CharmData { Id = 18, KName = "修长之钉", KDesc = "允许打击更远处的敌人。\n\n增加骨钉攻击范围。", KCost = 2, IconFile = "18_long_nail", Name = "修长之钉", Cost = 2,
                Desc = "增加持有者法杖的攻击范围，允许打击更远处的敌人。\n\n有时候就差一点点。" },
            new CharmData { Id = 19, KName = "骄傲印记", KDesc = "由螳螂部落慷慨赠予他们尊敬的人。\n\n大大增加骨钉的攻击范围。", KCost = 3, IconFile = "19_pride", Name = "骄傲印记", Cost = 3,
                Desc = "大大增加持有者法杖的攻击范围，使其能够从更远处打击敌人。\n\n应得的荣誉。" },
            new CharmData { Id = 20, KName = "亡者之怒", KDesc = "体现了那些将死之人的愤怒和英勇。\n\n接近死亡时，使骨钉的伤害大幅提升。", KCost = 2, IconFile = "20_fury", Name = "亡者之怒", Cost = 4,
                Desc = "由苦痛、不甘与愤怒凝聚而成。\n\n当接近死亡时，持有者的力量会全面爆发，但仍然会达到身体的极限。" },
            new CharmData { Id = 21, KName = "苦痛荆棘", KDesc = "感受持有者的痛苦并鞭打周围的世界。\n\n受到伤害时，对周围的敌人造成伤害。", KCost = 1, IconFile = "21_thorns", Name = "苦痛荆棘", Cost = 2,
                Desc = "感受持有者的痛苦并鞭打周围的世界。\n\n当受到伤害时，对附近的敌人双倍奉还。" },
            new CharmData { Id = 22, KName = "巴尔德之壳", KDesc = "在凝聚灵魂时，产生硬壳保护它的持有者。\n\n凝聚时抵挡攻击，最多4次。", KCost = 2, IconFile = "22_baldur_shell", Name = "巴尔德之壳", Cost = 3,
                Desc = "在咏唱魔法时，产生硬壳保护它的持有者。\n\n外壳不是无法破坏的，吸收太多的伤害后将会破碎。一定时间后才能恢复。" },
            new CharmData { Id = 23, KName = "吸虫之巢", KDesc = "吸虫之母肠道诞生的活的护符。\n\n将复仇之魂法术变成一群不稳定的幼小吸虫。", KCost = 3, IconFile = "23_nest", Name = "吸虫之巢", Cost = 3,
                Desc = "将纯白之箭和聚能火球法术变成一群不稳定的幼小吸虫。" },
            new CharmData { Id = 24, KName = "防御者纹章", KDesc = "圣巢国王赋予最忠诚的骑士的独特护符。虽然有些刮痕和污渍，但依旧保存得很好。\n\n这个护符汲取了森林中凶猛魔物的力量，小型魔物难以承受这么强大的魔力。", KCost = 1, IconFile = "24_shelter", Name = "防御者纹章", Cost = 1,
                Desc = "仿照森林中最强大的魔物制成的小法阵。\n\n法阵富含魔力，但对于饥饿的魔物来说有些过量了。" },
            new CharmData { Id = 25, KName = "发光子宫", KDesc = "从持有者的身上汲取灵魂，用来产生幼崽。幼崽会飞向敌人保护持有者。\n\n幼崽体内同时具有光和暗两股力量，极不稳定。", KCost = 2, IconFile = "25_uterus", Name = "发光子宫", Cost = 2,
                Desc = "巢厄体内用来繁殖小剑山的器官\n\n经过人为干预后，这些剑山没有了进食的意愿，并会牺牲自己来保护持有者。" },
            new CharmData { Id = 26, KName = "快速聚集", KDesc = "包含水晶镜片的护符。\n\n降低凝聚所需时间。", KCost = 3, IconFile = "26_fast_gather", Name = "快速聚集", Cost = 3,
                Desc = "姐姐做的紫水晶护符，非常好看，而且相当有实用价值，配合一些咏唱快的法杖能瞬间释放法术\n\n也许那时候姐姐戴了这个护符？" },
            new CharmData { Id = 27, KName = "深度聚集", KDesc = "在水晶内长时间自然形成。从周围的空气中吸取灵魂。\n\n凝聚能获得更多血量，但延长凝聚所需时间。开启宝箱时，轮转速度降低75%", KCost = 4, IconFile = "27_deep_gather", Name = "深度聚集", Cost = 4,
                Desc = "姐姐做的紫水晶护符，感觉充满了魔力。姐姐总说紫水晶中蕴含着强大的力量......童话里也这么说，难道水晶山的故事是真的？\n\n降低咏唱速度，但咏唱时能缓慢恢复生命，且使造成的伤害提升" },
            new CharmData { Id = 28, KName = "生命血之心", KDesc = "包含一个活着的核心，浸出宝贵的生命血。\n\n在长椅上休息时回复2格生命血血量。", KCost = 2, IconFile = "28_blue_heart_1", Name = "生命血之心", Cost = 2,
                Desc = "异界的护符，能够将少部分血量置换为魔力。\n\n感觉护符里有东西在跳动..." },
            new CharmData { Id = 29, KName = "生命血核心", KDesc = "包含一个活着的核心，流出宝贵的生命血。\n\n在长椅上休息时回复4格生命血血量。", KCost = 3, IconFile = "29_blue_heart_2", Name = "生命血核心", Cost = 3,
                Desc = "异界的护符，能够将部分血量置换为魔力。\n\n感觉护符里有东西在看自己..." },
            new CharmData { Id = 30, KName = "乔尼的祝福", KDesc = "由仁慈的异教徒乔尼给予的祝福。使所有血量变成生命血，并提升血量上限。\n\n持有者无法聚集灵魂回复生命。", KCost = 4, IconFile = "30_Johnny", Name = "乔尼的祝福", Cost = 4,
                Desc = "异界的护符，能够将全部血量置换为魔力。\n\n感觉全身的器官都被控制，但似乎还不错？" },
            new CharmData { Id = 31, KName = "蜂巢之血", KDesc = "蜂巢中珍贵的金色硬化花蜜块。\n\n使持有者每10秒能够回复1血量", KCost = 4, IconFile = "31_hive", Name = "蜂巢之血", Cost = 4,
                Desc = "蜂巢中珍贵的金色硬化花蜜块。\n\n使持有者能够缓慢恢复生命。" },
            new CharmData { Id = 32, KName = "蘑菇孢子", KDesc = "由活的真菌物质组成。\n\n凝聚将释放出一片孢子云，对敌人持续造成伤害。", KCost = 1, IconFile = "32_mushroom", Name = "蘑菇孢子", Cost = 1,
                Desc = "由活的真菌物质组成，证明了你在蘑菇中的地位。\n\n可以和蘑菇们愉快的玩耍了！" },
            new CharmData { Id = 33, KName = "锋利之影", KDesc = "含有被禁用的法术，能将影子转换为致命的武器。\n\n使用暗影冲刺穿过敌人会对其造成伤害。将暗影冲刺的速度提升40%。", KCost = 2, IconFile = "33_shadow", Name = "锋利之影", Cost = 4,
                Desc = "含有被禁用的法术，短暂蓄力后能将影子转换为致命的武器。\n\n你无法使用护盾或者闪避。" },
            new CharmData { Id = 34, KName = "乌恩之形", KDesc = "让持有者展现出内在的乌恩形态\n\n凝聚时可以在地面上移动。", KCost = 2, IconFile = "34_wuen", Name = "乌恩之形", Cost = 2,
                Desc = "让持有者展现出内在的乌恩形态，使其在蹲下或爬行时缓慢恢复生命，同时使持有者能够在水下畅通无阻。\n\n被魔物抓到的话，这辈子就完了吧...不过蹲下的时候，魔物好像看不见我了？" },
            new CharmData { Id = 35, KName = "骨钉大师的荣耀", KDesc = "包含一个骨钉大师的激情、技能和遗憾。\n\n使骨钉技艺的蓄力时间由1.35秒降至0.75秒。", KCost = 1, IconFile = "35_nail_master", Name = "骨钉大师的荣耀", Cost = 5,
                Desc = "以抛弃魔法为代价，加强普通攻击的力量，并能在短暂蓄力后使用强力技能。\n\n童话里的骨钉大师，然而现实里没有骨钉，只能做法杖大师了。" },
            new CharmData { Id = 36, KName = "编织者之歌", KDesc = "一个缠丝的护符，蕴含着那些离开圣巢返回家乡的编织者所留下的离别之歌。\n\n召唤三只小小的编织者幼体攻击敌人。这些编织者在故乡的灾难中幸存，拥有更强的力量。", KCost = 2, IconFile = "36_spider", Name = "编织者之歌", Cost = 2,
                Desc = "从洞穴里找到的小蜘蛛，跟随并保护救出它们的持有者。\n\n它们没有魔物的那种器官，是从哪里来的呢？" },
            // 37〜39（+41 国王之魂）按 `docs/护符效果描述.md`：**可选中但无法佩戴**，只给"未解锁"文案。
            // 37/38/39/41：诺艾尔侧已实装（需求 2026-09-27），可正常佩戴；小骑士侧仍用 KName/KDesc/KCost。
            new CharmData { Id = 37, KName = "舞梦者", KDesc = "专门给挥动梦之钉和收集精华的人准备的护符。用梦之钉击中敌人获得的灵魂增加，同时使用梦之钉攻击速度加快。\n\n这里的生物虽使用魔力，但仍然具有灵魂和鲜活的梦境。", KCost = 1, IconFile = "37_dream", Name = "舞梦者", Cost = 1,
                Desc = "传说蛾族战士们曾佩戴这种印记\n\n对圣光爆发进行了改造，能够吸收周围魔物的魔力。" },
            new CharmData { Id = 38, KName = "梦之盾", KDesc = "生成一面缓慢围绕持有者旋转的盾牌，对敌人造成与当前骨钉相等的接触伤害。\n\n这个护符蕴含了伟大战士的精神力，在持有者凝聚时会尽力保护持有者。", KCost = 3, IconFile = "38_dream_protecter", Name = "梦之盾", Cost = 3,
                Desc = "传说蛾族战士们曾使用这种武器，攻防兼备。\n\n对格拉提亚商店里卖的周边玩具做了改造，现在它能像童话里那样保护持有者了！" },
            new CharmData { Id = 39, KName = "格林之子", KDesc = "一场完成的仪式的标志。包含着一团跳动的猩红之火。\n\n火焰必须燃烧，梦魇终将再临。", KCost = 2, IconFile = "39_Grimm", Name = "格林之子", Cost = 2,
                Desc = "童话里带来梦魇的恶魔，很多小孩子都喜欢这个恐怖又优雅的角色。\n\n对格拉提亚商店里卖的周边玩具做了改造，赋予其智能索敌技术" },
            new CharmData { Id = 40, KName = "虚空之心", KDesc = "隐藏在内部的空虚，现在不再受到约束。使虚空在持有者的意志下联合起来。\n\n这个护符是持有者的一部分，不能卸下。", KCost = 0, IconFile = "40_VOID", Name = "虚空之心", Desc = "隐藏在内部的空虚，现在不再受到约束。使虚空在持有者的意志下联合起来。\n\n这个护符是持有者的一部分，不能卸下。", Cost = 0 },
            new CharmData { Id = 41, KName = "国王之魂", KDesc = "象征着高等生灵相互结合的圣洁护符。\n\n持有者能缓慢吸收其中无限的灵魂。", KCost = 5, IconFile = "41_KING", Name = "国王之魂", Cost = 5,
                Desc = "童话里的苍白之王，他的王国万世长存，能使持有者吸收其中无限的灵魂。\n\n只是一个超大的魔力背包..." },
            new CharmData { Id = 43, KName = "无忧旋律", KDesc = "纪念一份友谊建立的信物。包含一首可能使持有者免受伤害的守护之歌。", KCost = 3, IconFile = "42_tune", Name = "无忧旋律", Desc = "纪念一份友谊建立的信物。包含一首可能使持有者免受伤害的守护之歌。", Cost = 3 },
            new CharmData { Id = 42, KName = "束缚", KDesc = "", KCost = 0, IconFile = "gg_godseeker_mode_selector", Name = "束缚", Desc = "", Cost = 0 },
        };

        public static CharmData Get(int id)
        {
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].Id == id) return All[i];
            }
            return null;
        }

        /// <summary>是否"未解锁"。需求 2026-09-27：不设解锁机制，默认全部解锁。</summary>
        public static bool IsLocked(int id)
        {
            return false;
        }

        /// <summary>同上（带 owner 的重载，保留接口）。</summary>
        public static bool IsLocked(int id, CharmOwner owner)
        {
            return false;
        }
    }
}
