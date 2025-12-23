namespace DeckMiner.Data
{
    // 该类用于映射 CenterSkills.json 中的单条数据
    public class CenterSkillDbData
    {
        // CenterSkillSeriesId: 10213010 (int)
        public int CenterSkillSeriesId { get; set; }

        // CenterSkillName: "APゲイン" (string)
        public string CenterSkillName { get; set; }

        // Description: "..." (string)
        public string Description { get; set; }

        // CenterSkillConditionIds: ["3000000"] (List<string>)
        public List<string> CenterSkillConditionIds { get; set; }

        // CenterSkillEffectId: [100010000] (List<int>)
        public List<int> CenterSkillEffectId { get; set; }
    }
}