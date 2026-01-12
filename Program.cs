using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TqdmSharp;

using DeckMiner.Config;
using DeckMiner.Data;
using DeckMiner.Models;
using DeckMiner.Services;
using System.Runtime.InteropServices; // DataManager 所在的命名空间

// 注意：如果 Card 类中的 _initStatus 方法依赖 CardDataManager.CardDatabase，
// 那么必须确保 CardDataManager 在 Deck 初始化之前被初始化。

/// <summary>
/// 輔助類別：同時寫入多個 TextWriter
/// </summary>
class MultiTextWriter : TextWriter
{
    private readonly TextWriter[] _writers;

    public MultiTextWriter(params TextWriter[] writers)
    {
        _writers = writers;
    }

    public override System.Text.Encoding Encoding => System.Text.Encoding.UTF8;

    public override void Write(char value)
    {
        foreach (var writer in _writers)
            writer.Write(value);
    }

    public override void Write(string? value)
    {
        foreach (var writer in _writers)
            writer.Write(value);
    }

    public override void WriteLine(string? value)
    {
        foreach (var writer in _writers)
            writer.WriteLine(value);
    }

    public override void Flush()
    {
        foreach (var writer in _writers)
            writer.Flush();
    }
}

class Program
{
#if WINDOWS
    /// <summary>
    /// GUI mode runner - launches WPF application (Windows only)
    /// </summary>
    static class GuiRunner
    {
        public static int Run()
        {
            Console.WriteLine("[INFO] Launching GUI mode...");

            try
            {
                var app = new DeckMiner.Gui.App();
                app.InitializeComponent();
                app.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] GUI startup failed: {ex.Message}");
                Console.WriteLine($"[HINT] Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[HINT] Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"[HINT] Inner Stack trace: {ex.InnerException.StackTrace}");
                }

                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
                return 1;
            }
        }
    }
#endif

    [STAThread]
    static void Main(string[] args)
    {
        // 設定 Console 編碼為 UTF-8，避免中日文亂碼
        try
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
        }
        catch (System.IO.IOException)
        {
            // 某些環境 (如 Git Bash) 不支援設定編碼，忽略錯誤
        }

        Console.WriteLine("--- SukuShow Deck Miner Lite ---");

#if WINDOWS
        // Windows version: Launch GUI if no args
        if (args.Length == 0)
        {
            Environment.Exit(GuiRunner.Run());
        }
#else
        // Linux version: Show hint if no args
        if (args.Length == 0)
        {
            Console.WriteLine("[INFO] No arguments provided");
            Console.WriteLine("[HINT] Usage examples:");
            Console.WriteLine("[HINT]   ./DeckMinerLite --config ../config/member-example.yaml");
            Console.WriteLine("[HINT]   ./DeckMinerLite --test-yaml");
            Console.WriteLine("[HINT]   ./DeckMinerLite --debug <6 card IDs>");
            return;
        }
#endif

        // === CLI Mode Entry Point ===

        // === 测试 YAML 配置系统 ===
        // 如果命令行包含 --test-yaml，运行测试后退出
        if (args.Contains("--test-yaml"))
        {
            TestYamlConfigLoading();
            return;
        }

        // ------------------------------------------------------------------
        // 步骤 1: 加载数据库文件
        // ------------------------------------------------------------------
        Console.WriteLine("正在加载数据库...");
        DataManager dataManager = DataManager.Instance;

        var cardDb = dataManager.GetCardDatabase();
        var skillDb = dataManager.GetSkillDatabase();
        var centerAttrDb = dataManager.GetCenterAttributeDatabase();
        var centerSkillDb = dataManager.GetCenterSkillDatabase();
        var musicDb = dataManager.GetMusicDatabase();

        CardDataManager.Initialize(cardDb);
        SkillDataManager.Initialize(skillDb, centerAttrDb, centerSkillDb);

        // === Debug Mode ===
        if (args.Contains("--debug"))
        {
            // 設定 Console 輸出為 UTF-8
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            int debugIndex = Array.IndexOf(args, "--debug");
            if (debugIndex + 6 < args.Length)
            {
                List<int> debugDeck = new List<int>();
                int? friendCardId = null;

                // 讀取 6 張主要卡片
                for (int i = 1; i <= 6; i++)
                {
                    if (int.TryParse(args[debugIndex + i], out int cardId))
                        debugDeck.Add(cardId);
                }

                // 檢查是否有第 7 個參數（好友卡）
                if (debugIndex + 7 < args.Length && int.TryParse(args[debugIndex + 7], out int friendCard))
                {
                    friendCardId = friendCard;
                }

                if (debugDeck.Count == 6)
                {
                    // 檢查是否指定 --config 參數
                    YamlConfigManager? debugYamlConfig = null;
                    if (args.Contains("--config"))
                    {
                        int configIndex = Array.IndexOf(args, "--config");
                        if (configIndex + 1 < args.Length)
                        {
                            string configPath = args[configIndex + 1];
                            try
                            {
                                debugYamlConfig = new YamlConfigManager(configPath);
                                Console.WriteLine($"[Config] Loaded YAML config: {configPath}");
                                Console.WriteLine($"[Config] Member: {debugYamlConfig.MemberName}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[Warning] Failed to load YAML config: {ex.Message}");
                                Console.WriteLine("[Info] Falling back to cardConfig.jsonc");
                            }
                        }
                    }

                    // 同時輸出到檔案（UTF-8 編碼）
                    string debugLogPath = Path.Combine(AppContext.BaseDirectory, "csharp_debug_log.txt");
                    using var fileWriter = new StreamWriter(debugLogPath, false, System.Text.Encoding.UTF8);
                    var originalOut = Console.Out;

                    // 建立同時寫入 Console 和檔案的 TextWriter
                    var multiWriter = new MultiTextWriter(originalOut, fileWriter);
                    Console.SetOut(multiWriter);

                    Console.WriteLine($"\n--- Debug Mode ---");
                    Console.WriteLine($"Deck: {string.Join(", ", debugDeck)}");
                    if (friendCardId.HasValue)
                    {
                        Console.WriteLine($"Friend Card: {friendCardId.Value}");
                    }
                    if (debugYamlConfig != null)
                    {
                        Console.WriteLine($"Config: {debugYamlConfig.MemberName} (YAML)");
                    }
                    else
                    {
                        Console.WriteLine($"Config: cardConfig.jsonc (default)");
                    }

                    Simulator.DebugMode = true;
                    string musicId = "405204";
                    string tier = "02";
                    int masterLv = 50;

                    Simulator sim = new Simulator(musicId, tier, masterLv);

                    // Find center (assume first valid center or just try all)
                    int centerChar = musicDb[musicId].CenterCharacterId;
                    var potentialCenters = debugDeck.Where(id => id / 1000 == centerChar).ToList();

                    if (potentialCenters.Count == 0)
                    {
                         Console.WriteLine("No valid center card found in deck.");
                         Console.SetOut(originalOut);
                         return;
                    }

                    foreach(var centerId in potentialCenters)
                    {
                        Console.WriteLine($"\nTesting Center: {centerId}");

                        // 每次測試新 center 時重新創建 Deck 物件 (與 Python 邏輯一致)
                        List<CardDeckInfo> deckInfo;
                        if (debugYamlConfig != null)
                        {
                            // 使用 YAML 配置的卡牌練度
                            deckInfo = debugDeck.Select(id => new CardDeckInfo(
                                id,
                                debugYamlConfig.GetCardLevels(id)
                            )).ToList();
                            Console.WriteLine($"  Using card levels from YAML config");
                        }
                        else
                        {
                            // 使用 cardConfig.jsonc 的配置
                            deckInfo = CardConfig.ConvertDeckToSimulatorFormat(debugDeck);
                            Console.WriteLine($"  Using card levels from cardConfig.jsonc");
                        }

                        Deck deck = new Deck(deckInfo);

                        // 設定好友卡
                        if (friendCardId.HasValue)
                        {
                            deck.FriendCard = Card.GetInstance(friendCardId.Value);
                            Console.WriteLine($"  Friend Card Applied: {friendCardId.Value} ({deck.FriendCard.FullName})");
                        }

                        long score = sim.Run(deck, centerId);
                        Console.WriteLine($"Score: {score}");
                    }

                    Console.SetOut(originalOut);
                    Console.WriteLine($"\nDebug log saved to: {debugLogPath}");
                    return;
                }
            }
        }

        // ------------------------------------------------------------------
        // 步骤 2: 读取模拟任务（支持 YAML 或 JSONC）
        // ------------------------------------------------------------------
        YamlConfigManager? yamlConfig = null;
        List<int> globalCardPool;
        List<SimulationTask> tasks;
        string logDir;
        bool lgpMode = true;  // 默认 LGP 模式

        try
        {
            yamlConfig = new YamlConfigManager();

            if (yamlConfig.IsYamlMode)
            {
                Console.WriteLine($"✓ 使用 YAML 配置 (成员: {yamlConfig.MemberName})");

                // 从 YAML 获取卡池
                globalCardPool = yamlConfig.Config.CardIds;

                // 应用全局禁卡
                if (yamlConfig.Config.Optimizer.ForbiddenCards.Count > 0)
                {
                    Console.WriteLine($"[全局禁卡] 过滤 {yamlConfig.Config.Optimizer.ForbiddenCards.Count} 张卡片");
                    globalCardPool = globalCardPool
                        .Except(yamlConfig.Config.Optimizer.ForbiddenCards)
                        .ToList();
                }

                // 转换 YAML SongConfig 到 SimulationTask
                tasks = yamlConfig.Config.Songs.Select(song => new SimulationTask
                {
                    MusicId = song.MusicId,
                    Tier = song.Difficulty,
                    MLv = song.MasteryLevel,
                    ExcludeCards = new List<int>(),  // YAML 使用 banned_cards
                    SecondaryCenter = song.SecondaryCenter,
                    MustCards = new RequiredCards
                    {
                        All = song.MustcardsAll,
                        Any = song.MustcardsAny
                    },
                    MustSkills = song.MustSkills  // 如果 YAML 中沒有配置則為空列表（不檢查）
                }).ToList();

                logDir = yamlConfig.GetLogDir();
                lgpMode = yamlConfig.Config.LgpMode;
            }
            else
            {
                throw new FileNotFoundException("未找到 YAML 配置");
            }
        }
        catch (FileNotFoundException)
        {
            Console.WriteLine("⚠ 未找到 YAML 配置，使用 JSONC 配置");
            var taskConfig = TaskLoader.LoadTasks("task.jsonc");
            globalCardPool = taskConfig.CardPool;
            tasks = taskConfig.Task;
            logDir = "log";
            lgpMode = true;  // JSONC 默认 LGP 模式
        }

        // ------------------------------------------------------------------
        // 步骤 3: 使用 BatchSimulationService 執行批次模擬
        // ------------------------------------------------------------------
        Console.WriteLine("\n開始批次模擬...");
        try
        {
            BatchSimulationService.RunBatchSimulation(
                yamlConfig,
                onLog: Console.WriteLine,
                onProgress: null,  // CLI 已有 Tqdm 進度條，不需額外進度回呼
                cancellationToken: default
            );
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[FAIL] 批次模擬發生錯誤: {ex.Message}");
            Console.WriteLine($"堆疊追蹤: {ex.StackTrace}");
            Console.ResetColor();
            Console.WriteLine("\n按 [Enter] 鍵退出程序...");
            Console.ReadLine();
            Environment.Exit(1);
        }

        Console.WriteLine($"\n已完成全部模擬任務，按 [Enter] 退出程序...");
        Console.Read();
    }

    // 原本的 foreach 迴圈已移至 BatchSimulationService，保留供參考
    /*
        // ------------------------------------------------------------------
        // 步骤 3: 遍历每首歌曲进行模拟
        // ------------------------------------------------------------------
        foreach (var task in tasks)
        {
            string MusicId = task.MusicId;
            string Tier = task.Tier;

            Console.WriteLine($"\n--- 歌曲: {musicDb[MusicId].Title} ({Tier}) ---");
            Console.WriteLine("[卡池配置]");

            // 应用歌曲级禁卡
            HashSet<int> bannedCards = new HashSet<int>(task.ExcludeCards);
            if (yamlConfig != null && yamlConfig.IsYamlMode)
            {
                // 从 YAML 获取歌曲配置
                var songConfig = yamlConfig.Config.Songs
                    .FirstOrDefault(s => s.MusicId == MusicId && s.Difficulty == Tier);

                if (songConfig != null)
                {
                    // 合并歌曲级禁卡
                    var mergedBanned = yamlConfig.GetMergedBannedCards(songConfig);
                    bannedCards.UnionWith(mergedBanned);

                    if (mergedBanned.Count > 0)
                    {
                        Console.WriteLine($"[歌曲禁卡] 本曲禁用 {mergedBanned.Count} 张: [{string.Join(", ", mergedBanned)}]");
                    }
                }
            }

            List<int> secondaryCenter = task.SecondaryCenter;
            List<List<int>> mustcards = [task.MustCards.All, task.MustCards.Any, task.MustSkills];

            int centerChar = musicDb[MusicId].CenterCharacterId;

            HashSet<int> cardIdsSet = new(globalCardPool);
            cardIdsSet.ExceptWith(bannedCards);
            var cardPool = cardIdsSet.ToList();
            HashSet<int> primaryCenter = new();
            HashSet<int> otherCenter = new();

            foreach (int card in cardIdsSet)
                if (card / 1000 == centerChar)
                {
                    // TODO: 暫時將 UR(5)/LR(7)/BR(8)/DR(9) 都加入 primaryCenter
                    // 原邏輯: 只有 LR(7) 和 BR(8) 才加入 primaryCenter
                    // 問題: 地平系列等強力 UR 卡也應該可作為中心卡
                    // 未來需要: 根據卡片實際能力值或特定卡片 ID 白名單來精確判斷
                    var rarity = card / 100 % 10;
                    if (rarity == 5 || rarity == 7 || rarity == 8 || rarity == 9)
                        primaryCenter.Add(card);
                    else
                        otherCenter.Add(card);
                }

            foreach (int card in secondaryCenter)
                if (card / 1000 == centerChar && cardIdsSet.Contains(card))
                    primaryCenter.Add(card);

            HashSet<int> availableCenter;
            if (primaryCenter.Count > 0) 
                availableCenter = primaryCenter;
            else
                availableCenter = otherCenter;

            if (availableCenter.Count > 0)
                Console.WriteLine($"可用C位卡牌 ({availableCenter.Count}): [{string.Join(", ", availableCenter)}]");
            else
            {
                Console.WriteLine("无可用的C位卡牌");
            }

            Console.WriteLine($"共计 {cardPool.Count} 张备选卡牌，正在计算卡组数量...");
            Stopwatch sw = new();
            sw.Start();

            // 使用动态 log 目录
            string logPath = Path.Combine(
                    AppContext.BaseDirectory,
                    logDir,
                    $"simulation_results_{MusicId}_{Tier}.json"
                );

            DeckGenerator deckgen = new DeckGenerator(cardPool, mustcards, centerChar, availableCenter, logPath, lgpMode);
            sw.Stop();
            Console.WriteLine($"  卡组数量: {deckgen.TotalDecks}");
            Console.WriteLine($"  计算用时: {sw.ElapsedTicks / (decimal)Stopwatch.Frequency:F2}s");

            if (deckgen.TotalDecks == 0) continue;

            Simulator sim2 = new(MusicId, Tier, task.MLv);

            // 计算 BONUS_SFL（用于 PT 计算）
            double bonusSfl = 6.6;  // 默认值
            string tempDir = "temp";
            if (yamlConfig != null && yamlConfig.IsYamlMode)
            {
                // 获取歌唱成员 ID 列表（包含中心角色）
                var singerIds = new List<int>(musicDb[MusicId].SingerCharacters);
                singerIds.Add(centerChar);

                // 使用 YAML 配置计算 BONUS_SFL
                bonusSfl = yamlConfig.CalculateBonusSFL(singerIds);
                Console.WriteLine($"[PT 计算] BONUS_SFL = {bonusSfl:F4}");

                // 使用成员隔离的 temp 目录
                tempDir = yamlConfig.GetTempDir(MusicId);
            }

            // 读取朋友卡池配置
            List<int> friendCardPool = new();
            if (yamlConfig != null && yamlConfig.IsYamlMode)
            {
                var songConfig = yamlConfig.Config.Songs
                    .FirstOrDefault(s => s.MusicId == MusicId && s.Difficulty == Tier);

                // 优先使用歌曲级别朋友卡池，否则使用全局朋友卡池
                if (songConfig != null && songConfig.FriendCardPool.Count > 0)
                {
                    friendCardPool = songConfig.FriendCardPool;
                    Console.WriteLine($"[朋友卡池] 使用歌曲级别配置: {friendCardPool.Count} 张");
                }
                else if (yamlConfig.Config.FriendCardIds.Count > 0)
                {
                    friendCardPool = yamlConfig.Config.FriendCardIds;
                    Console.WriteLine($"[朋友卡池] 使用全局配置: {friendCardPool.Count} 张");
                }
            }

            if (friendCardPool.Count > 0)
            {
                Console.WriteLine($"  候选朋友卡: [{string.Join(", ", friendCardPool)}]");
            }

            Console.WriteLine($"[开始模拟]");
            Stopwatch sw2 = new();
            long bestScore = -1;
            int[] bestDeck = new int[6];
            int? bestCenter = 0;
            int? bestFriendCard = null;
            List<string> bestLog = new();
            Exception fatalError = null;
            string errorContextInfo = string.Empty;
            object lockObject = new();

            SimulationBuffer buffer = new(
                musicId: MusicId,
                tier: Tier,
                batchSize: 10000000,
                yamlConfig: yamlConfig,
                bonusSfl: bonusSfl,
                tempDir: tempDir,
                logDir: logDir
            );

            // 预先过滤 DR 卡 (全局过滤)
            var validFriendCandidates = friendCardPool
                .Where(fid => !(DB.DB_TAG.TryGetValue(fid, out var tags) && tags.Contains(Rarity.DR)))
                .ToList();

            IEnumerable<(int[] deck, int? center, int? friendCard)> workSource;
            long totalWorkItems;

            // 本地函数：展开工作项
            IEnumerable<(int[] deck, int? center, int? friendCard)> ExpandDecksWithFriends(DeckGenerator generator, List<int> friends)
            {
                foreach (var item in generator)
                {
                    bool any = false;
                    foreach (var fid in friends)
                    {
                        // 检查重复：如果卡组中已包含该朋友卡，则跳过
                        if (Array.IndexOf(item.deck, fid) != -1) continue;

                        yield return (item.deck, item.center, fid);
                        any = true;
                    }
                    // 如果没有有效的朋友卡（例如全部重复），则回退到无朋友卡模式
                    if (!any)
                    {
                        yield return (item.deck, item.center, null);
                    }
                }
            }

            if (validFriendCandidates.Count == 0)
            {
                // 无朋友卡：1:1 映射
                workSource = deckgen.Select(d => (d.deck, d.center, (int?)null));
                totalWorkItems = deckgen.TotalDecks;
            }
            else
            {
                // 有朋友卡：展开
                workSource = ExpandDecksWithFriends(deckgen, validFriendCandidates);
                // 估算总数 (可能略多于实际数，因为未扣除重复卡的情况)
                totalWorkItems = deckgen.TotalDecks * validFriendCandidates.Count;
            }

            sw2.Start();
            Parallel.ForEach(Tqdm.Wrap(workSource, total: totalWorkItems, printsPerSecond: 5), (item, state) =>
            {
                if (state.ShouldExitCurrentIteration || fatalError != null) return;

                var card_id_list = item.deck;
                var center_card = item.center;
                var friendCardId = item.friendCard;

                // 使用 YAML 配置的卡牌练度（如果有）
                List<CardDeckInfo> deckInfo;
                if (yamlConfig != null && yamlConfig.IsYamlMode)
                {
                    deckInfo = card_id_list.Select(id => new CardDeckInfo(
                        id,
                        yamlConfig.GetCardLevels(id)
                    )).ToList();
                }
                else
                {
                    deckInfo = CardConfig.ConvertDeckToSimulatorFormat(card_id_list.ToList());
                }

                Deck deckToSimulate = new Deck(deckInfo);
                if (friendCardId.HasValue)
                {
                    deckToSimulate.FriendCard = Card.GetInstance(friendCardId.Value);
                }

                long newScore = -1;
                try
                {
                    newScore = sim2.Run(deckToSimulate, (int)center_card);
                }
                catch (Exception ex)
                {
                    if (Interlocked.CompareExchange(ref fatalError, ex, null) == null)
                    {
                        errorContextInfo = $"卡组: ({string.Join(", ", card_id_list)})\nC位: {center_card}\n朋友卡: {friendCardId}";
                        state.Stop();
                    }
                    return;
                }

                buffer.AddResult(card_id_list, center_card, newScore, friendCardId);

                if (newScore > bestScore)
                {
                    lock (lockObject)
                    {
                        if (newScore > bestScore)
                        {
                            bestScore = newScore;
                            bestDeck = card_id_list;
                            bestCenter = center_card;
                            bestFriendCard = friendCardId;
                            bestLog = new List<string>();  // 无法获取最佳卡组的 CardLog，需要重新模拟
                            Console.WriteLine($"NEW HI-SCORE! Score: {bestScore:N0}".PadRight(Console.BufferWidth));
                            Console.WriteLine($"  Cards: ({string.Join(", ", card_id_list)})");
                            Console.WriteLine($"  Center: {center_card}");
                            if (friendCardId.HasValue)
                            {
                                Console.WriteLine($"  Friend: {friendCardId}");
                            }
                        }
                    }
                }
            });
            sw2.Stop();
            buffer.FlushFinal();
            buffer.MergeTempFiles();
            if (fatalError != null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n========== 模拟过程中发生严重错误 ==========");
                Console.WriteLine(errorContextInfo);
                Console.WriteLine($"错误详情: {fatalError.Message}");
                Console.WriteLine($"堆栈追踪: {fatalError.StackTrace}");
                Console.ResetColor();

                Console.WriteLine("\n按 [Enter] 键退出程序...");
                Console.ReadLine();

                Environment.Exit(1);
            }
            Console.WriteLine($"\n--- 模拟结果 ---");
            Console.WriteLine($"模拟 {deckgen.TotalDecks} 个卡组用时: {sw2.ElapsedTicks / (decimal)Stopwatch.Frequency:F2}s");
            Console.WriteLine($"歌曲: {musicDb[MusicId].Title} ({Tier})");
            Console.WriteLine($"最高分: {bestScore:N0}");
            Console.WriteLine($"卡组: ({string.Join(", ", bestDeck)})");
            Console.WriteLine($"C位:   {bestCenter}");
            var bestLogStr = string.Join(
                Environment.NewLine,
                bestLog
                    .Select((s, i) => new { s, i })
                    .GroupBy(x => x.i / 3)
                    .Select(g => string.Join(" | ", g.Select(x => x.s)))
            );
            Console.WriteLine($"Log ({bestLog.Count}):\n{bestLogStr}");
        }
        Console.WriteLine($"\n已完成全部模拟任务，按 [Enter] 退出程序...");
        Console.Read();
        */

    // === YAML 配置测试方法 ===
    static void TestYamlConfigLoading()
    {
        Console.WriteLine("\n=== YAML 配置测试 ===\n");

        try
        {
            var config = new YamlConfigManager();

            if (!config.IsYamlMode)
            {
                Console.WriteLine("❌ 未找到 YAML 配置，回退到 JSONC 模式");
                Console.WriteLine("\n提示: 使用 --config 参数指定配置文件");
                Console.WriteLine("示例: dotnet run -- --config ../config/member-stu92054.yaml");
                return;
            }

            Console.WriteLine($"✓ 成员名称: {config.MemberName}");
            Console.WriteLine($"✓ LGP 模式: {config.Config.LgpMode}");
            Console.WriteLine($"✓ Season 模式: {config.Config.SeasonMode}");
            Console.WriteLine($"✓ 卡池数量: {config.Config.CardIds.Count} 张");
            Console.WriteLine($"✓ 歌曲数量: {config.Config.Songs.Count} 首");
            Console.WriteLine($"✓ Log 目录: {config.GetLogDir()}");

            Console.WriteLine("\n✅ YAML 配置测试通过！");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n❌ 错误: {ex.Message}");
        }
    }
}