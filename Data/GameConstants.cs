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
            { 9, "BR" }
        };

        public static string GetRarityString(int rarity)
        {
            return RarityNames.TryGetValue(rarity, out var name) ? name : $"★{rarity}";
        }

        public static string GetCharacterName(int charId)
        {
            return CharacterNames.TryGetValue(charId, out var name) ? name : $"Unknown({charId})";
        }
    }
}
