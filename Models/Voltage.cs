using System.Collections.Concurrent;

namespace DeckMiner.Models
{
    public class Voltage
    {
        // 静态缓存：用于存储达到某个等级所需的总点数
        private static readonly ConcurrentDictionary<int, int> _levelPointsCache = new ConcurrentDictionary<int, int>();

        private int _currentPoints;
        private int _currentLevel; 

        // 公共属性
        public int Level { get; private set; }
        public double Bonus { get; private set; }
        public bool IsFever { get; private set; }

        public Voltage(int initialPoints = 0)
        {
            _currentPoints = 0;
            _currentLevel = 0;
            Level = 0;
            Bonus = 1.0;
            IsFever = false;

            // 设置初始点数并计算初始等级
            SetPoints(initialPoints);
        }

        // 辅助函数：计算达到某个等级所需的总点数 (带缓存)
        private static int PointsNeededForLevel(int level)
        {
            if (level <= 0) return 0;

            return _levelPointsCache.GetOrAdd(level, (lvl) => 
            {
                if (lvl <= 20)
                {
                    // 公式：5 * N * (N + 1)
                    return 5 * lvl * (lvl + 1);
                }
                else
                {
                    // 公式：2100 + (lvl - 20) * 200
                    return lvl * 200 - 1900; 
                }
            });
        }

        /// <summary>
        /// 根据当前点数和上次的等级，高效地更新 Voltage 等级。
        /// </summary>
        private void UpdateLevel()
        {
            // int oldLevel = Level;

            // 1. 检查是否可以升级
            while (_currentPoints >= PointsNeededForLevel(_currentLevel + 1))
            {
                _currentLevel++;
            }

            // 2. 检查是否需要降级
            while (_currentPoints < PointsNeededForLevel(_currentLevel) && _currentLevel > 0)
            {
                _currentLevel--;
            }

            // 更新公共显示等级和加成
            int displayLevel = _currentLevel;
            if (IsFever)
            {
                displayLevel *= 2;
            }

            Level = displayLevel;
            Bonus = (Level + 10.0) / 10.0; // 始终使用 double 进行浮点运算

            // 可以在这里添加日志输出 (例如使用 ILogger)
            // if (oldLevel != Level) { Logger.LogDebug($"Voltage等级变化: 从 Lv.{oldLevel} -> Lv.{Level}"); }
        }

        public void AddPoints(int amount)
        {
            if (amount < 0 && _currentPoints + amount < 0)
            {
                _currentPoints = 0;
            }
            else
            {
                _currentPoints += amount;
            }
            // 可以在这里添加日志输出
            UpdateLevel();
        }

        public void SetPoints(int newPoints)
        {
            if (newPoints < 0) throw new ArgumentOutOfRangeException(nameof(newPoints), "设置的 VoltagePt 必须是非负整数。");

            // 可以在这里添加日志输出
            _currentPoints = newPoints;
            UpdateLevel();
        }

        public int GetPoints() => _currentPoints;

        public void SetFever(bool value)
        {
            IsFever = value;
            UpdateLevel();
        }
        
        // 简化版的 ToString()
        public override string ToString() => $"Voltage: {_currentPoints} Pt (Lv.{Level})";
    }
}