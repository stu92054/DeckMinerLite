# Phase 2: GUI 基礎實作完成報告

**完成日期**: 2025-12-26
**更新日期**: 2026-01-05 (文件更新)

---

## 📋 完成摘要

Phase 2 成功實作了 DeckMinerLite 的 WPF 圖形化介面基礎框架,實現 Windows 版本的 GUI/CLI 雙模式自動切換。

### 核心成就

✅ **多目標架構**: `net10.0-windows` (WPF) + `net10.0` (CLI)
✅ **自動模式切換**: 無參數啟動 GUI,有參數執行 CLI
✅ **配置載入顯示**: 完整顯示 YAML 配置資訊
✅ **4 分頁布局**: Configuration, Simulation, Results, About
✅ **編譯成功**: Windows/Linux 分別建置無錯誤

---

## 🎯 實作內容

### 1. 專案配置更新

#### DeckMiner.csproj
```xml
<!-- Multi-target: Windows 專用版 + 跨平台版 -->
<TargetFrameworks>net10.0-windows;net10.0</TargetFrameworks>

<!-- Windows 版本啟用 WPF -->
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0-windows'">
  <UseWPF>true</UseWPF>
  <PublishAot>false</PublishAot>
</PropertyGroup>

<!-- 跨平台版本保持 AOT 啟用 -->
<PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
  <PublishAot>true</PublishAot>
  <InvariantGlobalization>true</InvariantGlobalization>
</PropertyGroup>
```

#### GlobalUsingsWindows.cs (新增)
修正 WPF 隱式 using 不包含 System.IO 的問題:
```csharp
#if WINDOWS
global using System.IO;
#endif
```

### 2. GUI 程式碼實作

#### Gui/App.xaml + App.xaml.cs
- WPF Application 入口點
- UTF-8 Console 編碼設定
- 全域樣式定義 (Button, TextBox, ComboBox, CheckBox)

#### Gui/MainWindow.xaml (700x1000)
**4 個主要分頁**:

1. **Configuration Tab**
   - 載入/重新載入 YAML 配置
   - 顯示基本設定 (成員名稱、賽季模式、LGP 模式)
   - 顯示卡池大小
   - 歌曲列表 (MusicId, Difficulty, Mastery)

2. **Simulation Tab**
   - 開始/停止按鈕 (佔位實作)
   - 進度條
   - 即時日誌輸出區域
   - 清除日誌按鈕

3. **Results Tab**
   - 預留區域 (未來顯示模擬結果統計)

4. **About Tab**
   - 版本資訊 (v1.3 GUI Edition)
   - 功能列表
   - CLI 模式說明

**底部狀態列**:
- 狀態訊息顯示
- 開啟輸出資料夾按鈕
- 結束按鈕

#### Gui/MainWindow.xaml.cs
**核心功能**:
- `LoadConfiguration()`: 載入 YAML 並更新 UI
- `AppendLog()`: 新增日誌訊息 (含時間戳)
- `LoadConfigButton_Click()`: 檔案選擇對話框
- `ReloadConfigButton_Click()`: 重新載入配置
- `StartSimulationButton_Click()`: 模擬啟動 (佔位)
- `OpenOutputFolderButton_Click()`: 開啟輸出目錄

### 3. 程式入口整合

#### Program.cs
```csharp
static void Main(string[] args)
{
    Console.WriteLine("--- SukuShow Deck Miner Lite ---");

#if WINDOWS
    // Windows 版: 無參數 → GUI, 有參數 → CLI
    if (args.Length == 0)
    {
        Environment.Exit(GuiRunner.Run());
    }
#else
    // Linux 版: 無參數 → 顯示提示
    if (args.Length == 0)
    {
        Console.WriteLine("[INFO] No arguments provided");
        Console.WriteLine("[HINT] Usage: ./DeckMinerLite --config <file>");
        return;
    }
#endif

    // === CLI Mode Entry Point ===
    // (現有的 CLI 邏輯...)
}
```

#### GuiRunner (內部類別)
```csharp
#if WINDOWS
static class GuiRunner
{
    public static int Run()
    {
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
            return 1;
        }
    }
}
#endif
```

---

## 🐛 解決的技術問題

### 問題 1: WPF 隱式 using 缺少 System.IO
**現象**: 29 個編譯錯誤 (File, Directory, Path, IOException 等)
**原因**: WPF SDK 改變預設 implicit usings
**解決**: 建立 `GlobalUsingsWindows.cs` 加入 `global using System.IO;`

### 問題 2: GuiRunner 可見性問題
**現象**: 條件編譯導致 GuiRunner.cs 分離檔案無法被 Program.cs 引用
**原因**: 條件編譯順序問題
**解決**: GuiRunner 改為 Program 內部類別

### 問題 3: Gui 命名空間找不到
**現象**: `new Gui.App()` 報錯
**原因**: 條件編譯下需要完整命名空間
**解決**: 使用 `new DeckMiner.Gui.App()`

### 問題 4: WPF 與 AOT 不相容
**現象**: NETSDK1168 錯誤
**原因**: WPF 框架不支援 PublishAot=true
**解決**: Windows 版本設定 `<PublishAot>false</PublishAot>`

### 問題 5: 跨平台 NativeAOT 編譯限制
**現象**: "Cross-OS native compilation is not supported"
**原因**: 無法在 Windows 上編譯 Linux 的 NativeAOT 版本
**解決**:
- publish.bat 暫時移除 Linux 版本的 `-p:PublishAot=true`
- Linux AOT 版本需在 Linux 環境下建置
- Windows 發布腳本產生的 Linux 版本為 self-contained (包含 runtime) 但非 AOT

---

## 📦 建置與發布更新

### publish.bat 更新
```batch
# Windows 版本 (含 GUI)
dotnet publish -c Release --framework net10.0-windows -r win-x64 --self-contained

# Linux 版本 (CLI with AOT)
dotnet publish -c Release --framework net10.0 -r linux-x64 --self-contained -p:PublishAot=true
```

### README.txt 更新
```
Quick Start
-----------

1. Double-click DeckMinerLite.exe to launch GUI mode (Windows only)

2. Or use command line for automation with custom config:
   DeckMinerLite.exe --config config/member-example.yaml
```

---

## 📚 文件更新

### README_zh-tw.md 新增內容
- ✅ GUI 功能說明章節
- ✅ Windows 版本 (GUI) vs Linux 版本 (CLI) 使用說明
- ✅ 4 分頁功能介紹
- ✅ 快速操作指南
- ✅ 效能比較表新增「圖形化介面」項目
- ✅ 開發資訊更新 (multi-target 架構)

### README_zh-cn.md 完全重寫
- ✅ 同步繁體版所有內容
- ✅ 修正舊版文件過於簡略的問題

### wpf_gui_design_spec.md 更新
- ✅ Phase 2 標記為完成
- ✅ 實作優先級更新
- ✅ 專案結構反映實際狀態
- ✅ GuiRunner 實作方式文件化
- ✅ 新增實作進度總結章節

---

## 🎯 驗收標準達成

### 功能驗收
- ✅ Windows 版雙擊啟動 GUI 視窗
- ✅ 可載入 YAML 配置並正確顯示
- ✅ 配置資訊完整顯示 (member name, lgp_mode, season_mode, card pool size, songs)
- ✅ GUI 與 CLI 雙模式自動切換
- ✅ Linux 版本無參數顯示提示訊息

### 編譯驗收
- ✅ Windows 版本 (net10.0-windows) 編譯成功
- ✅ Linux 版本 (net10.0) 編譯成功
- ✅ 無編譯錯誤
- ✅ 無編譯警告

### 文件驗收
- ✅ README 檔案更新完整
- ✅ publish.bat 腳本更新
- ✅ GUI 設計文件更新

---

## 📊 完成度評估

| 類別 | 完成度 |
|------|--------|
| **架構設計** | 100% ✅ |
| **條件編譯** | 100% ✅ |
| **配置載入** | 100% ✅ |
| **配置顯示** | 100% ✅ |
| **UI 框架** | 100% ✅ |
| **配置編輯** | 0% ⚪ |
| **模擬執行** | 10% ⚪ (佔位實作) |
| **結果顯示** | 0% ⚪ |

**整體進度**: 40% (2/5 Phase 完成)

---

## 🚀 下一步規劃

### Phase 3: 配置編輯功能
- 卡池編輯器 (新增/移除卡片,虛擬化列表)
- 歌曲配置編輯 (新增/編輯/刪除歌曲)
- Fan Level / Card Level 編輯器
- YAML 儲存功能

### Phase 4: 模擬執行整合
- SimulationService 非同步執行
- IProgress + CancellationToken
- 即時進度更新與日誌
- 暫停/停止/繼續功能

---

## 💡 技術亮點

1. **優雅的雙模式設計**: 同一執行檔根據參數自動切換 GUI/CLI
2. **最小侵入性**: 核心邏輯完全重用,無需修改現有 Services
3. **條件編譯隔離**: Windows GUI 與 Linux CLI 完全分離,互不影響
4. **問題解決經驗**: GlobalUsingsWindows.cs 創新解決 WPF implicit usings 問題

---

## 📝 開發者筆記

### 關鍵決策
- **GuiRunner 內部類別**: 避免條件編譯可見性問題
- **Code-behind 模式**: Phase 2 保持簡單,未使用 MVVM
- **重用 YamlConfigManager**: 避免重複邏輯,確保一致性

### 學到的經驗
- WPF SDK 改變 implicit usings 行為,需要額外補充
- 條件編譯 #if WINDOWS 可能影響跨檔案類別可見性
- 內部類別是解決條件編譯問題的簡單方案

---

**狀態**: ✅ Phase 2 完成
**可發布版本**: v1.0-gui-alpha
**下一階段**: Phase 3 或 Phase 4 (待決定優先級)
