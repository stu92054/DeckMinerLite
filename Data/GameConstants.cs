using System.Collections.Generic;

namespace DeckMiner.Data
{
    public static class GameConstants
    {
        public static readonly Dictionary<int, string> CharacterNames = new()
        {
            { 1011, "大賀美 沙知" },
            { 1021, "乙宗 梢" },
            { 1022, "夕霧 綴理" },
            { 1023, "藤島 慈" },
            { 1031, "日野下 花帆" },
            { 1032, "村野 さやか" },
            { 1033, "大沢 瑠璃乃" },
            { 1041, "百生 吟子" },
            { 1042, "徒町 小鈴" },
            { 1043, "安養寺 姫芽" },
            { 1051, "桂城 泉" },
            { 1052, "セラス 柳田 リリエンフェルト" }
        };

        public static readonly Dictionary<int, string> RarityNames = new()
        {
            { 3, "R" },
            { 4, "SR" },
            { 5, "UR" },
            { 7, "LR" },
            { 8, "DR" },
            { 9, "BR" },
            { 94, "MSR" },
            { 95, "MUR" }
        };

        /// <summary>
        /// 預設滿練卡牌等級（覺醒後）
        /// </summary>
        public static readonly Dictionary<int, int> DefaultCardLevels = new()
        {
            { 3, 80 },    // R
            { 4, 100 },   // SR
            { 5, 120 },   // UR
            { 7, 140 },   // LR
            { 8, 140 },   // DR
            { 9, 120 },   // BR
            { 94, 100 },  // MSR
            { 95, 120 }   // MUR
        };

        /// <summary>
        /// 預設滿練 Center Skill 等級
        /// </summary>
        public const int DefaultCenterSkillLevel = 14;

        /// <summary>
        /// 預設滿練 Skill 等級
        /// </summary>
        public const int DefaultSkillLevel = 14;

        public static string GetRarityString(int rarity)
        {
            return RarityNames.TryGetValue(rarity, out var name) ? name : $"★{rarity}";
        }

        /// <summary>
        /// 根據稀有度取得預設滿練等級
        /// </summary>
        public static int GetDefaultCardLevel(int rarity)
        {
            return DefaultCardLevels.GetValueOrDefault(rarity, 100);
        }

        public static string GetCharacterName(int charId)
        {
            return CharacterNames.TryGetValue(charId, out var name) ? name : $"Unknown({charId})";
        }
    }
}
