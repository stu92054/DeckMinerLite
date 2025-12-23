namespace DeckMiner.Data
{
    // 这个类对应 JSON 中每个 Skill ID 下的对象
    public class SkillDbData
    {
        // JSON 键名和 C# 属性名一致，都使用 PascalCase
        public int RhythmGameSkillSeriesId { get; set; }
        public string RhythmGameSkillName { get; set; }
        public int ConsumeAP { get; set; }
        public string Description { get; set; }
        public List<string> RhythmGameSkillConditionIds { get; set; }
        public List<int> RhythmGameSkillEffectId { get; set; }
    }
}