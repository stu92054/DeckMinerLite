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

        // TODO: 延迟MISS的NoteType
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

            foreach (Card c in d.Cards)
            {
                int cid = int.Parse(c.CardId);
                
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
                        if (afkMental != 0 && Player.Mental.Rate > afkMental)
                        {
                            Player.ComboAdd("MISS", currentEvent.Type);
                            if (Player.Mental.CurrentHp == 0)
                            {
                                return Player.Score;
                            }
                        }
                        else
                        {
                            Player.ComboAdd("PERFECT+");
                            if (Player.CDAvailable && cardNow != null && Player.Ap >= cardNow.Cost)
                            {
                                Player.Ap -= cardNow.Cost;
                                var (condition, effects) = d.TopSkill();
                                SkillResolver.UseCardSkill(Player, effects, condition, cardNow);
                                Player.CDAvailable = false;
                                var nextCd = currentEvent.Time + Player.Cooldown;
                                extraEvents.Enqueue(
                                    new RuntimeEvent(nextCd, LiveEventType.CDavailable),
                                    nextCd
                                    );
                                cardNow = d.TopCard;
                            }
                        }
                        break;

                    case LiveEventType.CDavailable:
                        Player.CDAvailable = true;
                        if (cardNow != null && Player.Ap >= cardNow.Cost)
                        {
                            Player.Ap -= cardNow.Cost;
                            var (condition, effects) = d.TopSkill();
                            SkillResolver.UseCardSkill(Player, effects, condition, cardNow);
                            Player.CDAvailable = false;
                            var nextCd = currentEvent.Time + Player.Cooldown;
                            extraEvents.Enqueue(
                                new RuntimeEvent(nextCd, LiveEventType.CDavailable),
                                nextCd
                                );
                            cardNow = d.TopCard;
                        }
                        break;

                    case LiveEventType.Ignore:
                        break;

                    case LiveEventType.LiveStart:
                    case LiveEventType.LiveEnd:
                    case LiveEventType.FeverStart:
                        if (currentEvent.Type == LiveEventType.FeverStart)
                        {
                            Player.Voltage.SetFever(true);
                        }
                        if (CenterCard != null)
                        {
                            foreach (var (condition, effect) in CenterCard.GetCenterSkill())
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
            return Player.Score;
        }
    }

}