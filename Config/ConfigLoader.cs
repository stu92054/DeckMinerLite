using System.IO;
using System.Text.Json;
using System.Reflection;

using DeckMiner.Data;

namespace DeckMiner.Config
{
    public static class ConfigLoader
    {
        private static CardConfig _configInstance;
        private static readonly string ConfigFileName = "cardConfig.jsonc";

        public static CardConfig Config
        {
            get
            {
                if (_configInstance == null)
                {
                    // 首次访问时加载配置
                    _configInstance = LoadConfig();
                }
                return _configInstance;
            }
        }

        private static CardConfig LoadConfig()
        {
            // 获取配置文件在应用程序目录下的完整路径
            string configPath = Path.Combine(
                AppContext.BaseDirectory, 
                ConfigFileName
            );

            if (!File.Exists(configPath))
            {
                // 如果文件不存在，则创建默认配置并保存
                var defaultConfig = new CardConfig();
                SaveConfig(defaultConfig, configPath);
                Console.WriteLine($"配置文件 '{ConfigFileName}' 不存在，已创建默认文件。");
                return defaultConfig;
            }

            try
            {
                string jsonString = File.ReadAllText(configPath);
                // 反序列化：将 JSON 字符串转换为 C# 对象
                var typeInfo = AppJsonSerializerContext.Default.CardConfig;
                var config = JsonSerializer.Deserialize(jsonString, typeInfo);
                
                // 确保反序列化成功且不为 null
                return config ?? new CardConfig(); 
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"加载配置失败: {ex.Message}。使用默认配置。");
                return new CardConfig();
            }
        }

        // 可选：用于将配置对象保存回文件
        private static void SaveConfig(CardConfig config, string path)
        {
            var typeInfo = AppJsonSerializerContext.Default.CardConfig;
            string jsonString = JsonSerializer.Serialize(config, typeInfo);
            File.WriteAllText(path, jsonString);
        }
    }
}