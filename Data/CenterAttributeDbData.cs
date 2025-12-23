namespace DeckMiner.Data
{
    // 该类用于映射 CenterAttributes.json 中的单条数据
    public class CenterAttributeDbData
    {
        // CenterAttributeSeriesId: 20213010 (int)
        public int CenterAttributeSeriesId { get; set; }

        // CenterAttributeName: "アピールアップ" (string)
        public string CenterAttributeName { get; set; }

        // Description: "..." (string)
        public string Description { get; set; }

        // TargetIds: ["50000", "50000", "50000"] (List<string>)
        // 注意：这是 CenterAttribute 类构造函数中需要被 split(',') 的原始数据
        public List<string> TargetIds { get; set; }

        // CenterAttributeEffectId: [10004000, 20004000, 30004000] (List<int>)
        public List<int> CenterAttributeEffectId { get; set; }
    }
}