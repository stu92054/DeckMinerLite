namespace DeckMiner.Models
{
    /// <summary>
    /// C位特性目标
    /// </summary>
    public enum TargetType
    {
        Member = 1,
        Unit = 2,
        Generation = 3,
        StyleType = 4,
        All = 5
    }

    /// <summary>
    /// C位特性效果类型
    /// </summary>
    public enum CenterAttributeEffectType
    {
        SmileRateChange = 1,
        PureRateChange = 2,
        CoolRateChange = 3,
        SmileValueChange = 4,
        PureValueChange = 5,
        CoolValueChange = 6,
        MentalRateChange = 7,
        MentalValueChange = 8,
        ConsumeAPChange = 9,
        CoolTimeChange = 10,
        APGainRateChange = 11,
        VoltageGainRateChange = 12,
        APRateChangeResetGuard = 13
    }

    /// <summary>
    /// 技能触发条件类型枚举。
    /// </summary>
    public enum SkillConditionType
    {
        FeverTime = 1,
        VoltageLevel = 2,
        MentalRate = 3,
        UsedAllSkillCount = 4,
        UsedSkillCount = 5
    }

    /// <summary>
    /// 技能条件中的比较运算符。
    /// </summary>
    public enum SkillComparisonOperator
    {
        UNDEFINED = 0,
        ABOVE_OR_EQUAL = 1, // 以上 (>=)
        BELOW_OR_EQUAL = 2  // 以下 (<=)
    }

    /// <summary>
    /// 卡牌技能效果类型枚举。
    /// </summary>
    public enum SkillEffectType
    {
        APChange = 1,
        ScoreGain = 2,
        VoltagePointChange = 3,
        MentalRateChange = 4,
        DeckReset = 5,
        CardExcept = 6,
        NextAPGainRateChange = 7, // 实际效果为NextScoreGainRateChange
        NextVoltageGainRateChange = 8
    }
    
    /// <summary>
    /// C位技能触发条件类型枚举。
    /// </summary>
    public enum CenterSkillConditionType
    {
        LiveStart = 1,
        LiveEnd = 2,
        FeverStart = 3,
        FeverTime = 4,
        VoltageLevel = 5,
        MentalRate = 6,
        AfterUsedAllSkillCount = 7
    }
    
    /// <summary>
    /// 卡牌C位技能效果类型枚举。
    /// </summary>
    public enum CenterSkillEffectType
    {
        APChange = 1,
        ScoreGain = 2,
        VoltagePointChange = 3,
        MentalRateChange = 4
    }
}