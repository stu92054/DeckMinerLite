using System.Collections.Generic;

namespace DeckMiner.Data
{
    /// <summary>
    /// 全局静态数据管理器，用于存储所有卡牌的数据库信息。
    /// </summary>
    public static class CardDataManager
    {
        // 静态属性：存储 CardDbData 字典
        // Key: CardSeriesId (string)
        private static Dictionary<string, CardDbData> _cardDatabase;

        /// <summary>
        /// 获取卡牌数据库。
        /// 确保在访问前已调用 Initialize 方法。
        /// </summary>
        public static Dictionary<string, CardDbData> CardDatabase
        {
            get
            {
                if (_cardDatabase == null)
                {
                    // 在实际应用中，这里可能抛出异常或触发加载逻辑
                    throw new InvalidOperationException("CardDataManager 尚未初始化。请在创建 Card 实例前调用 Initialize(db).");
                }
                return _cardDatabase;
            }
        }

        /// <summary>
        /// 初始化卡牌数据库。
        /// 应当在应用程序启动时只调用一次。
        /// </summary>
        public static void Initialize(Dictionary<string, CardDbData> dbCard)
        {
            if (_cardDatabase != null)
            {
                // 避免重复初始化
                return; 
            }
            _cardDatabase = dbCard;
        }
    }
}