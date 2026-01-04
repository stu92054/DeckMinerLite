# SukuShow Deck Miner Lite

適用於 [Link！Like！LoveLive！](https://www.lovelive-anime.jp/hasunosora/system/) (リンクラ)
音遊模式 **School Idol Show (スクショウ)** 的 **卡組模擬器（C# 高效能版）**。

本專案是 Python 版 [SukuShow Deck Miner](https://github.com/BlueNoBaka/SukuShow-Deck-Miner) 的 C# 實作，**效能更高**。
**僅實作了批次模擬**的功能，輸出的 Log 與 Python 版相容。

---

## 🎮 使用方式

### ▶ 執行主程式

本專案在 .Net 10 環境下開發，採用 **NativeAOT** 建置，使用時不需要額外安裝 .Net 執行環境。

雙擊 `DeckMinerLite.exe` 即可執行，練度、卡池、模擬任務可透過配置檔修改。

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

## 🔄 與 Python 版的整合流程

推薦工作流程：

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
- 多曲優化結果：`output/{member}/optimization_results_{timestamp}.json`

---

## ⚠ 與 Python 版的主要差異

### ✅ 已實作
- ✅ YAML 配置完全相容
- ✅ 禁卡功能（三級合併）
- ✅ LGP 模式 / 日常模式切換
- ✅ PT 動態計算（Fan Level + Limitbreak）
- ✅ 輸出目錄隔離
- ✅ 卡牌練度自訂

### ⚠ 未實作
- ❌ 花火吟的延後 Miss（影響仰臥起坐精度）
- ❌ 多曲優化功能（請使用 Python 版 `multi_optimizer_2.py`）
- ❌ PT 重算工具（請使用 Python 版 `log_tool.py`）

---

## 📊 效能比較

| 項目 | C# (DeckMinerLite) | Python (MainBatch.py) |
|------|--------------------|-----------------------|
| 單曲模擬速度 | **極快** | 較慢 |
| 記憶體使用 | 低 | 中等 |
| 多曲優化 | ❌ | ✅ |
| YAML 配置 | ✅ | ✅ |

---

## 🛠 開發資訊

- **語言**：C# (.NET 10)
- **建置**：NativeAOT（無需執行環境）
- **配置格式**：YAML（推薦）或 JSONC
- **YAML 解析**：YamlDotNet 16.2.0

### 編譯專案

```bash
cd DeckMinerLite
dotnet build
dotnet run -- --config ../config/member-test.yaml
```

### 測試 YAML 配置

```bash
dotnet run -- --test-yaml --config ../config/member-test.yaml
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
