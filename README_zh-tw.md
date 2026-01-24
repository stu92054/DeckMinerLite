# SukuShow Deck Miner Lite

適用於 [Link！Like！LoveLive！](https://www.lovelive-anime.jp/hasunosora/system/) (リンクラ)
音遊模式 **School Idol Show (スクショウ)** 的 **卡組模擬器（C# 高效能版）**。

本專案是 Python 版 [SukuShow Deck Miner](https://github.com/BlueNoBaka/SukuShow-Deck-Miner) 的 C# 實作，**效能更高**。
**僅實作了批次模擬**的功能，輸出的 Log 與 Python 版相容。

---

## 🎮 使用方式

### ▶ 執行主程式

本專案在 .Net 10 環境下開發，Windows 版本提供 **WPF 圖形化介面 (GUI)**，Linux 版本採用 **NativeAOT** 建置的 CLI，使用時不需要額外安裝 .Net 執行環境。

#### Windows 版本（含 GUI）

**GUI 模式（推薦）**：
- 雙擊 `DeckMinerLite.exe` 啟動圖形化介面
- 透過介面載入 YAML 配置檔
- 視覺化顯示卡池、歌曲配置、模擬日誌
- 適合一般使用者和互動式操作

**CLI 模式（自動化）**：
```bash
# 傳入參數時自動切換為 CLI 模式
DeckMinerLite.exe --config config/member-example.yaml
DeckMinerLite.exe --test-yaml
```

#### Linux 版本（純 CLI）

Linux 版本僅提供命令列介面，採用 NativeAOT 優化：
```bash
chmod +x DeckMinerLite
./DeckMinerLite --config config/member-example.yaml
```

---

## 🖥 GUI 功能說明（Windows 專屬）

### 主視窗介面

GUI 提供 4 個分頁：

#### 1️⃣ Configuration（配置）
- 載入 YAML 配置檔（支援 `config/*.yaml`）
- 顯示基本設定：成員名稱、賽季模式、LGP 模式
- 顯示卡池大小
- 列出所有歌曲配置（歌曲 ID、難度、熟練度）

#### 2️⃣ Simulation（模擬）
- 執行模式選擇：完整優化（模擬 + 多曲優化）或僅模擬
- 開始/停止模擬按鈕
- 進度條顯示模擬進度
- 即時日誌輸出（與 CLI 相同格式）
- 清除日誌按鈕

#### 3️⃣ Results（結果）
- 顯示多曲優化結果（`best_3_song_combo.txt` 或 `best_2_song_combo.txt`）
- 重新整理按鈕可載入最新結果

#### 4️⃣ About（關於）
- 版本資訊
- 功能說明
- CLI 模式提示

### 快速操作

- **載入配置**：點擊「Load Config」選擇 YAML 檔案
- **重新載入**：點擊「Reload」刷新配置
- **開始模擬**：載入配置後，「Start Simulation」按鈕會啟用
- **開啟輸出資料夾**：點擊「Open Output Folder」快速開啟結果目錄

---

## ⚙ 配置說明

### 📋 YAML 配置（推薦）

支援使用 YAML 配置檔，與 Python 版完全相容。

#### 配置檔優先順序
1. 命令列參數 `--config`
2. 環境變數 `CONFIG_FILE`
3. `config/default.yaml`
4. 回退到 `task.jsonc`（舊版配置）

#### 使用範例

```bash
# 使用指定配置檔執行
DeckMinerLite.exe --config ../config/member-test.yaml

# 測試 YAML 配置載入
DeckMinerLite.exe --test-yaml --config ../config/member-test.yaml
```

#### YAML 配置檔格式

```yaml
# 輸出目錄配置
output:
  base_dir: "output"
  enable_isolation: true  # 開啟隔離模式，每次執行獨立目錄

# 歌曲配置（用於模擬器）
songs:
  - music_id: "405126"
    difficulty: "02"        # 01=Normal, 02=Hard, 03=Expert, 04=Master
    mastery_level: 50
    mustcards_all: []       # 必須全部包含的卡牌
    mustcards_any: []       # 必須包含至少一張的卡牌
    banned_cards: []        # 禁止使用的卡牌

# 卡池（該成員擁有的所有卡牌 ID）
card_ids:
  - 1031533
  - 1021701
  # ... 更多卡牌

# 賽季模式（用於計算粉絲等級加成）
season_mode: "sukushow"   # sukushow 或 sukuste

# LGP 模式
lgp_mode: true            # true=大賽模式（允許雙卡），false=日常模式（每角色最多1張）

# 粉絲等級配置
fan_levels:
  1031: 10  # 角色ID: 粉絲等級
  1021: 8

# 特定卡牌練度覆蓋（未滿練的卡）
card_levels:
  1021701: [140, 14, 11]  # [等級, 中心技能等級, 技能等級]

# 優化器配置（用於 multi_optimizer_2.py）
optimizer:
  top_n: 50000
  show_card_names: true
  forbidden_cards: []     # 全域禁卡（三首歌均生效）

  songs:                  # 優化器專屬歌曲配置（可選）
    - music_id: "405128"
      difficulty: "02"
      banned_cards: []    # 該首歌的禁卡（與全域合併）
```

#### 關鍵功能

##### 1. 禁卡功能
支援三級禁卡配置：
- **歌曲級** (`songs[].banned_cards`)：該首歌禁用
- **優化器級** (`optimizer.songs[].banned_cards`)：多曲優化時該首歌禁用
- **全域級** (`optimizer.forbidden_cards`)：所有歌曲禁用

最終禁卡 = 歌曲級 ∪ 優化器級 ∪ 全域級

##### 2. LGP 模式 vs 日常模式
- **LGP 模式** (`lgp_mode: true`)：大賽規則，允許 0-3 個角色使用雙卡
- **日常模式** (`lgp_mode: false`)：每個角色最多 1 張卡

##### 3. PT 動態計算
自動根據粉絲等級和賽季模式計算 BONUS_SFL：
```
PT = score × BONUS_SFL × LIMITBREAK_BONUS
```

其中：
- `BONUS_SFL` = (1 + Σ fan_level_bonus) × singing_count_correction
- `LIMITBREAK_BONUS` 根據卡牌練度（中心技能/技能等級）決定

##### 4. 輸出目錄隔離
使用 `member-*.yaml` 配置時：
- **Log 目錄**：`log/{member}/`
- **Temp 目錄**：`temp/{member}/{timestamp}/`

---

### 📄 JSONC 配置（舊版）

如果不使用 YAML，可以透過 `cardConfig.jsonc` 和 `task.jsonc` 進行配置。

模擬器支援讀取帶註解的 Json，但**註解內容需要以 `//` 開頭**，而不是 Python 註解的 `#`。

* **卡牌等級配置**
  * 檔案：`cardConfig.jsonc`
  * 功能與 Python 版的 `CardLevelConfig.py` 一致
  * 與 Python 版不同，練度中的卡牌 ID 需要帶引號，例如 `"1021701": [140, 14, 11]`

* **卡池配置**
  * 檔案：`task.jsonc`
  * 欄位：`CardPool`
  * 填寫卡牌 ID 即可，與 Python 版一致

* **模擬任務配置**
  * 檔案：`task.jsonc`
  * 欄位：`Task`
  * 單個任務的填寫規則及用途與 Python 版基本一致，填寫多個任務則會順序執行
  * 卡組的技能約束 `MustSkills` 需要填寫技能類型的編號，具體參考下表

#### 🎯 技能類型對照表

| 編號  | 列舉名稱                      | 說明 |
|------:|------------------------------|------|
| 1     | `APChange`                   | 回費/扣費 |
| 2     | `ScoreGain`                  | 分 |
| 3     | `VoltagePointChange`         | 加電/扣電 |
| 4     | `MentalRateChange`           | 回血/扣血 |
| 5     | `DeckReset`                  | 洗牌 |
| 6     | `CardExcept`                 | 除外 |
| 7     | `NextAPGainRateChange`       | 分加成 |
| 8     | `NextVoltageGainRateChange`  | 電加成 |

---

## 🔄 使用流程

### 方式一：GUI 一鍵執行（推薦）

1. 啟動 `DeckMinerLite.exe`
2. 載入 YAML 配置檔
3. 切換到 Simulation 分頁
4. 選擇「完整優化」模式
5. 點擊「開始」即可自動完成模擬 + 多曲優化

### 方式二：CLI 分步執行

```bash
# 1. 使用 C# 模擬器產生單曲結果（高效能）
cd DeckMinerLite
dotnet run -- --config ../config/member-test.yaml

# 2. 使用 Python 多曲優化器（靈活）
cd ..
python multi_optimizer_2.py --config config/member-test.yaml
```

**輸出檔案**：
- 單曲模擬結果：`log/{member}/simulation_results_{music_id}_{difficulty}.json`
- 多曲優化結果：`best_3_song_combo.txt` / `best_2_song_combo.txt`

---

## ⚠ 與 Python 版的主要差異

### ✅ 已實作
- ✅ YAML 配置完全相容
- ✅ 禁卡功能（三級合併）
- ✅ LGP 模式 / 日常模式切換
- ✅ PT 動態計算（Fan Level + Limitbreak）
- ✅ 輸出目錄隔離
- ✅ 卡牌練度自訂
- ✅ **WPF 圖形化介面（Windows）**
- ✅ **GUI/CLI 雙模式自動切換**
- ✅ **GUI 模擬執行整合**（支援完整優化流程或僅模擬）

### ⚠ 未實作
- ❌ 花火吟的延後 Miss（影響仰臥起坐精度）
- ❌ PT 重算工具（請使用 Python 版 `log_tool.py`）

---

## 📊 效能比較

| 項目 | C# (DeckMinerLite) | Python (MainBatch.py) |
|------|--------------------|-----------------------|
| 單曲模擬速度 | **極快** | 較慢 |
| 記憶體使用 | 低 | 中等 |
| 多曲優化 | ✅ (GUI 整合) | ✅ |
| YAML 配置 | ✅ | ✅ |
| **圖形化介面** | **✅ (Windows)** | ❌ |
| 跨平台支援 | Windows (GUI+CLI) / Linux (CLI) | 全平台 CLI |

---

## 🛠 開發資訊

- **語言**：C# (.NET 10)
- **建置架構**：
  - Windows: `net10.0-windows` (WPF GUI, 無 AOT)
  - Linux: `net10.0` (純 CLI, NativeAOT)
- **GUI 框架**：WPF (Windows Presentation Foundation)
- **配置格式**：YAML（推薦）或 JSONC
- **依賴套件**：
  - YamlDotNet 16.2.0（YAML 解析）
  - TqdmSharp 0.4.3（進度條）
  - CommunityToolkit.Mvvm 8.3.2（MVVM 支援，僅 Windows）

### 編譯專案

```bash
cd DeckMinerLite

# 編譯 Windows 版本（含 GUI）
dotnet build --framework net10.0-windows

# 編譯 Linux 版本（純 CLI）
dotnet build --framework net10.0

# 編譯所有目標
dotnet build
```

### 執行開發版本

```bash
# Windows: CLI 模式（需傳入參數）
dotnet run --framework net10.0-windows -- --config ../config/member-test.yaml

# Linux: CLI 模式
dotnet run --framework net10.0 -- --config ../config/member-test.yaml

# 測試 YAML 配置
dotnet run -- --test-yaml --config ../config/member-test.yaml
```

### 發布套件

```bash
# 使用自動化腳本（推薦）
publish.bat

# 手動發布
dotnet publish -c Release --framework net10.0-windows -r win-x64 --self-contained
dotnet publish -c Release --framework net10.0 -r linux-x64 --self-contained
```

---

## ❓ 常見問題 (FAQ)

### Q: Console 輸出中文亂碼怎麼辦？

**A**: 程式已自動設定 UTF-8 編碼，大多數情況下不會出現亂碼。若仍有問題，請嘗試以下方法：

#### 方法 1: 使用 Windows Terminal（推薦）
Windows Terminal 原生支援 UTF-8，不會有亂碼問題。
- Windows 11: 預設安裝
- Windows 10: 從 Microsoft Store 下載

#### 方法 2: 設定 CMD 代碼頁
在執行程式前，先在 CMD 中輸入：
```cmd
chcp 65001
```

#### 方法 3: 設定 PowerShell 編碼
在 PowerShell 中執行：
```powershell
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
```

#### 方法 4: 修改 CMD 預設代碼頁（永久）
1. 執行 `regedit` 開啟登錄編輯程式
2. 導航至 `HKEY_LOCAL_MACHINE\Software\Microsoft\Command Processor`
3. 新增字串值 `Autorun`，設定為 `chcp 65001 >nul`

### Q: 為什麼某些環境下程式啟動較慢？

**A**: 首次執行時，.NET 運行時會進行 JIT 編譯，後續執行會更快。使用 NativeAOT 版本可避免此問題。

### Q: 如何確認程式版本？

**A**: 查看 git commit hash 或執行：
```bash
DeckMinerLite.exe --version
```

---

## 📝 授權

與上游專案相同

## 🔗 相關連結

- Python 版專案：[SukuShow-Deck-Miner](https://github.com/BlueNoBaka/SukuShow-Deck-Miner)
- 遊戲官網：[Link！Like！LoveLive！](https://www.lovelive-anime.jp/hasunosora/)
