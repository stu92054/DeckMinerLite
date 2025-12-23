using System;
using DeckMiner.Services;
using static System.Math;

namespace DeckMiner.Models
{
    public class Mental
    {
        // 属性
        public int CurrentHp { get; private set; }
        public int MaxHp { get; private set; }
        public double Rate { get; private set; }
        
        private int _badMinus;
        private int _missMinus;
        private int _traceMinus;

        public Mental()
        {
            CurrentHp = 100;
            MaxHp = 100;
            Rate = 100.0;
            // 初始值 (Python 原始值)
            _badMinus = 30;
            _missMinus = 50;
            _traceMinus = 20;
        }

        public void SetHp(int hp)
        {
            MaxHp = hp;
            CurrentHp = MaxHp;
            
            // 计算扣血值：int(self.max_hp * 0.0X)
            _badMinus = 30 + (int)(MaxHp * 0.03);
            _missMinus = 50 + (int)(MaxHp * 0.05);
            _traceMinus = 20 + (int)(MaxHp * 0.02);
        }

        public void Sub(string judgement, LiveEventType noteType = LiveEventType.Unknown)
        {
            int damage = judgement switch
            {
                "BAD" => _badMinus,
                "MISS" => noteType == LiveEventType.Trace || noteType == LiveEventType.HoldMid ? _traceMinus : _missMinus,
                _ => 0
            };

            if (damage > 0)
            {
                CurrentHp = Max(0, CurrentHp - damage);
                Rate = MaxHp == 0 ? 0.0 : CurrentHp * 100.0 / MaxHp;
            }
        }

        public void SkillAdd(double value) // value 是百分比 (e.g., 50 表示 50%)
        {
            // Python: max(1, self.current_hp + ceil(self.max_hp * value / 100))
            int healAmount = (int)Ceiling(MaxHp * value / 100.0);
            CurrentHp = Max(1, CurrentHp + healAmount);
            Rate = MaxHp == 0 ? 0.0 : CurrentHp * 100.0 / MaxHp;
        }

        public override string ToString() => 
            $"Mental: {CurrentHp} / {MaxHp} ({Rate:F2}%)";
    }
}