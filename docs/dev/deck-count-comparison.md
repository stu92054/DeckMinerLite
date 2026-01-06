# DeckReset 卡位置限制邏輯 - 卡組數量比較

## 測試配置
- 配置檔案: `config/member-test.yaml`
- 測試歌曲: チャーミングな花束を！ (ID: 405126, Difficulty: 02)
- 卡池數量: 20 張卡
- 測試日期: 2024-12-26

## 測試分支

### Main 分支 (無限制)
- **卡組數量**: **4,093,968**
- 邏輯: 只有基本限制（第一位不能是分卡、最後一位不能是 DeckReset 卡）

### Exp 分支 (DeckReset 位置限制)
- **卡組數量**: **2,162,264**
- 新增邏輯: DeckReset 卡之前不能有分卡或電卡

## 結果分析

### 卡組數量變化
- **減少數量**: 1,931,704 個卡組
- **減少比例**: **47.2%**
- **保留比例**: 52.8%

### 影響說明
新增的 DeckReset 卡位置限制過濾掉了接近一半的卡組排列，這些被過濾的卡組存在以下問題：
- DeckReset 卡（洗牌卡）之前存在分卡（ScoreGain）或電卡（VoltagePointChange）
- 這些排列會導致在洗牌前就消耗掉增益卡疊加的增益效果
- 不符合最優策略（應該是：增益卡 → 洗牌 → 輸出卡）

### 效能影響
- **卡組生成時間**: 相近（都在 1 秒內完成）
- **模擬效能提升**: 47.2% 的卡組被提前過濾，減少無效模擬
- **卡組品質**: 保留的卡組更符合實際遊戲策略

## 結論

DeckReset 卡位置限制邏輯成功過濾掉了約 47% 的無效卡組排列，這些卡組雖然符合基本限制，但不符合最優遊戲策略。新邏輯能夠：

1. **提高卡組品質** - 只生成符合最優策略的卡組
2. **減少計算量** - 減少約 47% 的模擬任務
3. **保持效能** - 額外的檢查邏輯對生成時間影響極小
4. **符合實際策略** - 確保增益效果能被充分利用

建議在充分測試後，可以考慮將此邏輯合併到主分支。

## 測試指令

### 測試 main 分支
```cmd
cd DeckMinerLite
git checkout main
set CONFIG_FILE=..\config\member-test.yaml
D:\SukuShow-Deck-Miner\Portable\dotnet-sdk-10.0.101-win-x64\dotnet.exe run --configuration Release
```

### 測試 exp 分支
```cmd
cd DeckMinerLite
git checkout exp
set CONFIG_FILE=..\config\member-test.yaml
D:\SukuShow-Deck-Miner\Portable\dotnet-sdk-10.0.101-win-x64\dotnet.exe run --configuration Release
```

觀察輸出中的「卡組數量」數據即可比較差異。
