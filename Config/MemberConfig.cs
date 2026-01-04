using YamlDotNet.Core;
using YamlDotNet.Core.Events;
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
        [YamlConverter(typeof(FlowIntListYamlConverter))]
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
        /// - false: 日常模式，每个角色最多1張卡
        /// - true: LGP 大賽模式，允許0-3個角色使用雙卡（預設）
        /// </summary>
        [YamlMember(Alias = "lgp_mode")]
        public bool LgpMode { get; set; } = true;

        /// <summary>
        /// 粉絲等級配置
        /// 格式: { 角色ID: 粉絲等級 }
        /// 例如: { 1031: 10, 1032: 8 }
        /// </summary>
        [YamlMember(Alias = "fan_levels")]
        public Dictionary<int, int> FanLevels { get; set; } = new();

        /// <summary>
        /// 特定卡牌練度覆蓋（如果有未滿練的卡）
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
        /// 進程數量（null = 使用所有 CPU 核心）
        /// </summary>
        [YamlMember(Alias = "num_processes")]
        public int? NumProcesses { get; set; }

        /// <summary>
        /// 緩存配置
        /// </summary>
        [YamlMember(Alias = "cache")]
        public CacheConfig Cache { get; set; } = new();

        /// <summary>
        /// 優化器配置（用於 multi_optimizer_2.py）
        /// </summary>
        [YamlMember(Alias = "optimizer")]
        public OptimizerConfig Optimizer { get; set; } = new();
    }

    /// <summary>
    /// 輸出目錄配置
    /// </summary>
    public class OutputConfig
    {
        [YamlMember(Alias = "base_dir")]
        public string BaseDir { get; set; } = "output";

        /// <summary>
        /// 開啟隔離，每次運行生成獨立目錄
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
        [YamlMember(Alias = "music_id", ScalarStyle = ScalarStyle.DoubleQuoted)]
        public string MusicId { get; set; }

        /// <summary>
        /// 難度
        /// - "01": Normal
        /// - "02": Hard
        /// - "03": Expert
        /// - "04": Master
        /// </summary>
        [YamlMember(Alias = "difficulty", ScalarStyle = ScalarStyle.DoubleQuoted)]
        public string Difficulty { get; set; }

        /// <summary>
        /// 熟練度等級（通常為 50）
        /// </summary>
        [YamlMember(Alias = "mastery_level")]
        public int MasteryLevel { get; set; } = 50;

        /// <summary>
        /// 必須包含的所有卡牌（全部都要在卡組中）
        /// </summary>
        [YamlMember(Alias = "mustcards_all")]
        [YamlConverter(typeof(FlowIntListYamlConverter))]
        public List<int> MustcardsAll { get; set; } = new();

        /// <summary>
        /// 必須包含的任意卡牌（至少一張在卡組中）
        /// </summary>
        [YamlMember(Alias = "mustcards_any")]
        [YamlConverter(typeof(FlowIntListYamlConverter))]
        public List<int> MustcardsAny { get; set; } = new();

        /// <summary>
        /// 必須包含的技能類型（卡組必須包含所有指定的技能類型）
        /// 例如: [2, 3, 5, 7, 8] = [分卡, 電, 洗牌, 分加成, 電加成]
        /// 技能類型定義：
        /// - 2: ScoreGain（分卡）
        /// - 3: VoltagePointChange（電）
        /// - 5: DeckReset（洗牌/DR）
        /// - 7: NextAPGainRateChange（分加成）
        /// - 8: NextVoltageGainRateChange（電加成）
        /// </summary>
        [YamlMember(Alias = "mustskills")]
        [YamlConverter(typeof(FlowIntListYamlConverter))]
        public List<int> MustSkills { get; set; } = new();

        /// <summary>
        /// 禁止使用的卡牌（模擬時不會加入卡組）
        /// 例如: [1011501, 1052506]
        /// </summary>
        [YamlMember(Alias = "banned_cards")]
        [YamlConverter(typeof(FlowIntListYamlConverter))]
        public List<int> BannedCards { get; set; } = new();

        /// <summary>
        /// 覆蓋中心角色 ID（null = 使用歌曲默認）
        /// </summary>
        [YamlMember(Alias = "center_override")]
        public int? CenterOverride { get; set; }

        /// <summary>
        /// 覆蓋歌曲顏色
        /// - 1: Smile
        /// - 2: Pure
        /// - 3: Cool
        /// - null: 使用歌曲默認
        /// </summary>
        [YamlMember(Alias = "color_override")]
        public int? ColorOverride { get; set; }

        /// <summary>
        /// 隊長指定（"0" = 自動選擇）
        /// </summary>
        [YamlMember(Alias = "leader_designation", ScalarStyle = ScalarStyle.DoubleQuoted)]
        public string LeaderDesignation { get; set; } = "0";

        /// <summary>
        /// 次要中心角色卡片列表（可選的額外中心卡）
        /// 用於指定非高稀有度但想作為中心卡的特定卡片
        /// 例如: [1031533, 1032530, 1033528]
        /// </summary>
        [YamlMember(Alias = "secondary_center")]
        [YamlConverter(typeof(FlowIntListYamlConverter))]
        public List<int> SecondaryCenter { get; set; } = new();

        /// <summary>
        /// 朋友卡片池（該首歌可用的朋友卡片 ID 列表）
        /// 朋友卡片提供：基礎數值（受隊長被動影響）+ Center Skill
        /// 朋友卡片不提供：一般技能 + 被動技能
        /// </summary>
        [YamlMember(Alias = "friend_card_pool")]
        [YamlConverter(typeof(FlowIntListYamlConverter))]
        public List<int> FriendCardPool { get; set; } = new();
    }

    /// <summary>
    /// 緩存配置
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
    /// 優化器配置（用於 multi_optimizer_2.py）
    /// </summary>
    public class OptimizerConfig
    {
        /// <summary>
        /// 每首歌保留得分排名前 N 名的卡組
        /// </summary>
        [YamlMember(Alias = "top_n")]
        public int TopN { get; set; } = 50000;

        /// <summary>
        /// 在輸出中顯示卡牌名稱
        /// </summary>
        [YamlMember(Alias = "show_card_names")]
        public bool ShowCardNames { get; set; } = true;

        /// <summary>
        /// 全局禁止使用的卡牌 ID 列表（三面均生效）
        /// 例如: [1011501, 1052506]
        /// </summary>
        [YamlMember(Alias = "forbidden_cards")]
        [YamlConverter(typeof(FlowIntListYamlConverter))]
        public List<int> ForbiddenCards { get; set; } = new();

        /// <summary>
        /// 多曲優化器歌曲配置（可選，優先級高於上方的 songs 配置）
        /// 如果配置了此區塊，優化器將使用這裡的歌曲列表和禁卡設定
        /// </summary>
        [YamlMember(Alias = "songs")]
        public List<OptimizerSongConfig>? Songs { get; set; }
    }

    /// <summary>
    /// 優化器歌曲配置
    /// </summary>
    public class OptimizerSongConfig
    {
        [YamlMember(Alias = "music_id", ScalarStyle = ScalarStyle.DoubleQuoted)]
        public string MusicId { get; set; }

        [YamlMember(Alias = "difficulty", ScalarStyle = ScalarStyle.DoubleQuoted)]
        public string Difficulty { get; set; }

        /// <summary>
        /// 該首歌的禁卡（與全局禁卡合併使用）
        /// </summary>
        [YamlMember(Alias = "banned_cards")]
        [YamlConverter(typeof(FlowIntListYamlConverter))]
        public List<int> BannedCards { get; set; } = new();
    }
}
