namespace DeckMiner.Data
{
    /// <summary>
    /// 映射 Musics.json 中单条歌曲数据的结构。
    /// </summary>
    public class MusicDbData
    {
        // 基础信息和 ID
        public int Id { get; set; }
        public int OrderId { get; set; }
        public string Title { get; set; }
        public string TitleFurigana { get; set; }
        public int JacketId { get; set; }
        public int SoundId { get; set; }
        public string Description { get; set; }

        // 角色和小组信息
        public int GenerationsId { get; set; }
        public int UnitId { get; set; }
        public int CenterCharacterId { get; set; }
        
        // 注意：SingerCharacterId 和 SupportCharacterId 在 JSON 中是逗号分隔的字符串
        public string SingerCharacterId { get; set; } 
        public string SupportCharacterId { get; set; }
        
        // 私有缓存字段：用于存储计算后的 List<int>
        private List<int> _singerCharactersCache = null;
        private List<int> _supportCharactersCache = null;

        // 游戏属性
        public int MusicType { get; set; }
        public int ExperienceType { get; set; }
        public int BeatPointCoefficient { get; set; }
        public int ApIncrement { get; set; }
        public int SongTime { get; set; }
        public int PlayTime { get; set; }
        public int FeverSectionNo { get; set; }
        public int MaxAp { get; set; }
        
        // 预览时间（单位：毫秒）
        public int PreviewStartTime { get; set; }
        public int PreviewEndTime { get; set; }
        public int PreviewFadeInTime { get; set; }
        public int PreviewFadeOutTime { get; set; }

        // 发布条件
        public int ReleaseConditionType { get; set; }
        public int ReleaseConditionDetail { get; set; }
        public string ReleaseConditionText { get; set; }
        
        // 视频模式和背景
        public int IsVideoMode { get; set; }
        public int VideoBgId { get; set; }
        public int SongType { get; set; }

        // 时间戳 (关键：使用 DateTimeOffset)
        // System.Text.Json 会自动将 ISO 8601 字符串（例如 "2022-12-31T15:00:00+00:00"）
        // 反序列化为带时区信息的 C# 对象。
        public DateTimeOffset StartTime { get; set; }
        public DateTimeOffset EndTime { get; set; }
        public DateTimeOffset MusicScoreReleaseTime { get; set; }

        // =======================================================
        // 新增的计算属性 (Computed Properties)
        // =======================================================
        
        public List<int> SingerCharacters
        {
            get
            {
                // 检查缓存：
                if (_singerCharactersCache == null)
                {
                    // 第一次访问：执行昂贵的计算，并存入缓存
                    _singerCharactersCache = ParseIdString(this.SingerCharacterId);
                }
                // 后续访问：直接返回缓存的值
                return _singerCharactersCache;
            }
        }
        
        public List<int> SupportCharacters
        {
            get
            {
                if (_supportCharactersCache == null)
                {
                    _supportCharactersCache = ParseIdString(this.SupportCharacterId);
                }
                return _supportCharactersCache;
            }
        }

        // 私有辅助方法：执行实际的字符串分割和转换
        private static List<int> ParseIdString(string idString)
        {
            if (string.IsNullOrWhiteSpace(idString))
            {
                return new List<int>(); // 如果为空或null，返回空列表
            }

            // 使用 LINQ 进行高效转换：
            // 1. Split(',')：按逗号分割字符串
            // 2. Select(s => int.Parse(s.Trim()))：遍历每个分割后的字符串，移除空格并解析为整数
            // 3. ToList()：转换为 List<int>
            try
            {
                // 0 代表無成員 (如 solo 曲的 SingerCharacterId="0")，需過濾掉
                return idString
                    .Split(',')
                    .Select(s => int.Parse(s.Trim()))
                    .Where(id => id != 0)
                    .ToList();
            }
            catch (FormatException ex)
            {
                // 处理解析失败的情况，例如字符串中包含非数字字符
                Console.WriteLine($"错误：无法解析 ID 字符串 '{idString}'。{ex.Message}");
                // 返回空列表或根据项目需求抛出异常
                return new List<int>();
            }
        }
    }
}