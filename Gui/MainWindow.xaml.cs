using DeckMiner.Config;
using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows;
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
        AppendLog("[HINT] Load a YAML configuration file to begin");
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
            LgpModeTextBox.Text = config.LgpMode.ToString();
            CardPoolSizeTextBox.Text = $"{config.CardIds?.Count ?? 0} cards";

            if (config.Songs != null && config.Songs.Count > 0)
            {
                SongsSummary.Text = $"{config.Songs.Count} song(s) configured";
                SongsListBox.ItemsSource = config.Songs;
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
}
