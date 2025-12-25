using YamlDotNet.Serialization;

namespace DeckMiner.Config
{
    /// <summary>
    /// 成员配置数据模型 - 完全兼容 Python YAML 配置
    /// 对应文件: config/member-example.yaml
    /// 支持所有字段的完整映射
    /// </summary>
    public class MemberConfig
    {
        /// <summary>
        /// 输出目录配置
        /// </summary>
        [YamlMember(Alias = "output")]
        public OutputConfig Output { get; set; } = new();

        /// <summary>
        /// 歌曲配置列表（支持多首歌曲）
        /// </summary>
        [YamlMember(Alias = "songs")]
        public List<SongConfig> Songs { get; set; } = new();

        /// <summary>
        /// Debug 卡组（可选，用于单卡组测试）
        /// </summary>
        [YamlMember(Alias = "debug_deck_cards")]
        public List<int>? DebugDeckCards { get; set; }

        /// <summary>
        /// 卡池（该成员拥有的所有卡牌 ID）
        /// </summary>
        [YamlMember(Alias = "card_ids")]
        public List<int> CardIds { get; set; } = new();

        /// <summary>
        /// 全局朋友卡片池（默认可用的朋友卡片 ID 列表）
        /// 朋友卡片提供：基础数值（受队长被动影响）+ Center Skill
        /// 朋友卡片不提供：一般技能 + 被动技能
        /// 优先级：歌曲配置的 friend_card_pool > 全局 friend_card_ids
        /// </summary>
        [YamlMember(Alias = "friend_card_ids")]
        public List<int> FriendCardIds { get; set; } = new();

        /// <summary>
        /// 赛季模式（用于计算粉丝等级加成）
        /// - "sukushow": 只计算歌唱成员（默认）
        /// - "sukuste": 计算所有成员
        /// </summary>
        [YamlMember(Alias = "season_mode")]
        public string SeasonMode { get; set; } = "sukushow";

        /// <summary>
        /// LGP 模式（是否允许同角色双卡）
        /// - false: 日常模式，每个角色最多1张卡
        /// - true: LGP 大赛模式，允许0-3个角色使用双卡（默认）
        /// </summary>
        [YamlMember(Alias = "lgp_mode")]
        public bool LgpMode { get; set; } = true;

        /// <summary>
        /// 粉丝等级配置
        /// 格式: { 角色ID: 粉丝等级 }
        /// 例如: { 1031: 10, 1032: 8 }
        /// </summary>
        [YamlMember(Alias = "fan_levels")]
        public Dictionary<int, int> FanLevels { get; set; } = new();

        /// <summary>
        /// 特定卡牌练度覆盖（如果有未满练的卡）
        /// 格式: { 卡牌ID: [level, center_skill_level, skill_level] }
        /// 例如: { 1021701: [140, 11, 11] }
        /// </summary>
        [YamlMember(Alias = "card_levels")]
        public Dictionary<int, List<int>> CardLevels { get; set; } = new();

        /// <summary>
        /// 批次大小
        /// </summary>
        [YamlMember(Alias = "batch_size")]
        public int BatchSize { get; set; } = 1000000;

        /// <summary>
        /// 进程数量（null = 使用所有 CPU 核心）
        /// </summary>
        [YamlMember(Alias = "num_processes")]
        public int? NumProcesses { get; set; }

        /// <summary>
        /// 缓存配置
        /// </summary>
        [YamlMember(Alias = "cache")]
        public CacheConfig Cache { get; set; } = new();

        /// <summary>
        /// 优化器配置（用于 multi_optimizer_2.py）
        /// </summary>
        [YamlMember(Alias = "optimizer")]
        public OptimizerConfig Optimizer { get; set; } = new();
    }

    /// <summary>
    /// 输出目录配置
    /// </summary>
    public class OutputConfig
    {
        [YamlMember(Alias = "base_dir")]
        public string BaseDir { get; set; } = "output";

        /// <summary>
        /// 开启隔离，每次运行生成独立目录
        /// </summary>
        [YamlMember(Alias = "enable_isolation")]
        public bool EnableIsolation { get; set; } = true;
    }

    /// <summary>
    /// 歌曲配置
    /// </summary>
    public class SongConfig
    {
        /// <summary>
        /// 歌曲 ID（例如: "405117"）
        /// </summary>
        [YamlMember(Alias = "music_id")]
        public string MusicId { get; set; }

        /// <summary>
        /// 难度
        /// - "01": Normal
        /// - "02": Hard
        /// - "03": Expert
        /// - "04": Master
        /// </summary>
        [YamlMember(Alias = "difficulty")]
        public string Difficulty { get; set; }

        /// <summary>
        /// 熟练度等级（通常为 50）
        /// </summary>
        [YamlMember(Alias = "mastery_level")]
        public int MasteryLevel { get; set; } = 50;

        /// <summary>
        /// 必须包含的所有卡牌（全部都要在卡组中）
        /// </summary>
        [YamlMember(Alias = "mustcards_all")]
        public List<int> MustcardsAll { get; set; } = new();

        /// <summary>
        /// 必须包含的任意卡牌（至少一张在卡组中）
        /// </summary>
        [YamlMember(Alias = "mustcards_any")]
        public List<int> MustcardsAny { get; set; } = new();

        /// <summary>
        /// 必须包含的技能类型（卡组必须包含所有指定的技能类型）
        /// 例如: [2, 3, 5, 7, 8] = [分卡, 电, 洗牌, 分加成, 电加成]
        /// 技能类型定义：
        /// - 2: ScoreGain（分卡）
        /// - 3: VoltagePointChange（电）
        /// - 5: DeckReset（洗牌/DR）
        /// - 7: NextAPGainRateChange（分加成）
        /// - 8: NextVoltageGainRateChange（电加成）
        /// </summary>
        [YamlMember(Alias = "mustskills")]
        public List<int> MustSkills { get; set; } = new();

        /// <summary>
        /// 禁止使用的卡牌（模拟时不会加入卡组）
        /// 例如: [1011501, 1052506]
        /// </summary>
        [YamlMember(Alias = "banned_cards")]
        public List<int> BannedCards { get; set; } = new();

        /// <summary>
        /// 覆盖中心角色 ID（null = 使用歌曲默认）
        /// </summary>
        [YamlMember(Alias = "center_override")]
        public int? CenterOverride { get; set; }

        /// <summary>
        /// 覆盖歌曲颜色
        /// - 1: Smile
        /// - 2: Pure
        /// - 3: Cool
        /// - null: 使用歌曲默认
        /// </summary>
        [YamlMember(Alias = "color_override")]
        public int? ColorOverride { get; set; }

        /// <summary>
        /// 队长指定（"0" = 自动选择）
        /// </summary>
        [YamlMember(Alias = "leader_designation")]
        public string LeaderDesignation { get; set; } = "0";

        /// <summary>
        /// 次要中心角色卡片列表（可選的額外中心卡）
        /// 用於指定非高稀有度但想作為中心卡的特定卡片
        /// 例如: [1031533, 1032530, 1033528]
        /// </summary>
        [YamlMember(Alias = "secondary_center")]
        public List<int> SecondaryCenter { get; set; } = new();

        /// <summary>
        /// 朋友卡片池（该首歌可用的朋友卡片 ID 列表）
        /// 朋友卡片提供：基础数值（受队长被动影响）+ Center Skill
        /// 朋友卡片不提供：一般技能 + 被动技能
        /// </summary>
        [YamlMember(Alias = "friend_card_pool")]
        public List<int> FriendCardPool { get; set; } = new();
    }

    /// <summary>
    /// 缓存配置
    /// </summary>
    public class CacheConfig
    {
        [YamlMember(Alias = "max_fingerprints_in_memory")]
        public int MaxFingerprintsInMemory { get; set; } = 5000000;

        [YamlMember(Alias = "auto_cleanup")]
        public bool AutoCleanup { get; set; } = true;

        [YamlMember(Alias = "max_cache_age_days")]
        public int MaxCacheAgeDays { get; set; } = 7;
    }

    /// <summary>
    /// 优化器配置（用于 multi_optimizer_2.py）
    /// </summary>
    public class OptimizerConfig
    {
        /// <summary>
        /// 每首歌保留得分排名前 N 名的卡组
        /// </summary>
        [YamlMember(Alias = "top_n")]
        public int TopN { get; set; } = 50000;

        /// <summary>
        /// 在输出中显示卡牌名称
        /// </summary>
        [YamlMember(Alias = "show_card_names")]
        public bool ShowCardNames { get; set; } = true;

        /// <summary>
        /// 全局禁止使用的卡牌 ID 列表（三面均生效）
        /// 例如: [1011501, 1052506]
        /// </summary>
        [YamlMember(Alias = "forbidden_cards")]
        public List<int> ForbiddenCards { get; set; } = new();

        /// <summary>
        /// 多曲优化器歌曲配置（可选，优先级高于上方的 songs 配置）
        /// 如果配置了此区块，优化器将使用这里的歌曲列表和禁卡设定
        /// </summary>
        [YamlMember(Alias = "songs")]
        public List<OptimizerSongConfig>? Songs { get; set; }
    }

    /// <summary>
    /// 优化器歌曲配置
    /// </summary>
    public class OptimizerSongConfig
    {
        [YamlMember(Alias = "music_id")]
        public string MusicId { get; set; }

        [YamlMember(Alias = "difficulty")]
        public string Difficulty { get; set; }

        /// <summary>
        /// 该首歌的禁卡（与全局禁卡合并使用）
        /// </summary>
        [YamlMember(Alias = "banned_cards")]
        public List<int> BannedCards { get; set; } = new();
    }
}
