using DeckMiner.Config;
using DeckMiner.Services;
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
    private SimulationService _simulationService;

    public MainWindow()
    {
        InitializeComponent();

        // Initialize simulation service
        _simulationService = new SimulationService();
        _simulationService.ProgressChanged += OnSimulationProgressChanged;
        _simulationService.LogOutput += OnSimulationLogOutput;
        _simulationService.ExecutionCompleted += OnSimulationCompleted;

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
                SongsSummary.Text = $"已配置 {config.Songs.Count} 首歌曲";
                // 轉換為 ViewModel 以顯示歌曲名稱
                var songViewModels = config.Songs.Select(s => ViewModels.SongViewModel.FromConfig(s)).ToList();
                SongsListBox.ItemsSource = songViewModels;
                StartSimulationButton.IsEnabled = true;
            }
            else
            {
                SongsSummary.Text = "尚未配置歌曲";
                SongsListBox.ItemsSource = null;
                StartSimulationButton.IsEnabled = false;
            }

            ReloadConfigButton.IsEnabled = true;
            SaveConfigButton.IsEnabled = true;
            EditCardPoolButton.IsEnabled = true;
            EditSongsButton.IsEnabled = true;
            EditFanLevelsButton.IsEnabled = true;
            EditGlobalFriendCardsButton.IsEnabled = true;
            EditOptimizerButton.IsEnabled = true;
            UpdateGlobalFriendCardsSummary();
            UpdateOptimizerSummary();
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

    private void EditGlobalFriendCardsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_configManager == null) return;

        var friendCardSelector = new FriendCardSelectorWindow(_configManager.Config.FriendCardIds);
        friendCardSelector.Owner = this;

        if (friendCardSelector.ShowDialog() == true)
        {
            // Update global friend card pool
            _configManager.Config.FriendCardIds = friendCardSelector.SelectedCardIds;
            UpdateGlobalFriendCardsSummary();
            AppendLog($"[INFO] Updated global friend cards: {_configManager.Config.FriendCardIds.Count} cards selected");
        }
    }

    private void UpdateGlobalFriendCardsSummary()
    {
        if (_configManager?.Config?.FriendCardIds != null && _configManager.Config.FriendCardIds.Count > 0)
        {
            GlobalFriendCardsSummary.Text = $"已選擇 {_configManager.Config.FriendCardIds.Count} 張朋友卡";
        }
        else
        {
            GlobalFriendCardsSummary.Text = "未選擇朋友卡";
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
                SongsSummary.Text = $"已配置 {config.Songs.Count} 首歌曲";
                // 轉換為 ViewModel 以顯示歌曲名稱
                var songViewModels = config.Songs.Select(s => ViewModels.SongViewModel.FromConfig(s)).ToList();
                SongsListBox.ItemsSource = songViewModels;
                StartSimulationButton.IsEnabled = true;
            }
            else
            {
                SongsSummary.Text = "尚未配置歌曲";
                SongsListBox.ItemsSource = null;
                StartSimulationButton.IsEnabled = false;
            }

            AppendLog($"[INFO] Updated song configuration: {config.Songs?.Count ?? 0} songs configured");
            FooterStatusText.Text = $"Loaded: {Path.GetFileName(_currentConfigPath)} | {config.Songs?.Count ?? 0} songs | {config.CardIds?.Count ?? 0} cards";
        }
    }

    private void EditOptimizerButton_Click(object sender, RoutedEventArgs e)
    {
        if (_configManager == null) return;

        var optimizerWindow = new OptimizerConfigWindow(_configManager.Config);
        optimizerWindow.Owner = this;

        if (optimizerWindow.ShowDialog() == true)
        {
            // Config is updated inside OptimizerConfigWindow
            UpdateOptimizerSummary();
            AppendLog($"[INFO] Updated optimizer configuration");
        }
    }

    private void UpdateOptimizerSummary()
    {
        if (_configManager?.Config?.Optimizer != null)
        {
            int forbiddenCount = _configManager.Config.Optimizer.ForbiddenCards?.Count ?? 0;
            if (forbiddenCount > 0)
            {
                OptimizerSummary.Text = $"Top N: {_configManager.Config.Optimizer.TopN} | 全局禁卡: {forbiddenCount} 張";
            }
            else
            {
                OptimizerSummary.Text = $"Top N: {_configManager.Config.Optimizer.TopN} | 無全局禁卡";
            }
        }
        else
        {
            OptimizerSummary.Text = "用於 multi_optimizer_2.py";
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

    private async void StartSimulationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_configManager == null)
        {
            MessageBox.Show(
                "請先載入配置檔案",
                "無配置檔案",
                MessageBoxButton.OK,
                MessageBoxImage.Warning
            );
            return;
        }

        if (_simulationService.IsRunning)
        {
            MessageBox.Show(
                "模擬正在執行中",
                "執行中",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
            return;
        }

        // 在開始模擬前先儲存配置，確保使用最新的設定
        try
        {
            _configManager.SaveConfig();
            AppendLog("[INFO] Configuration saved before simulation");
        }
        catch (Exception ex)
        {
            AppendLog($"[WARN] Failed to save configuration: {ex.Message}");
            var result = MessageBox.Show(
                $"儲存配置時發生錯誤：\n{ex.Message}\n\n是否仍要繼續執行？",
                "儲存配置失敗",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning
            );

            if (result == MessageBoxResult.No)
            {
                return;
            }
        }

        // 禁用開始按鈕，啟用停止按鈕
        StartSimulationButton.IsEnabled = false;
        StopSimulationButton.IsEnabled = true;

        // 重置進度條
        SimulationProgressBar.Value = 0;
        SimulationStatusText.Text = "準備中...";

        // 根據選擇的執行模式執行
        if (FullOptimizationModeRadio.IsChecked == true)
        {
            await _simulationService.ExecuteFullOptimizationAsync(_configManager.Config, _currentConfigPath);
        }
        else
        {
            await _simulationService.ExecuteSimulationOnlyAsync(_configManager.Config, _currentConfigPath);
        }
    }

    private void StopSimulationButton_Click(object sender, RoutedEventArgs e)
    {
        if (_simulationService.IsRunning)
        {
            var result = MessageBox.Show(
                "確定要停止執行嗎？",
                "確認停止",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                _simulationService.Stop();
            }
        }
    }

    private void ClearLogButton_Click(object sender, RoutedEventArgs e)
    {
        LogTextBox.Clear();
        AppendLog("[INFO] Log cleared");
    }

    private void ExecutionModeChanged(object sender, RoutedEventArgs e)
    {
        // 避免在 XAML 初始化時觸發（此時 LogTextBox 尚未初始化）
        if (LogTextBox == null) return;

        if (FullOptimizationModeRadio?.IsChecked == true)
        {
            AppendLog("[INFO] Execution mode: Full Optimization (Simulation + Optimizer)");
        }
        else if (SimulationOnlyModeRadio?.IsChecked == true)
        {
            AppendLog("[INFO] Execution mode: Simulation Only");
        }
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

    // SimulationService 事件處理器
    private void OnSimulationProgressChanged(int progress, string message)
    {
        Dispatcher.Invoke(() =>
        {
            SimulationProgressBar.Value = progress;
            SimulationStatusText.Text = message;
        });
    }

    private void OnSimulationLogOutput(string message)
    {
        Dispatcher.Invoke(() =>
        {
            AppendLog(message);
        });
    }

    private void OnSimulationCompleted(bool success)
    {
        Dispatcher.Invoke(() =>
        {
            StartSimulationButton.IsEnabled = true;
            StopSimulationButton.IsEnabled = false;

            if (success)
            {
                // Automatically load results if available
                LoadResults();

                MessageBox.Show(
                    "執行完成！\n\n請切換到 Results 分頁查看結果",
                    "執行成功",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
            }
            else
            {
                MessageBox.Show(
                    "執行失敗或已取消。\n\n請查看日誌以了解詳情",
                    "執行失敗",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning
                );
            }
        });
    }

    private void RefreshResultsButton_Click(object sender, RoutedEventArgs e)
    {
        LoadResults();
    }

    private void LoadResults()
    {
        try
        {
            // Determine the working directory where results file should be
            string baseDir = AppContext.BaseDirectory;

            // First try the base directory (for packaged exe)
            string resultsPath = Path.Combine(baseDir, "best_3_song_combo.txt");

            // If not found, try the parent directory (for development mode)
            if (!File.Exists(resultsPath))
            {
                string parentDir = Path.GetFullPath(Path.Combine(baseDir, ".."));
                resultsPath = Path.Combine(parentDir, "best_3_song_combo.txt");
            }

            // Also check for 2-song combo file
            string results2Path = resultsPath.Replace("best_3_song_combo.txt", "best_2_song_combo.txt");

            string resultsContent = null;
            string fileName = null;

            // Try to load 3-song results first, then 2-song
            if (File.Exists(resultsPath))
            {
                resultsContent = File.ReadAllText(resultsPath, System.Text.Encoding.UTF8);
                fileName = "best_3_song_combo.txt";
            }
            else if (File.Exists(results2Path))
            {
                resultsContent = File.ReadAllText(results2Path, System.Text.Encoding.UTF8);
                fileName = "best_2_song_combo.txt";
            }

            if (resultsContent != null)
            {
                ResultsTextBox.Text = resultsContent;
                var fileInfo = new FileInfo(fileName == "best_3_song_combo.txt" ? resultsPath : results2Path);
                ResultsStatusText.Text = $"已載入: {fileName} (更新時間: {fileInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss})";
                ResultsStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                AppendLog($"[PASS] Results loaded from {fileName}");
            }
            else
            {
                ResultsTextBox.Text = "尚未找到結果檔案。\n\n請執行「完整優化」模式以生成 best_3_song_combo.txt 或 best_2_song_combo.txt 檔案。\n\n檔案搜尋路徑:\n" +
                                      $"1. {Path.Combine(baseDir, "best_3_song_combo.txt")}\n" +
                                      $"2. {Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "..")), "best_3_song_combo.txt")}";
                ResultsStatusText.Text = "尚無結果檔案";
                ResultsStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                AppendLog("[INFO] No results file found");
            }
        }
        catch (Exception ex)
        {
            ResultsTextBox.Text = $"載入結果時發生錯誤:\n\n{ex.Message}";
            ResultsStatusText.Text = "載入失敗";
            ResultsStatusText.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            AppendLog($"[FAIL] Failed to load results: {ex.Message}");
        }
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
