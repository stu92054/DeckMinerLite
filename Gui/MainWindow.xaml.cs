using DeckMiner.Config;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace DeckMiner.Gui;

/// <summary>
/// Main window for WPF GUI
/// </summary>
public partial class MainWindow : Window
{
    private YamlConfigManager _configManager;
    private string _currentConfigPath;

    public MainWindow()
    {
        InitializeComponent();

        // Use Loaded event to ensure UI is fully initialized before logging
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        AppendLog("[INFO] GUI initialized");
        AppendLog("[HINT] Load a YAML configuration file or create a new one to begin");
    }

    private void NewConfigButton_Click(object sender, RoutedEventArgs e)
    {
        var newConfigDialog = new NewConfigDialog();
        newConfigDialog.Owner = this;

        if (newConfigDialog.ShowDialog() == true)
        {
            string memberName = newConfigDialog.MemberName;
            string savePath = newConfigDialog.SavePath;

            try
            {
                // Create a new default configuration
                var newConfig = new MemberConfig
                {
                    CardIds = new System.Collections.Generic.List<int>(),
                    SeasonMode = "sukushow",
                    LgpMode = true,
                    Songs = new System.Collections.Generic.List<SongConfig>(),
                    FanLevels = new System.Collections.Generic.Dictionary<int, int>(),
                    CardLevels = new System.Collections.Generic.Dictionary<int, System.Collections.Generic.List<int>>(),
                    BatchSize = 1000000,
                    NumProcesses = null,
                    Cache = new CacheConfig
                    {
                        MaxFingerprintsInMemory = 5000000,
                        AutoCleanup = true,
                        MaxCacheAgeDays = 7
                    },
                    Output = new OutputConfig
                    {
                        BaseDir = "output",
                        EnableIsolation = true
                    }
                };

                // Save the new configuration
                var yamlContent = GenerateDefaultYamlContent(memberName);
                System.IO.File.WriteAllText(savePath, yamlContent);

                AppendLog($"[PASS] Created new configuration: {savePath}");

                // Load the newly created configuration
                LoadConfiguration(savePath);
            }
            catch (Exception ex)
            {
                AppendLog($"[FAIL] Failed to create new configuration: {ex.Message}");
                MessageBox.Show(
                    $"Failed to create new configuration:\n\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }
    }

    private string GenerateDefaultYamlContent(string memberName)
    {
        return $@"# ============================================
# Configuration for {memberName}
# ============================================

# 輸出目錄配置
output:
  base_dir: ""output""
  enable_isolation: true       # 開啟隔離，每次運行生成獨立目錄

# 歌曲配置 (支援多首歌曲)
songs: []

# Debug 卡組 (可選，用於單卡組測試)
debug_deck_cards: null

# 卡池
card_ids: []

# 賽季模式 (用於計算粉絲等級加成)
# - ""sukushow"": 只計算歌唱成員 (預設)
# - ""sukuste"": 計算所有成員
season_mode: ""sukushow""

# LGP 模式 (是否允許同角色雙卡)
# - false: 日常模式，每個角色最多1張卡
# - true: LGP 大賽模式，允許0-3個角色使用雙卡 (預設)
lgp_mode: true

# 粉絲等級
fan_levels:
  1011: 0   # 沙知
  1021: 0   # 梢
  1022: 0   # 綴理
  1023: 0   # 慈
  1031: 0   # 帆
  1032: 0   # 沙
  1033: 0   # 乃
  1041: 0   # 吟
  1042: 0   # 鈴
  1043: 0   # 芽
  1051: 0   # 泉
  1052: 0   # 塞

# 特定卡牌練度覆蓋 (如果有未滿練的卡)
# 格式: card_id: [level, center_skill_level, skill_level]
card_levels: {{}}

batch_size: 1000000
num_processes: null            # 使用所有 CPU 核心

cache:
  max_fingerprints_in_memory: 5000000
  auto_cleanup: true
  max_cache_age_days: 7

# 優化器配置 (用於 multi_optimizer_2.py)
optimizer:
  top_n: 50000                 # 每首歌保留得分排名前 N 名的卡組
  show_card_names: true        # 在輸出中顯示卡牌名稱
  forbidden_cards: []          # 全局禁止使用的卡牌 ID 列表 (三面均生效)
  songs: []
";
    }

    private void LoadConfigButton_Click(object sender, RoutedEventArgs e)
    {
        string initialDir = AppContext.BaseDirectory;
        
        // Attempt to locate the 'config' directory relative to the executable
        // Common scenarios:
        // 1. Deployed: ./config
        // 2. Dev (bin/Debug/net10.0-windows): ../../../../config
        
        string[] possiblePaths = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "config"),
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "config")
        };

        foreach (var path in possiblePaths)
        {
            try 
            {
                string fullPath = Path.GetFullPath(path);
                if (Directory.Exists(fullPath))
                {
                    initialDir = fullPath;
                    break;
                }
            }
            catch
            {
                // Ignore invalid paths
            }
        }

        var dialog = new OpenFileDialog
        {
            Title = "Select YAML Configuration File",
            Filter = "YAML Files (*.yaml;*.yml)|*.yaml;*.yml|All Files (*.*)|*.*",
            InitialDirectory = initialDir
        };

        if (dialog.ShowDialog() == true)
        {
            LoadConfiguration(dialog.FileName);
        }
    }

    private void ReloadConfigButton_Click(object sender, RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(_currentConfigPath))
        {
            LoadConfiguration(_currentConfigPath);
        }
    }

    private void LoadConfiguration(string configPath)
    {
        try
        {
            AppendLog($"[CHECK] Loading configuration: {configPath}");

            _configManager = new YamlConfigManager(configPath);
            _currentConfigPath = configPath;

            var config = _configManager.Config;

            // Update UI
            ConfigPathTextBox.Text = configPath;
            MemberNameTextBox.Text = _configManager.MemberName ?? "Unknown";
            SeasonModeTextBox.Text = config.SeasonMode;

            // Set LGP Mode ComboBox
            foreach (ComboBoxItem item in LgpModeComboBox.Items)
            {
                if (item.Tag?.ToString() == config.LgpMode.ToString())
                {
                    LgpModeComboBox.SelectedItem = item;
                    break;
                }
            }

            CardPoolSizeTextBox.Text = $"{config.CardIds?.Count ?? 0} cards";

            // Update Fan Levels Summary
            UpdateFanLevelsSummary();

            if (config.Songs != null && config.Songs.Count > 0)
            {
                SongsSummary.Text = $"{config.Songs.Count} song(s) configured";
                // 轉換為 ViewModel 以顯示歌曲名稱
                var songViewModels = config.Songs.Select(s => ViewModels.SongViewModel.FromConfig(s)).ToList();
                SongsListBox.ItemsSource = songViewModels;
                StartSimulationButton.IsEnabled = true;
            }
            else
            {
                SongsSummary.Text = "No songs configured";
                SongsListBox.ItemsSource = null;
                StartSimulationButton.IsEnabled = false;
            }

            ReloadConfigButton.IsEnabled = true;
            SaveConfigButton.IsEnabled = true;
            EditCardPoolButton.IsEnabled = true;
            EditSongsButton.IsEnabled = true;
            EditFanLevelsButton.IsEnabled = true;
            StatusText.Text = $"Configuration loaded: {_configManager.MemberName}";
            FooterStatusText.Text = $"Loaded: {Path.GetFileName(configPath)} | {config.Songs?.Count ?? 0} songs | {config.CardIds?.Count ?? 0} cards";

            AppendLog($"[PASS] Configuration loaded successfully");
            AppendLog($"[INFO] Member: {_configManager.MemberName}");
            AppendLog($"[INFO] Songs: {config.Songs?.Count ?? 0} | Cards: {config.CardIds?.Count ?? 0} | LGP Mode: {config.LgpMode}");
        }
        catch (Exception ex)
        {
            AppendLog($"[FAIL] Failed to load configuration: {ex.Message}");
            MessageBox.Show(
                $"Failed to load configuration:\n\n{ex.Message}",
                "Configuration Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private void EditCardPoolButton_Click(object sender, RoutedEventArgs e)
    {
        if (_configManager == null) return;

        var cardPoolWindow = new CardPoolWindow(_configManager.Config);
        cardPoolWindow.Owner = this;

        if (cardPoolWindow.ShowDialog() == true)
        {
            // Config is updated inside CardPoolWindow
            CardPoolSizeTextBox.Text = $"{_configManager.Config.CardIds.Count} cards";
            AppendLog($"[INFO] Updated card pool: {_configManager.Config.CardIds.Count} cards selected");
        }
    }

    private void EditFanLevelsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_configManager == null) return;

        var fanLevelsWindow = new FanLevelsWindow(_configManager.Config);
        fanLevelsWindow.Owner = this;

        if (fanLevelsWindow.ShowDialog() == true)
        {
            // Config is updated inside FanLevelsWindow
            UpdateFanLevelsSummary();
            AppendLog($"[INFO] Updated fan levels");
        }
    }

    private void UpdateFanLevelsSummary()
    {
        if (_configManager?.Config?.FanLevels != null && _configManager.Config.FanLevels.Count > 0)
        {
            FanLevelsSummary.Text = "已設定粉絲等級";
        }
        else
        {
            FanLevelsSummary.Text = "未設定粉絲等級";
        }
    }

    private void EditSongsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_configManager == null) return;

        var songConfigWindow = new SongConfigWindow(_configManager.Config);
        songConfigWindow.Owner = this;

        if (songConfigWindow.ShowDialog() == true)
        {
            // Config is updated inside SongConfigWindow
            var config = _configManager.Config;

            if (config.Songs != null && config.Songs.Count > 0)
            {
                SongsSummary.Text = $"{config.Songs.Count} song(s) configured";
                // 轉換為 ViewModel 以顯示歌曲名稱
                var songViewModels = config.Songs.Select(s => ViewModels.SongViewModel.FromConfig(s)).ToList();
                SongsListBox.ItemsSource = songViewModels;
                StartSimulationButton.IsEnabled = true;
            }
            else
            {
                SongsSummary.Text = "No songs configured";
                SongsListBox.ItemsSource = null;
                StartSimulationButton.IsEnabled = false;
            }

            AppendLog($"[INFO] Updated song configuration: {config.Songs?.Count ?? 0} songs configured");
            FooterStatusText.Text = $"Loaded: {Path.GetFileName(_currentConfigPath)} | {config.Songs?.Count ?? 0} songs | {config.CardIds?.Count ?? 0} cards";
        }
    }

    private void SaveConfigButton_Click(object sender, RoutedEventArgs e)
    {
        if (_configManager == null) return;

        try
        {
            _configManager.SaveConfig();
            AppendLog($"[PASS] Configuration saved successfully");
            MessageBox.Show("Configuration saved successfully.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            AppendLog($"[FAIL] Failed to save configuration: {ex.Message}");
            MessageBox.Show($"Failed to save configuration:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StartSimulationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_configManager == null)
        {
            MessageBox.Show(
                "Please load a configuration file first.",
                "No Configuration",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        AppendLog("\n[INFO] Starting simulation...");
        AppendLog("[INFO] This feature will be fully implemented in Phase 3");
        AppendLog("[HINT] For now, use CLI mode for full simulation functionality");

        MessageBox.Show(
            "Simulation feature is under development (Phase 3).\n\n" +
            "Current implementation:\n" +
            "- Configuration loading: ✓ Complete\n" +
            "- UI controls: ✓ Complete\n" +
            "- Async simulation: ⚠ In Progress\n" +
            "- Results display: ⚠ Planned\n\n" +
            "For full simulation, use CLI mode:\n" +
            "DeckMinerLite.exe --config path/to/config.yaml",
            "Feature In Development",
            MessageBoxButton.OK,
            MessageBoxImage.Information
        );
    }

    private void StopSimulationButton_Click(object sender, RoutedEventArgs e)
    {
        AppendLog("[INFO] Stop requested (placeholder)");
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
        AppendLog("[INFO] Log cleared");
    }

    private void OpenOutputFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            string outputDir;
            if (_configManager != null)
            {
                outputDir = _configManager.GetLogDir();
            }
            else
            {
                outputDir = Path.Combine(AppContext.BaseDirectory, "log");
            }

            if (!Directory.Exists(outputDir))
            {
                Directory.CreateDirectory(outputDir);
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = outputDir,
                UseShellExecute = true
            });

            AppendLog($"[INFO] Opened output folder: {outputDir}");
        }
        catch (Exception ex)
        {
            AppendLog($"[FAIL] Failed to open output folder: {ex.Message}");
            MessageBox.Show(
                $"Failed to open output folder:\n{ex.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void AppendLog(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogTextBox.AppendText($"[{timestamp}] {message}\n");
        LogScrollViewer.ScrollToEnd();
    }

    private void BasicSettingsChanged(object sender, RoutedEventArgs e)
    {
        if (_configManager == null) return;

        try
        {
            // Update LGP Mode
            if (LgpModeComboBox.SelectedItem is ComboBoxItem lgpModeItem)
            {
                string lgpModeStr = lgpModeItem.Tag?.ToString();
                if (bool.TryParse(lgpModeStr, out bool lgpMode))
                {
                    _configManager.Config.LgpMode = lgpMode;
                }
            }

            FooterStatusText.Text = $"Loaded: {Path.GetFileName(_currentConfigPath)} | {_configManager.Config.Songs?.Count ?? 0} songs | {_configManager.Config.CardIds?.Count ?? 0} cards | Modified";
        }
        catch (Exception ex)
        {
            AppendLog($"[WARN] Failed to update basic settings: {ex.Message}");
        }
    }
}
