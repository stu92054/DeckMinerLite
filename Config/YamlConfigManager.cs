using System;
using System.IO;
using System.Text.RegularExpressions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DeckMiner.Config
{
    /// <summary>
    /// YAML 配置管理器 - 完全兼容 Python config_manager.py
    /// 支持配置文件优先级、输出目录隔离、PT 动态计算等
    /// </summary>
    public class YamlConfigManager
    {
        private readonly MemberConfig _config;
        private readonly string? _configFilePath;
        private readonly string _memberName;
        private readonly string _runTimestamp;

        public MemberConfig Config => _config;
        public string MemberName => _memberName;
        public string RunTimestamp => _runTimestamp;
        public bool IsYamlMode => _configFilePath != null;

        /// <summary>
        /// 初始化配置管理器
        /// </summary>
        /// <param name="configFile">配置文件路径（可选）</param>
        public YamlConfigManager(string? configFile = null)
        {
            _configFilePath = ResolveConfigFile(configFile);
            _config = LoadConfig(_configFilePath);
            _memberName = ExtractMemberName(_configFilePath);
            _runTimestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (IsYamlMode)
            {
                Console.WriteLine($"[Config] Loaded YAML: {_configFilePath}");
                Console.WriteLine($"[Config] Member: {_memberName}");
            }
        }

        /// <summary>
        /// 配置文件解析优先级（完全兼容 Python 版本）:
        /// 1. 函数参数 configFile
        /// 2. 命令行 --config
        /// 3. 环境变量 CONFIG_FILE
        /// 4. config/default.yaml
        /// 5. 返回 null（回退到 JSONC）
        /// </summary>
        private string? ResolveConfigFile(string? configFile)
        {
            // 优先级 1: 直接指定
            if (!string.IsNullOrEmpty(configFile) && File.Exists(configFile))
            {
                return Path.GetFullPath(configFile);
            }

            // 优先级 2: 命令行参数 --config
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "--config")
                {
                    string cliConfig = args[i + 1];
                    if (File.Exists(cliConfig))
                    {
                        return Path.GetFullPath(cliConfig);
                    }
                    throw new FileNotFoundException($"CLI 指定的配置文件不存在: {cliConfig}");
                }
            }

            // 优先级 3: 环境变量
            string? envConfig = Environment.GetEnvironmentVariable("CONFIG_FILE");
            if (!string.IsNullOrEmpty(envConfig) && File.Exists(envConfig))
            {
                return Path.GetFullPath(envConfig);
            }

            // 优先级 4: 默认配置（相对于项目根目录）
            string defaultConfig = Path.Combine("..", "config", "default.yaml");
            if (File.Exists(defaultConfig))
            {
                return Path.GetFullPath(defaultConfig);
            }

            // 优先级 5: 回退到 JSONC
            return null;
        }

        /// <summary>
        /// 加载 YAML 配置
        /// </summary>
        private MemberConfig LoadConfig(string? filePath)
        {
            if (filePath == null)
            {
                // 返回默认配置（将使用 JSONC）
                return new MemberConfig();
            }

            string yamlContent = File.ReadAllText(filePath);

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();

            return deserializer.Deserialize<MemberConfig>(yamlContent);
        }

        /// <summary>
        /// 从配置文件名提取成员名称
        /// 例如: config/member-alice.yaml -> alice
        ///       config/member-stu92054.yaml -> stu92054
        /// </summary>
        private string ExtractMemberName(string? configPath)
        {
            if (configPath == null) return "default";

            string fileName = Path.GetFileNameWithoutExtension(configPath);
            var match = Regex.Match(fileName, @"member-(.+)");
            return match.Success ? match.Groups[1].Value : "default";
        }

        /// <summary>
        /// 获取日志目录（兼容 Python 隔离逻辑）
        /// - member-*.yaml: log/{member_name}/
        /// - 其他: log/
        /// </summary>
        public string GetLogDir()
        {
            string logDir;

            if (_memberName != "default")
            {
                // 成员隔离目录
                logDir = Path.Combine("log", _memberName);
            }
            else
            {
                // 默认目录
                logDir = "log";
            }

            Directory.CreateDirectory(logDir);
            return logDir;
        }

        /// <summary>
        /// 获取临时目录（兼容 Python 隔离逻辑）
        /// - 启用隔离: temp/{member_name}/{timestamp}/
        /// - 不隔离: temp/
        /// </summary>
        public string GetTempDir(string? musicId = null)
        {
            string tempBase;

            if (_config.Output.EnableIsolation)
            {
                // 隔离模式
                tempBase = Path.Combine("temp", _memberName, _runTimestamp);
            }
            else
            {
                // 非隔离模式
                tempBase = "temp";
            }

            if (musicId != null)
            {
                tempBase = Path.Combine(tempBase, $"temp_{musicId}");
            }

            Directory.CreateDirectory(tempBase);
            return tempBase;
        }

        /// <summary>
        /// 计算 Season Fan Level 加成（完全兼容 Python 逻辑）
        /// </summary>
        /// <param name="singerIds">歌唱成员 ID 列表（包含中心角色）</param>
        /// <returns>BONUS_SFL 值</returns>
        public double CalculateBonusSFL(List<int> singerIds)
        {
            // Fan Level 加成表（与 Python 完全一致）
            var fanLvBonusTable = new Dictionary<int, double>
            {
                {1, 0.00}, {2, 0.20}, {3, 0.275}, {4, 0.35}, {5, 0.425},
                {6, 0.50}, {7, 0.55}, {8, 0.60}, {9, 0.65}, {10, 0.70}
            };

            // 歌唱人数补正表
            var singingCorrection = _config.SeasonMode == "sukushow"
                ? new Dictionary<int, double> { {2, 2.75}, {8, 1.00}, {9, 0.90} }
                : new Dictionary<int, double> { {2, 2.33}, {8, 1.00} };

            // 计算基础 Fan Level 加成
            double sumBonus = 0.0;
            foreach (int cid in singerIds)
            {
                int lv = _config.FanLevels.GetValueOrDefault(cid, 10);  // 默认 Lv 10
                lv = Math.Clamp(lv, 1, 10);
                sumBonus += fanLvBonusTable[lv];
            }

            double baseBonus = 1.0 + sumBonus;

            // 应用歌唱人数补正
            double correction = singingCorrection.GetValueOrDefault(singerIds.Count, 1.0);

            return baseBonus * correction;
        }

        /// <summary>
        /// 获取卡牌练度（优先级: YAML 配置 > 默认满练）
        /// </summary>
        /// <param name="cardId">卡牌 ID</param>
        /// <returns>[level, center_skill_level, skill_level]</returns>
        public List<int> GetCardLevels(int cardId)
        {
            if (_config.CardLevels.TryGetValue(cardId, out var levels))
            {
                return levels;
            }

            // 默认满练（根据稀有度）
            int rarity = (cardId / 100) % 10;
            int defaultLevel = rarity switch
            {
                5 => 100,  // R
                6 => 110,  // SR
                7 => 120,  // SSR
                8 => 140,  // LR
                9 => 120,  // BR
                _ => 100
            };

            return new List<int> { defaultLevel, 14, 14 };  // [level, cskill_max, skill_max]
        }

        /// <summary>
        /// 合并歌曲禁卡列表（歌曲级 + 全局级 + 优化器级）
        /// </summary>
        public HashSet<int> GetMergedBannedCards(SongConfig song)
        {
            var merged = new HashSet<int>();

            // 1. 歌曲级禁卡
            merged.UnionWith(song.BannedCards);

            // 2. 全局禁卡
            merged.UnionWith(_config.Optimizer.ForbiddenCards);

            // 3. 优化器歌曲级禁卡（如果配置了）
            if (_config.Optimizer.Songs != null)
            {
                var optimizerSong = _config.Optimizer.Songs
                    .FirstOrDefault(s => s.MusicId == song.MusicId && s.Difficulty == song.Difficulty);

                if (optimizerSong != null)
                {
                    merged.UnionWith(optimizerSong.BannedCards);
                }
            }

            return merged;
        }
    }
}
