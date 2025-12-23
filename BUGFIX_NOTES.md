# 重大缺陷修復說明 (2025-12-24)

## ⭐ 關鍵修復: ExceptCard() 缺少 TopCard 更新

### 問題
當卡片被除外時,`Deck.ExceptCard()` 從隊列中移除卡片,但**沒有更新 TopCard 指標**,導致後續技能使用錯誤的卡片。

### 影響
- 正式環境分數低估 5.6% (64M)
- 卡組最佳化結果錯誤

### 修復
**檔案**: `Models/Deck.cs:59-72`

```diff
public void ExceptCard(Card card)
{
    if (card == null) return;
    card.IsExcept = true;
    var index = Queue.IndexOf(card);
    if (index != -1)
    {
        Queue.RemoveAt(index);
        if (Queue.Count == 0)
            Reset();
+       else
+           TopCard = Queue.First();  // ✅ 關鍵修復
    }
}
```

### 驗證
測試環境: `member-stu92054.yaml`, 歌曲 405128

| 版本 | Score | 差距 |
|------|-------|------|
| 舊版 | 1,148,043,161 | - |
| 新版 | 1,211,932,549 | **+5.6%** |

---

## 其他修復

### 1. Debug 模式狀態污染
**檔案**: `Program.cs:133-143`
- 每次測試新 center 時重建 Deck 物件
- 與 Python 邏輯一致

### 2. Reset() 錯誤重置 IsExcept
**檔案**: `Models/Deck.cs:45-57`
- 移除 IsExcept 重置邏輯
- 被除外卡片保持除外狀態

---

## 升級建議

1. ✅ **必須升級**: 此修復解決了技能系統核心缺陷
2. ⚠️ **重新計算**: 舊版模擬結果可能偏低,建議重新計算
3. 📊 **預期差異**: 分數可能提升 3-8%,卡組構成可能改變

---

## 詳細文檔

- 完整調查記錄: [SCORE_DISCREPANCY_LOG.md](SCORE_DISCREPANCY_LOG.md)
- 修改總結: [CHANGES_SUMMARY.md](CHANGES_SUMMARY.md)
