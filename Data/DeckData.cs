using System.Collections.Generic;
using System.Linq;
using static System.Math;

namespace DeckMiner.Data
{
    /// <summary>
    /// 卡牌稀有度
    /// </summary>
    public enum Rarity
    {
        R = 3,
        SR = 4,
        UR = 5,
        LR = 7,
        DR = 8,
        BR = 9
    }

    /// <summary>
    /// 用于存储插值曲线的点 (等级, 百分比值)
    /// </summary>
    public record StatusCurvePoint(int Level, int Value);

    /// <summary>
    /// 包含所有静态成长曲线和辅助函数的工具类
    /// </summary>
    public static class CurveCalculator
    {
        // 状态值成长曲线 (等级, 基础值百分比)
        public static readonly Dictionary<Rarity, List<StatusCurvePoint>> STATUS_CURVES = new Dictionary<Rarity, List<StatusCurvePoint>>
        {
            { Rarity.R, new List<StatusCurvePoint> { new(1, 1), new(30, 50), new(40, 70), new(60, 100), new(70, 110), new(80, 120) } },
            { Rarity.SR, new List<StatusCurvePoint> { new(1, 1), new(40, 50), new(60, 70), new(80, 100), new(90, 110), new(100, 120) } },
            { Rarity.UR, new List<StatusCurvePoint> { new(1, 1), new(60, 50), new(80, 70), new(100, 100), new(110, 110), new(120, 120) } },
            { Rarity.LR, new List<StatusCurvePoint> { new(1, 1), new(100, 70), new(120, 100), new(130, 110), new(140, 120) } },
            { Rarity.DR, new List<StatusCurvePoint> { new(1, 1), new(100, 70), new(120, 100), new(130, 110), new(140, 120) } },
            { Rarity.BR, new List<StatusCurvePoint> { new(1, 1), new(80, 70), new(100, 100), new(110, 110), new(120, 120) } },
        };

        // HP 成长曲线
        public static readonly Dictionary<Rarity, List<StatusCurvePoint>> HP_CURVES = new Dictionary<Rarity, List<StatusCurvePoint>>
        {
            { Rarity.R, new List<StatusCurvePoint> { new(1, 20), new(30, 50), new(40, 70), new(60, 100) } },
            { Rarity.SR, new List<StatusCurvePoint> { new(1, 20), new(40, 50), new(60, 70), new(80, 100) } },
            { Rarity.UR, new List<StatusCurvePoint> { new(1, 20), new(60, 50), new(80, 70), new(100, 100) } },
            { Rarity.LR, new List<StatusCurvePoint> { new(1, 20), new(100, 70), new(120, 100) } },
            { Rarity.DR, new List<StatusCurvePoint> { new(1, 20), new(100, 70), new(120, 100) } },
            { Rarity.BR, new List<StatusCurvePoint> { new(1, 20), new(80, 70), new(100, 100) } },
        };

        // 进化等级 (等级, 进化阶段)
        public static readonly Dictionary<Rarity, List<StatusCurvePoint>> EVOLUTION = new Dictionary<Rarity, List<StatusCurvePoint>>
        {
            { Rarity.R, new List<StatusCurvePoint> { new(40, 0), new(60, 2), new(70, 3), new(80, 4) } },
            { Rarity.SR, new List<StatusCurvePoint> { new(60, 0), new(80, 2), new(90, 3), new(100, 4) } },
            { Rarity.UR, new List<StatusCurvePoint> { new(80, 0), new(100, 2), new(110, 3), new(120, 4) } },
            { Rarity.LR, new List<StatusCurvePoint> { new(100, 0), new(120, 2), new(130, 3), new(140, 4) } },
            { Rarity.DR, new List<StatusCurvePoint> { new(100, 0), new(120, 2), new(130, 3), new(140, 4) } },
            { Rarity.BR, new List<StatusCurvePoint> { new(80, 0), new(100, 2), new(110, 3), new(120, 4) } },
        };

        // ----------------------------------------------------
        // 核心辅助函数
        // ----------------------------------------------------

        /// <summary>
        /// 线性插值计算值。
        /// 对应 Python 的 _interpolate_value
        /// </summary>
        public static double InterpolateValue(List<StatusCurvePoint> curve, int level)
        {
            if (level <= curve[0].Level)
            {
                return curve[0].Value;
            }

            for (int i = 1; i < curve.Count; i++)
            {
                var pStart = curve[i - 1];
                var pEnd = curve[i];

                if (level <= pEnd.Level)
                {
                    // t = (lv - lv_start) / (lv_end - lv_start)
                    double t = (double)(level - pStart.Level) / (pEnd.Level - pStart.Level);
                    // val_start + t * (val_end - val_start)
                    return pStart.Value + t * (pEnd.Value - pStart.Value);
                }
            }

            return curve.Last().Value;
        }

        /// <summary>
        /// 获取卡牌在指定等级下的状态、HP和进化等级。
        /// 对应 Python 的 _get_card_status
        /// </summary>
        public static (double Status, double Hp, int Evo) GetCardStatus(Rarity rarity, int level)
        {
            // 状态值
            double status = InterpolateValue(STATUS_CURVES[rarity], level);
            // HP
            double hp = InterpolateValue(HP_CURVES[rarity], level);
            // 进化等级
            int evo = GetEvolution(rarity, level);
            
            return (status, hp, evo);
        }
        
        /// <summary>
        /// 获取卡牌在指定等级下的进化阶段。
        /// 对应 Python 的 _get_evolution
        /// </summary>
        public static int GetEvolution(Rarity rarity, int level)
        {
            var stages = EVOLUTION[rarity];
            foreach (var point in stages)
            {
                if (level <= point.Level)
                {
                    return point.Value;
                }
            }
            return stages.Last().Value;
        }
    }
}