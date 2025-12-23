using System.Collections.Generic;

namespace DeckMiner.Data
{
    /// <summary>
    /// 映射 CardData.json 中单条卡牌数据的结构。
    /// </summary>
    public class CardDbData
    {
        // CardSeriesId: 1010400 (int)
        public int CardSeriesId { get; set; }

        // Name: "???" (string)
        public string Name { get; set; }

        // Description: "???" (string)
        public string Description { get; set; }

        // CharactersId: 1021 (int)
        public int CharactersId { get; set; }

        // Rarity: 4 (int)
        public int Rarity { get; set; }

        // CenterSkillSeriesId: 0 (int)
        // 这是 CenterSkill 的系列 ID，用于 Card 类的构造函数中
        public int CenterSkillSeriesId { get; set; }

        // CenterAttributeSeriesId: 0 (int)
        // 这是 CenterAttribute 的系列 ID，用于 Card 类的构造函数中
        public int CenterAttributeSeriesId { get; set; }

        // MaxSmile: [2100, 2940, ...] (List<int>)
        // C# List<int> 对应 JSON 数组
        public List<int> MaxSmile { get; set; }

        // MaxPure: [2100, 2940, ...] (List<int>)
        public List<int> MaxPure { get; set; }

        // MaxCool: [2100, 2940, ...] (List<int>)
        public List<int> MaxCool { get; set; }

        // MaxMental: [210, 294, ...] (List<int>)
        public List<int> MaxMental { get; set; }

        // RhythmGameSkillSeriesId: [30213010, ...] (List<int>)
        // 注意：这个数组包含多个技能系列 ID，对应卡牌不同觉醒等级的技能
        public List<int> RhythmGameSkillSeriesId { get; set; }
    }
}