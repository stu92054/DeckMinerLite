using System.Text.Json;
using DeckMiner.Data;

namespace DeckMiner.Config
{
    public class TaskConfig
    {
        public List<int> CardPool { get; set; } = new();
        public List<SimulationTask> Task { get; set; } = new();
    }

    public class SimulationTask
    {
        public string MusicId { get; set; } = string.Empty;
        public string Tier { get; set; } = "02";
        public int MLv { get; set; } = 50;
        public RequiredCards MustCards { get; set; } = new();
        public List<int> ExcludeCards { get; set; } = new();
        public List<int> SecondaryCenter { get; set; } = new();
        public List<int> MustSkills { get; set; } = new();
    }

    public class RequiredCards
    {
        public List<int> All { get; set; } = new();
        public List<int> Any { get; set; } = new();
    }

    public static class TaskLoader
    {
        public static TaskConfig LoadTasks(string filePath)
        {
            try
            {
                string jsonString = File.ReadAllText(filePath);
                var typeInfo = AppJsonSerializerContext.Default.TaskConfig;
                return JsonSerializer.Deserialize(jsonString, typeInfo) ?? new TaskConfig();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载配置文件失败: {ex.Message}");
                return new TaskConfig();
            }
        }
    }
}