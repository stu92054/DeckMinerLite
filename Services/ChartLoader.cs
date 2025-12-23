using DeckMiner.Data;
using System.IO;
using System.Text.Json;
using System;

namespace DeckMiner.Services
{
    public class ChartLoader
    {
        /// <summary>
        /// 从 JSON 文件加载 ChartData。
        /// </summary>
        /// <param name="jsonPath">JSON 文件的完整路径。</param>
        /// <returns>反序列化后的 ChartData 对象。</returns>
        public ChartData LoadChartFromJson(string jsonPath)
        {
            if (!File.Exists(jsonPath))
            {
                throw new FileNotFoundException($"Chart JSON 文件未找到: {jsonPath}");
            }

            try
            {
                // 1. 读取 JSON 字符串
                string jsonString = File.ReadAllText(jsonPath);

                // 2. AOT 兼容的反序列化
                // 获取 ChartData 类型的 TypeInfo
                var typeInfo = AppJsonSerializerContext.Default.ChartData;
                
                // 进行反序列化
                var chart = (ChartData)JsonSerializer.Deserialize(jsonString, typeInfo);
                
                if (chart == null)
                {
                    throw new JsonException("JSON 反序列化失败，返回 null。数据格式可能不匹配。");
                }

                return chart;
            }
            catch (JsonException ex)
            {
                throw new JsonException($"JSON 文件解析失败 ({jsonPath})。请检查内容格式是否与 ChartData 匹配。", ex);
            }
            catch (Exception ex)
            {
                throw new IOException($"读取文件失败: {jsonPath}", ex);
            }
        }

        public static ChartData GetChart(string musicId, string tier)
        {
            Console.WriteLine($"\n--- 正在加载谱面 (ID: {musicId}, Tier: {tier}) ---");

            // 1. 定义 JSON 文件路径
            string baseDir = AppContext.BaseDirectory;
            // 假设 Python 预处理后的 JSON 文件放在 GameData/json/ 目录下
            string chartJsonPath = Path.Combine(baseDir, "GameData", "Chart", $"ChartEvents_{musicId}_{tier}.json");

            // 2. 实例化服务
            var chartLoader = new ChartLoader();

            try
            {
                // A. 文件 I/O 和 AOT JSON 反序列化
                ChartData chart = chartLoader.LoadChartFromJson(chartJsonPath);

                // --- 结果输出 ---
                Console.WriteLine($"[谱面信息]");
                Console.WriteLine($"  Note数: {chart.AllNoteSize}");
                
                var liveEnd = chart.Events.FirstOrDefault(e => e.Name == "LiveEnd");
                Console.WriteLine($"  歌曲时长: {liveEnd?.Time:F3}s");
                var feverStart = chart.Events.FirstOrDefault(e => e.Name == "FeverStart");
                Console.WriteLine($"  Fever开始: {feverStart?.Time:F3}s");
                var feverEnd = chart.Events.FirstOrDefault(e => e.Name == "FeverEnd");
                Console.WriteLine($"  Fever结束: {feverEnd?.Time:F3}s");

                Console.WriteLine("-----------------------------------------------------");
                
                // 此时，chart.Events 就是您的模拟器可以直接使用的有序事件列表。
                return chart;
            }
            catch (FileNotFoundException ex)
            {
                Console.WriteLine($"\n[FATAL ERROR] JSON 文件未找到: {ex.Message}");
                Console.WriteLine($"请确保 Python 生成的 JSON 文件已复制到输出目录的 GameData/json 文件夹中。");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n[FATAL ERROR] 测试失败: {ex.Message}");
                Console.WriteLine($"类型: {ex.GetType().Name}");
                return null;
            }
        }
    }
}