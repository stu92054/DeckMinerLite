using System.Collections;
using DeckMiner.Data;
using DeckMiner.Models;
using TqdmSharp;

namespace DeckMiner.Services
{
    public class SkillData
    {
        public List<int> RhythmGameSkillEffectId { get; set; }
    }

    public static class DB
    {
        public static Dictionary<int, HashSet<object>> DB_TAG = new();
    }

    // ========== 生成 DB_TAG ==========
    public static class TagGenerator
    {
        public static void BuildDBTag()
        {
            DB.DB_TAG.Clear();
            var dataManager = DataManager.Instance;
            var cardDb = dataManager.GetCardDatabase();
            var skillDb = dataManager.GetSkillDatabase();

            foreach (var kv in cardDb)
            {
                var data = kv.Value;
                if (data.RhythmGameSkillSeriesId == null || data.RhythmGameSkillSeriesId.Count == 0)
                    continue;

                int skillSeries = data.RhythmGameSkillSeriesId.Last();
                string skillId = $"{skillSeries}14";

                if (!skillDb.TryGetValue(skillId, out var skillData) || skillData.RhythmGameSkillEffectId == null)
                    continue;

                List<int> effects = skillData.RhythmGameSkillEffectId;

                HashSet<object> tag = new();

                foreach (int effect in effects)
                {
                    int effectType = effect / 100000000;
                    tag.Add((SkillEffectType)effectType);
                }

                tag.Add((Rarity)data.Rarity);
                DB.DB_TAG[data.CardSeriesId] = tag;
            }
        }
    }

    // ================== 计数 tag ==================
    public static class SkillTagCounter
    {
        // 直接累加到字典，避免额外分配与 GroupBy
        public static Dictionary<object, int> CountSkillTags(List<int> cardIds)
        {
            var dict = new Dictionary<object, int>();

            foreach (var cid in cardIds)
            {
                if (DB.DB_TAG.TryGetValue(cid, out var tagset))
                {
                    foreach (var t in tagset)
                    {
                        if (dict.TryGetValue(t, out int v))
                            dict[t] = v + 1;
                        else
                            dict[t] = 1;
                    }
                }
                else
                {
                    Console.WriteLine($"警告: 卡牌 {cid} 未在映射中找到。");
                }
            }

            return dict;
        }
    }

    // ================== 卡牌衝突規則 (已禁用) ==================
    // 注意：在某些特殊條件下 IDOME 卡可以與這些卡共存
    // 此規則已註解掉，保留供未來可能需要時使用
    // 參考：src/deck_gen/DeckGen2.py:14-30
    public static class CardConflictRules
    {
        /*
        // IDOME 卡與特定舊卡的衝突規則（已停用）
        // 原因：特殊條件下可共存，且過濾會減少卡組多樣性
        private static readonly Dictionary<int, HashSet<int>> ConflictRules = new()
        {
            // IDOME 帆 -> P吟、Blast芽、暧昧Mayday、水果帆、水果吟、太陽沙、COCO夏芽
            { 1031530, new HashSet<int> { 1041513, 1042515, 1043515, 1031531, 1041516, 1032529, 1043516 } },
            // IDOME 沙 -> 同上
            { 1032528, new HashSet<int> { 1041513, 1042515, 1043515, 1031531, 1041516, 1032529, 1043516 } },
            // IDOME 乃 -> 同上
            { 1033524, new HashSet<int> { 1041513, 1042515, 1043515, 1031531, 1041516, 1032529, 1043516 } }
        };

        /// <summary>
        /// 檢查卡組中是否存在衝突卡牌（已停用）
        /// </summary>
        /// <param name="cardIds">卡組中的卡牌 ID 列表</param>
        /// <returns>如果存在衝突則返回 true</returns>
        public static bool HasCardConflict(IEnumerable<int> cardIds)
        {
            var deckSet = new HashSet<int>(cardIds);

            foreach (var (restrictedCard, conflictingCards) in ConflictRules)
            {
                if (deckSet.Contains(restrictedCard))
                {
                    foreach (var conflictCard in conflictingCards)
                    {
                        if (deckSet.Contains(conflictCard))
                            return true;
                    }
                }
            }

            return false;
        }
        */
    }

    // ================== 生成角色分布 ==================
    public static class RoleDistribution
    {
        // 返回去重后的角色分布（sorted arrays）
        public static List<int[]> GenerateRoleDistributions(List<int> allCharacters, bool lgpMode = true)
        {
            var seen = new HashSet<string>();
            var results = new List<int[]>();

            int n = allCharacters.Count;
            // LGP 模式: doubleCount = 0..3 (允许 0-3 个角色使用双卡)
            // 日常模式: doubleCount = 0 (每个角色最多 1 张卡)
            int maxDoubleCount = lgpMode ? 3 : 0;
            for (int doubleCount = 0; doubleCount <= maxDoubleCount; doubleCount++)
            {
                // 选择 doubleCount 个角色作为双卡角色
                foreach (var doubles in CombinationsIndexBased(allCharacters, doubleCount))
                {
                    int remaining = 6 - doubleCount * 2;
                    var remainChars = new List<int>(allCharacters.Count - doubles.Count);
                    // build remainChars quickly
                    var doublesSet = new HashSet<int>(doubles);
                    foreach (var ch in allCharacters)
                        if (!doublesSet.Contains(ch))
                            remainChars.Add(ch);

                    // choose singles
                    foreach (var singles in CombinationsIndexBased(remainChars, remaining))
                    {
                        // build distribution: doubles twice + singles
                        int totalLen = doubles.Count * 2 + singles.Count;
                        var dist = new int[totalLen];
                        int idx = 0;
                        foreach (var d in doubles) dist[idx++] = d;
                        foreach (var d in doubles) dist[idx++] = d;
                        foreach (var s in singles) dist[idx++] = s;

                        var sorted = dist.OrderBy(x => x).ToArray();
                        string key = string.Join(",", sorted);
                        if (seen.Add(key))
                        {
                            results.Add(sorted);
                        }
                    }
                }
            }

            return results;
        }

        // index-based combination generator returns List<int> per combination
        static IEnumerable<List<T>> CombinationsIndexBased<T>(List<T> list, int k)
        {
            if (k == 0)
            {
                yield return new List<T>();
                yield break;
            }
            if (k > list.Count) yield break;

            var indices = new int[k];
            for (int i = 0; i < k; i++)
                indices[i] = i;

            while (true)
            {
                var res = new List<T>(k);
                for (int i = 0; i < k; i++)
                    res.Add(list[indices[i]]);
                yield return res;

                // move to next indices
                int pos = k - 1;
                while (pos >= 0 && indices[pos] == list.Count - k + pos)
                    pos--;
                if (pos < 0) break;
                indices[pos]++;
                for (int j = pos + 1; j < k; j++)
                    indices[j] = indices[j - 1] + 1;
            }
        }
    }

    // =================== 主生成器 ===================
    public class DeckGenerator : IEnumerable<(int[] deck, int? center)>
    {
        List<int> cardpool;
        List<List<int>> mustcards;
        int? centerChar;
        HashSet<int> centerCard;
        Dictionary<int, List<int>> charCards = new();
        HashSet<string> simulated = new();
        bool lgpMode;

        public long TotalDecks { get; private set; }

        public DeckGenerator(
            List<int> cardpool,
            List<List<int>> mustcards,
            int? center_char = null,
            HashSet<int> center_card = null,
            string logPath = null,
            bool lgpMode = true)
        {
            this.cardpool = cardpool;
            this.mustcards = mustcards;
            this.centerChar = center_char;
            this.centerCard = center_card;
            this.lgpMode = lgpMode;

            try
            {
                var simulatedResult = SimulationBuffer.LoadResultsFromJson(logPath);
                foreach (var result in simulatedResult)
                {
                    simulated.Add(SimulationBuffer.MakeKey(result.DeckCardIds));
                }
            }
            catch (FileNotFoundException){}
            TagGenerator.BuildDBTag();

            foreach (int cid in cardpool)
            {
                int charId = cid / 1000;
                if (!charCards.TryGetValue(charId, out var list))
                {
                    list = new List<int>();
                    charCards[charId] = list;
                }
                list.Add(cid);
            }

            // Pre-sort each char's card pool for deterministic behavior and slightly better cache locality
            foreach (var kv in charCards)
                kv.Value.Sort();

            TotalDecks = ComputeTotalCount();
        }

        // 迭代生成所有卡组
        public IEnumerator<(int[] deck, int? center)> GetEnumerator()
        {
            var allChars = new List<int>(charCards.Keys);
            if (allChars.Count < 3) yield break;

            foreach (var distr in RoleDistribution.GenerateRoleDistributions(allChars, lgpMode))
            {
                if (centerChar.HasValue && Array.IndexOf(distr, centerChar.Value) < 0)
                    continue;

                foreach (var item in GenerateByDistribution(distr))
                    yield return item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        // ---------------- 检查技能 ----------------
        bool CheckSkillTags(Dictionary<object, int> tagCount)
        {
            // mustcards[2]：需要所有技能都不为0（与原逻辑一致）
            foreach (var skill in mustcards[2])
            {
                var k = (SkillEffectType)skill;
                if (!tagCount.TryGetValue(k, out int cnt) || cnt == 0)
                    return false;
            }

            // DR 数量限制
            // LGP 模式（双卡）：DR <= 1
            // 日常模式（单卡）：不限制 DR 数量
            if (lgpMode)
            {
                tagCount.TryGetValue(Rarity.DR, out int drCount);
                if (drCount > 1)
                    return false;
            }

            return true;
        }

        // ================== 分发角色 → 枚举卡组 ==================
        
        IEnumerable<(int[], int?)> GenerateByDistribution(int[] distribution)
        {
            // char -> count
            var charCounts = new Dictionary<int, int>();
            foreach (var c in distribution)
            {
                if (charCounts.TryGetValue(c, out int v)) charCounts[c] = v + 1;
                else charCounts[c] = 1;
            }

            // build per-character card choices (each entry is a List<int[]> where each int[] is chosen cards for that char)
            var cardChoices = new List<List<int[]>>(charCounts.Count);
            foreach (var kv in charCounts)
            {
                int cid = kv.Key;
                int count = kv.Value;
                var pool = charCards[cid];
                if (count == 1)
                {
                    var choices = new List<int[]>(pool.Count);
                    foreach (var card in pool) choices.Add(new[] { card });
                    cardChoices.Add(choices);
                }
                else if (count == 2)
                {
                    // combinations(pool, 2)
                    var combos = new List<int[]>();
                    int m = pool.Count;
                    for (int i = 0; i < m; i++)
                        for (int j = i + 1; j < m; j++)
                            combos.Add(new[] { pool[i], pool[j] });
                    cardChoices.Add(combos);
                }
                else
                {
                    throw new InvalidOperationException("角色数量超过2，不符合规则");
                }
            }

            // 笛卡尔积迭代（索引递归，避免中间大分配）
            foreach (var pick in CartesianIndexBased(cardChoices))
            {
                // pick is List<int[]> of chosen per-role arrays
                int deckSize = 0;
                foreach (var arr in pick) deckSize += arr.Length;

                var deck = new List<int>(deckSize);
                foreach (var arr in pick) deck.AddRange(arr);

                // sort deck to produce its key (used for simulated check etc.)
                deck.Sort();
                if (simulated.Contains(SimulationBuffer.MakeKey(deck))) continue;
                if (!CheckMustCards(deck)) continue;
                // if (HasCardConflict(deck)) continue;

                var tags = SkillTagCounter.CountSkillTags(deck);
                if (!CheckSkillTags(tags)) continue;

                // build centers
                HashSet<int> centers;
                if (centerCard != null)
                {
                    centers = new HashSet<int>();
                    foreach (var d in deck)
                        if (centerCard.Contains(d)) centers.Add(d);
                    if (centers.Count == 0) continue;
                }
                else
                {
                    centers = new HashSet<int> { 0 };
                }

                // prepare allowed first/last candidates to reduce permutations
                var allowedFirst = new List<int>();
                var allowedLast = new List<int>();
                foreach (var d in deck)
                {
                    if (!DB.DB_TAG.TryGetValue(d, out var tagset)) continue;
                    if (!tagset.Contains(SkillEffectType.ScoreGain)) allowedFirst.Add(d);
                    if (!tagset.Contains(SkillEffectType.DeckReset)) allowedLast.Add(d);
                }

                if (allowedFirst.Count == 0 || allowedLast.Count == 0)
                    continue;

                // generate permutations with fixed first and last to reduce search space
                foreach (var perm in PermutationsWithFixedEnds(deck, allowedFirst, allowedLast))
                {
                    // yield for each allowed center
                    if (centerCard != null)
                    {
                        foreach (var c in centers)
                            yield return (perm, c);
                    }
                    else
                    {
                        yield return (perm, null);
                    }
                }
            }
        }

        // =================================== 工具方法 ===================================

        bool CheckMustCards(List<int> deck)
        {
            // mustcards[0]: 必须全部包含 (All)
            if (mustcards[0].Count > 0)
            {
                if (!mustcards[0].All(deck.Contains))
                    return false;
            }

            // mustcards[1]: 至少包含一个 (Any)
            if (mustcards[1].Count > 0)
            {
                if (!mustcards[1].Any(deck.Contains))
                    return false;
            }

            return true;
        }

        bool HasCardConflict(List<int> deck)
        {
            // 你可以在这里实现你自己的冲突判断逻辑
            return false;
        }

        // Cartesian using index recursion to avoid LINQ allocations
        static IEnumerable<List<int[]>> CartesianIndexBased(List<List<int[]>> sequences)
        {
            int k = sequences.Count;
            if (k == 0) { yield break; }

            var indices = new int[k];
            var sizes = new int[k];
            for (int i = 0; i < k; i++)
            {
                sizes[i] = sequences[i].Count;
                if (sizes[i] == 0) yield break; // one sequence empty -> no product
            }

            while (true)
            {
                var res = new List<int[]>(k);
                for (int i = 0; i < k; i++)
                    res.Add(sequences[i][indices[i]]);
                yield return res;

                // increment indices
                int pos = k - 1;
                while (pos >= 0 && ++indices[pos] >= sizes[pos])
                {
                    indices[pos] = 0;
                    pos--;
                }
                if (pos < 0) break;
            }
        }

        // permutations optimization:
        // 固定首位和末位只排列中间元素（如果 deck 长度为 n，则排列中间 n-2 项）
        static IEnumerable<int[]> PermutationsWithFixedEnds(List<int> deck, List<int> allowedFirst, List<int> allowedLast)
        {
            int n = deck.Count;
            if (n <= 1)
            {
                yield return deck.ToArray();
                yield break;
            }
            if (n == 2)
            {
                // only two orders, still filter by allowedFirst/allowedLast
                var a = deck[0]; var b = deck[1];
                if (allowedFirst.Contains(a) && allowedLast.Contains(b))
                    yield return new[] { a, b };
                if (allowedFirst.Contains(b) && allowedLast.Contains(a))
                    yield return new[] { b, a };
                yield break;
            }

            // build set for quick membership check
            var deckSet = new HashSet<int>(deck);

            // middle pool is deck minus chosen first and last
            // iterate allowed first/last pairs
            foreach (var first in allowedFirst)
            {
                if (!deckSet.Contains(first)) continue;
                foreach (var last in allowedLast)
                {
                    if (!deckSet.Contains(last)) continue;
                    if (first == last) continue; // same element cannot be both first and last for unique elements

                    // build middle list
                    var middle = new List<int>(n - 2);
                    foreach (var d in deck)
                    {
                        if (d == first || d == last) continue;
                        middle.Add(d);
                    }

                    // permute middle (size n-2, often 4 -> 24 permutations)
                    foreach (var midPerm in PermuteInternalArray(middle))
                    {
                        var arr = new int[n];
                        arr[0] = first;
                        for (int i = 0; i < midPerm.Length; i++) arr[1 + i] = midPerm[i];
                        arr[n - 1] = last;
                        yield return arr;
                    }
                }
            }
        }

        // permute small array (returns all permutations)
        static IEnumerable<int[]> PermuteInternalArray(List<int> list)
        {
            int n = list.Count;
            if (n == 0) { yield return Array.Empty<int>(); yield break; }
            var arr = list.ToArray();
            foreach (var perm in HeapPermutation(arr, n))
                yield return perm;
        }

        // Heap's algorithm implementation returning copies
        static IEnumerable<int[]> HeapPermutation(int[] array, int size)
        {
            int n = size;
            var a = new int[n];
            Array.Copy(array, a, n);
            var c = new int[n];
            yield return (int[])a.Clone();

            int i = 0;
            while (i < n)
            {
                if (c[i] < i)
                {
                    if (i % 2 == 0)
                    {
                        Swap(a, 0, i);
                    }
                    else
                    {
                        Swap(a, c[i], i);
                    }
                    yield return (int[])a.Clone();
                    c[i]++;
                    i = 0;
                }
                else
                {
                    c[i] = 0;
                    i++;
                }
            }
        }

        static void Swap(int[] a, int i, int j)
        {
            int t = a[i]; a[i] = a[j]; a[j] = t;
        }

        // 计算卡组总数
        long ComputeTotalCount()
        {
            var allChars = new List<int>(charCards.Keys);
            if (allChars.Count < 3) return 0;

            var dists = RoleDistribution.GenerateRoleDistributions(allChars, lgpMode);

            long total = 0;

            Parallel.ForEach(
                Tqdm.Wrap(
                    dists,
                    printsPerSecond: 5),
                () => 0L, // 每个线程的局部计数器
                (dist, state, localCount) =>
                {
                    if (centerChar.HasValue && Array.IndexOf(dist, centerChar.Value) < 0)
                        return localCount;

                    localCount += CountByDistribution(dist);
                    return localCount;
                },
                localCount =>
                {
                    Interlocked.Add(ref total, localCount);
                }
            );

            return total;
        }

        int CountByDistribution(int[] distribution)
        {
            int count = 0;

            foreach (var (deck, center) in GenerateByDistribution(distribution))
                count++;

            return count;
        }
    }
}
