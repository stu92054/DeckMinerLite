using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DeckMiner.Config;
using DeckMiner.Data;
using DeckMiner.Models;
using TqdmSharp;

namespace DeckMiner.Services
{
    /// <summary>
    /// 批次模擬服務
    /// 提供給 CLI 和 GUI 共用的批次模擬邏輯
    /// </summary>
    public static class BatchSimulationService
    {
        /// <summary>
        /// 執行批次模擬
        /// </summary>
        /// <param name="yamlConfig">YAML 配置管理器</param>
        /// <param name="onLog">日誌輸出回呼</param>
        /// <param name="onProgress">進度更新回呼 (currentSong, totalSongs, message)</param>
        /// <param name="cancellationToken">取消令牌</param>
        public static void RunBatchSimulation(
            YamlConfigManager yamlConfig,
            Action<string> onLog = null,
            Action<int, int, string> onProgress = null,
            CancellationToken cancellationToken = default,
            int maxDegreeOfParallelism = -1)
        {
            // 預設日誌輸出到 Console
            onLog ??= Console.WriteLine;

            try
            {
                // 初始化資料管理器
                var dataManager = DataManager.Instance;
                var cardDb = dataManager.GetCardDatabase();
                var skillDb = dataManager.GetSkillDatabase();
                var centerAttrDb = dataManager.GetCenterAttributeDatabase();
                var centerSkillDb = dataManager.GetCenterSkillDatabase();
                var musicDb = dataManager.GetMusicDatabase();

                // 初始化 CardDataManager 和 SkillDataManager
                CardDataManager.Initialize(cardDb);
                SkillDataManager.Initialize(skillDb, centerAttrDb, centerSkillDb);

                // 從 YAML 配置轉換為 SimulationTask
                var tasks = yamlConfig.Config.Songs.Select(song => new SimulationTask
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
                    MustSkills = song.MustSkills
                }).ToList();

                // 從 YAML 獲取卡池
                var globalCardPool = yamlConfig.Config.CardIds;

                // 應用全域禁卡
                if (yamlConfig.Config.Optimizer.ForbiddenCards.Count > 0)
                {
                    onLog($"[全域禁卡] 過濾 {yamlConfig.Config.Optimizer.ForbiddenCards.Count} 張卡片");
                    globalCardPool = globalCardPool
                        .Except(yamlConfig.Config.Optimizer.ForbiddenCards)
                        .ToList();
                }

                var lgpMode = yamlConfig.Config.LgpMode;
                var logDir = yamlConfig.GetLogDir();

                onLog($"[INFO] Starting batch simulation for {tasks.Count} tasks");
                onLog($"[INFO] LGP Mode: {lgpMode}");
                onLog($"[INFO] Global card pool: {globalCardPool.Count} cards");

                int currentSongIndex = 0;
                int totalSongs = tasks.Count;

                // 遍歷每首歌曲進行模擬
                foreach (var task in tasks)
                {
                    // 檢查取消
                    cancellationToken.ThrowIfCancellationRequested();

                    currentSongIndex++;
                    string MusicId = task.MusicId;
                    string Tier = task.Tier;

                    onLog($"\n--- 歌曲 {currentSongIndex}/{totalSongs}: {musicDb[MusicId].Title} ({Tier}) ---");
                    onProgress?.Invoke(currentSongIndex, totalSongs, $"模擬 {musicDb[MusicId].Title} ({Tier})");

                    onLog("[卡池配置]");

                    // 應用歌曲級禁卡
                    HashSet<int> bannedCards = new HashSet<int>(task.ExcludeCards);
                    if (yamlConfig != null && yamlConfig.IsYamlMode)
                    {
                        // 從 YAML 獲取歌曲配置
                        var songConfig = yamlConfig.Config.Songs
                            .FirstOrDefault(s => s.MusicId == MusicId && s.Difficulty == Tier);

                        if (songConfig != null)
                        {
                            // 合併歌曲級禁卡
                            var mergedBanned = yamlConfig.GetMergedBannedCards(songConfig);
                            bannedCards.UnionWith(mergedBanned);

                            if (mergedBanned.Count > 0)
                            {
                                onLog($"[歌曲禁卡] 本曲禁用 {mergedBanned.Count} 張: [{string.Join(", ", mergedBanned)}]");
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
                        onLog($"可用C位卡牌 ({availableCenter.Count}): [{string.Join(", ", availableCenter)}]");
                    else
                    {
                        onLog("無可用的C位卡牌");
                    }

                    onLog($"共計 {cardPool.Count} 張備選卡牌，正在計算卡組數量...");
                    Stopwatch sw = new();
                    sw.Start();

                    // 使用動態 log 目錄
                    string logPath = Path.Combine(
                            AppContext.BaseDirectory,
                            logDir,
                            $"simulation_results_{MusicId}_{Tier}.json"
                        );

                    bool forceRecalc = yamlConfig?.Config?.ForceRecalc ?? false;
                    DeckGenerator deckgen = new DeckGenerator(cardPool, mustcards, centerChar, availableCenter, logPath, lgpMode, forceRecalc);
                    sw.Stop();
                    onLog($"  卡組數量: {deckgen.TotalDecks}");
                    onLog($"  計算用時: {sw.ElapsedTicks / (decimal)Stopwatch.Frequency:F2}s");

                    if (deckgen.TotalDecks == 0)
                    {
                        onLog("[WARN] 無有效卡組，跳過此歌曲");
                        continue;
                    }

                    Simulator sim2 = new(MusicId, Tier, task.MLv);

                    // 計算 BONUS_SFL（用於 PT 計算）
                    double bonusSfl = 6.6;  // 預設值
                    string tempDir = "temp";
                    if (yamlConfig != null && yamlConfig.IsYamlMode)
                    {
                        // 獲取歌唱成員 ID 列表（包含中心角色）
                        var singerIds = new List<int>(musicDb[MusicId].SingerCharacters);
                        singerIds.Add(centerChar);

                        // 使用 YAML 配置計算 BONUS_SFL
                        bonusSfl = yamlConfig.CalculateBonusSFL(singerIds);
                        onLog($"[PT 計算] BONUS_SFL = {bonusSfl:F4}");

                        // 使用成員隔離的 temp 目錄
                        tempDir = yamlConfig.GetTempDir(MusicId);
                    }

                    // 讀取朋友卡池配置
                    List<int> friendCardPool = new();
                    if (yamlConfig != null && yamlConfig.IsYamlMode)
                    {
                        var songConfig = yamlConfig.Config.Songs
                            .FirstOrDefault(s => s.MusicId == MusicId && s.Difficulty == Tier);

                        // 優先使用歌曲級別朋友卡池，否則使用全域朋友卡池
                        if (songConfig != null && songConfig.FriendCardPool.Count > 0)
                        {
                            friendCardPool = songConfig.FriendCardPool;
                            onLog($"[朋友卡池] 使用歌曲級別配置: {friendCardPool.Count} 張");
                        }
                        else if (yamlConfig.Config.FriendCardIds.Count > 0)
                        {
                            friendCardPool = yamlConfig.Config.FriendCardIds;
                            onLog($"[朋友卡池] 使用全域配置: {friendCardPool.Count} 張");
                        }
                    }

                    if (friendCardPool.Count > 0)
                    {
                        onLog($"  候選朋友卡: [{string.Join(", ", friendCardPool)}]");
                    }

                    onLog($"[開始模擬]");
                    Stopwatch sw2 = new();
                    long bestScore = -1;
                    int[] bestDeck = new int[6];
                    int? bestCenter = 0;
                    int? bestFriendCard = null;
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
                        logDir: logDir,
                        forceRecalc: forceRecalc
                    );

                    // 預先過濾 DR 卡 (全域過濾)
                    var validFriendCandidates = friendCardPool
                        .Where(fid => !(DB.DB_TAG.TryGetValue(fid, out var tags) && tags.Contains(Rarity.DR)))
                        .ToList();

                    IEnumerable<(int[] deck, int? center, int? friendCard)> workSource;
                    long totalWorkItems;

                    // 本地函數：展開工作項
                    IEnumerable<(int[] deck, int? center, int? friendCard)> ExpandDecksWithFriends(DeckGenerator generator, List<int> friends)
                    {
                        foreach (var item in generator)
                        {
                            // 檢查取消
                            if (cancellationToken.IsCancellationRequested)
                                yield break;

                            bool any = false;
                            foreach (var fid in friends)
                            {
                                // 檢查重複：如果卡組中已包含該朋友卡，則跳過
                                if (Array.IndexOf(item.deck, fid) != -1) continue;

                                yield return (item.deck, item.center, fid);
                                any = true;
                            }
                            // 如果沒有有效的朋友卡（例如全部重複），則回退到無朋友卡模式
                            if (!any)
                            {
                                yield return (item.deck, item.center, null);
                            }
                        }
                    }

                    if (validFriendCandidates.Count == 0)
                    {
                        // 無朋友卡：1:1 映射
                        workSource = deckgen.Select(d => (d.deck, d.center, (int?)null));
                        totalWorkItems = deckgen.TotalDecks;
                    }
                    else
                    {
                        // 有朋友卡：展開
                        workSource = ExpandDecksWithFriends(deckgen, validFriendCandidates);
                        // 估算總數 (可能略多於實際數，因為未扣除重複卡的情況)
                        totalWorkItems = deckgen.TotalDecks * validFriendCandidates.Count;
                    }

                    sw2.Start();
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = maxDegreeOfParallelism > 0 ? maxDegreeOfParallelism : -1
                    };
                    if (maxDegreeOfParallelism == 1)
                        onLog?.Invoke("[INFO] 單執行緒模式 (MaxDegreeOfParallelism=1)");
                    try
                    {
                        Parallel.ForEach(Tqdm.Wrap(workSource, total: totalWorkItems, printsPerSecond: 5), parallelOptions, (item, state) =>
                        {
                            // 檢查取消
                            if (cancellationToken.IsCancellationRequested)
                            {
                                state.Stop();
                                return;
                            }

                            if (state.ShouldExitCurrentIteration || fatalError != null) return;

                            var card_id_list = item.deck;
                            var center_card = item.center;
                            var friendCardId = item.friendCard;

                            // 使用 YAML 配置的卡牌練度（如果有）
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
                                deckToSimulate.FriendCard = Card.GetFriendInstance(friendCardId.Value);
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
                                    errorContextInfo = $"卡組: ({string.Join(", ", card_id_list)})\nC位: {center_card}\n朋友卡: {friendCardId}";
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
                                        onLog($"NEW HI-SCORE! Score: {bestScore:N0}");
                                        onLog($"  Cards: ({string.Join(", ", card_id_list)})");
                                        onLog($"  Center: {center_card}");
                                        if (friendCardId.HasValue)
                                        {
                                            onLog($"  Friend: {friendCardId}");
                                        }
                                    }
                                }
                            }
                        });
                    }
                    finally
                    {
                        // 無論模擬是否異常中斷，都確保已累積的結果被寫入磁碟
                        sw2.Stop();
                        buffer.FlushFinal();
                        buffer.MergeTempFiles();
                    }

                    // 檢查是否被取消
                    cancellationToken.ThrowIfCancellationRequested();

                    if (fatalError != null)
                    {
                        onLog("\n========== 模擬過程中發生嚴重錯誤 ==========");
                        onLog(errorContextInfo);
                        onLog($"錯誤詳情: {fatalError.Message}");
                        onLog($"堆疊追蹤: {fatalError.StackTrace}");
                        throw fatalError;
                    }

                    onLog($"\n--- 模擬結果 ---");
                    onLog($"模擬 {deckgen.TotalDecks} 個卡組用時: {sw2.ElapsedTicks / (decimal)Stopwatch.Frequency:F2}s");
                    onLog($"歌曲: {musicDb[MusicId].Title} ({Tier})");
                    onLog($"最高分: {bestScore:N0}");
                    onLog($"卡組: ({string.Join(", ", bestDeck)})");
                    onLog($"C位:   {bestCenter}");
                    if (bestFriendCard.HasValue)
                    {
                        onLog($"朋友卡: {bestFriendCard}");
                    }
                }

                onLog($"\n[PASS] 已完成全部 {totalSongs} 首歌曲的模擬任務");
            }
            catch (OperationCanceledException)
            {
                onLog("\n[INFO] 模擬已被使用者取消");
                throw;
            }
            catch (Exception ex)
            {
                onLog($"\n[FAIL] 批次模擬發生錯誤: {ex.Message}");
                throw;
            }
        }
    }
}
