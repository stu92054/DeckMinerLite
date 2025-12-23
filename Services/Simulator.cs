using System.Collections.Generic;
using DeckMiner.Config;
using DeckMiner.Data;
using DeckMiner.Models;

namespace DeckMiner.Services
{
    public enum LiveEventType : byte
    {
        Unknown = 0,
        // 基础音符
        Single, Hold, HoldMid, Flick, Trace,
        // 系统事件
        CDavailable,

        // 延遲 MISS (花火吟機制)
        DelayedSingle, DelayedHold, DelayedHoldMid, DelayedFlick, DelayedTrace,

        Ignore,
        // 生命周期
        LiveStart, LiveEnd,
        // Fever
        FeverStart, FeverEnd,
    }

    public readonly struct RuntimeEvent
    {
        public readonly double Time;
        public readonly LiveEventType Type;

        public RuntimeEvent(double time, LiveEventType type)
        {
            Time = time;
            Type = type;
        }
    }

    public static class ChartConverter
    {
        public static RuntimeEvent[] PrepareRuntimeEvents(ChartData chart)
        {
            var runtimeEvents = new List<RuntimeEvent>(chart.Events.Count);
            
            foreach (var ev in chart.Events)
            {
                var type = ev.Name switch
                {
                    "Single" => LiveEventType.Single,
                    "Hold"   => LiveEventType.Hold,
                    "HoldMid"   => LiveEventType.HoldMid,
                    "Flick"  => LiveEventType.Flick,
                    "Trace"  => LiveEventType.Trace,
                    "LiveStart"  => LiveEventType.LiveStart,
                    "LiveEnd"    => LiveEventType.LiveEnd,
                    "FeverStart" => LiveEventType.FeverStart,
                    "FeverEnd" => LiveEventType.FeverEnd,
                    _ => LiveEventType.Unknown
                };

                if (type != LiveEventType.Ignore)
                {
                    runtimeEvents.Add(new RuntimeEvent(ev.Time, type));
                }
            }
            
            return runtimeEvents.ToArray(); // 转为数组，遍历速度最快
        }
    }

    public class Simulator
    {
        public static bool DebugMode = false;

        // 花火吟延遲 MISS 時間 (單位：秒)
        // 參考：src/core/Simulator_core.py:33-39
        private static readonly Dictionary<LiveEventType, double> MISS_TIMING = new()
        {
            { LiveEventType.Single, 0.125 },
            { LiveEventType.Hold, 0.125 },
            { LiveEventType.Flick, 0.100 },
            { LiveEventType.HoldMid, 0.070 },
            { LiveEventType.Trace, 0.070 }
        };

        public ChartData Chart;
        public RuntimeEvent[] ChartEvent;
        public MusicDbData Music;
        public int MasterLv;
        public CardConfig Config;

        public Simulator(string musicId, string tier, int masterLv = 50)
        {
            Chart = ChartLoader.GetChart(musicId, tier);
            ChartEvent = ChartConverter.PrepareRuntimeEvents(Chart);
            Music = DataManager.Instance.GetMusicDatabase()[musicId];
            MasterLv = masterLv;
            Config = ConfigLoader.Config;
        }


        public long Run(Deck d, int centerCardId)
        {
            Card CenterCard = null;
            LiveStatus Player = new(MasterLv);
            Player.SetDeck(d);

            double afkMental = 0.0;
            bool flagHanabiGinko = false;  // 花火吟卡標誌

            foreach (Card c in d.Cards)
            {
                int cid = int.Parse(c.CardId);

                // 檢測花火吟卡 (1041517)
                if (cid == 1041517)
                {
                    flagHanabiGinko = true;
                }

                if (Config.DeathNote.TryGetValue(cid, out double hpThreshold))
                {
                    if (afkMental > 0) afkMental = Math.Min(afkMental, hpThreshold);
                    else afkMental = hpThreshold;
                }

                if (cid == centerCardId) CenterCard = c;
            }

            if (CenterCard != null)
            {
                foreach (var (target, effect) in CenterCard.GetCenterAttribute())
                {
                    SkillResolver.ApplyCenterAttribute(Player, effect, target);
                }
            }
            
            if (DebugMode) Console.WriteLine($"[Simulator] Initial afkMental: {afkMental}");

            d.AppealCalc(Music.MusicType);
            Player.HpCalc();
            Player.BaseScoreCalc(Chart.AllNoteSize);

            var chartEvents = ChartEvent;
            var extraEvents = new PriorityQueue<RuntimeEvent, double>();
            extraEvents.Enqueue(
                new RuntimeEvent(Player.Cooldown, LiveEventType.CDavailable),
                Player.Cooldown
                );

            int i_event = 0;
            Card cardNow = d.TopCard;

            // 動態重新計算血線的函數
            void RecalculateAfkMental()
            {
                double newAfkMental = 0.0;
                foreach (Card card in d.Cards)
                {
                    // 只檢查未被除外的卡片
                    if (!card.IsExcept)
                    {
                        int cid = int.Parse(card.CardId);
                        if (Config.DeathNote.TryGetValue(cid, out double threshold))
                        {
                            if (newAfkMental > 0)
                                newAfkMental = Math.Min(newAfkMental, threshold);
                            else
                                newAfkMental = threshold;
                        }
                    }
                }
                // 如果沒有剩餘的背水卡，血線重置為 0（禁用背水）
                afkMental = newAfkMental;
            }

            // 提取重複的技能觸發邏輯為內聯函數
            void TryUseSkill(double timestamp)
            {
                if (cardNow != null && Player.Ap >= cardNow.Cost)
                {
                    if (DebugMode) Console.WriteLine($"[Skill] {cardNow.FullName} at {timestamp:F3}s (AP: {Player.Ap:F2}, Combo: {Player.Combo})");
                    Player.Ap -= cardNow.Cost;

                    // 記錄打出前有多少卡片被除外
                    int cardsExceptBefore = d.Cards.Count(c => c.IsExcept);

                    var (condition, effects) = d.TopSkill();
                    SkillResolver.UseCardSkill(Player, effects, condition, cardNow);

                    // 檢查是否有新的卡片被除外
                    int cardsExceptAfter = d.Cards.Count(c => c.IsExcept);
                    if (cardsExceptAfter > cardsExceptBefore)
                    {
                        // 有卡片被除外，重新計算血線
                        RecalculateAfkMental();
                    }
                    if (DebugMode)
                    {
                        Console.WriteLine("当前属性:");
                        Console.WriteLine($"  AP: {Player.Ap:F5}  Combo: {Player.Combo}\tAP Gain Rate: {Player.ApRate:F2}x\t{Player.Mental}");
                        Console.WriteLine($"  Score: {Player.Score}\t{Player.Voltage}\t分加成: [{string.Join(", ", Player.NextScoreGainRate)}]\t电加成: [{string.Join(", ", Player.NextVoltageGainRate)}]\t");
                    }
                    Player.CDAvailable = false;
                    double nextCd = timestamp + Player.Cooldown;
                    extraEvents.Enqueue(
                        new RuntimeEvent(nextCd, LiveEventType.CDavailable),
                        nextCd
                    );
                    cardNow = d.TopCard;
                }
            }

            while (i_event < chartEvents.Length)
            {
                RuntimeEvent currentEvent;
                double nextChartTime = chartEvents[i_event].Time;

                // 获取下一个动态 Extra 事件的时间
                double nextExtraTime = (extraEvents.Count > 0)
                    ? extraEvents.Peek().Time // Peek() 获取优先级 (Time)
                    : double.MaxValue;
                if (nextChartTime <= nextExtraTime)
                {
                    // 1. 选择 Chart Event
                    currentEvent = chartEvents[i_event];
                    i_event++;
                }
                else
                {
                    currentEvent = extraEvents.Dequeue();
                }

                switch (currentEvent.Type)
                {
                    case LiveEventType.Single:
                    case LiveEventType.Hold:
                    case LiveEventType.HoldMid:
                    case LiveEventType.Flick:
                    case LiveEventType.Trace:
                        if (DebugMode) Console.WriteLine($"[Event] {currentEvent.Type} at {currentEvent.Time:F3}s (Combo: {Player.Combo})");
                        if (afkMental != 0 && Player.Mental.Rate > afkMental)
                        {
                            if (DebugMode) Console.WriteLine($"[Simulator] Intentional MISS at {currentEvent.Time:F3}s. HP Rate: {Player.Mental.Rate:F2}% > {afkMental}%");
                            // 計算 MISS 傷害
                            int missDamage = (currentEvent.Type == LiveEventType.Trace ||
                                             currentEvent.Type == LiveEventType.HoldMid)
                                ? Player.Mental.TraceMinus
                                : Player.Mental.MissMinus;

                            // 檢查 MISS 是否會導致血量歸零
                            bool willDie = (Player.Mental.CurrentHp <= missDamage);

                            if (willDie)
                            {
                                // 如果 MISS 會導致遊戲結束，改為 PERFECT
                                Player.ComboAdd("PERFECT");
                            }
                            else
                            {
                                // 需要仰臥起坐時，將 MISS 時機延後以提高精度（花火吟機制）
                                if (flagHanabiGinko)
                                {
                                    // 將 MISS 延遲到 timestamp + MISS_TIMING
                                    LiveEventType delayedType = currentEvent.Type switch
                                    {
                                        LiveEventType.Single => LiveEventType.DelayedSingle,
                                        LiveEventType.Hold => LiveEventType.DelayedHold,
                                        LiveEventType.HoldMid => LiveEventType.DelayedHoldMid,
                                        LiveEventType.Flick => LiveEventType.DelayedFlick,
                                        LiveEventType.Trace => LiveEventType.DelayedTrace,
                                        _ => LiveEventType.Unknown
                                    };

                                    if (delayedType != LiveEventType.Unknown && MISS_TIMING.TryGetValue(currentEvent.Type, out double delay))
                                    {
                                        double delayedTime = currentEvent.Time + delay;
                                        extraEvents.Enqueue(
                                            new RuntimeEvent(delayedTime, delayedType),
                                            delayedTime
                                        );
                                    }
                                }
                                else
                                {
                                    // 立即執行 MISS
                                    Player.ComboAdd("MISS", currentEvent.Type);
                                }
                            }
                        }
                        else
                        {
                            Player.ComboAdd("PERFECT+");
                        }

                        if (Player.CDAvailable)
                        {
                            TryUseSkill(currentEvent.Time);
                        }
                        break;

                    case LiveEventType.CDavailable:
                        Player.CDAvailable = true;
                        TryUseSkill(currentEvent.Time);
                        break;

                    case LiveEventType.Ignore:
                        break;

                    // 延遲 MISS 事件處理（花火吟機制）
                    case LiveEventType.DelayedSingle:
                    case LiveEventType.DelayedHold:
                    case LiveEventType.DelayedHoldMid:
                    case LiveEventType.DelayedFlick:
                    case LiveEventType.DelayedTrace:
                        if (Player.Mental.Rate > afkMental)
                        {
                            // 再次檢查 MISS 是否會致命（血量可能已變化）
                            LiveEventType originalType = currentEvent.Type switch
                            {
                                LiveEventType.DelayedSingle => LiveEventType.Single,
                                LiveEventType.DelayedHold => LiveEventType.Hold,
                                LiveEventType.DelayedHoldMid => LiveEventType.HoldMid,
                                LiveEventType.DelayedFlick => LiveEventType.Flick,
                                LiveEventType.DelayedTrace => LiveEventType.Trace,
                                _ => LiveEventType.Unknown
                            };

                            int missDamage = (originalType == LiveEventType.Trace ||
                                             originalType == LiveEventType.HoldMid)
                                ? Player.Mental.TraceMinus
                                : Player.Mental.MissMinus;

                            bool willDie = (Player.Mental.CurrentHp <= missDamage);

                            if (willDie)
                            {
                                // 延遲後仍會致命，改為 PERFECT
                                Player.ComboAdd("PERFECT");
                            }
                            else
                            {
                                // 執行延遲的 MISS
                                Player.ComboAdd("MISS", originalType);
                            }
                        }
                        else
                        {
                            // 血量已恢復，不再需要 MISS
                            Player.ComboAdd("PERFECT+");
                        }
                        break;

                    case LiveEventType.LiveStart:
                    case LiveEventType.LiveEnd:
                    case LiveEventType.FeverStart:
                        if (currentEvent.Type == LiveEventType.FeverStart)
                        {
                            Player.Voltage.SetFever(true);
                        }
                        // 檢查自己的隊長技能
                        if (CenterCard != null)
                        {
                            if (DebugMode && currentEvent.Type == LiveEventType.LiveStart)
                            {
                                Console.WriteLine($"[LiveStart] Checking Center Skill for Card {CenterCard.CardId}");
                            }
                            foreach (var (condition, effect) in CenterCard.GetCenterSkill())
                            {
                                if (DebugMode && currentEvent.Type == LiveEventType.LiveStart)
                                {
                                    Console.WriteLine($"  Condition: {condition}, Effect: {effect}");
                                }
                                if (SkillResolver.CheckCenterSkillCondition(Player, condition, currentEvent.Type))
                                {
                                    if (DebugMode && currentEvent.Type == LiveEventType.LiveStart)
                                    {
                                        Console.WriteLine($"  -> Condition Met! Applying Effect {effect}");
                                    }
                                    SkillResolver.ApplyCenterSkillEffect(Player, effect);
                                }
                                else
                                {
                                    if (DebugMode && currentEvent.Type == LiveEventType.LiveStart)
                                    {
                                        Console.WriteLine($"  -> Condition Not Met.");
                                    }
                                }
                            }
                        }
                        // 檢查朋友的隊長技能（新增）
                        if (d.FriendCard != null)
                        {
                            foreach (var (condition, effect) in d.FriendCard.GetCenterSkill())
                            {
                                if (SkillResolver.CheckCenterSkillCondition(Player, condition, currentEvent.Type))
                                {
                                    SkillResolver.ApplyCenterSkillEffect(Player, effect);
                                }
                            }
                        }
                        break;
                    case LiveEventType.FeverEnd:
                        Player.Voltage.SetFever(false);
                        break;
                    default:
                        Console.WriteLine($"未处理的事件: {currentEvent.Time}, {currentEvent.Type}");
                        break;

                }
            }

            // Console.WriteLine($"{Player}");
            if (DebugMode)
            {
                Console.WriteLine(Player.ToString());
                Console.WriteLine($"Final Score: {Player.Score}");
                Console.WriteLine($"打出記錄: [{string.Join(", ", d.CardLog.Select(name => $"'{name}'"))}]");
                Console.WriteLine($"打出次數: {d.CardLog.Count}");
            }
            return Player.Score;
        }
    }

}