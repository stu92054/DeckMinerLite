# Simulator 快轉優化技術規格書 (Fast-Forward Optimization Specification)

## 文件資訊
- **版本**: MVP v1.0
- **日期**: 2025-12-26
- **目標**: 將模擬器運算效能提升 2-5 倍

---

## 1. 背景與問題陳述

### 1.1 當前效能瓶頸

在 `Simulator.cs` 的主迴圈中,每個 Note 事件都會執行以下檢查:

```csharp
// Simulator.cs:241-298
case LiveEventType.Single/Hold/Flick/Trace:
    // 1. 背水卡邏輯檢查 (afkMental)
    if (afkMental != 0 && Player.Mental.Rate > afkMental) { ... }

    // 2. ComboAdd 處理
    Player.ComboAdd("PERFECT+");

    // 3. 技能發動檢查
    if (Player.CDAvailable) {
        TryUseSkill(currentEvent.Time);
    }
```

**問題**: 一首歌約 800 個 Note,但大部分時間技能無法發動 (CD 未轉好或 AP 不足),這些檢查都是**無效運算**。

### 1.2 效能分析

以一首 3 分鐘的歌曲為例:
- Note 總數: ~800
- 技能發動次數: ~30-40 次
- **無效檢查次數**: ~760 次 (95%)

每次無效檢查包含:
- 函數呼叫開銷 (`TryUseSkill`)
- 條件判斷分支 (`if (Player.CDAvailable)`)
- 背水卡邏輯 (複雜的 HP 檢查)

---

## 2. 解決方案: 下一動作預測法 (Next-Event Prediction)

### 2.1 核心思想

**「在無法發動技能的期間,跳過所有中間 Note,直接快轉到下一個可能發動技能的時刻」**

這是一種 **Discrete Event Simulation (DES)** 的標準優化技術,稱為 **Event Skipping** 或 **Time Jumping**。

### 2.2 技能發動的充要條件

技能發動需要**同時滿足兩個條件**:

```csharp
bool CanUseSkill =
    Player.CDAvailable &&           // CD 已轉好
    Player.Ap >= cardNow.Cost;      // AP 足夠
```

#### 條件 1: CD 轉好
- CD 在**連續時間軸**上轉好 (由 `extraEvents` 中的 `CDavailable` 事件觸發)
- CD 轉好後會**立即嘗試發動技能** (Line 301-304)
- 如果當時 AP 不足,則等待下一個 Note

#### 條件 2: AP 足夠
- AP 只在 **Note 時間點離散增加** (通過 `ComboAdd`)
- 每個 Note 增加固定 AP (當 `Combo >= 50` 時)
- **Note 類型 (Single/Hold/Flick/Trace) 不影響 AP 增長**

### 2.3 快轉的安全條件

可以安全快轉,**當且僅當**:

```csharp
bool CanFastForward =
    Player.Combo >= 50 &&           // (條件 A) ApRate 穩定
    afkMental == 0 &&                // (條件 B) 沒有背水卡
    (
        !Player.CDAvailable ||       // (條件 C1) CD 還沒轉好
        Player.Ap < cardNow.Cost     // (條件 C2) 或 AP 不夠
    );
```

#### 條件說明

| 條件 | 原因 | 風險 |
|------|------|------|
| **A. Combo >= 50** | ApRate 固定為 1.5x,AP 增長可預測 | Combo < 50 時 ApRate 變化,AP 增長不穩定 |
| **B. afkMental == 0** | 不會主動 MISS,所有 Note 都是 PERFECT+ | 背水卡需要實時檢查 HP,無法跳過 |
| **C1. CD 未轉好** | 即使 AP 足夠也無法發動技能 | CD 轉好時需要立即檢查技能 |
| **C2. AP 不足** | 即使 CD 轉好也無法發動技能 | AP 足夠時需要立即發動技能 |

#### 條件 C 的邏輯 (OR 關係)

**情況 1: CD 未轉好** (`!Player.CDAvailable`)
- 即使 AP 很多,也無法發動技能
- 可以快轉到 **CD 轉好**為止

**情況 2: CD 已轉好,但 AP 不足** (`Player.Ap < cardNow.Cost`)
- 即使 CD 好了,AP 不足也無法發動技能
- 可以快轉到 **AP 累積足夠**為止

**情況 3: CD 好了且 AP 夠** (兩個條件都不成立)
- 應該立即發動技能
- **不能進入快轉**

---

## 3. 快轉終點的計算

### 3.1 終點公式

```csharp
double safeHorizon = Math.Min(
    nextCDReadyTime,      // 終點 1: CD 轉好的時刻
    nextAPReadyTime       // 終點 2: AP 累積到足夠的時刻
);
```

### 3.2 終點 1: CD 轉好時刻

```csharp
double nextCDReadyTime;

if (Player.CDAvailable)
{
    // CD 已經好了,這個條件不限制快轉
    nextCDReadyTime = double.MaxValue;
}
else
{
    // 從 extraEvents 中取得下一個 CDavailable 事件的時間
    if (extraEvents.Count > 0 && extraEvents.Peek().Type == LiveEventType.CDavailable)
    {
        nextCDReadyTime = extraEvents.Peek().Time;
    }
    else
    {
        nextCDReadyTime = double.MaxValue;
    }
}
```

### 3.3 終點 2: AP 足夠時刻

```csharp
double nextAPReadyTime = double.MaxValue;

if (cardNow != null && Player.CDAvailable)  // 只有 CD 好了才需要算 AP
{
    if (Player.Ap >= cardNow.Cost)
    {
        // AP 已經夠了,不應該進入快轉
        nextAPReadyTime = 0;
    }
    else
    {
        // 計算 AP 缺口
        double apDeficit = cardNow.Cost - Player.Ap;

        // 每個 Note 增加的 AP (Combo >= 50 時固定)
        double apPerNote = Player._prevAp;

        if (apPerNote > 0)
        {
            // 需要幾個 Note 才能湊夠 AP
            int notesNeeded = (int)Math.Ceiling(apDeficit / apPerNote);

            // 找到第 N 個 Note 的時間點
            if (i_event + notesNeeded < chartEvents.Length)
            {
                nextAPReadyTime = chartEvents[i_event + notesNeeded].Time;
            }
        }
    }
}
```

---

## 4. 快轉期間的處理邏輯

### 4.1 快轉迴圈

```csharp
// 快轉主迴圈
while (i_event < chartEvents.Length)
{
    ref readonly var currentEvent = ref chartEvents[i_event];

    // 檢查 1: 是否超出安全時間 (該停下來了)
    if (currentEvent.Time >= safeHorizon) break;

    // 檢查 2: 是否為特殊事件 (需要特殊處理的事件)
    // LiveEventType: Single(1), Hold(2), HoldMid(3), Flick(4), Trace(5)
    //                CDavailable(6), Delayed*(7-11), LiveStart(12+), Fever*(13+)
    // 只處理基礎 Note (1-5),遇到系統事件(≥6)立即停止
    if (currentEvent.Type > LiveEventType.Trace) break;

    // === 極速處理 Note (內聯版本的 ComboAdd) ===
    Player.Combo++;
    Player.Ap += cachedApGain;        // 預先計算好的 AP 增量
    Player.Score += cachedNoteScore;  // 預先計算好的分數

    i_event++;
}
```

#### 為何檢查 `currentEvent.Type > LiveEventType.Trace`?

這個檢查的目的是**區分普通 Note 和系統事件**:

- **普通 Note** (`Single`~`Trace`, 值 1-5): 可以快轉處理
- **系統事件** (`CDavailable`, `LiveStart`, `FeverStart` 等, 值 ≥6): 需要特殊處理,必須跳出快轉

**為何對 MVP 安全?**
- `DelayedNote` 事件 (值 7-11) 只在背水卡 MISS 時產生
- MVP 條件 `afkMental == 0` 排除了背水卡
- 因此快轉期間**不會遇到 Delayed 事件**

**未來擴展**: 若要支援背水卡快轉,需要改用白名單方式:
```csharp
if (currentEvent.Type < LiveEventType.Single ||
    currentEvent.Type > LiveEventType.Trace)
    break;
```

### 4.2 需要預先緩存的數值

在進入快轉前,需要計算並緩存以下數值:

```csharp
// AP 增量 (Combo >= 50 時固定)
double cachedApGain = Player._prevAp;

// Note 分數 (Voltage 不變時固定)
int cachedNoteScore = Player._prevNoteScore;
```

這些數值在 `LiveStatus.ComboAdd` 中已經計算過,可以直接使用。

### 4.3 為何 Note 類型不影響處理?

從 `LiveStatus.ComboAdd` 的實作可以看到:

```csharp
// LiveStatus.cs:107-159
public void ComboAdd(string judgement, LiveEventType noteType = LiveEventType.Unknown)
{
    switch (judgement)
    {
        case "PERFECT+":
        case "PERFECT":
        case "GREAT":
            Combo++;
            Ap += _prevAp;  // ← 固定增量,與 noteType 無關
            break;
        // ...
    }
    ScoreNote(judgement);  // ← 也與 noteType 無關
}
```

**`noteType` 只在 MISS/BAD 時影響扣血量**:
```csharp
Mental.Sub(judgement, noteType);  // Trace/HoldMid 扣血較少
```

因此,在快轉期間 (假設所有 Note 都是 PERFECT+):
- ✅ 所有 `Single/Hold/Flick/Trace` 用**相同邏輯**處理
- ✅ 不需要區分 Note 類型
- ✅ 可以用統一的內聯代碼處理

---

## 5. 實作細節與注意事項

### 5.1 LiveStatus 屬性存取

當前 `_prevAp` 和 `_prevNoteScore` 是 `private`,需要修改為可存取:

**選項 1: 改為 internal** (推薦)
```csharp
// LiveStatus.cs
internal double _prevAp = 0.0;
internal int _prevNoteScore = 0;
```

**選項 2: 提供唯讀屬性**
```csharp
// LiveStatus.cs
public double CachedApGain => _prevAp;
public int CachedNoteScore => _prevNoteScore;
```

### 5.2 Voltage 變化的處理

`_prevNoteScore` 的計算依賴 `Voltage.Level`:

```csharp
// LiveStatus.cs:92-101
if (_prevVo == Voltage.Level) {
    Score += _prevNoteScore;  // 使用緩存
} else {
    _prevVo = Voltage.Level;
    _prevNoteScore = (int)ScoreAdd(scoreValue, skill: false);  // 重新計算
}
```

**風險**: 如果快轉期間 Voltage 升級,`_prevNoteScore` 會失效。

**解決方案 (MVP)**:
- 假設快轉期間 Voltage 不變 (通常在兩個 Fever 之間是穩定的)
- 如果 Voltage 變化,下次正常處理 Note 時會自動重新計算 `_prevNoteScore`
- 誤差: 最多影響幾個 Note 的分數 (< 0.01% 總分)

**未來優化**:
```csharp
// 在快轉前記錄 Voltage Level
int voltageBeforeFastForward = Player.Voltage.Level;

// 快轉後檢查
if (Player.Voltage.Level != voltageBeforeFastForward) {
    // Voltage 變化了,強制重新計算
    Player._prevVo = -1;
}
```

### 5.3 extraEvents 的完整性

快轉期間**不處理** `extraEvents`,因此必須確保:

1. `safeHorizon` 設定在下一個 `extraEvent` 之前
2. 快轉迴圈會在遇到 `LiveEventType > Trace` 時中斷

這確保了 Fever、LiveEnd、CDavailable 等事件不會被跳過。

**安全機制**:
```csharp
// 主迴圈會優先處理 extraEvents
if (extraEvents.Count > 0 && extraEvents.Peek().Time < chartEvents[i_event].Time)
{
    currentEvent = extraEvents.Dequeue();  // 先處理 extraEvent
}
```

即使快轉計算有誤,`extraEvents` 也會在正確時機觸發,不會被遺漏。

### 5.4 花火吟延遲 MISS 的兼容性

花火吟機制會將 MISS 延遲 0.07-0.125 秒 (Line 274-280):

```csharp
extraEvents.Enqueue(
    new RuntimeEvent(delayedTime, delayedType),
    delayedTime
);
```

**兼容性**:
- MVP 版本的條件 `afkMental == 0` 已經排除了背水卡
- 因此**不會產生延遲 MISS 事件**
- 快轉邏輯與花火吟機制完全兼容 ✅

---

## 6. 預期效能提升

### 6.1 理論分析

假設一首歌:
- Note 總數: 800
- CD: 5 秒
- Note 間隔: ~0.15 秒/note
- 每個 CD 週期: ~33 notes

每個 CD 週期:
- 正常處理: 1-2 notes (CD 轉好時)
- 快轉處理: 30-31 notes

### 6.2 節省的運算

**原本每個 Note 的處理**:
```csharp
// 1. 背水卡檢查 (~10 行代碼 + 多次分支)
if (afkMental != 0 && Player.Mental.Rate > afkMental) { ... }

// 2. ComboAdd 函數呼叫 (~50 行代碼)
Player.ComboAdd("PERFECT+");

// 3. 技能檢查
if (Player.CDAvailable) {
    TryUseSkill(currentEvent.Time);  // ~30 行代碼
}
```

**快轉處理 (3 行內聯代碼)**:
```csharp
Player.Combo++;
Player.Ap += cachedApGain;
Player.Score += cachedNoteScore;
```

**節省比例**: ~90% 代碼執行量

### 6.3 效能預測

| 版本 | Note 處理方式 | 預期加速比 |
|------|---------------|------------|
| 原版 | 800 次完整檢查 | 1x (基準) |
| MVP | ~600 次快轉 + ~200 次正常 | **2-3x** |
| 進階版 (Combo < 50 也快轉) | ~700 次快轉 + ~100 次正常 | **3-5x** |

### 6.4 實際測試方法

```csharp
// Benchmark 代碼
var stopwatch = Stopwatch.StartNew();

for (int i = 0; i < 100000; i++)
{
    simulator.Run(deck, centerCardId);
}

stopwatch.Stop();
Console.WriteLine($"Time: {stopwatch.ElapsedMilliseconds}ms");
```

---

## 7. MVP 實作檢查清單

### 7.1 程式碼修改

#### Phase 1: 準備工作
- [ ] **LiveStatus.cs**: 將 `_prevAp` 和 `_prevNoteScore` 改為 `internal`
  ```csharp
  internal double _prevAp = 0.0;
  internal int _prevNoteScore = 0;
  ```

#### Phase 2: 核心實作
- [ ] **Simulator.cs**: 在主迴圈開始前聲明快轉變數
  ```csharp
  double cachedApGain = 0;
  int cachedNoteScore = 0;
  ```

- [ ] **Simulator.cs**: 在主迴圈中計算快轉條件
  ```csharp
  // 檢查是否可以快轉
  bool canFastForward =
      Player.Combo >= 50 &&
      afkMental == 0 &&
      (!Player.CDAvailable || Player.Ap < cardNow.Cost);
  ```

- [ ] **Simulator.cs**: 計算 `safeHorizon`
  ```csharp
  if (canFastForward)
  {
      // 計算終點 1 和 2
      // safeHorizon = Math.Min(...)
  }
  ```

- [ ] **Simulator.cs**: 實作快轉迴圈
  ```csharp
  if (canFastForward && safeHorizon > chartEvents[i_event].Time)
  {
      // 快轉處理
  }
  ```

### 7.2 測試驗證

#### 功能測試
- [ ] **正確性測試**: 對比優化前後的 `Player.Score` (誤差應為 0)
  - [ ] 無背水卡的卡組
  - [ ] 有背水卡的卡組 (應該不進入快轉)
  - [ ] Combo < 50 時不快轉
  - [ ] Combo >= 50 時正常快轉

#### 效能測試
- [ ] **Benchmark**: 單次模擬執行時間
  - [ ] 短歌 (1 分鐘, ~250 notes)
  - [ ] 中歌 (2 分鐘, ~500 notes)
  - [ ] 長歌 (3+ 分鐘, ~800 notes)

- [ ] **壓力測試**: 執行 100,000 次模擬
  - [ ] 記憶體使用量穩定
  - [ ] 無記憶體洩漏
  - [ ] 平均速度提升 2-3x

#### 邊界測試
- [ ] **邊界情況**:
  - [ ] Combo 正好 = 50
  - [ ] AP 正好 = Cost
  - [ ] CD 正好在兩個 Note 之間轉好
  - [ ] 歌曲最後一個 Note
  - [ ] 只有 1 張卡的卡組
  - [ ] Cost = 0 的卡片

#### 事件處理測試
- [ ] **特殊事件**:
  - [ ] FeverStart 事件正確觸發
  - [ ] FeverEnd 事件正確觸發
  - [ ] LiveEnd 事件正確觸發
  - [ ] Voltage 升級時分數正確

### 7.3 回歸測試

- [ ] 所有現有測試案例通過
- [ ] DeathNote (背水卡) 邏輯不受影響
- [ ] 花火吟延遲 MISS 邏輯不受影響 (不應進入快轉)
- [ ] Friend Card 邏輯不受影響
- [ ] 除外卡片 (IsExcept) 邏輯不受影響

---

## 8. 完整程式碼範例

### 8.1 LiveStatus.cs 修改

```csharp
// LiveStatus.cs
public class LiveStatus(int masterLv = 50)
{
    // ... 其他屬性 ...

    // ✅ 改為 internal 以供 Simulator 存取
    internal double _prevAp = 0.0;
    internal int _prevNoteScore = 0;

    // ... 其他代碼不變 ...
}
```

### 8.2 Simulator.cs 主迴圈修改

```csharp
// Simulator.cs Run 方法

int i_event = 0;
Card cardNow = d.TopCard;

// ✅ 新增: 快轉用的緩存變數
double cachedApGain = 0;
int cachedNoteScore = 0;

// ... (TryUseSkill, RecalculateAfkMental 等內聯函數) ...

while (i_event < chartEvents.Length)
{
    // === 處理 Extra Events (優先級最高) ===
    double nextChartTime = chartEvents[i_event].Time;
    double nextExtraTime = (extraEvents.Count > 0)
        ? extraEvents.Peek().Time
        : double.MaxValue;

    if (nextExtraTime <= nextChartTime)
    {
        // 處理 extraEvent
        RuntimeEvent currentEvent = extraEvents.Dequeue();

        switch (currentEvent.Type)
        {
            case LiveEventType.CDavailable:
                Player.CDAvailable = true;
                TryUseSkill(currentEvent.Time);
                break;
            // ... 其他 extraEvent 處理 ...
        }
        continue;
    }

    // === ✅ 新增: 快轉邏輯 ===
    bool canFastForward =
        Player.Combo >= 50 &&
        afkMental == 0 &&
        cardNow != null &&
        (!Player.CDAvailable || Player.Ap < cardNow.Cost);

    if (canFastForward)
    {
        // 更新緩存值
        cachedApGain = Player._prevAp;
        cachedNoteScore = Player._prevNoteScore;

        // 計算終點 1: CD 轉好時刻
        double nextCDTime = Player.CDAvailable
            ? double.MaxValue
            : nextExtraTime;

        // 計算終點 2: AP 足夠時刻
        double nextAPTime = double.MaxValue;
        if (Player.CDAvailable && Player.Ap < cardNow.Cost)
        {
            double apDeficit = cardNow.Cost - Player.Ap;
            if (cachedApGain > 0)
            {
                int notesNeeded = (int)Math.Ceiling(apDeficit / cachedApGain);
                if (i_event + notesNeeded < chartEvents.Length)
                {
                    nextAPTime = chartEvents[i_event + notesNeeded].Time;
                }
            }
        }

        double safeHorizon = Math.Min(nextCDTime, nextAPTime);

        // 快轉迴圈
        int fastForwardCount = 0;  // Debug 用
        while (i_event < chartEvents.Length)
        {
            ref readonly var currentEvent = ref chartEvents[i_event];

            // 停止條件 1: 超出安全時間
            if (currentEvent.Time >= safeHorizon) break;

            // 停止條件 2: 遇到特殊事件
            if (currentEvent.Type > LiveEventType.Trace) break;

            // === 快速處理 Note ===
            Player.Combo++;
            Player.Ap += cachedApGain;
            Player.Score += cachedNoteScore;

            i_event++;
            fastForwardCount++;
        }

        if (DebugMode && fastForwardCount > 0)
        {
            Console.WriteLine($"[FastForward] Skipped {fastForwardCount} notes, " +
                            $"AP: {Player.Ap:F2}, Combo: {Player.Combo}");
        }
    }

    // === 正常處理當前事件 ===
    if (i_event >= chartEvents.Length) break;

    RuntimeEvent currentEvent = chartEvents[i_event];
    i_event++;

    switch (currentEvent.Type)
    {
        case LiveEventType.Single:
        case LiveEventType.Hold:
        case LiveEventType.HoldMid:
        case LiveEventType.Flick:
        case LiveEventType.Trace:
            // ... 原有邏輯不變 ...
            if (afkMental != 0 && Player.Mental.Rate > afkMental)
            {
                // 背水卡邏輯
            }
            else
            {
                Player.ComboAdd("PERFECT+");
            }

            if (Player.CDAvailable)
            {
                TryUseSkill(currentEvent.Time);
            }
            break;

        // ... 其他事件處理不變 ...
    }
}
```

---

## 9. 未來擴展方向

### 9.1 階段 2: 放寬 Combo 限制

**目標**: Combo < 50 時也能快轉

**挑戰**: ApRate 動態變化 (每 10 combo 增加 0.1)

**解決方案**:
```csharp
// 預測 Combo 變化對 AP 的影響
double predictAPAfterNotes(int startCombo, int noteCount)
{
    double totalAp = 0;
    for (int i = 0; i < noteCount; i++)
    {
        int combo = startCombo + i;
        if (combo <= 50)
        {
            double apRate = 1.0 + (combo / 10) / 10.0;
            totalAp += Math.Ceiling(_fullApPlus * apRate) / 10000.0;
        }
        else
        {
            totalAp += _prevAp;  // 固定值
        }
    }
    return totalAp;
}
```

### 9.2 階段 3: 支援背水卡快轉

**目標**: `afkMental != 0` 時也能快轉

**挑戰**: 需要實時追蹤 HP 變化

**解決方案**:
- 預測快轉期間 HP 是否會觸發背水閾值
- 如果會觸發,計算觸發時刻並縮短 `safeHorizon`

### 9.3 階段 4: 跨 CD 週期快轉

**目標**: 預測 CD 轉好後仍然 AP 不足,直接快轉到 AP 足夠

**範例**:
```
當前: CD 還有 1 秒, AP = 0.5, Cost = 5
預測: CD 轉好時 AP ≈ 1.0, 仍不足
優化: 直接快轉到 AP 累積到 5.0 的時刻 (約 50 個 Note 後)
```

**實作**:
```csharp
// 預測 CD 轉好時的 AP
double apAtCDReady = Player.Ap + (notesUntilCD * cachedApGain);

if (apAtCDReady >= cardNow.Cost)
{
    // CD 轉好時就夠了
    safeHorizon = nextCDTime;
}
else
{
    // CD 轉好後還需要更多 Note
    double remainingDeficit = cardNow.Cost - apAtCDReady;
    int moreNotesNeeded = (int)Math.Ceiling(remainingDeficit / cachedApGain);
    safeHorizon = chartEvents[indexAtCDReady + moreNotesNeeded].Time;
}
```

---

## 10. 已知限制與風險

### 10.1 Voltage 變化導致分數誤差

**情況**: 快轉期間 Voltage 升級

**影響**: 少數 Note 使用舊的 `_prevNoteScore`

**誤差**: < 0.01% 總分

**緩解**: 下次正常處理 Note 時會自動修正

### 10.2 技能效果改變 ApRate

**情況**: 某些技能可能影響 `ApGainRate` (技能 223)

**風險**: 快轉期間使用錯誤的 `cachedApGain`

**解決方案**:
- MVP 版本假設快轉期間不使用技能 (CD 未轉好)
- 未來版本需要檢測 `ApGainRate` 變化

### 10.3 除外卡片導致卡組變化

**情況**: 技能效果除外卡片 (如「除外笑顏」)

**風險**: `cardNow.Cost` 可能改變

**解決方案**:
- 技能發動時會呼叫 `RecalculateAfkMental()`
- 同時更新 `cardNow = d.TopCard`
- 下次快轉時會使用新的 `cardNow.Cost`

---

## 11. 參考資料

### 11.1 理論基礎
- **Discrete Event Simulation (DES)**: [Wikipedia](https://en.wikipedia.org/wiki/Discrete-event_simulation)
- **Event Skipping**: 跳過中間狀態,直接模擬到下一個關鍵事件
- **Time-Stepped vs Event-Driven Simulation**: [Comparison](https://www.sciencedirect.com/topics/computer-science/discrete-event-simulation)

### 11.2 相關檔案
- `Simulator.cs`: 主模擬器邏輯 (Line 213-410 主迴圈)
- `LiveStatus.cs`: 玩家狀態管理 (Line 107-159 ComboAdd)
- `Deck.cs`: 卡組管理

### 11.3 關鍵程式碼位置
- 主迴圈: `Simulator.cs:213-410`
- 技能發動: `Simulator.cs:177-211 (TryUseSkill)`
- AP 累積: `LiveStatus.cs:107-159 (ComboAdd)`
- CD 管理: `Simulator.cs:301-304 (CDavailable)`
- Note 分數: `LiveStatus.cs:87-105 (ScoreNote)`

---

## 附錄 A: 完整範例情境

### 範例 1: CD 未轉好的快轉

```
初始狀態:
- Time: 45.0s
- Combo: 60
- AP: 8.5
- CD: 還有 2.0 秒轉好 (47.0s)
- cardNow.Cost: 3

計算:
1. canFastForward = true (Combo >= 50, afkMental = 0, CD 未轉好)
2. cachedApGain = 0.09
3. nextCDTime = 47.0s
4. nextAPTime = MaxValue (CD 還沒好,不需要算)
5. safeHorizon = 47.0s

快轉:
- 處理 45.0s ~ 47.0s 之間的所有 Note
- 約 13 個 Note (每個間隔 0.15s)
- Combo: 60 → 73
- AP: 8.5 → 9.67

結果:
- 在 47.0s 停止快轉
- extraEvents 中的 CDavailable 事件觸發
- Player.CDAvailable = true
- TryUseSkill() → 發動技能 (AP 9.67 >= Cost 3)
```

### 範例 2: CD 已轉好,AP 不足的快轉

```
初始狀態:
- Time: 60.0s
- Combo: 80
- AP: 1.2
- CDAvailable: true
- cardNow.Cost: 3

計算:
1. canFastForward = true (Combo >= 50, afkMental = 0, AP < Cost)
2. cachedApGain = 0.09
3. nextCDTime = MaxValue (CD 已經好了)
4. apDeficit = 3.0 - 1.2 = 1.8
5. notesNeeded = ceil(1.8 / 0.09) = 20
6. nextAPTime = chartEvents[i_event + 20].Time ≈ 63.0s
7. safeHorizon = 63.0s

快轉:
- 處理 20 個 Note
- Combo: 80 → 100
- AP: 1.2 → 3.0

結果:
- 在第 20 個 Note 停止快轉
- 正常處理該 Note (Player.ComboAdd)
- TryUseSkill() → 發動技能 (AP 3.0 >= Cost 3)
```

### 範例 3: Fever 事件中斷快轉

```
初始狀態:
- Time: 90.0s
- Combo: 120
- AP: 2.0
- CDAvailable: false
- nextCDTime = 92.0s
- FeverStart 事件在 91.0s

計算:
1. safeHorizon = 92.0s (以為可以快轉到 CD 轉好)

快轉:
- 開始處理 Note
- 在 i_event = 某個位置時,遇到 chartEvents[i_event].Type = FeverStart
- if (currentEvent.Type > LiveEventType.Trace) break; ✅ 跳出快轉

結果:
- 快轉提前結束
- 正常處理 FeverStart 事件
- Voltage.SetFever(true)
- Center Skill 觸發
```

---

## 附錄 B: Debug 輸出範例

啟用 `DebugMode = true` 時的輸出:

```
[Simulator] Initial afkMental: 0
[Event] Single at 5.123s (Combo: 33)
[Event] Hold at 5.456s (Combo: 34)
[FastForward] Skipped 28 notes, AP: 3.45, Combo: 62
[Skill] [μ's 高坂穗乃果] SP絕技 at 10.234s (AP: 3.45, Combo: 62)
当前属性:
  AP: 0.45  Combo: 62  AP Gain Rate: 1.50x  Mental: 120/120 (100.00%)
  Score: 125340  Voltage: Lv2 (1.15x)  分加成: [1.2]  电加成: [1.1]
[FastForward] Skipped 31 notes, AP: 3.24, Combo: 93
[Skill] [Aqours 渡邊曜] AP獲得 at 15.678s (AP: 3.24, Combo: 93)
...
Final Score: 1523400
打出記錄: ['高坂穗乃果 SP絕技', '渡邊曜 AP獲得', ...]
打出次數: 42
```

---

## 附錄 C: 效能測試腳本

```csharp
using System.Diagnostics;

public class SimulatorBenchmark
{
    public static void RunBenchmark()
    {
        var simulator = new Simulator("music_001", "expert", 50);
        var deck = /* 構建測試卡組 */;
        int centerCardId = 1234567;

        // 熱身
        for (int i = 0; i < 100; i++)
        {
            simulator.Run(deck, centerCardId);
        }

        // 正式測試
        var stopwatch = Stopwatch.StartNew();
        int iterations = 100000;

        for (int i = 0; i < iterations; i++)
        {
            simulator.Run(deck, centerCardId);
        }

        stopwatch.Stop();

        double avgTime = (double)stopwatch.ElapsedMilliseconds / iterations;
        Console.WriteLine($"平均每次模擬: {avgTime:F3} ms");
        Console.WriteLine($"每秒模擬次數: {1000.0 / avgTime:F0}");
    }
}
```

---

## 附錄 D: 關鍵詞索引

- **Fast-Forward**: 快轉
- **Time Jumping**: 時間跳躍
- **Event Skipping**: 事件跳過
- **Next-Event Prediction**: 下一事件預測
- **Discrete Event Simulation (DES)**: 離散事件模擬
- **Safe Horizon**: 安全終點/地平線
- **MVP (Minimum Viable Product)**: 最小可行產品
- **Cached Values**: 緩存數值 (`cachedApGain`, `cachedNoteScore`)
- **Inline Processing**: 內聯處理 (避免函數呼叫開銷)
- **ApRate Stability**: ApRate 穩定性 (Combo >= 50)
- **Background Card (背水卡)**: afkMental 機制
- **Hanabi Ginko (花火吟)**: 延遲 MISS 機制
- **ExtraEvents**: 動態事件佇列 (CD, Delayed MISS 等)
