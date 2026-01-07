using DeckMiner.Config;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DeckMiner.Services
{
    /// <summary>
    /// 模擬執行服務
    /// 負責管理模擬流程與進度回報
    /// </summary>
    public class SimulationService
    {
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isRunning = false;

        public bool IsRunning => _isRunning;

        // 事件：進度更新 (progress: 0-100, message: 狀態訊息)
        public event Action<int, string> ProgressChanged;

        // 事件：日誌輸出
        public event Action<string> LogOutput;

        // 事件：執行完成 (success: 是否成功)
        public event Action<bool> ExecutionCompleted;

        /// <summary>
        /// 執行完整流程（模擬 + 優化）
        /// </summary>
        public async Task ExecuteFullOptimizationAsync(MemberConfig config, string configPath)
        {
            if (_isRunning)
            {
                OnLogOutput("[WARN] Simulation is already running");
                return;
            }

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            bool success = false;

            try
            {
                OnLogOutput("\n=== 開始執行完整優化流程 ===");
                OnProgressChanged(0, "準備中...");

                // 驗證配置
                if (!ValidateConfiguration(config))
                {
                    OnLogOutput("[FAIL] Configuration validation failed");
                    return;
                }

                // 階段 1：模擬歌曲 (0-70%)
                OnLogOutput("\n--- 階段 1：模擬歌曲 ---");
                OnProgressChanged(5, $"開始模擬 {config.Songs.Count} 首歌曲...");

                bool simulationSuccess = await ExecuteSimulationAsync(config, configPath, _cancellationTokenSource.Token);

                if (!simulationSuccess)
                {
                    OnLogOutput("[FAIL] Simulation failed");
                    return;
                }

                OnProgressChanged(70, "模擬完成");

                // 階段 2：多曲優化 (70-100%)，僅在 3 首歌時執行
                if (config.Songs.Count == 3)
                {
                    OnLogOutput("\n--- 階段 2：多曲優化 ---");
                    OnProgressChanged(75, "開始多曲優化...");

                    bool optimizationSuccess = await ExecuteOptimizerAsync(config, configPath, _cancellationTokenSource.Token);

                    if (!optimizationSuccess)
                    {
                        OnLogOutput("[FAIL] Optimization failed");
                        return;
                    }

                    OnProgressChanged(100, "優化完成");
                }
                else
                {
                    OnLogOutput($"[INFO] Skipping multi-song optimization (only {config.Songs.Count} song(s) configured)");
                    OnProgressChanged(100, "模擬完成");
                }

                OnLogOutput("\n=== 完整優化流程執行成功 ===");
                success = true;
            }
            catch (OperationCanceledException)
            {
                OnLogOutput("[WARN] Execution cancelled by user");
            }
            catch (Exception ex)
            {
                OnLogOutput($"[FAIL] Execution error: {ex.Message}");
                OnLogOutput($"[DEBUG] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                _isRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                OnExecutionCompleted(success);
            }
        }

        /// <summary>
        /// 僅執行模擬（不執行優化）
        /// </summary>
        public async Task ExecuteSimulationOnlyAsync(MemberConfig config, string configPath)
        {
            if (_isRunning)
            {
                OnLogOutput("[WARN] Simulation is already running");
                return;
            }

            _isRunning = true;
            _cancellationTokenSource = new CancellationTokenSource();
            bool success = false;

            try
            {
                OnLogOutput("\n=== 開始執行模擬 ===");
                OnProgressChanged(0, "準備中...");

                // 驗證配置
                if (!ValidateConfiguration(config))
                {
                    OnLogOutput("[FAIL] Configuration validation failed");
                    return;
                }

                // 執行模擬
                OnProgressChanged(5, $"開始模擬 {config.Songs.Count} 首歌曲...");
                success = await ExecuteSimulationAsync(config, configPath, _cancellationTokenSource.Token);

                if (success)
                {
                    OnProgressChanged(100, "模擬完成");
                    OnLogOutput("\n=== 模擬執行成功 ===");
                }
                else
                {
                    OnLogOutput("[FAIL] Simulation failed");
                }
            }
            catch (OperationCanceledException)
            {
                OnLogOutput("[WARN] Simulation cancelled by user");
            }
            catch (Exception ex)
            {
                OnLogOutput($"[FAIL] Simulation error: {ex.Message}");
                OnLogOutput($"[DEBUG] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                _isRunning = false;
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                OnExecutionCompleted(success);
            }
        }

        /// <summary>
        /// 停止執行
        /// </summary>
        public void Stop()
        {
            if (_isRunning && _cancellationTokenSource != null)
            {
                OnLogOutput("[INFO] Stopping execution...");
                _cancellationTokenSource.Cancel();
            }
        }

        /// <summary>
        /// 驗證配置
        /// </summary>
        private bool ValidateConfiguration(MemberConfig config)
        {
            if (config.Songs == null || config.Songs.Count == 0)
            {
                OnLogOutput("[FAIL] No songs configured");
                return false;
            }

            if (config.Songs.Count > 3)
            {
                OnLogOutput($"[FAIL] Too many songs configured ({config.Songs.Count}), maximum is 3");
                return false;
            }

            if (config.CardIds == null || config.CardIds.Count == 0)
            {
                OnLogOutput("[FAIL] Card pool is empty");
                return false;
            }

            OnLogOutput($"[PASS] Configuration validated: {config.Songs.Count} song(s), {config.CardIds.Count} card(s)");
            return true;
        }

        /// <summary>
        /// 執行模擬 (C# 實作，階段 1)
        /// </summary>
        private async Task<bool> ExecuteSimulationAsync(MemberConfig config, string configPath, CancellationToken cancellationToken)
        {
            OnLogOutput("[INFO] Starting C# batch simulation");
            OnLogOutput($"[INFO] Config: {configPath}");
            OnLogOutput($"[INFO] Songs: {config.Songs.Count}");
            OnLogOutput($"[INFO] Card pool: {config.CardIds.Count} cards");

            try
            {
                // 載入 YAML 配置
                var yamlConfig = new YamlConfigManager(configPath);

                // 初始化進度追蹤
                int totalSongs = config.Songs.Count;
                int currentSong = 0;

                // 在背景執行緒執行批次模擬
                await Task.Run(() =>
                {
                    BatchSimulationService.RunBatchSimulation(
                        yamlConfig,
                        onLog: OnLogOutput,
                        onProgress: (current, total, message) =>
                        {
                            currentSong = current;
                            // 計算進度百分比 (5-70% 分配給模擬階段)
                            int progress = 5 + (int)((current / (double)total) * 65.0);
                            OnProgressChanged(progress, message);
                        },
                        cancellationToken: cancellationToken
                    );
                }, cancellationToken);

                OnLogOutput("[PASS] Batch simulation completed successfully");
                return true;
            }
            catch (OperationCanceledException)
            {
                OnLogOutput("[WARN] Simulation cancelled by user");
                throw;
            }
            catch (Exception ex)
            {
                OnLogOutput($"[FAIL] Batch simulation error: {ex.Message}");
                OnLogOutput($"[DEBUG] Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// 執行多曲優化器 (打包的 exe 或 Python 腳本，階段 2)
        /// </summary>
        private async Task<bool> ExecuteOptimizerAsync(MemberConfig config, string configPath, CancellationToken cancellationToken)
        {
            OnLogOutput("[INFO] Starting multi-song optimizer");
            OnLogOutput($"[DEBUG] Config path: {configPath}");

            // 優先使用打包的 exe，否則回退到 Python 腳本
            string baseDir = AppContext.BaseDirectory;
            string optimizerExe = Path.Combine(baseDir, "multi_optimizer_2.exe");
            string optimizerPy = Path.Combine(Path.GetFullPath(Path.Combine(baseDir, "..")), "multi_optimizer_2.py");

            string fileName;
            string arguments;
            string workingDir;

            if (File.Exists(optimizerExe))
            {
                // 使用打包的 exe（工作目錄設為 DeckMinerLite.exe 所在目錄）
                // GameData/Musics.yaml 會在這個目錄下，optimizer 會優先載入
                OnLogOutput("[INFO] Using packaged optimizer: multi_optimizer_2.exe");
                fileName = optimizerExe;
                arguments = $"--config \"{configPath}\"";
                workingDir = baseDir;
                OnLogOutput($"[DEBUG] Command: {fileName} {arguments}");
                OnLogOutput($"[DEBUG] Working directory: {workingDir}");
            }
            else if (File.Exists(optimizerPy))
            {
                // 回退到 Python 腳本（開發環境）
                OnLogOutput("[INFO] Using Python script: multi_optimizer_2.py (development mode)");
                fileName = "python";
                arguments = $"\"{optimizerPy}\" --config \"{configPath}\"";
                workingDir = Path.GetDirectoryName(optimizerPy);
                OnLogOutput($"[DEBUG] Command: {fileName} {arguments}");
                OnLogOutput($"[DEBUG] Working directory: {workingDir}");
            }
            else
            {
                OnLogOutput($"[FAIL] Optimizer not found!");
                OnLogOutput($"[FAIL] Checked: {optimizerExe}");
                OnLogOutput($"[FAIL] Checked: {optimizerPy}");
                return false;
            }

            try
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    WorkingDirectory = workingDir,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = new Process { StartInfo = processStartInfo };

                // 訂閱輸出事件
                process.OutputDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        OnLogOutput($"[OPTIMIZER] {e.Data}");
                    }
                };

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (!string.IsNullOrEmpty(e.Data))
                    {
                        OnLogOutput($"[OPTIMIZER ERROR] {e.Data}");
                    }
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                // 等待執行完成，同時處理取消請求
                while (!process.HasExited)
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        OnLogOutput("[WARN] Killing optimizer process...");
                        process.Kill();
                        cancellationToken.ThrowIfCancellationRequested();
                    }

                    await Task.Delay(100, cancellationToken);

                    // 模擬進度更新 (70-100%)
                    // TODO: 解析 Python 輸出以取得實際進度
                }

                await process.WaitForExitAsync(cancellationToken);

                if (process.ExitCode == 0)
                {
                    OnLogOutput("[PASS] Optimizer completed successfully");
                    return true;
                }
                else
                {
                    OnLogOutput($"[FAIL] Optimizer exited with code {process.ExitCode}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                OnLogOutput($"[FAIL] Optimizer execution error: {ex.Message}");
                return false;
            }
        }

        // 事件觸發方法
        private void OnProgressChanged(int progress, string message)
        {
            ProgressChanged?.Invoke(progress, message);
        }

        private void OnLogOutput(string message)
        {
            LogOutput?.Invoke(message);
        }

        private void OnExecutionCompleted(bool success)
        {
            ExecutionCompleted?.Invoke(success);
        }
    }
}
