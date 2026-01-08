# Changelog

All notable changes to DeckMinerLite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.4.2] - 2026-01-08

### Fixed
- 配置儲存功能修正
  - 修正 SaveConfig 方法，確保完整儲存所有配置項目
  - 解決 fan_levels、songs、friend_card_ids、optimizer 等無法儲存的問題
- 多曲優化器配置傳遞修正
  - 改為透過命令列參數 `--config` 傳遞配置檔案路徑
  - 解決優化器使用預設配置而非用戶設定的問題
- 新增優化器執行除錯日誌
  - 顯示配置路徑、執行命令、工作目錄
  - 協助診斷優化器執行問題

## [1.4.1] - 2026-01-07

### Added
- Chart 資料轉換工具 (`export_all_charts.py`)
  - 批次轉換 .bytes 譜面為 JSON 格式
  - 智能增量更新（自動跳過相同檔案）
  - 詳細的差異檢測與提示
  - Music 資料庫同步更新
  - 保持原始格式（CRLF 換行、無縮排、無尾隨換行）
- 測試工具 (`test_chart_export.py`)
  - 驗證譜面轉換正確性
  - 比對新舊檔案差異
- Results Tab 完整實作
  - 自動載入優化結果 (best_3_song_combo.txt / best_2_song_combo.txt)
  - 手動重新整理功能
  - 多路徑搜尋支援（開發模式 / 打包版）
  - 檔案狀態顯示（更新時間）
- 版本資訊系統
  - Assembly Version: 1.4.1.0
  - File Version: 1.4.1.0
  - Product Version: 1.4.1

### Changed
- 遊戲資料更新
  - Music 資料庫: 218 首歌曲 (+5 新歌)
  - Chart 譜面: 524 個譜面 (+28 新譜面)
  - 修正假名錯誤: 405201 テレパシ (てらぱし → てれぱし)
- GUI About 頁面版本號更新至 1.4.1
- 配置標籤統一為「英文 (中文)」格式
- 日誌字型支援日文顯示 (`Consolas, Microsoft YaHei UI, Yu Gothic UI`)

### Fixed
- GUI/CLI 結果差異問題 (ResultBuffer Race Condition)
  - 引入字典序打破平局機制
  - 確保並行計算結果一致性
  - 不可變更新保證線程安全
- 配置自動儲存功能
  - 模擬前自動儲存編輯內容
  - 錯誤處理與使用者確認機制
- 日文字型顯示亂碼
  - 實作字型回退機制
  - 正確顯示日文歌曲名稱

### Documentation
- 更新 `wpf_gui_design_spec.md`
  - 新增版本資訊區域
  - 記錄資料更新詳情
  - 完成功能清單更新
- 更新 `PROGRESS.md`
  - Phase 8.4 完成標記
  - 遊戲資料更新章節
  - 發佈版本號更新
- 新增 `BUG_REPORT_20260106.md`
  - GUI/CLI 差異調查完整報告
  - 實驗驗證與解決方案
- 新增 `UPSTREAM_COMPARISON.md`
  - 與 upstream 分支差異比對

## [1.3.0] - 2026-01-06

### Added
- WPF GUI 完整實作
  - 主視窗框架 (4 個分頁)
  - 配置管理系統
  - 卡池編輯視窗
  - 歌曲配置視窗
  - 粉絲等級編輯
  - 朋友卡選擇器
  - 優化器配置視窗
- 模擬執行服務
  - BatchSimulationService (CLI/GUI 共用)
  - SimulationService (GUI 專用)
  - 非同步執行框架
  - 進度更新與日誌系統
  - 取消功能
- Python 優化器整合
  - multi_optimizer_2.py 呼叫
  - 結果檔案自動載入

### Changed
- Program.cs 重構
  - 混合架構 (CLI + GUI)
  - 條件編譯支援
- 專案結構優化
  - Multi-target (net10.0-windows / net10.0)
  - WPF 條件啟用

### Fixed
- YAML 配置讀取錯誤處理
- 空配置檔案驗證

## [1.2.0] - 2025-12-26

### Fixed
- Console 亂碼修正 (ea6f6d5)
- will_die 邏輯修正 (5c177b0)

### Changed
- 模擬器核心邏輯優化

---

## 版本命名規則

- **Major** (X.0.0): 重大架構變更或不相容更新
- **Minor** (1.X.0): 新功能、新特性
- **Patch** (1.0.X): Bug 修復、小改進

---

**最後更新**: 2026-01-07
