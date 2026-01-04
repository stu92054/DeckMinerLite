using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

            try
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .IgnoreUnmatchedProperties()
                    .WithTypeConverter(new FlowIntListYamlConverter()) // Register explicitly
                    .WithTypeConverter(new CardLevelsYamlConverter()) // Register explicitly
                    .Build();

                return deserializer.Deserialize<MemberConfig>(yamlContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading config: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Stack: {ex.InnerException.StackTrace}");
                }
                throw;
            }
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
                3 => 80,   // R
                4 => 100,  // SR
                5 => 120,  // UR
                7 => 140,  // LR
                8 => 140,  // DR
                9 => 120,  // BR
                _ => 100
            };

            // 修正：選卡時最高預設練度不符合其稀有度
            // 根據 CardDatas.json，不同稀有度的最大等級如下：
            // R (3): 60 (未覺醒) -> 80 (覺醒)
            // SR (4): 80 (未覺醒) -> 100 (覺醒)
            // UR (5): 100 (未覺醒) -> 120 (覺醒)
            // LR (7): 140
            // DR (8): 140
            // BR (9): 120
            // 注意：這裡返回的是預設滿練度，所以應該是覺醒後的最大等級。
            // 上面的 switch 已經正確反映了覺醒後的最大等級。
            // 如果用戶指的是"選卡時"，可能是指在 GUI 中添加新卡片時的預設值。
            // 但這裡是 GetCardLevels，用於獲取配置中的等級或默認等級。
            // 如果配置中沒有，則返回默認滿練度。
            
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

        /// <summary>
        /// 保存当前配置到 YAML 文件 (保留註解與格式)
        /// </summary>
        public void SaveConfig()
        {
            if (_configFilePath == null)
            {
                throw new InvalidOperationException("Cannot save configuration: No file path specified.");
            }

            // We will process the file line by line to preserve comments
            var lines = File.Exists(_configFilePath) ? File.ReadAllLines(_configFilePath).ToList() : new List<string>();
            
            // 修正：按夏save時未真正寫入
            // 問題可能出在 lines 為空時的處理，或者文件寫入權限/路徑問題。
            // 但更可能是因為之前的邏輯中，如果文件存在但內容為空，或者讀取失敗，導致 lines 為空。
            // 另外，如果 lines 不為空，但沒有 card_levels 區塊，之前的代碼會追加。
            // 這裡確保如果 lines 為空，則初始化為空列表，並在後面處理。
            
            if (lines.Count == 0)
            {
                // If file doesn't exist or is empty, fallback to full serialization
                var serializer = new SerializerBuilder()
                    .WithNamingConvention(UnderscoredNamingConvention.Instance)
                    .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
                    .DisableAliases()
                    .WithTypeConverter(new CardLevelsYamlConverter())
                    .WithTypeConverter(new FlowIntListYamlConverter())
                    .Build();
                
                string yamlContent = serializer.Serialize(_config);
                
                // Add comments for card_levels
                string commentBlock = 
@"# 特定卡牌練度覆蓋 (預設滿練度，僅需填寫非滿練卡)
# 格式: card_id: [level, center_skill_level, skill_level]
# R: 80, SR: 100, UR: 120, LR: 140, DR: 140, BR: 120
card_levels:";
                yamlContent = yamlContent.Replace("card_levels:", commentBlock);
                
                File.WriteAllText(_configFilePath, yamlContent);
                return;
            }

            var newLines = new List<string>();
            bool inCardLevels = false;
            bool foundCardLevelsBlock = false;
            bool inCardIds = false;
            int indentLevel = -1;
            var processedCardIds = new HashSet<int>();
            var validCardIds = new HashSet<int>(_config.CardIds);

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];

                if (!inCardLevels && !inCardIds)
                {
                    if (Regex.IsMatch(line, @"^card_levels:\s*"))
                    {
                        inCardLevels = true;
                        foundCardLevelsBlock = true;
                        newLines.Add(line);
                        continue;
                    }
                    if (Regex.IsMatch(line, @"^card_ids:\s*"))
                    {
                        inCardIds = true;
                        newLines.Add("card_ids:");
                        foreach (var id in _config.CardIds.OrderBy(x => x))
                        {
                            newLines.Add($"  - {id}");
                        }
                        continue;
                    }
                    newLines.Add(line);
                }
                else if (inCardIds)
                {
                    // We are inside card_ids block. Skip old entries until block ends.
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        inCardIds = false;
                        newLines.Add(line);
                        continue;
                    }
                    
                    // If line is not indented and not a comment, it's a new top-level key
                    if (!line.StartsWith(" ") && !line.StartsWith("-") && !line.StartsWith("#"))
                    {
                        inCardIds = false;
                        newLines.Add(line);
                        continue;
                    }
                    
                    // Skip everything else (old list items, comments inside the list)
                    continue;
                }
                else
                {
                    // We are inside card_levels
                    // Check indentation to see if block ended
                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#"))
                    {
                        newLines.Add(line);
                        continue;
                    }

                    int currentIndent = line.TakeWhile(char.IsWhiteSpace).Count();
                    if (indentLevel == -1) indentLevel = currentIndent;

                    if (currentIndent < indentLevel)
                    {
                        // Block ended (indentation decreased)
                        inCardLevels = false;
                        // Append new cards before closing the block
                        AppendNewCards(newLines, processedCardIds, indentLevel, validCardIds);
                        newLines.Add(line);
                        continue;
                    }

                    // Parse the entry
                    // Expected format: "  123456: [100, 14, 14] # comment"
                    var match = Regex.Match(line, @"^\s*(\d+):\s*\[(.*)\](.*)$");
                    if (match.Success)
                    {
                        int cardId = int.Parse(match.Groups[1].Value);
                        string trailingComment = match.Groups[3].Value;
                        
                        // Check if card is valid (in current pool)
                        if (!validCardIds.Contains(cardId))
                        {
                            // Skip (remove invalid/old cards)
                            continue;
                        }

                        if (_config.CardLevels.TryGetValue(cardId, out var levels))
                        {
                            // Check if values are default
                            if (IsDefaultLevel(cardId, levels))
                            {
                                // Skip (remove default)
                                processedCardIds.Add(cardId);
                                continue;
                            }
                            else
                            {
                                // Update line with current values from memory
                                string indent = new string(' ', currentIndent);
                                string valStr = $"[{levels[0]}, {levels[1]}, {levels[2]}]";
                                newLines.Add($"{indent}{cardId}: {valStr}{trailingComment}");
                                processedCardIds.Add(cardId);
                            }
                        }
                        else
                        {
                            // Not in config anymore (deleted by user)
                            // Skip
                            continue;
                        }
                    }
                    else
                    {
                        // Could not parse, keep it to be safe
                        newLines.Add(line);
                    }
                }
            }

            // If we finished the file and were inside card_levels (EOF ended block)
            if (inCardLevels)
            {
                AppendNewCards(newLines, processedCardIds, indentLevel == -1 ? 2 : indentLevel, validCardIds);
            }
            // If we never found the block, append it at the end
            else if (!foundCardLevelsBlock)
            {
                newLines.Add("");
                newLines.Add("# 特定卡牌練度覆蓋 (預設滿練度，僅需填寫非滿練卡)");
                newLines.Add("# 格式: card_id: [level, center_skill_level, skill_level]");
                newLines.Add("# R: 80, SR: 100, UR: 120, LR: 140, DR: 140, BR: 120");
                newLines.Add("card_levels:");
                AppendNewCards(newLines, processedCardIds, 2, validCardIds);
            }

            File.WriteAllLines(_configFilePath, newLines);
            
            // 確保寫入磁碟
            // File.WriteAllLines 已經會 flush 和 close，但為了保險起見，可以再次檢查文件是否存在。
            if (!File.Exists(_configFilePath))
            {
                throw new IOException($"Failed to save config file: {_configFilePath}");
            }
        }

        private void AppendNewCards(List<string> lines, HashSet<int> processedIds, int indentLevel, HashSet<int> validCardIds)
        {
            string indent = new string(' ', indentLevel);
            // Sort keys for deterministic output
            var sortedKeys = _config.CardLevels.Keys.OrderBy(k => k).ToList();
            
            foreach (var cardId in sortedKeys)
            {
                if (!processedIds.Contains(cardId) && validCardIds.Contains(cardId))
                {
                    var levels = _config.CardLevels[cardId];
                    if (!IsDefaultLevel(cardId, levels))
                    {
                         lines.Add($"{indent}{cardId}: [{levels[0]}, {levels[1]}, {levels[2]}]");
                    }
                }
            }
        }

        private bool IsDefaultLevel(int cardId, List<int> values)
        {
            if (values.Count != 3) return false;
            int rarity = (cardId / 100) % 10;
            int defaultLevel = rarity switch
            {
                3 => 80,   // R
                4 => 100,  // SR
                5 => 120,  // UR
                7 => 140,  // LR
                8 => 140,  // DR
                9 => 120,  // BR
                _ => 100
            };
            return values[0] == defaultLevel && values[1] == 14 && values[2] == 14;
        }
    }
}
