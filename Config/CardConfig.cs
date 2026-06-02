using System.Collections.Concurrent;
using DeckMiner.Models;

namespace DeckMiner.Config
{
    // 完整的配置结构
    public class CardConfig
    {
        // 默认等级配置 (RarityId: Level)
        // 对应 default_card_level
        public Dictionary<int, int> DefaultCardLevels { get; set; } = new()
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

        // 默认技能等级
        public int DefaultSkillLevel { get; set; } = 14; 
        public int DefaultCenterSkillLevel { get; set; } = 14;

        // 自定义卡牌等级配置 (CardId: CardOverride 对象)
        // 对应 CARD_CACHE
        public ConcurrentDictionary<int, List<int>> CardCache { get; set; } = new();

        // 背水血线配置 (CardId: HPThreshold)
        // 对应 DEATH_NOTE
        public Dictionary<int, double> DeathNote { get; set; } = new();

        // --- 核心方法：获取卡牌等级 ---

        /// <summary>
        /// 获取指定卡牌ID的等级配置：[卡牌等级, C位技能等级, 普通技能等级]。
        /// 如果 CardCache 中不存在，则根据稀有度使用默认值，并将结果存入 CardCache。
        /// </summary>
        /// <param name="cardId">卡牌的唯一ID。</param>
        /// <returns>包含三个等级值的 List<int>。</returns>
        public static List<int> GetCardLevels(int cardId)
        {
            var config = ConfigLoader.Config;

            // ----------------------------------------------------
            // 1. 定义创建默认等级列表的逻辑 (Factory 函数)
            // ----------------------------------------------------
            // 将计算默认等级的逻辑封装成一个本地函数或 Lambda 表达式，作为 GetOrAdd 的参数
            Func<int, List<int>> createDefaultLevels = (id) =>
            {
                // 提取稀有度数字
                int rarityDigit = id / 100 % 10;
                
                int defaultLevel = config.DefaultCardLevels.GetValueOrDefault(
                    rarityDigit, 
                    config.DefaultCardLevels.Values.Max()
                );

                // 构建并返回默认等级列表
                return new List<int>
                {
                    defaultLevel,
                    config.DefaultCenterSkillLevel,
                    config.DefaultSkillLevel
                };
            };

            // ----------------------------------------------------
            // 2. 使用 GetOrAdd 获取或计算等级
            // ----------------------------------------------------
            // GetOrAdd 尝试获取键的值。
            // 如果键不存在，它会调用上面的 createDefaultLevels 函数，
            // 将结果添加到字典中，然后返回该结果。整个过程是线程安全的。
            List<int> levels = config.CardCache.GetOrAdd(cardId, createDefaultLevels);

            // ----------------------------------------------------
            // 3. 检查并返回结果
            // ----------------------------------------------------
            // 检查数组长度依然是必要的，以防配置数据结构被破坏
            if (levels.Count < 3)
            {
                // 如果缓存中的数据不完整，可以强制重新计算并更新，但这需要额外的 GetOrAdd 逻辑或使用 Update
                // 简单的处理是返回默认值
                return createDefaultLevels(cardId);
            }
            
            return levels;
        }

        /// <summary>
        /// 将卡牌ID列表转换为模拟器所需的格式：List<(int CardId, List<int> Levels)>
        /// </summary>
        public static List<CardDeckInfo> ConvertDeckToSimulatorFormat(
            List<int> deckCardIdsList)
        {
            // 使用 Linq 简洁地实现转换
            return deckCardIdsList
                .Select(cardId => 
                {
                    // 调用上面实现的静态配置方法获取等级
                    List<int> levels = GetCardLevels(cardId);
                    return new CardDeckInfo(cardId, levels);
                })
                .ToList();
        }
    }
}