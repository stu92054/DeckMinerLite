using DeckMiner.Data;

namespace DeckMiner.Models
{
    // ===============================================
    // 1. Skill 类 (对应 RHYTHM GAME 技能)
    // ===============================================
    public class Skill
    {
        // 属性定义
        public string SkillId { get; set; }
        public int Cost { get; set; }
        public List<List<string>> Condition { get; set; } // List of List of strings
        public List<int> Effect { get; set; }

        /// <summary>
        /// Skill 构造函数：初始化卡牌的激活技能。
        /// </summary>
        /// <param name="db">技能数据库</param>
        /// <param name="seriesId">技能系列ID</param>
        /// <param name="lv">技能等级，默认为 14</param>
        public Skill(int seriesId, int lv = 14)
        {
            // 对应 self.skill_id = str(series_id * 100 + lv)
            this.SkillId = ((uint)seriesId * 100 + lv).ToString();

            var db = SkillDataManager.SkillDatabase;

            // 检查 SkillId 是否存在于数据库中
            if (!db.TryGetValue(this.SkillId, out SkillDbData skillData))
            {
                 // 抛出异常，因为这是核心逻辑，数据必须存在
                 throw new KeyNotFoundException($"SkillId {this.SkillId} not found in Skill DB.");
            }

            // 对应 self.cost: int = db[self.skill_id]["ConsumeAP"]
            Cost = skillData.ConsumeAP;

            // 对应 self.condition: list[list[str]] = [condition.split(",") for condition in ...]
            // C# 复杂列表处理和类型转换
            var conditionIds = skillData.RhythmGameSkillConditionIds;
            Condition = conditionIds
                .Select(conditionStr => conditionStr.Split(',').ToList())
                .ToList();
            
            // 对应 self.effect: list[int] = db[self.skill_id]["RhythmGameSkillEffectId"]
            Effect = skillData.RhythmGameSkillEffectId;
        }
    }

    // ===============================================
    // 2. CenterSkill 类 (对应队长技能的主部分)
    // ===============================================
    public class CenterSkill
    {
        // 属性定义
        public string SkillId { get; set; }
        public List<string> Condition { get; set; }
        public List<int> Effect { get; set; }

        /// <summary>
        /// CenterSkill 构造函数。
        /// </summary>
        /// <param name="db">技能数据库</param>
        /// <param name="seriesId">技能系列ID</param>
        /// <param name="lv">技能等级，默认为 14</param>
        public CenterSkill(int seriesId, int lv = 14)
        {
            // 对应 Python 的初始定义和检查
            SkillId = "0"; // 默认值

            if (seriesId == 0)
            {
                // 如果 series_id == 0，则执行 return; C# 构造函数只需执行到最后。
                Condition = new List<string>();
                Effect = new List<int>();
                return;
            }
            
            // 对应 self.skill_id = str(series_id * 100 + lv)
            SkillId = (seriesId * 100 + lv).ToString();

            var db = SkillDataManager.CenterSkillDatabase;

            if (db.TryGetValue(SkillId, out CenterSkillDbData skillData))
            {

                // 对应 self.condition: list[str] = db[self.skill_id]["CenterSkillConditionIds"]
                Condition = skillData.CenterSkillConditionIds;

                // 对应 self.effect: list[int] = db[self.skill_id]["CenterSkillEffectId"]
                Effect = skillData.CenterSkillEffectId;
            }
            else
            {
                // 如果找不到，初始化空列表或抛出异常，这里初始化空列表以匹配 seriesId=0 的情况
                Condition = new List<string>();
                Effect = new List<int>();
            }
        }
    }

    // ===============================================
    // 3. CenterAttribute 类 (对应队长技能的属性部分)
    // ===============================================
    public class CenterAttribute
    {
        // 属性定义
        public string SkillId { get; set; }
        public List<List<string>> Target { get; set; } // List of List of strings
        public List<int> Effect { get; set; }

        /// <summary>
        /// CenterAttribute 构造函数。
        /// </summary>
        /// <param name="db">技能数据库</param>
        /// <param name="seriesId">系列ID</param>
        public CenterAttribute(int seriesId)
        {
            // 对应 Python 的初始定义和检查
            SkillId = "0"; // 默认值
            
            if (seriesId == 0)
            {
                Target = new List<List<string>>();
                Effect = new List<int>();
                return;
            }
            
            // 对应 self.skill_id = str(series_id + 1)
            SkillId = (seriesId + 1).ToString();

            var db = SkillDataManager.CenterAttributeDatabase;

            if (db.TryGetValue(SkillId, out CenterAttributeDbData skillData))
            {

                // --- 处理 Target ---
                // 对应 db[self.skill_id].get("TargetIds", None)
                // C# 中 Dictionary 的 TryGetValue 或使用 if(ContainsKey) 结合空检查
                var targetIds = skillData.TargetIds;
                
                if (targetIds != null)
                {
                    // 对应 [target.split(",") for target in ...]
                    Target = targetIds
                        .Select(targetStr => targetStr.Split(',').ToList())
                        .ToList();
                }
                else
                {
                    Target = new List<List<string>>();
                }

                // --- 处理 Effect ---
                // 对应 db[self.skill_id].get("CenterAttributeEffectId", None)
                var effectIds = skillData.CenterAttributeEffectId;
                
                if (effectIds != null)
                {
                    Effect = effectIds;
                }
                else
                {
                    Effect = new List<int>();
                }
            }
            else
            {
                // 如果找不到，初始化空列表
                Target = new List<List<string>>();
                Effect = new List<int>();
            }
        }
    }
}