namespace DeckMiner.Data
{
    // 定义您的数据库类型别名，方便代码阅读
    using SkillDbDictionaryType = Dictionary<string, SkillDbData>;
    using CenterAttrDbDictionaryType = Dictionary<string, CenterAttributeDbData>;
    using CenterSkillDbDictionaryType = Dictionary<string, CenterSkillDbData>;

    /// <summary>
    /// 全局静态数据管理器，用于存储所有技能相关的数据库信息，
    /// 供 Skill、CenterAttribute、CenterSkill 等类直接访问。
    /// </summary>
    public static class SkillDataManager
    {
        private static SkillDbDictionaryType _skillDatabase;
        private static CenterAttrDbDictionaryType _centerAttrDatabase;
        private static CenterSkillDbDictionaryType _centerSkillDatabase;

        private static readonly string UninitializedError = "SkillDataManager 尚未初始化。请在创建依赖技能数据的实例前调用 Initialize(...) 方法。";

        // --- 静态属性：提供全局访问 ---

        /// <summary>
        /// 获取普通技能数据库 (RhythmGameSkills.json)。
        /// </summary>
        public static SkillDbDictionaryType SkillDatabase
        {
            get
            {
                if (_skillDatabase == null) throw new InvalidOperationException(UninitializedError);
                return _skillDatabase;
            }
        }

        /// <summary>
        /// 获取 Center Attribute 数据库 (CenterAttributes.json)。
        /// </summary>
        public static CenterAttrDbDictionaryType CenterAttributeDatabase
        {
            get
            {
                if (_centerAttrDatabase == null) throw new InvalidOperationException(UninitializedError);
                return _centerAttrDatabase;
            }
        }

        /// <summary>
        /// 获取 Center Skill 数据库 (CenterSkills.json)。
        /// </summary>
        public static CenterSkillDbDictionaryType CenterSkillDatabase
        {
            get
            {
                if (_centerSkillDatabase == null) throw new InvalidOperationException(UninitializedError);
                return _centerSkillDatabase;
            }
        }

        // --- 静态初始化方法 ---

        /// <summary>
        /// 初始化所有技能相关的数据库。应当在应用程序启动时调用一次。
        /// </summary>
        public static void Initialize(
            SkillDbDictionaryType skillDb,
            CenterAttrDbDictionaryType centerAttrDb,
            CenterSkillDbDictionaryType centerSkillDb)
        {
            if (_skillDatabase != null)
            {
                // 避免重复初始化
                return; 
            }
            
            _skillDatabase = skillDb;
            _centerAttrDatabase = centerAttrDb;
            _centerSkillDatabase = centerSkillDb;
        }
    }
}