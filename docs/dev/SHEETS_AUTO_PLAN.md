# Google Sheets 自動監控與計算腳本

## Context

目前玩家透過 Google Spreadsheet 管理卡組資料，手動匯出 CSV → 用 web config generator 產生 YAML → 執行模擬器。此腳本將自動化這個流程：監控 Spreadsheet，當玩家標記「待計算」時自動生成 config、執行 C# 模擬器和多曲優化器、將結果寫回 Spreadsheet。腳本主要部署在 Linux 上。

## 架構

```
Google Sheets (每個分頁 = 一個玩家)
    │  poll every 60s
    ▼
sheets_auto/main.py (監控 daemon)
    │
    ├─ 讀取玩家分頁資料 (Google Sheets API v4)
    ├─ 生成 config/member-{name}.yaml
    ├─ 執行 DeckMinerLite (C# 模擬器)
    ├─ 執行 multi_optimizer_2.py (多曲優化)
    └─ 將結果寫回玩家分頁
```

## 新增檔案

```
sheets_auto/
├── __init__.py
├── main.py              # 入口點 + daemon 循環
├── auth.py              # Google Service Account 認證
├── monitor.py           # 輪詢邏輯、狀態管理
├── sheet_parser.py      # 解析 Spreadsheet → PlayerData
├── config_writer.py     # PlayerData → member YAML
├── runner.py            # 調用 DeckMinerLite + multi_optimizer
├── result_writer.py     # 解析結果 → 寫回 Spreadsheet
├── models.py            # 資料模型 (dataclass)
└── constants.py         # 儲存格位置常數、角色映射

config/
└── sheets-auto.yaml     # 服務配置 (Spreadsheet ID、認證路徑等)

sheets_auto.service      # systemd unit file (可選)
```

## 關鍵實作細節

### 1. Google Sheets 認證 (`auth.py`)

- 使用 **Service Account** (JSON key file)，適合 Linux 無頭伺服器
- 依賴：`google-auth`, `google-api-python-client`
- Service account 需要被加為 Spreadsheet 的 Editor

### 2. 狀態儲存格位置

在每個玩家分頁的 **H20** 放置狀態：
- 空白 = 不處理
- `待計算` = 等待處理
- `計算中` = 正在處理
- `已完成` = 計算完成
- `錯誤: {訊息}` = 出錯

Row 20 在 song config (rows 2-19) 和 DR section (row 28+) 之間，是空閒區域。

### 3. 分頁格式偵測 (`sheet_parser.py`)

參考 `web/config-generator.js:1127-1366` 的 CSV 解析邏輯：

**格式偵測**: 讀取 A1 儲存格
- `"角色"` → 標準格式 (多數玩家)
- `"第 1 欄"` → 替代格式 (震, 蛋頭, 真田みかん)

**卡片解析** (兩種格式統一，A-F 列):
- A=角色名, B=卡名, C=卡片ID, D=等級, E=C位技, F=主技能

**歌曲解析** (根據實際儲存格內容動態判斷):
- 掃描 H 列，找到 `A`/`B`/`C` 值的行 → 該行 `[H=標籤, I=熟練度, J=難度, K=music_id, L=歌名, M=C位]`
- 若 H 列找到 music_id 數字且 L 列有 A/B/C → 多行格式 (每首歌 6 行)
- 實作時兩種都偵測，取先匹配的

**DR 卡解析** (H-M 列):
- H 列為角色名 (非 A/B/C/DR/"") 時為 DR 卡資料
- DR 格式：`[角色名, 卡名, 等級, C位, 主技能, 卡片ID]` (col H-M)

**跳過的分頁**: `填完>>>`, `計算中完畢>>>`, `未填>>>>`, `樣本文文`, `複製用`, `掛卡表`, `全卡資料庫`, `Musics`

**跳過條件** (同 `config-generator.js:1201-1206`):
- 角色名為 `"- - -"` 的佔位行
- 等級/C位/技能全為 `1` 的未持有卡 (預設佔位)

### 4. Config 生成 (`config_writer.py`)

參考 `config/member-stu92054.yaml` 格式生成完整 YAML：

```yaml
output:
  base_dir: output
  enable_isolation: true
songs:
  - music_id: "405136"
    difficulty: "02"
    mastery_level: 50
    mustcards_all: []
    mustcards_any: []
    mustskills: [2, 3, 5, 7, 8]    # 預設值
    banned_cards: []
    leader_designation: "0"
    secondary_center: []
    friend_card_pool: []             # 從 spreadsheet 解析
card_ids: [...]                      # 一般卡 + DR卡
season_mode: sukushow
lgp_mode: true
fan_levels: {1011: 10, ...}         # 預設全部 10
card_levels: {card_id: [lv, c, s]}  # 每張卡的練度
batch_size: 1000000
optimizer:
  top_n: 50000
  show_card_names: true
  forbidden_cards: []
```

**難度對應**: `Normal→01, Hard→02, Expert→03, Master→04`

### 5. 模擬執行 (`runner.py`)

依序執行：
1. `./DeckMinerLite --config /abs/path/config/member-{name}.yaml`
   - cwd 設為 DeckMinerLite 目錄
   - timeout: 2 小時
2. `python3 multi_optimizer_2.py --config config/member-{name}.yaml`
   - cwd 設為專案根目錄
   - timeout: 30 分鐘

**一次只處理一個玩家**，避免 CPU 過載。

### 6. 結果寫回 (`result_writer.py`)

解析 `multi_optimizer_2.py` 輸出的 `best_3_song_combo.txt` (或 `best_2_song_combo.txt`)，寫回玩家分頁的 **H21:N27** 區域：

```
H21: "計算結果"        I21: "Total PT: 123,456"
H22: "Song A"          I22: "{歌名}"      J22: "PT: 45,000"
H23: "卡組"            I23-N23: [6張卡片ID]
H24: "Song B"          I24: "{歌名}"      J24: "PT: 40,000"
H25: "卡組"            I25-N25: [6張卡片ID]
H26: "Song C"          I26: "{歌名}"      J26: "PT: 38,456"
H27: "卡組"            I27-N27: [6張卡片ID]
```

### 7. 監控循環 (`monitor.py`)

```python
while True:
    # 用 batchGet 一次讀取所有玩家分頁的 H20 (狀態)
    for sheet_name in player_sheets:
        if status == "待計算":
            update_status("計算中")
            try:
                player_data = parse_sheet(sheet_name)
                config_path = write_config(player_data)
                run_simulation(config_path)
                write_results(sheet_name, config_path)
                update_status("已完成")
            except Exception as e:
                update_status(f"錯誤: {str(e)[:50]}")
    sleep(60)
```

### 8. 服務配置 (`config/sheets-auto.yaml`)

```yaml
google_sheets:
  spreadsheet_id: "1VWtA40KNUe2quET1sWfzVlD0R88q99n_ZpA9qXpZE-o"
  credentials_path: "/etc/deck-miner/service-account.json"
polling:
  interval_seconds: 60
simulation:
  project_root: "/path/to/SukuShow-Deck-Miner"
  deckminer_binary: "DeckMinerLite/bin/Release/net10.0/DeckMinerLite"
defaults:
  mustskills: [2, 3, 5, 7, 8]
  fan_levels_default: 10
status_cell: "H20"
results_start_row: 21
```

## 依賴

```
google-auth
google-api-python-client
PyYAML
```

## 驗證方式

1. `--dry-run` 模式：解析分頁 + 生成 config 但不執行模擬，用來驗證解析和 config 正確性
2. `--sheet "玩家名"` 模式：只處理指定玩家，方便測試
3. 對比生成的 YAML 和手動產生的 `member-stu92054.yaml`，確認格式一致
4. 在 Linux 上完整跑一次：待計算 → 計算中 → 已完成，確認結果寫回正確
