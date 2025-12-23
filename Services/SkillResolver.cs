using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using static System.Math;
using DeckMiner.Data;
using DeckMiner.Models;
using System.Diagnostics;

namespace DeckMiner.Services
{
    // 在 C# 中，使用静态类实现工具函数集合
    public static class SkillResolver
    {
        // 对应 Python 中的 UNIT_DICT，C# 中使用 Dictionary<int, HashSet<int>>
        private static readonly Dictionary<int, HashSet<int>> UnitDict = new Dictionary<int, HashSet<int>>
        {
            { 101, new HashSet<int> { 1021, 1031, 1041 } },
            { 102, new HashSet<int> { 1022, 1032, 1042 } },
            { 103, new HashSet<int> { 1023, 1033, 1043 } },
            { 105, new HashSet<int> { 1051, 1052 } }
        };

        private static AttributeType MapEffectToAttribute(CenterAttributeEffectType effectType)
        {
            return effectType switch
            {
                CenterAttributeEffectType.SmileRateChange => AttributeType.Smile,
                CenterAttributeEffectType.PureRateChange => AttributeType.Pure,
                CenterAttributeEffectType.CoolRateChange => AttributeType.Cool,
                CenterAttributeEffectType.MentalRateChange => AttributeType.Mental,
                CenterAttributeEffectType.ConsumeAPChange => AttributeType.Cost,
                _ => AttributeType.None,
                // _ => throw new ArgumentOutOfRangeException(nameof(effectType), $"未预期的效果类型: {effectType}"),
            };
        }

        // -------------------------------------------------------------------
        // I. C位特性目标 (Target Resolution)
        // -------------------------------------------------------------------

        /// <summary>
        /// 根据ID检查给定条件是否满足。对应 Python 的 CheckTarget。
        /// </summary>
        public static bool CheckTarget(string targetId, int? charId = null)
        {
            // C# 逻辑与 Python 保持一致，包括长度检查、解析和匹配逻辑
            if (targetId.Length != 5)
            {
                // logger.Error(" 错误: 目标ID长度不符合已知规则...");
                return false;
            }

            if (!int.TryParse(targetId.Substring(0, 1), out int typeValue) ||
                !int.TryParse(targetId.Substring(1), out int targetValue))
            {
                // logger.Error(" 错误: 无法解析条件ID...");
                return false;
            }

            if (!Enum.IsDefined(typeof(TargetType), typeValue))
            {
                // logger.Error(" 未知条件类型...");
                return false;
            }
            TargetType targetType = (TargetType)typeValue;

            bool isSatisfied;

            // C# 中的 switch 表达式 (或 switch 语句) 替代 Python 的 match/case
            switch (targetType)
            {
                case TargetType.Member:
                    // charId.HasValue 检查 charId 是否传入
                    isSatisfied = charId.HasValue && charId.Value == targetValue;
                    break;
                case TargetType.Unit:
                    // 使用 UnitDict 进行检查
                    isSatisfied = charId.HasValue && UnitDict.ContainsKey(targetValue) && UnitDict[targetValue].Contains(charId.Value);
                    break;
                case TargetType.Generation:
                    isSatisfied = charId.HasValue && (charId.Value / 10 == targetValue);
                    break;
                case TargetType.StyleType:
                    isSatisfied = false; // 暂无实装
                    break;
                case TargetType.All:
                    isSatisfied = true;
                    break;
                default:
                    isSatisfied = false;
                    break;
            }
            return isSatisfied;
        }

        /// <summary>
        /// 检查多个目标条件中是否任一满足。对应 Python 的 CheckMultiTarget。
        /// </summary>
        public static bool CheckMultiTarget(List<string> targetIds, int? charId = null)
        {
            // Python 的 any 对应 C# 的 Linq.Any
            return targetIds.Any(id => CheckTarget(id, charId));
        }

        // -------------------------------------------------------------------
        // II. C位特性效果应用 (Center Attribute Effect Application)
        // -------------------------------------------------------------------

        /// <summary>
        /// 根据EffectsID解析并应用C位特性。对应 Python 的 ApplyCenterAttribute。
        /// </summary>
        public static void ApplyCenterAttribute(LiveStatus playerAttrs, int effectId, List<string> targetIds)
        {
            int enumBaseValue, changeDirection, valueData;

            // 假设 effectId 为 8位 (10000000-99999999) 或 9位 (100000000-999999999)
            if (effectId >= 10000000 && effectId <= 99999999) // 8位
            {
                enumBaseValue = effectId / 10000000;
                changeDirection = effectId / 1000000 % 10;
                valueData = effectId % 1000000;
            }
            else if (effectId >= 100000000 && effectId <= 999999999) // 9位
            {
                enumBaseValue = effectId / 10000000; // 此时解析出前两位
                changeDirection = effectId / 1000000 % 10;
                valueData = effectId % 1000000;
            }
            else return;

            // 直接转换，不使用 Enum.IsDefined (反射开销太大)
            CenterAttributeEffectType effectType = (CenterAttributeEffectType)enumBaseValue;

            int changeSign = (changeDirection == 0) ? 1 : -1; // 0=增加/正向, 1=减少/负向
            int intChange;
            double doubleChange;
            double multiplier;
            var targetAttr = MapEffectToAttribute(effectType);
            var cards = playerAttrs.Deck.Cards;
            // C# switch 语句实现效果应用
            switch (effectType)
            {
                case CenterAttributeEffectType.SmileRateChange:
                case CenterAttributeEffectType.PureRateChange:
                case CenterAttributeEffectType.CoolRateChange:
                case CenterAttributeEffectType.MentalRateChange:
                    doubleChange = valueData / 10000.0;
                    multiplier = 1.0 + doubleChange * changeSign;
                    // 套用到自己的 6 張卡
                    for (int i = 0; i < 6; i++)
                    {
                        var card = cards[i];
                        if (CheckMultiTarget(targetIds, card.CharactersId))
                            card.ApplyAttributeRateChange(targetAttr, multiplier);
                    }
                    // 套用到朋友卡片（朋友卡片數值會受自己隊長被動影響）
                    if (playerAttrs.Deck.FriendCard != null)
                    {
                        if (CheckMultiTarget(targetIds, playerAttrs.Deck.FriendCard.CharactersId))
                            playerAttrs.Deck.FriendCard.ApplyAttributeRateChange(targetAttr, multiplier);
                    }
                    break;


                case CenterAttributeEffectType.SmileValueChange:
                case CenterAttributeEffectType.PureValueChange:
                case CenterAttributeEffectType.CoolValueChange:
                case CenterAttributeEffectType.MentalValueChange:
                case CenterAttributeEffectType.ConsumeAPChange:
                    intChange = valueData * changeSign;
                    // 应用数值变化，遍历卡牌并检查目标
                    for (int i = 0; i < 6; i++)
                    {
                        var card = cards[i];
                        if (CheckMultiTarget(targetIds, card.CharactersId))
                            card.ApplyAttributeValueChange(targetAttr, intChange);
                    }
                    // 套用到朋友卡片（朋友卡片數值會受自己隊長被動影響）
                    if (playerAttrs.Deck.FriendCard != null)
                    {
                        if (CheckMultiTarget(targetIds, playerAttrs.Deck.FriendCard.CharactersId))
                            playerAttrs.Deck.FriendCard.ApplyAttributeValueChange(targetAttr, intChange);
                    }
                    break;

                case CenterAttributeEffectType.CoolTimeChange:
                    doubleChange = valueData / 100.0 * changeSign;
                    playerAttrs.Cooldown += doubleChange;
                    break;

                case CenterAttributeEffectType.APGainRateChange:
                    doubleChange = valueData / 10000.0 * changeSign;
                    playerAttrs.ApGainRate += doubleChange;
                    break;
                
                case CenterAttributeEffectType.VoltageGainRateChange:
                    doubleChange = valueData / 10000.0 * changeSign;
                    playerAttrs.VoltageGainRate += doubleChange;
                    break;

                case CenterAttributeEffectType.APRateChangeResetGuard:
                    // 待实装
                    break;
                default:
                    // logger.Error($"未知效果类型: {effectType.ToString()}");
                    break;
            }
        }

        // -------------------------------------------------------------------
        // III. 普通技能条件检查 (Skill Condition Check)
        // -------------------------------------------------------------------

        // C# 中使用内部结构体/元组存储解析结果，并使用字典或 ConcurrentDictionary 模拟 lru_cache
        private static readonly ConcurrentDictionary<string, (SkillConditionType Type, SkillComparisonOperator Operator, int Value)> SkillConditionCache = new();

        /// <summary>
        /// 解析技能条件ID。对应 Python 的 parse_condition_id。
        /// </summary>
        private static (SkillConditionType Type, SkillComparisonOperator Operator, int Value) ParseSkillConditionId(string conditionId)
        {
            // 使用 GetOrAdd 尝试获取或添加结果。
            // 如果 conditionId 在缓存中，立即返回缓存值 (线程安全)。
            // 如果不在，则执行后面的 Lambda 表达式 (工厂函数)，然后原子地添加结果。
            
            // (SkillConditionType Type, SkillComparisonOperator Operator, int Value) 是结果的元组类型
            return SkillConditionCache.GetOrAdd(
                conditionId, 
                // 工厂函数: 只有在缓存未命中时才执行
                (key) => 
                {
                    // 原始的解析逻辑
                    if (key.Length != 7 ||
                        !int.TryParse(key.Substring(0, 1), out int typeValue) ||
                        !int.TryParse(key.Substring(1, 1), out int opValue) ||
                        !int.TryParse(key.Substring(2), out int valueData) ||
                        !Enum.IsDefined(typeof(SkillConditionType), typeValue) ||
                        !Enum.IsDefined(typeof(SkillComparisonOperator), opValue))
                    {
                        // 如果解析失败，缓存一个错误/默认结果，避免再次尝试解析
                        // (根据你的需求，你可能需要缓存 null 或抛出异常)
                        // 这里选择缓存一个默认值 (0, 0, 0)
                        // logger.Error("错误: 无法解析条件ID。");
                        return (0, 0, 0); 
                    }

                    var result = ((SkillConditionType)typeValue, (SkillComparisonOperator)opValue, valueData);
                    
                    // GetOrAdd 会自动将 result 添加到 SkillConditionCache 中
                    return result;
                }
            );
        }

        /// <summary>
        /// 根据ID检查给定条件是否满足。对应 Python 的 CheckSkillCondition。
        /// </summary>
        public static bool CheckSkillCondition(LiveStatus playerAttrs, string conditionId, Card card = null)
        {
            if (conditionId == "0") return true;

            var (conditionType, op, value) = ParseSkillConditionId(conditionId);

            switch (conditionType)
            {
                case SkillConditionType.FeverTime:
                    return playerAttrs.Voltage.IsFever;
                case SkillConditionType.VoltageLevel:
                    int currentLevel = playerAttrs.Voltage.Level;
                    if (op == SkillComparisonOperator.ABOVE_OR_EQUAL) return currentLevel >= value;
                    if (op == SkillComparisonOperator.BELOW_OR_EQUAL) return currentLevel <= value;
                    return false;
                case SkillConditionType.MentalRate:
                    double requiredRate = value / 100.0;
                    double currentRate = playerAttrs.Mental.Rate;
                    if (op == SkillComparisonOperator.ABOVE_OR_EQUAL) return currentRate >= requiredRate;
                    if (op == SkillComparisonOperator.BELOW_OR_EQUAL) return currentRate <= requiredRate;
                    return false;
                case SkillConditionType.UsedAllSkillCount:
                    int allCount = playerAttrs.Deck.UsedAllSkillCalc(); // 假设此方法已实现
                    if (op == SkillComparisonOperator.ABOVE_OR_EQUAL) return allCount >= value;
                    if (op == SkillComparisonOperator.BELOW_OR_EQUAL) return allCount <= value;
                    return false;
                case SkillConditionType.UsedSkillCount:
                    if (card == null) return false; // 缺少 card 参数，无法检查单卡次数
                    int cardCount = card.ActiveCount; // 假设 Card.ActiveCount 属性已实现
                    if (op == SkillComparisonOperator.ABOVE_OR_EQUAL) return cardCount >= value;
                    if (op == SkillComparisonOperator.BELOW_OR_EQUAL) return cardCount <= value;
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 检查所有技能条件是否都满足。对应 Python 的 CheckMultiSkillCondition。
        /// </summary>
        public static bool CheckMultiSkillCondition(LiveStatus playerAttrs, List<string> conditionIds, Card card = null)
        {
            // Python 的 all 对应 C# 的 Linq.All
            return conditionIds.All(id => CheckSkillCondition(playerAttrs, id, card));
        }

        // -------------------------------------------------------------------
        // IV. 普通技能效果应用 (Skill Effect Application)
        // -------------------------------------------------------------------

        // 与条件解析类似，使用缓存结构
        private static readonly ConcurrentDictionary<int, (SkillEffectType Type, int Usage, int Value, int Direction)> SkillEffectCache = new();

        /// <summary>
        /// 解析技能效果ID。对应 Python 的 parse_effect_id。
        /// </summary>
        private static (SkillEffectType Type, int UsageCount, int ValueData, int ChangeDirection) ParseSkillEffectId(int effectId)
        {
            // 使用 GetOrAdd 尝试获取缓存值。如果键不存在，执行后面的 Lambda 表达式。
            return SkillEffectCache.GetOrAdd(
                effectId, 
                // factory 函数: 只有在缓存未命中时才执行
                (key) => 
                {
                    // --- 原始的解析逻辑 ---
                    string idStr = key.ToString();
                    // 长度检查
                    if (idStr.Length != 9) return (0, 0, 0, 0); 

                    // 基本字段解析
                    if (!int.TryParse(idStr.Substring(0, 1), out int typeValue) ||
                        !int.TryParse(idStr.Substring(1, 1), out int directionValue) ||
                        !Enum.IsDefined(typeof(SkillEffectType), typeValue)) return (0, 0, 0, 0);

                    SkillEffectType effectType = (SkillEffectType)typeValue;
                    int usageCount;
                    int valueData;

                    // 根据类型进行特殊处理
                    if (effectType == SkillEffectType.NextAPGainRateChange || effectType == SkillEffectType.NextVoltageGainRateChange)
                    {
                        if (!int.TryParse(idStr.Substring(2, 1), out usageCount) ||
                            !int.TryParse(idStr.Substring(3), out valueData)) return (0, 0, 0, 0);
                    }
                    else
                    {
                        usageCount = 1;
                        if (!int.TryParse(idStr.Substring(2), out valueData)) return (0, 0, 0, 0);
                    }

                    // --- 返回结果 (GetOrAdd 会自动将结果添加到缓存) ---
                    return (effectType, usageCount, valueData, directionValue);
                }
            );
        }


        /// <summary>
        /// 根据EffectID解析并应用技能。对应 Python 的 ApplySkillEffect。
        /// </summary>
        public static void ApplySkillEffect(LiveStatus playerAttrs, int effectId, Card card = null)
        {
            var (effectType, usageCount, valueData, changeDirection) = ParseSkillEffectId(effectId);
            int changeFactor = (changeDirection == 0) ? 1 : -1;

            switch (effectType)
            {
                case SkillEffectType.APChange:
                    double apAmount = valueData * changeFactor / 10000.0;
                    playerAttrs.ApAddSkill(apAmount);
                    break;
                case SkillEffectType.ScoreGain:
                    double scoreRate = 100.0;
                    // 假设 next_score_gain_rate 是 List<double>
                    if (playerAttrs.NextScoreGainRate.Count != 0)
                    {
                        scoreRate += playerAttrs.NextScoreGainRate.First();
                        playerAttrs.NextScoreGainRate.RemoveAt(0);
                    }
                    double scoreResult = valueData / 1000000.0  * scoreRate;
                    playerAttrs.ScoreAdd(scoreResult);
                    break;
                case SkillEffectType.VoltagePointChange:
                    double voltageRate = playerAttrs.VoltageGainRate;
                    if (changeFactor == 1)
                    {
                        if (playerAttrs.NextVoltageGainRate.Count != 0)
                        {
                            voltageRate += playerAttrs.NextVoltageGainRate.First() / 100.0;
                            playerAttrs.NextVoltageGainRate.RemoveAt(0);
                        }
                    }
                    else voltageRate *= changeFactor;
                    int voltageResult = (int)Ceiling(valueData * voltageRate);
                    playerAttrs.Voltage.AddPoints(voltageResult);
                    break;
                case SkillEffectType.MentalRateChange:
                    double hpPercent = valueData / 100.0;
                    playerAttrs.Mental.SkillAdd(hpPercent * changeFactor);
                    break;
                case SkillEffectType.DeckReset:
                    playerAttrs.Deck.Reset(); // 假设 Deck.Reset() 已实现
                    break;
                case SkillEffectType.CardExcept:
                    playerAttrs.Deck.ExceptCard(card);
                    break;
                case SkillEffectType.NextAPGainRateChange: // Score Rate Change
                case SkillEffectType.NextVoltageGainRateChange:
                    double bonusPercent = valueData / 100.0;
                    var list = (effectType == SkillEffectType.NextAPGainRateChange)
                        ? playerAttrs.NextScoreGainRate
                        : playerAttrs.NextVoltageGainRate;

                    for (int i = 0; i < usageCount; i++)
                    {
                        if (list.Count > i)
                            list[i] += bonusPercent;
                        else
                            list.Add(bonusPercent);
                    }
                    break;
                default:
                    break;
            }
        }

        // -------------------------------------------------------------------
        // V. 技能统一执行 (Main Skill Execution)
        // -------------------------------------------------------------------

        /// <summary>
        /// 统一执行卡牌技能。对应 Python 的 UseCardSkill。
        /// </summary>
        // 注意：effects 是 List<int> (effect IDs), conditions 是 List<List<string>> (multi-conditions)
        public static void UseCardSkill(LiveStatus playerAttrs, List<int> effects, List<List<string>> conditions, Card card = null)
        {
            if (effects.Count != conditions.Count)
            {
                // logger.Error("技能效果数量与条件列表数量不匹配。");
                return;
            }

            List<bool> flags = [];

            foreach (var condition in conditions)
            {
                flags.Add(CheckMultiSkillCondition(playerAttrs, condition, card));
            }
            foreach (var (flag, effect) in flags.Zip(effects))
            {
                if (flag) ApplySkillEffect(playerAttrs, effect, card);
            }
            // Console.WriteLine($"{playerAttrs}");
        }

        private static readonly ConcurrentDictionary<string, (CenterSkillConditionType Type, SkillComparisonOperator Operator, int Value)> CenterSkillConditionCache = new();
        
        /// <summary>
        /// 解析C位技能条件ID。
        /// </summary>
        private static (CenterSkillConditionType Type, SkillComparisonOperator Operator, int Value) ParseCenterSkillConditionId(string conditionId)
        {
            // 使用 GetOrAdd 尝试获取缓存值。如果键不存在，执行后面的 Lambda 表达式。
            return CenterSkillConditionCache.GetOrAdd(
                conditionId, 
                // 工厂函数 (Func<string, TValue>): 只有在缓存未命中时才执行
                (key) => 
                {
                    // --- 原始的解析逻辑 ---
                    if (key.Length != 7 ||
                        !int.TryParse(key.Substring(0, 1), out int typeValue) ||
                        !int.TryParse(key.Substring(1, 1), out int opValue) ||
                        !int.TryParse(key.Substring(2), out int valueData) ||
                        !Enum.IsDefined(typeof(CenterSkillConditionType), typeValue) ||
                        !Enum.IsDefined(typeof(SkillComparisonOperator), opValue))
                    {
                        // 解析失败或格式不符，返回默认值 (该默认值也会被缓存)
                        return (0, 0, 0); 
                    }

                    // 构造解析结果
                    var result = ((CenterSkillConditionType)typeValue, (SkillComparisonOperator)opValue, valueData);
                    
                    // GetOrAdd 会自动将 result 添加到缓存中，无需手动调用 Add
                    return result;
                }
            );
        }
        
        /// <summary>
        /// 根据ID检查给定C位技能条件是否满足。
        /// </summary>
        /// <param name="playerAttrs">玩家属性实例。</param>
        /// <param name="condition_id">C位技能条件ID，多个条件用逗号分隔 (例如 "5100003,6150000")。</param>
        /// <param name="card">当前卡牌实例 (尽管此方法未使用，但保留签名以匹配原Python代码)。</param>
        /// <param name="event">当前的Live事件 ("LiveStart", "LiveEnd" 等)。</param>
        /// <returns>如果所有条件满足则返回 True，否则返回 False。</returns>
        public static bool CheckCenterSkillCondition(
            LiveStatus playerAttrs, 
            string conditionId, 
            LiveEventType liveEvent = LiveEventType.Unknown)
        {
            // C# 中的 switch 表达式/语句不能直接处理字符串作为 case 匹配，
            // 故使用原始的 if-else if 或 switch 语句 + enum

            string[] conditions = conditionId.Split(',');
            bool result = true;

            foreach (string condition in conditions)
            {
                // 所有条件ID（非0）都是7位数字
                if (condition.Length != 7)
                {
                    Console.WriteLine($"\t错误: 条件ID '{condition}' 长度不符合已知规则 (应为7位)。 -> 不满足");
                    return false;
                }

                var (conditionType, operatorOrFlag, conditionValue) = ParseCenterSkillConditionId(condition);

                bool isSatisfied = false;

                // 使用 C# 的 switch 语句，基于枚举进行匹配
                switch (conditionType)
                {
                    case CenterSkillConditionType.LiveStart:
                        isSatisfied = liveEvent == LiveEventType.LiveStart;
                        break;

                    case CenterSkillConditionType.LiveEnd:
                        isSatisfied = liveEvent == LiveEventType.LiveEnd;
                        break;

                    case CenterSkillConditionType.FeverStart:
                        isSatisfied = liveEvent == LiveEventType.FeverStart;
                        break;

                    case CenterSkillConditionType.FeverTime:
                        isSatisfied = playerAttrs.Voltage.IsFever;
                        break;

                    case CenterSkillConditionType.VoltageLevel:
                        int currentLevel = playerAttrs.Voltage.Level;
                        if (operatorOrFlag == SkillComparisonOperator.ABOVE_OR_EQUAL) // >=
                        {
                            isSatisfied = currentLevel >= conditionValue;
                        }
                        else if (operatorOrFlag == SkillComparisonOperator.BELOW_OR_EQUAL) // <= (注意原Python代码注释是 <, 但逻辑是 <=)
                        {
                            isSatisfied = currentLevel <= conditionValue;
                        }
                        else
                        {
                            Console.WriteLine($"\t错误: 未知的 VoltageLevel 运算符 '{condition}'。 -> 不满足");
                        }
                        break;

                    case CenterSkillConditionType.MentalRate:
                        double currentRate = playerAttrs.Mental.Rate;
                        // condition_value 例如 5000 代表 50.00%
                        double requiredRate = conditionValue / 100.0; 

                        if (operatorOrFlag == SkillComparisonOperator.ABOVE_OR_EQUAL) // >=
                        {
                            isSatisfied = currentRate >= requiredRate;
                        }
                        else if (operatorOrFlag == SkillComparisonOperator.BELOW_OR_EQUAL) // <= (注意原Python代码注释是 <, 但逻辑是 <=)
                        {
                            isSatisfied = currentRate <= requiredRate;
                        }
                        else
                        {
                            Console.WriteLine($"\t错误: 未知的 MentalRate 运算符 '{operatorOrFlag}'。 -> 不满足");
                        }
                        break;

                    case CenterSkillConditionType.AfterUsedAllSkillCount:
                        int usedCount = playerAttrs.Deck.UsedAllSkillCalc();

                        if (operatorOrFlag == SkillComparisonOperator.ABOVE_OR_EQUAL) // >=
                        {
                            isSatisfied = usedCount >= conditionValue;
                        }
                        else if (operatorOrFlag == SkillComparisonOperator.BELOW_OR_EQUAL) // <=
                        {
                            isSatisfied = usedCount <= conditionValue;
                        }
                        else
                        {
                            Console.WriteLine($"\t错误: 未知的 UsedAllSkillCount 运算符 '{operatorOrFlag}'。 -> 不满足");
                        }
                        break;

                    default:
                        Console.WriteLine($"\t未知条件类型: {conditionType} ({condition})。 -> 不满足");
                        break;
                }

                // 只要有一个条件不满足，最终结果就是 False
                result = result && isSatisfied;
                if (!result) return false; // 提前退出
            }

            return result;
        }


        private static readonly ConcurrentDictionary<int, (CenterSkillEffectType Type, int Value, int Direction)> CenterSkillEffectCache = new();

        /// <summary>
        /// 解析技能效果ID。对应 Python 的 parse_effect_id。
        /// </summary>
        private static (CenterSkillEffectType Type, int ValueData, int ChangeDirection) ParseCenterSkillEffectId(int effectId)
        {
            // 使用 GetOrAdd 尝试获取缓存值。如果键不存在，执行后面的 Lambda 表达式。
            return CenterSkillEffectCache.GetOrAdd(
                effectId, 
                // 工厂函数 (Func<int, TValue>): 只有在缓存未命中时才执行
                (key) => 
                {
                    // --- 原始的解析逻辑 ---
                    string idStr = key.ToString();
                    
                    // 长度检查
                    if (idStr.Length != 9) return (0, 0, 0); 

                    // 基本字段解析
                    if (!int.TryParse(idStr.Substring(0, 1), out int typeValue) ||
                        !int.TryParse(idStr.Substring(1, 1), out int directionValue) ||
                        !Enum.IsDefined(typeof(CenterSkillEffectType), typeValue)) return (0, 0, 0);

                    CenterSkillEffectType effectType = (CenterSkillEffectType)typeValue;

                    // 解析值数据
                    if (!int.TryParse(idStr.Substring(2), out int valueData)) return (0, 0, 0);

                    // 构造解析结果
                    var result = (effectType, valueData, directionValue);
                    
                    // GetOrAdd 会自动将 result 添加到缓存中，无需手动调用 Add
                    return result;
                }
            );
        }

        public static void ApplyCenterSkillEffect(LiveStatus playerAttrs, int effectId)
        {
            var (effectType, valueData, changeDirection) = ParseCenterSkillEffectId(effectId);
            int changeFactor = (changeDirection == 0) ? 1 : -1;

            switch (effectType)
            {
                case CenterSkillEffectType.APChange:
                    double apAmount = valueData * changeFactor / 10000.0;
                    playerAttrs.ApAddSkill(apAmount);
                    break;
                case CenterSkillEffectType.ScoreGain:
                    double scoreRate = 100.0;
                    // 假设 next_score_gain_rate 是 List<double>
                    if (playerAttrs.NextScoreGainRate.Count != 0)
                    {
                        scoreRate += playerAttrs.NextScoreGainRate.First();
                        playerAttrs.NextScoreGainRate.RemoveAt(0);
                    }
                    double scoreResult = valueData * scoreRate / 1000000.0;
                    playerAttrs.ScoreAdd(scoreResult); // 假设 ScoreAdd 方法已实现
                    break;
                case CenterSkillEffectType.VoltagePointChange:
                    double voltageRate = playerAttrs.VoltageGainRate;
                    if (changeFactor == 1)
                    {
                        if (playerAttrs.NextVoltageGainRate.Count != 0)
                        {
                            voltageRate += playerAttrs.NextVoltageGainRate.First();
                            playerAttrs.NextVoltageGainRate.RemoveAt(0);
                        }
                    }
                    else voltageRate *= changeFactor;
                    int voltageResult = (int)Ceiling(valueData * voltageRate / 100.0);
                    playerAttrs.Voltage.AddPoints(voltageResult);
                    break;
                case CenterSkillEffectType.MentalRateChange:
                    double hpPercent = valueData / 100.0;
                    playerAttrs.Mental.SkillAdd(hpPercent * changeFactor);
                    break;
                default:
                    break;
            }
        }
    }
}