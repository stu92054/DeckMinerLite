# WPF GUI 設計文件

## 專案概述

為 DeckMinerLite 開發 Windows 平台的 WPF 圖形化介面，提供友好的卡組配置與模擬執行體驗。

**目標使用者**: Windows 平台使用者
**設計原則**: 簡單易用、功能完整、與 CLI 版本功能對等

---

## 1. 架構設計

### 1.1 混合架構 (Hybrid Architecture)

```
┌─────────────────────────────────────────┐
│         DeckMinerLite.exe               │
├─────────────────────────────────────────┤
│  Entry Point: Program.Main(args)       │
│                                         │
│  ┌─────────────┐    ┌────────────────┐ │
│  │ args.Length │───>│ CLI Mode       │ │
│  │ > 0?        │    │ (All Platform) │ │
│  └─────────────┘    └────────────────┘ │
│         │ No                            │
│         v                               │
│  ┌─────────────┐                        │
│  │ #if WINDOWS │                        │
│  └─────────────┘                        │
│         │ Yes                           │
│         v                               │
│  ┌─────────────┐    ┌────────────────┐ │
│  │ Launch WPF  │───>│ WPF GUI        │ │
│  │ Application │    │ (Windows Only) │ │
│  └─────────────┘    └────────────────┘ │
│         │ No (Linux)                    │
│         v                               │
│  ┌─────────────┐                        │
│  │ Show Help   │                        │
│  │ Message     │                        │
│  └─────────────┘                        │
└─────────────────────────────────────────┘
```

### 1.2 專案結構

```
DeckMinerLite/
├── DeckMiner.csproj                    # 主專案 (Multi-target)
│   ├── TargetFrameworks: net10.0-windows;net10.0
│   └── UseWPF: true (Windows only)
│
├── Program.cs                          # 程式入口 (條件編譯)
│   └── GuiRunner (內部類別)            # GUI 啟動器 (條件編譯)
│
├── GlobalUsingsWindows.cs              # Windows 版本額外的 global using
│
├── Gui/                                # WPF GUI 程式碼 (條件編譯)
│   ├── App.xaml                        # WPF Application ✅
│   ├── App.xaml.cs                     # Application entry point ✅
│   ├── MainWindow.xaml                 # 主視窗 (4 分頁) ✅
│   ├── MainWindow.xaml.cs              # 主視窗邏輯 (code-behind) ✅
│   ├── ViewModels/                     # MVVM ViewModels (待實作)
│   │   ├── MainViewModel.cs
│   │   ├── SongConfigViewModel.cs
│   │   └── CardPoolViewModel.cs
│   ├── Views/                          # 子視圖控制項 (待實作)
│   │   ├── SongConfigPanel.xaml
│   │   ├── CardPoolPanel.xaml
│   │   └── SimulationPanel.xaml
│   └── Services/                       # GUI 專用服務 (待實作)
│       ├── ConfigService.cs            # 配置讀寫
│       └── SimulationService.cs        # 模擬執行
│
├── Config/                             # 核心配置類別 (共用) ✅
├── Services/                           # 核心服務 (共用) ✅
├── Models/                             # 核心模型 (共用) ✅
└── Data/                               # 資料模型 (共用) ✅

✅ = 已實作  ⚪ = 待實作
```

### 1.3 條件編譯策略

**DeckMiner.csproj**:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <!-- Multi-target: Windows 專用版 + 跨平台版 -->
    <TargetFrameworks>net10.0-windows;net10.0</TargetFrameworks>
    <OutputType>Exe</OutputType>

    <!-- Windows 版本啟用 WPF -->
    <UseWPF Condition="'$(TargetFramework)' == 'net10.0-windows'">true</UseWPF>

    <!-- 定義條件編譯符號 -->
    <DefineConstants Condition="'$(TargetFramework)' == 'net10.0-windows'">WINDOWS</DefineConstants>
  </PropertyGroup>

  <!-- GUI 檔案只在 Windows 版本編譯 -->
  <ItemGroup Condition="'$(TargetFramework)' == 'net10.0-windows'">
    <Compile Include="Gui\**\*.cs" />
    <Page Include="Gui\**\*.xaml" />
  </ItemGroup>
</Project>
```

**Program.cs**:
```csharp
static void Main(string[] args)
{
    // 設定 Console 編碼
    SetupConsoleEncoding();

    // 有命令列參數 → CLI 模式 (所有平台)
    if (args.Length > 0)
    {
        RunCliMode(args);
        return;
    }

#if WINDOWS
    // Windows 且無參數 → 啟動 WPF GUI
    GuiBootstrapper.LaunchGui();
#else
    // Linux 且無參數 → 顯示提示訊息
    Console.WriteLine("SukuShow Deck Miner Lite - CLI 模式");
    Console.WriteLine();
    Console.WriteLine("請使用命令列參數執行模擬:");
    Console.WriteLine("  --config <file>       指定配置檔");
    Console.WriteLine("  --test-yaml           測試 YAML 配置");
    Console.WriteLine("  --help                顯示完整說明");
    Console.WriteLine();
    Console.WriteLine("或參考 README.md 取得更多資訊");
#endif
}
```

---

## 2. WPF 介面設計

### 2.1 主視窗布局 (MainWindow)

```
┌───────────────────────────────────────────────────────────┐
│  SukuShow Deck Miner - 配置編輯器              [_][□][X] │
├───────────────────────────────────────────────────────────┤
│  [檔案] [工具] [說明]                                     │
├───────────┬───────────────────────────────────────────────┤
│           │  ┌─ 基本設定 ────────────────────────────┐   │
│           │  │ 成員名稱: [stu92054          ▼]       │   │
│  側邊欄    │  │ LGP 模式: [✓] 開啟                    │   │
│           │  │ 賽季模式: [SukuShow ▼]                │   │
│  ○ 基本    │  └────────────────────────────────────────┘   │
│  ○ 卡池    │                                              │
│  ○ 歌曲    │  ┌─ 輸出設定 ────────────────────────────┐   │
│  ○ 粉絲    │  │ 輸出目錄: [output           ]  [瀏覽] │   │
│  ○ 練度    │  │ 目錄隔離: [✓] 啟用                    │   │
│  ○ 優化器  │  └────────────────────────────────────────┘   │
│  ○ 執行    │                                              │
│           │  ┌─ 進階設定 ────────────────────────────┐   │
│           │  │ Batch Size: [1000000     ]            │   │
│           │  │ CPU 核心數: [自動 ▼]                  │   │
│           │  └────────────────────────────────────────┘   │
│           │                                              │
├───────────┴───────────────────────────────────────────────┤
│  [開啟配置] [儲存配置] [另存新檔]        [開始模擬]      │
└───────────────────────────────────────────────────────────┘
```

### 2.2 卡池管理面板

```
┌─ 卡池管理 ────────────────────────────────────────┐
│  已選卡片 (20 張):                   [全選][清空] │
│  ┌──────────────────────────────────────────────┐ │
│  │ [✓] 1052901 [16th BD] 塞萊涅·柳田·瑟利琳菲爾特 │ │
│  │ [✓] 1022701 [16th BD] 綴理                   │ │
│  │ [✓] 1033901 [Cheer] 乃愛                     │ │
│  │ [✓] 1052506 [Story] 塞萊涅                   │ │
│  │ ...                                          │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  搜尋: [______________________]  [依角色▼] [依稀有度▼] │
│                                                    │
│  可選卡片 (123 張):                               │
│  ┌──────────────────────────────────────────────┐ │
│  │ [ ] 1011501 [Story] 沙知                     │ │
│  │ [ ] 1021523 [Cheer] 梢                       │ │
│  │ [ ] 1031533 [地平] 帆                        │ │
│  │ ...                                          │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  [匯入卡池 JSON] [匯出卡池]          [確定][取消] │
└────────────────────────────────────────────────────┘
```

### 2.3 歌曲配置面板

```
┌─ 歌曲配置 ────────────────────────────────────────┐
│  歌曲列表:                        [+新增] [-移除] │
│  ┌──────────────────────────────────────────────┐ │
│  │ 1. Very! Very! COCO夏っ (405126) - Hard     │ │
│  │ 2. 私の番♡私の番！(405128) - Hard             │ │
│  │ 3. キラめき☆Never Ending (405120) - Hard     │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  選中歌曲詳細:                                    │
│  ┌────────────────────────────────────────────┐   │
│  │ 歌曲 ID: [405126        ▼]                 │   │
│  │ 難度:    [○ Normal ○ Hard ● Expert ○ Master] │   │
│  │ 精熟等級: [50]                              │   │
│  │                                             │   │
│  │ 必帶卡片 (全部): [1052901, 1022701]        │   │
│  │ 必帶卡片 (任一): []                         │   │
│  │ 禁用卡片:        [1041513, 1021701]        │   │
│  │                                             │   │
│  │ C 位覆蓋: [自動 ▼]                          │   │
│  │ 屬性覆蓋: [自動 ▼]                          │   │
│  │ 隊長指定: [0 (預設) ▼]                      │   │
│  └────────────────────────────────────────────┘   │
│                                                    │
│  [套用] [重設]                                    │
└────────────────────────────────────────────────────┘
```

### 2.4 模擬執行面板

```
┌─ 模擬執行 ────────────────────────────────────────┐
│  配置檔案: D:\config\member-stu92054.yaml        │
│  輸出目錄: D:\log\stu92054\                       │
│                                                    │
│  ┌─ 執行狀態 ───────────────────────────────────┐ │
│  │ [████████████░░░░░░░░░░░] 60% (2/3 首歌)    │ │
│  │                                              │ │
│  │ 當前: 405128 (Hard) - 生成卡組中...         │ │
│  │ 已模擬: 1,234,567 / 2,000,000 卡組          │ │
│  │ 速度: 45,678 it/s                           │ │
│  │ 預估剩餘時間: 00:02:15                      │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  ┌─ 執行日誌 ───────────────────────────────────┐ │
│  │ [2025-12-26 14:23:10] 載入配置成功          │ │
│  │ [2025-12-26 14:23:11] 開始模擬: 405126      │ │
│  │ [2025-12-26 14:23:45] 完成: 405126 (1.2M組) │ │
│  │ [2025-12-26 14:23:46] 開始模擬: 405128      │ │
│  │ [2025-12-26 14:24:12] 模擬中...             │ │
│  └──────────────────────────────────────────────┘ │
│                                                    │
│  [暫停] [停止]              [開啟輸出目錄][查看結果] │
└────────────────────────────────────────────────────┘
```

---

## 3. 資料流與 API 設計

### 3.1 MVVM 架構

```
View (XAML)  ⟷  ViewModel  ⟷  Service  ⟷  Core Logic
    │               │              │            │
MainWindow    MainViewModel   ConfigService  YamlConfigManager
    │               │              │            │
Controls      Properties      LoadConfig   ParseYaml
    │               │         SaveConfig   GenerateYaml
Bindings      Commands       ValidateConfig
```

### 3.2 核心服務

#### ConfigService.cs
```csharp
public class ConfigService
{
    // 載入配置檔
    public MemberConfig LoadConfig(string filePath);

    // 儲存配置檔
    public void SaveConfig(MemberConfig config, string filePath);

    // 驗證配置
    public ValidationResult ValidateConfig(MemberConfig config);

    // 取得預設配置
    public MemberConfig GetDefaultConfig();
}
```

#### DataService.cs
```csharp
public class DataService
{
    // 取得卡片資料庫
    public Dictionary<int, CardData> GetCardDatabase();

    // 取得音樂資料庫
    public Dictionary<string, MusicData> GetMusicDatabase();

    // 搜尋卡片
    public List<CardData> SearchCards(string keyword, int? characterId, int? rarity);

    // 取得角色列表
    public List<Character> GetCharacters();
}
```

#### SimulationService.cs
```csharp
public class SimulationService
{
    // 執行模擬 (非同步)
    public Task<SimulationResult> RunSimulationAsync(
        MemberConfig config,
        IProgress<SimulationProgress> progress,
        CancellationToken cancellationToken
    );

    // 停止模擬
    public void StopSimulation();

    // 暫停/繼續模擬
    public void PauseSimulation();
    public void ResumeSimulation();
}
```

### 3.3 ViewModel 設計

#### MainViewModel.cs
```csharp
public class MainViewModel : INotifyPropertyChanged
{
    // 配置資料
    public MemberConfig CurrentConfig { get; set; }

    // 當前頁面
    public string CurrentPage { get; set; }

    // 命令
    public ICommand LoadConfigCommand { get; }
    public ICommand SaveConfigCommand { get; }
    public ICommand StartSimulationCommand { get; }

    // 狀態
    public bool IsSimulating { get; set; }
    public double SimulationProgress { get; set; }
    public string StatusMessage { get; set; }
}
```

#### CardPoolViewModel.cs
```csharp
public class CardPoolViewModel : INotifyPropertyChanged
{
    // 卡片列表
    public ObservableCollection<CardViewModel> AvailableCards { get; }
    public ObservableCollection<CardViewModel> SelectedCards { get; }

    // 搜尋與篩選
    public string SearchKeyword { get; set; }
    public int? FilterCharacter { get; set; }
    public int? FilterRarity { get; set; }

    // 命令
    public ICommand AddCardCommand { get; }
    public ICommand RemoveCardCommand { get; }
    public ICommand SelectAllCommand { get; }
    public ICommand ClearAllCommand { get; }
}
```

---

## 4. 專業審閱建議整合 (Code Review Integration)

### 4.1 架構解耦優化

**問題**: Program.cs 可能因混合 CLI 與 GUI 邏輯而變得臃腫

**解決方案**: 引入 Runner 模式

```csharp
// Program.cs (簡化版)
static void Main(string[] args)
{
    SetupConsoleEncoding();

    if (args.Length > 0 || !ShouldLaunchGui())
    {
        CliRunner.Run(args);
    }
    else
    {
#if WINDOWS
        GuiRunner.Run();
#else
        CliRunner.ShowUsage();
#endif
    }
}

static bool ShouldLaunchGui()
{
#if WINDOWS
    return true;  // Windows 預設啟動 GUI
#else
    return false; // Linux 不支援 GUI
#endif
}
```

**CliRunner.cs** (所有平台):
```csharp
public static class CliRunner
{
    public static void Run(string[] args)
    {
        // 現有的 Main 邏輯搬移至此
        // 處理 --config, --test-yaml, --help 等參數
    }

    public static void ShowUsage()
    {
        Console.WriteLine("SukuShow Deck Miner Lite - CLI 模式");
        Console.WriteLine("請使用 --help 查看命令列選項");
    }
}
```

**GuiRunner** (僅 Windows, 作為 Program 內部類別):
```csharp
// Program.cs
class Program
{
#if WINDOWS
    /// <summary>
    /// GUI mode runner - launches WPF application (Windows only)
    /// </summary>
    static class GuiRunner
    {
        public static int Run()
        {
            Console.WriteLine("[INFO] Launching GUI mode...");

            try
            {
                var app = new DeckMiner.Gui.App();
                app.InitializeComponent();
                app.Run();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FAIL] GUI startup failed: {ex.Message}");
                Console.WriteLine($"[HINT] Stack trace: {ex.StackTrace}");
                Console.WriteLine("\nPress Enter to exit...");
                Console.ReadLine();
                return 1;
            }
        }
    }
#endif

    static void Main(string[] args)
    {
        // ... (見下方完整範例)
    }
}
```

**注**: GuiRunner 實作為 Program 內部類別而非獨立檔案,以避免條件編譯可見性問題。

### 4.2 ConfigService 與 YamlConfigManager 整合

**設計原則**: ConfigService 應封裝而非取代 YamlConfigManager

```csharp
public class ConfigService
{
    private readonly YamlConfigManager _yamlManager;

    public ConfigService()
    {
        _yamlManager = new YamlConfigManager(configFile: null);
    }

    // 載入配置 - 直接使用 YamlConfigManager
    public MemberConfig LoadConfig(string filePath)
    {
        var manager = new YamlConfigManager(filePath);
        return manager.GetMemberConfig();
    }

    // 儲存配置 - 重用現有邏輯
    public void SaveConfig(MemberConfig config, string filePath)
    {
        var yaml = SerializeToYaml(config);
        File.WriteAllText(filePath, yaml, Encoding.UTF8);
    }

    // 驗證配置 - 檢查必要欄位
    public ValidationResult ValidateConfig(MemberConfig config)
    {
        var errors = new List<string>();

        if (config.Songs == null || config.Songs.Count == 0)
            errors.Add("至少需要配置一首歌曲");

        if (config.CardIds == null || config.CardIds.Count < 6)
            errors.Add("卡池至少需要 6 張卡片");

        return new ValidationResult
        {
            IsValid = errors.Count == 0,
            Errors = errors
        };
    }
}
```

### 4.3 效能優化：UI 虛擬化與非同步處理

#### 4.3.1 卡片列表虛擬化

**問題**: 數百張卡片的 CheckBox 會造成介面卡頓

**解決方案**: 使用 VirtualizingStackPanel

```xaml
<!-- CardPoolPanel.xaml -->
<ListBox ItemsSource="{Binding AvailableCards}"
         VirtualizingPanel.IsVirtualizing="True"
         VirtualizingPanel.VirtualizationMode="Recycling"
         VirtualizingPanel.CacheLength="20,20"
         VirtualizingPanel.CacheLengthUnit="Item">
    <ListBox.ItemsPanel>
        <ItemsPanelTemplate>
            <VirtualizingStackPanel />
        </ItemsPanelTemplate>
    </ListBox.ItemsPanel>
</ListBox>
```

#### 4.3.2 非同步模擬執行

**問題**: DeckGenerator.ComputeTotalCount() 極耗 CPU

**解決方案**: Task.Run + IProgress + CancellationToken

```csharp
public class SimulationService
{
    private CancellationTokenSource _cts;

    public async Task<SimulationResult> RunSimulationAsync(
        MemberConfig config,
        IProgress<SimulationProgress> progress,
        CancellationToken cancellationToken)
    {
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        return await Task.Run(() =>
        {
            // 將現有的 CLI 模擬邏輯搬移至此
            // 使用 progress.Report() 回報進度
            // 定期檢查 _cts.Token.IsCancellationRequested

            foreach (var song in config.Songs)
            {
                if (_cts.Token.IsCancellationRequested)
                    break;

                progress.Report(new SimulationProgress
                {
                    CurrentSong = song.MusicId,
                    Percentage = ...,
                    Speed = ...,
                    Message = $"模擬中: {song.MusicId}"
                });

                // 執行模擬...
            }

        }, _cts.Token);
    }

    public void StopSimulation()
    {
        _cts?.Cancel();
    }
}
```

#### 4.3.3 日誌緩衝與限制

**問題**: DebugMode 下大量日誌會導致記憶體溢出

**解決方案**: 限制日誌行數 + 循環緩衝

```csharp
public class LogService
{
    private readonly int _maxLogLines = 1000;
    private readonly Queue<string> _logBuffer = new();

    public ObservableCollection<string> LogLines { get; } = new();

    public void AppendLog(string message)
    {
        _logBuffer.Enqueue($"[{DateTime.Now:HH:mm:ss}] {message}");

        // 超過限制時移除最舊的
        while (_logBuffer.Count > _maxLogLines)
            _logBuffer.Dequeue();

        // 同步到 UI (須在 UI 執行緒)
        App.Current.Dispatcher.Invoke(() =>
        {
            LogLines.Clear();
            foreach (var line in _logBuffer)
                LogLines.Add(line);
        });
    }
}
```

### 4.4 錯誤處理增強

**問題**: fatalError 會直接終止程式

**解決方案**: GUI 捕獲 Exception 並顯示友好對話框

```csharp
// MainViewModel.cs
private async void OnStartSimulation()
{
    try
    {
        IsSimulating = true;
        var result = await _simulationService.RunSimulationAsync(
            CurrentConfig,
            new Progress<SimulationProgress>(OnProgressChanged),
            CancellationToken.None
        );

        MessageBox.Show("模擬完成！", "成功", MessageBoxButton.OK, MessageBoxImage.Information);
    }
    catch (FileNotFoundException ex)
    {
        MessageBox.Show($"找不到檔案: {ex.FileName}\n請檢查配置路徑。",
            "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
    }
    catch (InvalidOperationException ex)
    {
        MessageBox.Show($"配置錯誤: {ex.Message}\n請檢查 YAML 格式。",
            "錯誤", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
    catch (Exception ex)
    {
        MessageBox.Show($"模擬過程發生錯誤:\n{ex.Message}\n\n詳細資訊已記錄到日誌。",
            "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
        LogService.AppendLog($"ERROR: {ex}");
    }
    finally
    {
        IsSimulating = false;
    }
}
```

### 4.5 實作優先級調整

根據審閱建議，調整開發優先級：

**第一優先**: 基礎架構 + 唯讀配置顯示 ✅ **已完成**
- ✅ 解耦 CliRunner / GuiRunner (GuiRunner 作為 Program 內部類別)
- ✅ 條件編譯測試 (Windows GUI + Linux CLI)
- ✅ YAML 載入與顯示 (Configuration Tab 唯讀顯示)
- ✅ 驗證與現有邏輯一致性 (重用 YamlConfigManager)
- ✅ 4 分頁 UI 框架建立
- ✅ 檔案選擇對話框整合

**第二優先**: 非同步模擬控制 ⏳ **待實作**
- ⚪ Task.Run + IProgress
- ⚪ CancellationToken 支援
- ⚪ 停止/暫停功能
- ⚪ 即時進度更新
- ⚪ 日誌緩衝與限制

**第三優先**: 配置編輯功能 ⏳ **待實作**
- ⚪ 卡池管理 (含虛擬化)
- ⚪ 歌曲配置編輯
- ⚪ YAML 儲存
- ⚪ Fan Level / Card Level 編輯器

**第四優先**: UI 體驗優化 ⏳ **未來版本**
- ⚪ 搜尋與過濾
- ⚪ 主題切換
- ⚪ 快捷鍵

**實際進度**: Phase 1-2 完成，已達成第一優先級全部目標

---

## 5. 實作時程規劃

### Phase 1: 基礎架構 (2-3 小時)

**目標**: 建立專案架構與條件編譯

- [x] 修改 DeckMiner.csproj 支援 multi-target
- [x] 建立 Gui/ 目錄結構
- [x] 實作 GuiBootstrapper.cs
- [x] 修改 Program.cs 支援條件編譯
- [x] 建立基本的 App.xaml 和 MainWindow.xaml
- [x] 測試條件編譯 (Windows vs Linux)

**驗收標準**:
- ✅ Windows 版雙擊啟動空白 WPF 視窗
- ✅ Linux 版執行顯示 CLI 提示訊息
- ✅ 兩版本都支援 --config 參數

---

### Phase 2: GUI 基礎實作 (已完成 ✅)

**目標**: 實作基本 WPF GUI 框架與配置顯示

- [x] 建立 WPF 基本框架 (App.xaml, MainWindow.xaml)
- [x] 實作 4 分頁布局 (Configuration, Simulation, Results, About)
- [x] 實作 YAML 配置載入功能
- [x] 實作配置資訊顯示 (唯讀)
- [x] 實作歌曲列表顯示
- [x] 實作模擬控制面板 (佔位實作)
- [x] 實作日誌輸出區域
- [x] 整合條件編譯與 GuiRunner

**實際實作內容**:
- ✅ MainWindow.xaml: 700x1000 主視窗，含 4 個 TabControl
- ✅ Configuration Tab: 載入 YAML、顯示基本設定、顯示卡池與歌曲
- ✅ Simulation Tab: 控制按鈕、進度條、即時日誌
- ✅ Results Tab: 預留區域
- ✅ About Tab: 版本資訊與功能說明
- ✅ 檔案選擇對話框整合
- ✅ 輸出資料夾快速開啟

**驗收標準**:
- ✅ Windows 版雙擊啟動 GUI
- ✅ 可載入現有 YAML 配置並顯示
- ✅ 配置資訊正確顯示 (member name, lgp_mode, season_mode, card pool size, songs)
- ✅ GUI 與 CLI 雙模式自動切換
- ✅ 編譯無錯誤，Windows/Linux 分別正常運作

**完成日期**: 2025-12-26

---

### Phase 3: 歌曲與進階配置 (2-3 小時)

**目標**: 實作歌曲配置與進階功能

- [x] 實作卡池管理 (新增/移除卡片)
- [x] 實作 Card Level 編輯器 (整合於卡池管理)
- [x] 實作 YAML 儲存功能
- [ ] 實作歌曲列表管理 (新增/編輯/刪除)
- [ ] 實作歌曲詳細配置 (難度、精熟、約束條件)
- [ ] 實作 Fan Level 編輯器
- [ ] 實作優化器配置面板

**驗收標準**:
- ✅ 可完整編輯所有歌曲配置
- ✅ 可設定 mustcards / banned_cards
- ✅ 可編輯 fan_levels 和 card_levels

---

### Phase 4: 模擬執行整合 (2-3 小時)

**目標**: 整合模擬執行功能

- [ ] 實作 SimulationService
- [ ] 實作進度條與狀態顯示
- [ ] 實作即時日誌輸出
- [ ] 實作暫停/停止功能
- [ ] 實作結果查看 (開啟輸出目錄)

**驗收標準**:
- ✅ 可啟動模擬並顯示即時進度
- ✅ 可暫停/停止模擬
- ✅ 模擬完成後可查看結果
- ✅ 錯誤處理完善

---

### Phase 5: 測試與優化 (1-2 小時)

**目標**: 完整測試與使用者體驗優化

- [ ] 測試所有功能組合
- [ ] 測試邊界情況 (空配置、大量卡片等)
- [ ] 優化 UI 響應速度
- [ ] 完善錯誤提示訊息
- [ ] 撰寫使用者文檔

**驗收標準**:
- ✅ 所有功能正常運作
- ✅ 無崩潰或記憶體洩漏
- ✅ 使用者體驗流暢

---

## 5. 技術規格

### 5.1 開發環境

- **.NET**: 10.0
- **IDE**: Visual Studio 2022 / Rider
- **UI 框架**: WPF (Windows Presentation Foundation)
- **架構模式**: MVVM (Model-View-ViewModel)
- **YAML 解析**: YamlDotNet

### 5.2 第三方套件

```xml
<PackageReference Include="YamlDotNet" Version="16.2.0" />
<PackageReference Include="CommunityToolkit.Mvvm" Version="8.3.2" />
```

**CommunityToolkit.Mvvm** 提供:
- `ObservableObject`: INotifyPropertyChanged 基底類別
- `RelayCommand`: ICommand 實作
- `ObservableProperty`: 自動產生屬性變更通知

### 5.3 效能目標

- **啟動時間**: < 2 秒 (Windows)
- **配置載入**: < 500 ms
- **UI 響應**: < 100 ms (所有操作)
- **記憶體佔用**: < 200 MB (GUI 模式)

---

## 6. 風險評估

### 6.1 技術風險

| 風險 | 影響 | 機率 | 緩解措施 |
|------|------|------|----------|
| WPF 學習曲線 | 高 | 中 | 使用 MVVM 工具包簡化開發 |
| 條件編譯複雜度 | 中 | 低 | 充分測試兩個 target |
| YAML 格式不相容 | 高 | 低 | 重用現有 YamlConfigManager |
| 跨執行緒問題 | 中 | 中 | 使用 Dispatcher 處理 UI 更新 |

### 6.2 使用者體驗風險

| 風險 | 影響 | 機率 | 緩解措施 |
|------|------|------|----------|
| UI 不直觀 | 高 | 中 | 參考 Python web 版設計 |
| 配置錯誤難以發現 | 中 | 中 | 即時驗證與錯誤提示 |
| 模擬執行中斷 | 中 | 低 | 實作暫停/恢復功能 |

---

## 7. 未來擴充

### 7.1 短期擴充 (v1.1)

- [ ] 配置檔範本庫 (快速套用常用配置)
- [ ] 卡片圖片顯示 (若有資源)
- [ ] 快捷鍵支援
- [ ] 深色主題

### 7.2 中期擴充 (v1.2)

- [ ] 多配置檔管理 (分頁切換)
- [ ] 歷史模擬記錄
- [ ] 結果對比工具
- [ ] 批次模擬 (多個配置檔)

### 7.3 長期擴充 (v2.0)

- [ ] Avalonia 移植 (支援 Linux GUI)
- [ ] 即時卡組評分預覽
- [ ] 整合 multi_optimizer_2.py (多曲優化)

---

## 8. 發布策略

### 8.1 版本管理

- **v1.0-gui-alpha**: Phase 1-2 完成，基本功能可用
- **v1.0-gui-beta**: Phase 3 完成，功能完整
- **v1.0-gui**: Phase 4-5 完成，正式發布

### 8.2 Publish 配置

**publish.bat** 修改:
```batch
REM Windows 版本 (含 GUI)
dotnet publish -c Release -r win-x64 --self-contained -f net10.0-windows

REM Linux 版本 (純 CLI)
dotnet publish -c Release -r linux-x64 --self-contained -f net10.0
```

### 8.3 文件更新

- [ ] README_zh-tw.md: 新增 GUI 使用說明
- [ ] CHANGES_SUMMARY.md: 記錄 GUI 功能
- [ ] PROGRESS.md: 標記 Phase 7 GUI 完成

---

---

## 9. 實作進度總結

### 已完成階段

#### ✅ Phase 1: 基礎架構 (完成)
- Multi-target 專案配置
- 條件編譯與 GuiRunner 整合
- Windows/Linux 分離編譯測試通過

#### ✅ Phase 2: GUI 基礎實作 (完成)
- WPF 主視窗與 4 分頁布局
- YAML 配置載入與顯示
- 模擬控制面板框架
- GUI/CLI 雙模式自動切換

### 待實作階段

#### ✅ Phase 3: 配置編輯功能 (已完成)
- ✅ 新建配置檔功能 (NewConfigDialog)
- ✅ Basic Settings 可編輯 (LGP Mode)
- ✅ 卡池編輯器 (新增/移除卡片)
- ✅ Card Level 編輯器 (整合於卡池管理)
- ✅ YAML 儲存功能
- ✅ 歌曲配置編輯 (SongConfigWindow)
- ✅ Fan Level 編輯器 (FanLevelsWindow)
- ✅ Friend Card 選擇器 (FriendCardSelectorWindow)

#### 🚧 Phase 4: 模擬執行整合 (進行中)
- ✅ SimulationService 非同步執行框架
- ✅ 執行模式選擇 UI (完整優化 vs 僅模擬)
- ✅ 即時進度更新與日誌
- ✅ 停止功能
- ✅ multi_optimizer_2.py 整合
- ⏳ C# 模擬邏輯整合 (待完成)
- ⏳ 結果查看與分析 (待完成)

#### ⏳ Phase 5: 測試與優化
- 完整功能測試
- 效能優化 (虛擬化、非同步)
- 錯誤處理增強
- 使用者文檔

### 當前狀態

**狀態**: 🚧 Phase 4 進行中 (Phase 4.1 框架完成)
**完成度**: 70% (3.5/5 階段)
**下一步**: Phase 4.2 - C# 模擬邏輯整合

---

## 10. Phase 3 完成內容詳細說明

### 10.1 新建配置檔功能

**檔案**: `NewConfigDialog.xaml`, `NewConfigDialog.xaml.cs`

**功能**:
- 輸入成員名稱，自動生成 `member-<name>.yaml` 格式的檔案名
- 即時更新儲存路徑顯示
- 預設配置包含所有必要欄位（fan_levels 預設為 0）
- 自動過濾不合法的檔案名稱字元

**使用方式**:
1. 點擊主視窗 "New" 按鈕
2. 輸入成員名稱（例如：Alice）
3. 路徑自動更新為 `config/member-Alice.yaml`
4. 點擊"建立"完成

### 10.2 Basic Settings 編輯

**可編輯項目**:
- **LGP Mode**: 下拉選單，可選擇 True (允許雙卡) 或 False (單卡模式)

**唯讀項目**:
- **Member Name**: 從配置檔案名稱自動提取
- **Season Mode**: 固定為 sukushow

**修改立即生效**: 變更後自動更新配置物件，按 Save 儲存

### 10.3 Fan Levels 編輯器

**檔案**: `FanLevelsWindow.xaml`, `FanLevelsWindow.xaml.cs`

**功能**:
- 顯示所有 12 個角色的粉絲等級輸入框
- 使用 GameConstants 中的角色全名顯示
- 預設值為 10（滿等）
- 驗證範圍：0-10
- 快速操作："全部設為 10" 按鈕

**格式**:
```
1011  大賀美 沙知      [10]
1021  乙宗 梢          [10]
1022  夕霧 綴理        [10]
...
```

### 10.4 歌曲配置編輯器改進

**檔案**: `SongConfigWindow.xaml`, `SongConfigWindow.xaml.cs`

**完成的改進**:
- ✅ 修正 YAML 格式問題（card_ids 縮排從 2 空格改為 1 空格）
- ✅ 修正 ComboBox 共享實例問題（每個 ComboBox 獨立 UI 元素）
- ✅ 修正 CheckBox 對齊問題（VerticalAlignment="Center"）
- ✅ 卡片顯示格式統一為 `{cardId} [{rarityName}] {charName} {cardName}`
- ✅ 必帶卡片與禁用卡片支援雙擊移除

### 10.5 Friend Card 選擇器

**檔案**: `FriendCardSelectorWindow.xaml`, `FriendCardSelectorWindow.xaml.cs`

**功能**:
- 完整的卡片資料庫顯示（ID、稀有度、角色、卡片名稱）
- 搜尋功能（支援 ID、角色名、卡片名、稀有度）
- 雙擊上方列表新增/移除卡片
- 雙擊下方已選列表移除卡片
- "全部清空"按鈕（含確認對話框）
- 卡片數量即時顯示

**用途**:
- 全局朋友卡池 (friend_card_ids): 所有歌曲的預設朋友卡
- 歌曲層級朋友卡池 (friend_card_pool): 覆蓋全局配置，只對該首歌生效

### 10.6 全局朋友卡池

**整合位置**: MainWindow - Configuration 分頁

**功能**:
- 在 Fan Levels 和 Songs 之間新增「Friend Cards (全局朋友卡池)」區塊
- 顯示摘要：「已選擇 X 張朋友卡」或「未選擇朋友卡」
- 重用 FriendCardSelectorWindow 進行編輯
- 配置載入時自動更新摘要

**配置層級**:
- **friend_card_ids** (全局): 所有歌曲的預設朋友卡池
- **songs[].friend_card_pool** (歌曲層級): 覆蓋全局配置

### 10.7 優化器配置

**檔案**: `OptimizerConfigWindow.xaml`, `OptimizerConfigWindow.xaml.cs`

**功能**:
- **Top N**: 設定每首歌保留前 N 名卡組（預設 50000）
- **Show Card Names**: 控制輸出中是否顯示卡牌名稱
- **Forbidden Cards**: 全局禁卡列表（三面均生效）
  - 從卡池中選擇
  - 雙擊移除
  - 一鍵清空

**用途**:
- 專用於 `multi_optimizer_2.py` 多曲優化器
- 尋找三首歌曲的最佳卡組組合

**與歌曲禁卡的區別**:
- 歌曲層級 banned_cards: 只對該首歌生效
- 優化器 forbidden_cards: 對所有三首歌都生效

---

## 11. Phase 4.1 完成內容詳細說明

### 11.1 執行模式選擇 UI

**更新檔案**: `MainWindow.xaml` (Simulation 分頁)

**功能**:
- **完整優化流程（推薦）**
  - 階段 1：模擬歌曲（1-3 首）
  - 階段 2：多曲優化（multi_optimizer_2.py，僅 3 首時執行）
- **僅執行模擬（進階）**
  - 只模擬歌曲，不進行多曲優化

**UI 元件**:
- RadioButton 選擇執行模式
- 清楚說明每種模式的執行流程
- 模式切換時在日誌中記錄

### 11.2 SimulationService 類別

**檔案**: `Services/SimulationService.cs`

**架構設計**:
- **事件驅動架構**
  - `ProgressChanged`: 進度更新事件 (0-100%, 狀態訊息)
  - `LogOutput`: 日誌輸出事件
  - `ExecutionCompleted`: 執行完成事件 (成功/失敗)

**核心方法**:
1. `ExecuteFullOptimizationAsync`: 完整優化流程
2. `ExecuteSimulationOnlyAsync`: 僅執行模擬
3. `Stop`: 停止執行
4. `ValidateConfiguration`: 配置驗證

**執行流程**:
```
完整優化流程:
  0-5%   : 配置驗證
  5-70%  : 模擬歌曲 (C# 實作，待整合)
  70-75% : 準備優化
  75-100%: 多曲優化 (multi_optimizer_2.py)

僅模擬:
  0-5%   : 配置驗證
  5-100% : 模擬歌曲
```

**取消機制**:
- 使用 `CancellationTokenSource` 實作
- 支援優雅取消（清理資源）
- Python 進程可被中止

### 11.3 MainWindow 整合

**更新檔案**: `MainWindow.xaml.cs`

**新增功能**:
1. **SimulationService 初始化與事件訂閱**
   - 建構函式中初始化服務
   - 訂閱三個事件：進度、日誌、完成

2. **事件處理器**
   - `OnSimulationProgressChanged`: 更新進度條與狀態文字
   - `OnSimulationLogOutput`: 將日誌輸出到 GUI
   - `OnSimulationCompleted`: 顯示完成對話框，重置按鈕狀態

3. **按鈕邏輯**
   - `StartSimulationButton_Click`: 根據選擇的模式執行
   - `StopSimulationButton_Click`: 確認對話框後停止執行
   - `ExecutionModeChanged`: 記錄模式切換

**UI 更新機制**:
- 使用 `Dispatcher.Invoke` 確保 UI 執行緒安全
- 按鈕狀態自動切換（執行中禁用開始、啟用停止）

### 11.4 multi_optimizer_2.py 整合

**實作位置**: `SimulationService.ExecuteOptimizerAsync`

**執行機制**:
- 使用 `Process` 類別執行 Python 腳本
- 工作目錄：專案根目錄
- 環境變數：`CONFIG_FILE` 設定為配置檔路徑

**輸出處理**:
- `StandardOutput`: 重導向到日誌（前綴 `[OPTIMIZER]`）
- `StandardError`: 重導向到日誌（前綴 `[OPTIMIZER ERROR]`）
- 監控 `ExitCode` 判斷執行成功

**取消支援**:
- 監控 `CancellationToken`
- 取消時呼叫 `Process.Kill()`

### 11.5 進度追蹤與日誌系統

**進度條**:
- 0-100% 範圍
- 分階段更新（模擬 5-70%，優化 70-100%）
- 即時狀態文字顯示

**日誌系統**:
- 時間戳記格式：`[HH:mm:ss]`
- 日誌等級：`[INFO]`, `[PASS]`, `[FAIL]`, `[WARN]`, `[DEBUG]`
- 自動捲動到最新訊息
- 清空日誌按鈕

### 11.6 配置驗證

**驗證項目**:
- ✅ 歌曲數量：1-3 首
- ✅ 卡池非空
- ✅ 配置物件完整性

**錯誤處理**:
- 驗證失敗時顯示錯誤訊息
- 記錄到日誌
- 阻止執行

### 11.7 待整合項目

**C# 模擬邏輯**（Phase 4.2）:
- 目前使用 placeholder 實作（模擬進度更新）
- 需要整合：
  - `DeckGenerator`: 卡組生成
  - `Simulator`: 模擬執行
  - 輸出結果到檔案

**結果查看**（Phase 4.3）:
- Results 分頁顯示模擬結果
- 解析輸出檔案
- 卡組排名展示

---

**最後更新**: 2026-01-06
**Phase 2 完成日期**: 2025-12-26
**Phase 3 完成日期**: 2026-01-05
**Phase 4.1 完成日期**: 2026-01-06
**累計開發時間**: ~15 小時

### 已實作的視窗列表

1. ✅ **MainWindow** - 主視窗 (4 分頁)
2. ✅ **NewConfigDialog** - 新建配置檔對話框
3. ✅ **CardPoolWindow** - 卡池管理視窗
4. ✅ **SongConfigWindow** - 歌曲配置編輯視窗
5. ✅ **FanLevelsWindow** - 粉絲等級編輯視窗
6. ✅ **FriendCardSelectorWindow** - 朋友卡選擇器
7. ✅ **OptimizerConfigWindow** - 優化器配置視窗

### 已實作的核心功能

**配置管理**:
- ✅ YAML 配置載入與儲存
- ✅ 新建配置檔（含預設值）
- ✅ 基本設定編輯（LGP Mode）
- ✅ 完整的卡池管理（新增、移除、練度設定）
- ✅ 完整的歌曲配置（3 首上限、各種約束條件、進階設定）
- ✅ 粉絲等級編輯（12 個角色）
- ✅ 全局朋友卡池管理（friend_card_ids）
- ✅ 歌曲層級朋友卡池管理（friend_card_pool）
- ✅ 優化器配置（multi_optimizer_2.py 專用）
- ✅ 配置驗證與錯誤處理

**模擬執行** (Phase 4.1):
- ✅ SimulationService 非同步執行框架
- ✅ 執行模式選擇（完整優化 vs 僅模擬）
- ✅ 事件驅動架構（進度、日誌、完成）
- ✅ 即時進度更新與狀態顯示
- ✅ 日誌系統（時間戳記、等級標記）
- ✅ 停止功能（優雅取消）
- ✅ multi_optimizer_2.py 整合
- ⏳ C# 模擬邏輯整合（待完成）
- ⏳ 結果查看與分析（待完成）
