using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.Swift;
using DeckMiner.Services;
using static System.Math;

namespace DeckMiner.Models
{
    public class LiveStatus(int masterLv = 50)
    {
        // 核心属性
        public double Ap { get; set; } = 0.0;
        public double Cooldown { get; set; } = 5.0;
        public double ApRate { get; private set; } = 1.0;
        public int Combo { get; internal set; } = 0;
        public long Score { get; internal set; } = 0;

        // 嵌套对象
        public Mental Mental { get; private set; } = new Mental();
        public Voltage Voltage { get; private set; } = new Voltage(0);

        // 辅助属性
        public double ApGainRate { get; set; } = 100.0;
        public double VoltageGainRate { get; set; } = 100.0;

        public List<double> NextScoreGainRate { get; set; } = new List<double>();
        public List<double> NextVoltageGainRate { get; set; } = new List<double>();
        public bool CDAvailable { get; set; } = false;

        // Deck 相关的计算属性
        public Deck Deck { get; private set; } // 假设 Deck 类已定义
        public int MasterLv { get; private set; } = masterLv;
        private double _baseScore;
        private Dictionary<string, double> _noteScore = new Dictionary<string, double>();
        private double _halfApPlus;
        private double _fullApPlus;
        private int _prevVo = -1;
        internal int _prevNoteScore = 0;
        private double _prevApRate = 0.0;
        internal double _prevAp = 0.0;

        public void SetDeck(Deck deck)
        {
            Deck = deck ?? throw new ArgumentNullException(nameof(deck));
        }

        public void HpCalc()
        {
            if (Deck == null) return;
            // 假设 Deck.MentalCalc() 返回 MaxHP 值
            Mental.SetHp(Deck.MentalCalc());
        }

        public void BaseScoreCalc(int allNoteSize)
        {
            if (Deck == null || allNoteSize == 0) return;

            double masterLvBonus = MasterLv / 100.0 + 1.0;
            _baseScore = Deck.Appeal * masterLvBonus;

            // 计算每个判定的基础分数
            double noteBase = _baseScore / allNoteSize;
            _noteScore["PERFECT+"] = 35.0 * noteBase;
            _noteScore["PERFECT"] = 30.0 * noteBase;
            _noteScore["GREAT"] = 25.0 * noteBase;
            _noteScore["GOOD"] = 15.0 * noteBase;
            _noteScore["BAD"] = 5.0 * noteBase;
            _noteScore["MISS"] = 0.0;

            // AP 恢复点数
            _halfApPlus = 300000.0 / allNoteSize;
            _fullApPlus = 600000.0 / allNoteSize;
        }

        public long ScoreAdd(double value, bool skill = true)
        {
            double voltageBonus = Voltage.Bonus;
            value *= voltageBonus;

            if (skill) value *= _baseScore;

            var result = (long)Ceiling(value);
            Score += result;
            return result;
        }

        public void ScoreNote(string judgement)
        {
            if (_noteScore.TryGetValue(judgement, out double scoreValue))
            {
                if (judgement == "PERFECT+")
                {
                    if (_prevVo == Voltage.Level)
                    {
                        Score += _prevNoteScore;
                    }
                    else
                    {
                        _prevVo = Voltage.Level;
                        _prevNoteScore = (int)ScoreAdd(scoreValue, skill: false);
                    }
                }
                else ScoreAdd(scoreValue, skill: false);
            }
        }

        public void ComboAdd(string judgement, LiveEventType noteType = LiveEventType.Unknown)
        {
            switch (judgement)
            {
                case "PERFECT+":
                case "PERFECT":
                case "GREAT":
                    Combo++;
                    if (Combo <= 50)
                    {
                        ApRate = 1.0 + Combo / 10 / 10.0;
                    }
                    if (_prevApRate != ApRate)
                    {
                        _prevApRate = ApRate;
                        _prevAp = Ceiling(_fullApPlus * ApRate) / 10000.0;
                    }
                    // Python: self.ap += ceil(self.full_ap_plus * self.ap_rate) / 10000
                    Ap += _prevAp;
                    break;

                case "GOOD":
                    Combo++;
                    if (Combo <= 50)
                    {
                        ApRate = 1.0 + (double)(Combo / 10) / 10.0;
                    }
                    // Python: self.ap += ceil(self.half_ap_plus * self.ap_rate) / 10000
                    Ap += Ceiling(_halfApPlus * ApRate) / 10000.0;
                    break;

                case "BAD":
                case "MISS":
                    Combo = 0;
                    ApRate = 1.0;

                    // 按判定扣血
                    Mental.Sub(judgement, noteType);

                    if (judgement == "MISS")
                    {
                        // MISS 不加分，直接返回
                        return;
                    }
                    break;

                default:
                    return;
            }

            // 判定为 BAD/GOOD/GREAT/PERFECT 时加分 (MISS 已返回)
            ScoreNote(judgement);
        }

        public void ApAddSkill(double apAmount)
        {
            double rate = ApRate * ApGainRate / 100.0;
            if (apAmount > 0) Ap += apAmount * rate;
            else Ap = Max(0, Ap + apAmount);
        }

        public override string ToString()
        {
            return (
                $"当前属性:\n" +
                $"  AP: {Ap:F5}  Combo: {Combo}\t" +
                $"AP Gain Rate: {ApRate:F2}x\t" +
                $"{Mental}\n" +
                $"  Score: {Score:N0}\t" +
                $"BaseScore: {_baseScore:N0}\t" +
                $"Appeal: {Deck.Appeal:N0}\t" +
                $"{Voltage}\t" +
                $"分加成: {string.Join(", ", NextScoreGainRate)}\t" +
                $"电加成: {string.Join(", ", NextVoltageGainRate)}\t"
            );
        }
    }
}