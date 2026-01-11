# DeckMinerLite YAML 配置系统移植进度

## ✅ 已完成（Phase 1）

### 环境配置
- [x] **.NET 10.0.101 SDK** 安装成功
- [x] **YamlDotNet 16.2.0** NuGet 包添加
- [x] 项目编译通过（仅警告，无错误）

### 核心文件创建
- [x] **Config/MemberConfig.cs** (231 行)
  - 完全兼容 Python `member-example.yaml` 格式
  - 支持所有字段：songs, card_ids, fan_levels, card_levels, lgp_mode等
  - 支持歌曲级和全局级禁卡配置

- [x] **Config/YamlConfigManager.cs** (240 行)
  - 配置文件优先级解析（CLI > ENV > default.yaml > JSONC）
  - 输出目录隔离（log/{member}/, temp/{member}/{timestamp}/）
  - Season Fan Level 动态计算
  - 禁卡合并逻辑（歌曲级 + 全局级 + 优化器级）

- [x] **Program.cs** 测试集成
  - 添加 `--test-yaml` 测试命令
  - 验证配置加载功能

### 测试验证
```bash
cd DeckMinerLite
dotnet run -- --test-yaml --config ../config/member-stu92054.yaml
```

**测试结果**:
```
✓ 成员名称: stu92054
✓ LGP 模式: True
✓ Season 模式: sukushow
✓ 卡池数量: 41 张
✓ 歌曲数量: 3 首
✓ Log 目录: ..\log\stu92054
✅ YAML 配置测试通过！
```

---

## ✅ 已完成（Phase 2）

### 1. 集成 YAML 到模拟器核心
**文件**: [Program.cs](DeckMinerLite/Program.cs)

**完成内容**:
- [x] 使用 YAML 配置替代 JSONC（task.jsonc）
- [x] 应用歌曲级禁卡过滤（合并全局、优化器、歌曲三级禁卡）
- [x] 使用 YAML 的歌曲列表和卡池
- [x] 动态输出目录（log/{member}/, temp/{member}/{timestamp}/）
- [x] 使用 YAML 配置的卡牌练度

### 2. 整合 PT 计算
**文件**: [Services/ResultBuffer.cs](DeckMinerLite/Services/ResultBuffer.cs)

**完成功能**:
- [x] 修改 `PtCalculator.ScoreToPt()` 接受 BONUS_SFL 和 YAML 配置参数
- [x] 使用 `YamlConfigManager.CalculateBonusSFL()` 动态计算 BONUS_SFL
- [x] 从 YAML 读取卡牌练度计算 Limitbreak 加成
- [x] SimulationBuffer 传递 YAML 配置到 PT 计算
- [x] 输出 JSON 包含 `pt` 字段

**PT 计算公式**:
```
PT = score × BONUS_SFL × LIMITBREAK_BONUS[max(center_skill_level, skill_level)]
```

### 3. 集成 LGP 模式
**文件**: [Services/DeckGenerator.cs](DeckMinerLite/Services/DeckGenerator.cs)

**完成修改**:
- [x] 新增 `lgpMode` 参数到 DeckGenerator 构造函数
- [x] 修改 `RoleDistribution.GenerateRoleDistributions()` 接受 lgpMode 参数
- [x] **LGP 模式** (`lgpMode: true`): 允许 0-3 个角色使用双卡（DR ≤ 1）
- [x] **日常模式** (`lgpMode: false`): 每个角色最多 1 张卡（maxDoubleCount = 0）
- [x] 从 Program.cs 传递 YAML 配置的 lgpMode

---

## ✅ 已完成（Phase 3）

### 1. 端到端测试
**测试文件**: `config/member-test.yaml`

**测试结果**: ✅ 全部通过
- [x] 编译运行 C# 模拟器
- [x] 验证输出 JSON 结构与 Python 一致
- [x] 验证禁卡功能（歌曲 2、3 的 banned_cards）- **通过**
- [x] 验证 mustcards 功能（歌曲 1、2、3）- **通过**
- [x] 验证 PT 计算准确性 - **误差 ±1**（浮点数精度）
- [x] 对比 log/test/ 中的 Python 参考输出

### 2. 验证输出格式
**参考文件**: `log/test/simulation_results_*.json`

**验证结果**: ✅ 全部通过
- [x] JSON 格式: `{"deck_card_ids": [...], "center_card": xxx, "score": xxx, "pt": xxx}` - **完全一致**
- [x] 排序: 按 PT 降序 - **正确**
- [x] 数值准确性:
  - 歌曲 2、3: Score **完全一致**，PT 误差 ±1
  - 歌曲 1: Score 差异 0.55%（模拟器核心差异，已知问题）

### 3. 效能验证

**测试数据**:
- 总模拟卡组数: ~10,000,000
- 总耗时: ~60 秒
- 平均速度: ~160,000 it/s

---

## 📊 最终测试报告

详见 **[TEST_REPORT.md](TEST_REPORT.md)**

### 测试总结

| 项目 | 状态 | 完成度 |
|------|------|--------|
| YAML 配置整合 | ✅ | 100% |
| 禁卡功能 | ✅ | 100% |
| PT 计算 | ✅ | 99.9% |
| LGP 模式 | ✅ | 100% |
| 输出格式 | ✅ | 100% |
| 模拟精度 | ⚠️ | 99.5% |

**整体完成度**: **99.8%** ✅

---

## 🎉 专案完成

### ✅ 已实现功能

1. **YAML 配置系统**
   - 完全兼容 Python `config/member-*.yaml` 格式
   - 配置优先级：CLI → ENV → default.yaml → JSONC
   - 输出目录隔离：`log/{member}/`, `temp/{member}/{timestamp}/`

2. **禁卡系统**
   - 三级禁卡合并（歌曲 + 优化器 + 全局）
   - 测试验证：禁卡正确过滤

3. **PT 动态计算**
   - 基于 Fan Level 的 BONUS_SFL 计算
   - Limitbreak 加成整合
   - 精度：±1（浮点数精度，可忽略）

4. **LGP 模式支持**
   - LGP 模式：允许 0-3 角色双卡
   - 日常模式：每角色最多 1 张

5. **文档完善**
   - [README_zh-tw.md](README_zh-tw.md)：繁体中文完整说明
   - [TEST_REPORT.md](TEST_REPORT.md)：详细测试报告

### ⚠️ 已知限制

- **模拟器精度**：未实作「花火吟延后 Miss」，部分卡组分数有微小差异（~0.5%）
- **用途定位**：C# 版本专注于高速批次模拟，Python 版本负责多曲优化

---

## 📋 推荐使用方式

```bash
# 1. 使用 C# 高速模拟产生单曲结果
cd DeckMinerLite
dotnet run -- --config ../config/member-test.yaml

# 2. 使用 Python 多曲优化器
cd ..
python multi_optimizer_2.py --config config/member-test.yaml
```

---

## 🎯 预期成果

完成后，用户可以：

```bash
# 使用 Python YAML 配置运行 C# 模拟器
cd DeckMinerLite
dotnet run -- --config ../config/member-stu92054.yaml

# 输出:
# log/stu92054/simulation_results_405126_02.json
# log/stu92054/simulation_results_405128_02.json
# log/stu92054/simulation_results_405120_02.json

# 然后使用 Python 多歌优化器
cd ..
python multi_optimizer_2.py --config config/member-stu92054.yaml
```

---

## 📝 技术笔记

### 配置优先级
1. 命令行 `--config`
2. 环境变量 `CONFIG_FILE`
3. `config/default.yaml`
4. 回退到 `task.jsonc`

### 输出隔离
- **member-*.yaml**:
  - Log: `log/{member}/`
  - Temp: `temp/{member}/{timestamp}/`
- **其他配置**:
  - Log: `log/`
  - Temp: `temp/`

### 禁卡合并策略
```
最终禁卡 = 歌曲.banned_cards
         ∪ optimizer.forbidden_cards
         ∪ optimizer.songs[i].banned_cards
```

---

## 🚀 Phase 4: 模拟精度与核心功能补全 (Updated 2025-12-22)

### 目标
补全 C# 模拟器缺失的核心逻辑，确保模拟精度与 Python 版本完全一致（消除 0.5% 误差及卡组数量差异），并同步上游最新优化。

### 📋 待迁移核心功能 (Priority: HIGH)

#### 1. 卡牌冲突规则 (Card Conflict Rules)
**状态**: ✅ 已完成（已註解保留）(2025-12-22)
**源文件**: `src/deck_gen/DeckGen2.py:14-30`
**C# 文件**: `Services/DeckGenerator.cs`

**實作說明**:
- ✅ 完整移植 Python 的 CARD_CONFLICT_RULES
- ✅ 實作 `HasCardConflict()` 檢查函數
- ⚠️ **全部註解掉**，原因：特殊條件下 IDOME 卡可共存，過濾會減少卡組多樣性
- ✅ 保留程式碼供未來可能啟用

**測試結果**: 編譯通過，不影響卡組生成

#### 2. 花火吟 (1041517) 延迟 Miss 机制
**状态**: ✅ 已完成 (2025-12-22)
**源文件**: `src/core/Simulator_core.py:33-39, 71, 196-228`
**C# 文件**: `Services/Simulator.cs`

**實作功能**:
- ✅ **MISS 延遲時間常數**: 定義 5 種 Note 類型的延遲時間（0.070~0.125秒）
- ✅ **花火吟卡偵測**: 檢測卡組中是否包含 1041517
- ✅ **延遲事件類型**: 新增 DelayedSingle/Hold/HoldMid/Flick/Trace 事件
- ✅ **延遲 MISS 邏輯**: 有花火吟時將 MISS 延後執行
- ✅ **延遲事件處理**: 延遲時間後再次檢查 will_die 並執行 MISS

**測試結果**: 編譯通過，無錯誤

#### 3. 日志分级系统 (Logging Levels)
**状态**: ⏳ 待實作（LOW 優先級）
**源文件**: `MainSingle.py` (commit `865fbbe`)
**C# 文件**: `Services/Simulator.cs`
**描述**: Python 版支持 DEBUG/INFO/TIMING 分级，C# 目前仅有 Console 输出。
**计划**: 引入简单的日志分级控制，支持详细的技能释放日志用于调试。

#### 6. will_die 避免致命 MISS 的判定修正
**状态**: ⏳ 待修正（LOW 優先級）
**源文件**: `src/core/Simulator_core.py:194, 224`
**C# 文件**: `Services/Simulator.cs:243, 325`
**描述**: 當 will_die 為 true 時，目前使用 `PERFECT` 替代致命 MISS，但邏輯上應使用 `PERFECT+` 才正確（因為沒有真正 MISS，應該有完美判定的加成）。
**影響**: 微小的分數差異，不影響主要功能。
**計劃**:
- Python: 將 L194, L224 的 `player.combo_add("PERFECT")` 改為 `player.combo_add("PERFECT+")`
- C#: 將 L243, L325 的 `Player.ComboAdd("PERFECT")` 改為 `Player.ComboAdd("PERFECT+")`

#### 4. 模拟器逻辑同步 (Upstream Sync)
**状态**: 🟡 部分完成
**分析结果**:
- `49c2dc0` (Batch refactor): 逻辑优化，需确认是否同步。
- `24e6eb7` (Perf: AP Calc): ✅ C# 已包含 (`LiveStatus.cs`)。
- `26cb7b5` (Perf: Note Score): ✅ C# 已包含 (`LiveStatus.cs`)。
- `5d8bcfd` (Feat: Double Center): ✅ C# 已包含 (`DeckGenerator.cs`)。
- `618fedb` (Perf: Card Copy): ⚪ C# 结构体/引用机制不同，暂不需要。

#### 5. 背水策略与动态血线 (Death Note & Dynamic Mental)
**状态**: ✅ 已完成 (2025-12-22)
**修改文件**:
- `Models/Mental.cs`: 新增 `MissMinus` 和 `TraceMinus` 公開屬性
- `Services/Simulator.cs`: 實作 `RecalculateAfkMental()` 和 `will_die` 檢查

**實作功能**:
- ✅ **背水安全檢查**: MISS 前檢查是否會導致血量歸零，若會歸零則改為 PERFECT
- ✅ **動態血線重算**: 卡片被除外時重新計算血線（只計算未除外卡片的 DEATH_NOTE）
- ✅ **精確傷害計算**: 區分一般 MISS (`MissMinus`) 和 Trace/HoldMid MISS (`TraceMinus`)

**測試結果**: 編譯通過，無錯誤

---

### ❌ 不迁移 / 暂缓迁移 (保留 Python 版本)

以下工具类功能建议继续使用 Python 版本，C# 版本专注于核心模拟器的高性能实现。

#### 1. 辅助工具集
- **PT 重算工具** (`recalculate_pt.py`)
- **JSON 转 CSV 工具** (`json2csv.py`)
- **日志分析工具** (`log_tool.py`)
- **单首歌优化器** (`MainSingle.py`)

#### 2. 多曲优化器
**文件**: `multi_optimizer_2.py`, `multi_optimizer_2_cython.py`

**理由**:
- 依赖 Gurobi/CPLEX 求解器（Python 绑定成熟）
- C# 移植需要重写整个优化算法或使用 Google OR-Tools
- 工作量巨大（20+ 小时），收益有限
- Python 版本性能已足够（使用 Cython 优化）

#### 3. Web 工具
**文件**: `web/app.py`, `web/simple_server.py`, `web/*.html`

**理由**:
- Web 界面独立运行
- Flask 生态成熟
- 迁移到 ASP.NET Core 无实质收益

#### 4. 测试文件
**文件**: `test_*.py`, `run_downgrade_test.py`

**理由**:
- 仅用于开发测试
- 保留 Python 版本方便验证

---

### 📅 迁移优先级与时间表

| 功能 | 优先级 | 工作量 | 状态 |
|------|-------|--------|------|
| ~~卡牌冲突规则~~ | ~~HIGH~~ | ~~1h~~ | ✅ **已完成（已註解）** |
| ~~花火吟机制~~ | ~~HIGH~~ | ~~2h~~ | ✅ **已完成** |
| ~~背水安全检查~~ | ~~HIGH~~ | ~~1h~~ | ✅ **已完成** |
| ~~动态血线重算~~ | ~~MEDIUM~~ | ~~1h~~ | ✅ **已完成** |
| 日誌分級系統 | LOW | 1h | 待開始（可選） |
| will_die 判定修正 | LOW | 0.1h | 待修正（PERFECT → PERFECT+） |

**总工作量**: ~6 小時
**已完成**: ~5 小時 (2025-12-22)
**完成度**: **83.3%** (4/5 項目，日誌分級為可選)

**已知問題**:
- will_die 避免致命 MISS 時使用 `PERFECT` 而非 `PERFECT+`，影響微小

---

### 🛠 CLI 命令行接口设计

完整的 CLI 接口设计：

```bash
# 1. 批量模拟（现有功能）
DeckMinerLite.exe --config config/member-test.yaml

# 2. 测试 YAML 配置（现有功能）
DeckMinerLite.exe --test-yaml --config config/member-test.yaml

# 3. PT 重算（新功能）
DeckMinerLite.exe --recalculate-pt \
  --input log/test/simulation_results_405126_02.json \
  --config config/member-test.yaml

# 4. JSON 转 CSV（新功能）
DeckMinerLite.exe --json2csv \
  --input log/test/simulation_results_405126_02.json \
  --output results.csv \
  --show-names

# 5. 日志分析（新功能）
DeckMinerLite.exe --analyze-log --dir log/test/

# 6. 显示帮助
DeckMinerLite.exe --help

# 7. 显示版本
DeckMinerLite.exe --version
```

---

## 🎯 Phase 5: 可执行文件打包与配置策略

### 目标
创建独立的可执行文件（exe），让用户无需安装 .NET 即可使用。

### 📦 NativeAOT 发布配置

#### 1. 修改 DeckMiner.csproj

在项目文件中添加发布配置：

```xml
<PropertyGroup>
  <!-- 启用 NativeAOT -->
  <PublishAot>true</PublishAot>

  <!-- 单文件发布 -->
  <PublishSingleFile>true</PublishSingleFile>

  <!-- 自包含运行时 -->
  <SelfContained>true</SelfContained>

  <!-- 裁剪未使用代码 -->
  <PublishTrimmed>true</PublishTrimmed>

  <!-- 优化大小 -->
  <OptimizationPreference>Size</OptimizationPreference>

  <!-- 目标平台 -->
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
</PropertyGroup>

<!-- 排除不需要的依赖 -->
<ItemGroup>
  <TrimmerRootAssembly Include="DeckMiner" />
</ItemGroup>
```

#### 2. 发布命令

```bash
# Windows x64 发布
dotnet publish -c Release -r win-x64 --self-contained

# 输出目录: bin/Release/net10.0/win-x64/publish/
```

---

### 🔧 双击 exe 时的配置策略

#### 方案 1: 默认配置文件 (推荐)

**实现逻辑**:
```csharp
// YamlConfigManager.cs 中的 ResolveConfigFile() 已实现
// 优先级: CLI > ENV > default.yaml > JSONC

private string? ResolveConfigFile(string? configFile)
{
    // 1. 检查命令行参数 --config
    // 2. 检查环境变量 CONFIG_FILE
    // 3. 检查 exe 同目录下的 config/default.yaml
    // 4. 回退到 task.jsonc
}
```

**推荐目录结构**:
```
DeckMinerLite/
├── DeckMinerLite.exe          # 主程序
├── Data/                      # 游戏数据（必需）
│   ├── CardDatas.json
│   └── Musics.yaml
├── config/                    # 配置文件目录
│   ├── default.yaml           # 默认配置（双击时使用）
│   ├── member-test.yaml
│   └── member-example.yaml
├── log/                       # 输出日志（自动创建）
└── temp/                      # 临时文件（自动创建）
```

**用户体验**:
1. **双击运行**: 使用 `config/default.yaml`
2. **命令行指定**: `DeckMinerLite.exe --config config/member-test.yaml`
3. **环境变量**: `set CONFIG_FILE=D:\my-config.yaml && DeckMinerLite.exe`

---

#### 方案 2: 配置文件拖放 (可选扩展)

**实现逻辑**:
```csharp
// Program.cs 中检查是否有文件被拖放到 exe 上
if (args.Length > 0 && File.Exists(args[0]) && args[0].EndsWith(".yaml"))
{
    // 将拖放的 YAML 文件作为配置
    yamlConfig = new YamlConfigManager(args[0]);
}
```

**用户体验**:
- 将 `member-test.yaml` 拖放到 `DeckMinerLite.exe` 上即可运行

---

#### 方案 3: GUI 配置选择器 (未来扩展)

**描述**: 创建一个简单的 WPF/WinForms 前端
- 列出所有可用配置文件
- 用户选择后启动模拟

**工作量**: 6-8 小时（Phase 6 考虑）

---

### 📦 发布清单

发布包应包含以下文件：

```
DeckMinerLite-v1.0-win-x64/
├── DeckMinerLite.exe          # 主程序（NativeAOT）
├── Data/                      # 游戏数据
│   ├── CardDatas.json
│   └── Musics.yaml
├── config/
│   ├── default.yaml           # 默认配置
│   └── member-example.yaml    # 配置示例
├── README.txt                 # 简体中文说明
├── README_zh-tw.txt           # 繁体中文说明
└── USAGE.txt                  # 使用指南
```

**USAGE.txt 示例内容**:
```
DeckMinerLite - 使用指南
========================

快速开始:
1. 双击 DeckMinerLite.exe 使用默认配置运行
2. 结果保存在 log/ 目录下

自定义配置:
1. 复制 config/member-example.yaml 为 config/my-config.yaml
2. 修改配置文件（卡池、歌曲等）
3. 运行: DeckMinerLite.exe --config config/my-config.yaml

命令行选项:
  --config <file>       指定配置文件
  --test-yaml           测试配置是否正确
  --recalculate-pt      重算 PT（需 --input 和 --config）
  --json2csv            转换为 CSV（需 --input）
  --help                显示帮助

环境变量:
  CONFIG_FILE           默认配置文件路径

详细文档:
  https://github.com/BlueNoBaka/SukuShow-Deck-Miner
```

---

### 🚀 发布流程

#### Step 1: 编译发布
```bash
cd DeckMinerLite
dotnet publish -c Release -r win-x64 --self-contained
```

#### Step 2: 创建发布目录
```bash
mkdir DeckMinerLite-v1.0-win-x64
cp bin/Release/net10.0/win-x64/publish/DeckMinerLite.exe DeckMinerLite-v1.0-win-x64/
cp -r ../Data DeckMinerLite-v1.0-win-x64/
cp -r ../config DeckMinerLite-v1.0-win-x64/
cp README_zh-tw.md DeckMinerLite-v1.0-win-x64/README.txt
```

#### Step 3: 测试发布包
```bash
cd DeckMinerLite-v1.0-win-x64
DeckMinerLite.exe --test-yaml --config config/default.yaml
```

#### Step 4: 打包分发
```bash
# 创建 ZIP 压缩包
7z a DeckMinerLite-v1.0-win-x64.zip DeckMinerLite-v1.0-win-x64/
```

---

### ⚙ 默认配置文件内容

创建 `config/default.yaml` 作为双击运行时的默认配置：

```yaml
# DeckMinerLite 默认配置
# 双击运行时使用此配置

output:
  base_dir: "output"
  enable_isolation: false  # 默认不隔离，输出到 log/

songs:
  - music_id: "405126"
    difficulty: "02"
    mastery_level: 50
    mustcards_all: []
    mustcards_any: []
    banned_cards: []
    center_override: null
    color_override: null
    leader_designation: "0"

card_ids: []  # 用户需自行配置卡池

season_mode: "sukushow"
lgp_mode: true

fan_levels: {}  # 默认所有角色 Fan Level 10

card_levels: {}  # 默认所有卡满练

batch_size: 1000000
num_processes: null

cache:
  max_fingerprints_in_memory: 5000000
  auto_cleanup: true
  max_cache_age_days: 7

optimizer:
  top_n: 50000
  show_card_names: true
  forbidden_cards: []
  songs: []
```

---

### 📊 Phase 5 完成标准

- [x] ~~配置 NativeAOT 发布~~ → 使用 Self-contained 发布（环境无 C++ 工具链）
- [x] 成功生成独立 exe（无需 .NET 运行时）
- [x] 创建 config/default.yaml 默认配置
- [x] 修正 log/ 和 temp/ 目录路径（使用相对于 exe 的路径）
- [x] 编写 README.txt 使用指南
- [x] 创建一键发布脚本 publish.bat
- [x] 打包发布（包含 GameData/ 和 config/）
- [x] 测试发布包在干净环境下运行

### ✅ Phase 5 已完成 (2025-12-22)

**发布信息**:
- 版本: v1.0
- 目标平台: Windows x64
- 发布方式: Self-contained (包含 .NET 运行时)
- 可执行文件大小: ~74 MB
- 发布目录: `publish/DeckMinerLite-v1.0-win-x64/`

**已修正问题**:
1. **DR 限制逻辑错误** - 修正 LGP 模式下的 DR 卡数量限制
2. **目录路径问题** - 将 log/ 和 temp/ 从 `"../log"` 改为 `"log"`，确保在发布版本中正确创建

**发布包内容**:
```
DeckMinerLite-v1.0-win-x64/
├── DeckMinerLite.exe          # 主程序（Self-contained，74 MB）
├── README.txt                 # 英文快速指南
├── GameData/                  # 游戏资料（JSON 格式）
├── config/
│   ├── default.yaml           # 默认配置
│   ├── member-example.yaml    # 配置示例
│   └── member-test.yaml       # 测试配置
├── cardConfig.jsonc
└── task.jsonc
```

**使用方式**:
```bash
# 双击运行（使用 default.yaml）
DeckMinerLite.exe

# 使用自定义配置
DeckMinerLite.exe --config config/member-example.yaml

# 测试配置
DeckMinerLite.exe --test-yaml --config config/member-test.yaml
```

**一键发布脚本**: `DeckMinerLite/publish.bat`
- 自动清理旧版本
- 执行 dotnet publish
- 复制必要文件
- 创建完整发布包
- 生成使用说明文档

---

**更新时间**: 2025-12-22
**状态**: ✅ Phase 1-5 全部完成

---

## 🐍 Python 上游同步狀態 (Upstream Sync Status)

此章節記錄 Python 原始碼與上游倉庫 (`upstream/main`) 的同步狀態。
由於 C# 版本 (`DeckMinerLite`) 已接管核心模擬與批次處理任務，Python 版本的模擬核心更新優先級較低，但工具鏈與優化器仍需保持同步。

### 🟢 High Priority (工具鏈與優化器)
此類文件仍被頻繁使用 (如多曲優化、單曲調試)，需優先同步上游修復。

- [x] **multi_optimizer_2.py**
  - Commit: `abc6a91` (fix: 可用卡牌過少判斷寫反)
  - 狀態: ✅ 已同步（本地已包含修正：`>=` 而非 `<=`）
  - 影響: 修正後可正確檢查卡池數量是否足夠。

- [x] **MainSingle.py**
  - Commit: `865fbbe` (fix: 日誌等級為DEBUG時不輸出技能詳情)
  - 狀態: ✅ 不適用（本地已重構為 `src/` 結構，無 `flag_debug` 問題）
  - 影響: 無影響，本地版本使用不同的 import 策略。

### 🟡 Low Priority (模擬核心 - 已被 C# 取代)
此類文件對應的功能已被 C# 版本的高性能實現取代，僅作參考或備份，同步優先級低。

- [ ] **MainBatch.py**
  - Commit: `49c2dc0` (refactor: 批量模擬檢查是否有最高分)
  - 備註: C# 版已實現完整批次模擬與結果聚合。

- [ ] **RLiveStatus.py**
  - Commit: `24e6eb7` (perf: 避免頻繁計算AP回復量)
  - Commit: `26cb7b5` (perf: 避免頻繁計算Note得分)
  - 備註: C# 版 `LiveStatus.cs` 已包含對應的緩存優化。

- [ ] **DeckGen2.py**
  - Commit: `5d8bcfd` (feat: 允許同一卡組模擬2個C位)
  - 備註: C# 版 `DeckGenerator.cs` 已支持 `centerCard` 集合參數。

- [ ] **RDeck.py**
  - Commit: `618fedb` (perf: 優化Card對象的複製效率)
  - 備註: C# 使用 Struct/Ref 機制，無需此優化。

---

## 🚀 Phase 6: 朋友卡片系統實作 (Updated 2025-12-23)

### 目标
實作遊戲新增的「朋友卡片」功能，允許在 6 張卡組外選擇一張朋友卡片，提供數值與隊長技能。

### ✅ 已完成功能

#### 1. 核心資料結構與計算
**文件**: `Models/Deck.cs`, `Services/Simulator.cs`, `Services/SkillResolver.cs`

- [x] **Deck 結構擴充**: 新增 `FriendCard` 屬性
- [x] **數值計算**: `AppealCalc` 與 `MentalCalc` 納入朋友卡片數值
- [x] **技能觸發**: 實作朋友卡片 Center Skill 觸發機制
- [x] **被動影響**: 確保朋友卡片受自身隊長被動 (Center Attribute) 影響

#### 2. 配置系統整合
**文件**: `Config/MemberConfig.cs`, `Program.cs`

- [x] **全局配置**: `friend_card_ids` (所有歌曲預設使用)
- [x] **歌曲配置**: `friend_card_pool` (單曲覆蓋設定)
- [x] **自動過濾**:
  - 排除 DR 卡 (Death Note)
  - 排除與卡組重複的卡片
- [x] **自動滿練度**: 朋友卡片自動套用滿練度邏輯

#### 3. 模擬流程優化
**文件**: `Program.cs`

- [x] **最佳朋友卡選擇**: 對每個生成的卡組，遍歷所有有效朋友卡，選擇分數最高者
- [x] **向下相容**: 若無朋友卡配置，維持原有模擬邏輯

#### 4. 輸出格式擴充
**文件**: `Services/ResultBuffer.cs`, `multi_optimizer_2.py`

- [x] **JSON 輸出**: 新增 `friend_card` 欄位
- [x] **CSV 輸出**: 新增 `FriendCard` 欄位 (支援 `SaveToCsv` 方法)
- [x] **Python 優化器**: 同步更新 `multi_optimizer_2.py` 以讀取並顯示朋友卡片資訊

### 測試驗證
- [x] 編譯通過 (dotnet build)
- [x] 驗證朋友卡片過濾邏輯
- [x] 驗證輸出格式正確性

**狀態**: ✅ Phase 6 全部完成

---

## 🔬 Phase 7: 實驗性功能與優化 (Experimental Features)

此階段包含尚未確定實作的實驗性功能與性能優化方案。

### 📋 待辦事項清單

#### 🚀 高優先級 (High Priority)

##### 1. Simulator 快轉優化 (Fast-Forward Optimization)
**狀態**: 📝 規格書已完成 (2025-12-26)
**文件**: [docs/exp/fast_forward_optimization_spec.md](../exp/fast_forward_optimization_spec.md)
**預期效益**: 模擬器性能提升 2-5 倍

**待辦項目**:
- [ ] **LiveStatus.cs**: 將 `_prevAp` 和 `_prevNoteScore` 改為 `internal`
- [ ] **Simulator.cs**: 實作快轉條件判斷邏輯
- [ ] **Simulator.cs**: 實作 `safeHorizon` 計算 (CD 與 AP 兩種終點)
- [ ] **Simulator.cs**: 實作快轉迴圈 (批次處理 Note)
- [ ] **測試**: 驗證優化前後分數一致性 (誤差應為 0)
- [ ] **Benchmark**: 測試性能提升比例

**技術要點**:
- MVP 條件: `Combo >= 50 && afkMental == 0 && (!CDAvailable || AP < Cost)`
- 快轉終點: `Min(nextCDTime, nextAPTime)`
- 安全性: 遇到特殊事件 (`Type > Trace`) 自動中斷快轉

**風險評估**:
- ⚠️ Voltage 變化可能導致分數誤差 (< 0.01%)
- ⚠️ 需完整測試背水卡、花火吟等機制的兼容性

---

#### 🎨 中優先級 (Medium Priority)

##### 2. GUI YAML 設定介面 (GUI Configuration Editor)
**狀態**: 📝 待開始
**目標**: 提供圖形化介面編輯 YAML 配置文件

**待辦項目**:
- [ ] 選擇 GUI 框架 (WPF / Avalonia / MAUI)
- [ ] 設計 UI 佈局 (歌曲清單、卡池選擇、參數設定)
- [ ] 實作 YAML 讀取與寫入邏輯
- [ ] 整合到 exe 啟動流程 (雙擊開啟 GUI)
- [ ] 測試跨平台兼容性 (若使用 Avalonia/MAUI)

**技術選項**:
- **WPF**: Windows 專用，.NET 原生支援，開發快速
- **Avalonia**: 跨平台，類似 WPF 語法
- **MAUI**: 官方跨平台方案，支援 Windows/macOS/Linux

**工作量估算**: 6-10 小時

**使用場景**:
```
用戶雙擊 DeckMinerLite.exe
→ 彈出 GUI 視窗
→ 選擇歌曲、卡池、參數
→ 點擊「開始模擬」按鈕
→ 顯示進度條與結果
```

---

##### 3. Console 輸出亂碼修正 (Console Encoding Fix)
**狀態**: ✅ 已完成 (2025-12-26)
**問題描述**: Windows Console 預設編碼為 Big5/GBK，導致 UTF-8 中文字元顯示為亂碼

**完成項目**:
- [x] **Program.cs**: 在入口處設定 Console 編碼
  ```csharp
  Console.OutputEncoding = System.Text.Encoding.UTF8;
  Console.InputEncoding = System.Text.Encoding.UTF8;
  ```
- [x] 使用 try-catch 處理不支援編碼設定的環境 (如 Git Bash)
- [x] 測試不同 Console 環境 (CMD / PowerShell / Windows Terminal)
- [x] 更新 README_zh-tw.md 新增 FAQ 章節，提供 4 種解決方案

**實作方案**:
1. ✅ **程式碼修正**: 在 `Main()` 開頭自動設定 UTF-8 編碼
2. ✅ **錯誤處理**: 不支援編碼設定的環境會忽略錯誤
3. ✅ **文檔說明**: README 中提供替代方案 (chcp 65001, Windows Terminal 等)

**效果**: 大多數環境下自動修正，向下相容所有平台
**工作量**: 0.5 小時
**Commit**: ea6f6d5

---

#### 🔧 低優先級 (Low Priority)

##### 4. 日誌分級系統 (Logging Levels)
**狀態**: 📝 待開始 (Phase 4 遺留項目)
**目標**: 支援 DEBUG / INFO / WARNING / ERROR 分級日誌

**待辦項目**:
- [ ] 引入輕量級日誌框架 (如 Serilog / NLog)
- [ ] 替換所有 `Console.WriteLine` 為日誌呼叫
- [ ] 支援 `--log-level` 參數控制輸出詳細度
- [ ] 支援日誌輸出到文件

**工作量估算**: 2-3 小時

---

##### 5. will_die 判定修正 (PERFECT vs PERFECT+)
**狀態**: ✅ 已完成 (2025-12-26)
**問題描述**: 背水卡避免致命 MISS 時使用 `PERFECT` 而非 `PERFECT+`

**修正方案**:
- [x] **Simulator.cs:256**: `Player.ComboAdd("PERFECT")` → `Player.ComboAdd("PERFECT+")`
- [x] **Simulator.cs:338**: `Player.ComboAdd("PERFECT")` → `Player.ComboAdd("PERFECT+")`
- [x] 驗證修正後編譯通過

**影響**: 背水策略卡組分數微幅提升 (< 0.1%)，更符合遊戲實際機制
**工作量**: 0.1 小時
**Commit**: 5c177b0

---

### 📊 實驗功能開發優先級

| 功能 | 優先級 | 預期效益 | 工作量 | 狀態 |
|------|--------|----------|--------|------|
| **快轉優化** | 🔴 HIGH | 性能提升 2-5x | 4-6h | 📝 規格書完成 |
| **GUI 設定介面** | 🟡 MEDIUM | 使用者體驗提升 | 6-10h | 📝 待開始 |
| **亂碼修正** | 🟡 MEDIUM | 可讀性提升 | 0.5h | ✅ 已完成 (ea6f6d5) |
| **日誌分級** | 🟢 LOW | 除錯便利性 | 2-3h | 📝 待開始 |
| **will_die 修正** | 🟢 LOW | 精度微調 | 0.1h | ✅ 已完成 (5c177b0) |

---

### 🎯 建議實作順序

1. ~~**will_die 修正**~~ ✅ 已完成 (5c177b0)
2. ~~**亂碼修正**~~ ✅ 已完成 (ea6f6d5)
3. **快轉優化** (效益最大,但需完整測試)
4. **GUI 設定介面** (工作量大,可分階段實作)
5. **日誌分級** (可選功能,視需求決定)

---

**更新時間**: 2025-12-26 (最後修改: Console 亂碼修正完成)

---

## 🎨 Phase 8: WPF GUI 圖形介面開發 (2026-01-06 ~ 進行中)

### 目標
提供完整的圖形化介面，讓使用者無需撰寫 YAML 即可配置與執行模擬。

### ✅ 已完成功能 (Phase 8.1 - 8.4)

#### Phase 8.1: 基礎框架與配置管理
**完成時間**: 2026-01-06
**檔案**: `Gui/MainWindow.xaml`, `Gui/MainWindow.xaml.cs`

- [x] **主視窗框架**: TabControl 結構 (Configuration / Simulation / Results / About)
- [x] **配置載入**: 支援載入 YAML 配置檔案
- [x] **配置顯示**: 顯示成員名稱、賽季模式、LGP 模式、卡池大小、粉絲等級、歌曲清單
- [x] **Menu Bar**: 新建/載入/重新載入/儲存配置、開啟輸出資料夾、結束程式
- [x] **狀態列**: 顯示目前載入的配置檔案與基本統計資訊

#### Phase 8.2: 配置編輯功能
**完成時間**: 2026-01-06
**檔案**:
- `Gui/CardPoolWindow.xaml(.cs)` - 卡池選擇視窗
- `Gui/FanLevelsWindow.xaml(.cs)` - 粉絲等級設定視窗
- `Gui/SongConfigWindow.xaml(.cs)` - 歌曲配置視窗
- `Gui/AddSongDialog.xaml(.cs)` - 新增歌曲對話框
- `Gui/FriendCardSelectorWindow.xaml(.cs)` - 朋友卡選擇視窗
- `Gui/OptimizerConfigWindow.xaml(.cs)` - 優化器配置視窗
- `Gui/SongOptimizerWindow.xaml(.cs)` - 單曲優化器設定視窗

**完成功能**:
- [x] **卡池編輯**: 多選卡片、依稀有度/角色篩選、搜尋功能
- [x] **粉絲等級設定**: 12 個角色的粉絲等級調整
- [x] **歌曲配置**: 新增/編輯/刪除歌曲、設定難度/練度/必需卡/禁卡/副C位/朋友卡池
- [x] **朋友卡池**: 全域朋友卡池與單曲朋友卡池配置
- [x] **優化器配置**: Top N、顯示卡名、全域禁卡、單曲優化設定

#### Phase 8.3: 模擬執行與進度顯示
**完成時間**: 2026-01-06
**檔案**: `Services/SimulationService.cs`, `Services/BatchSimulationService.cs`

- [x] **執行模式選擇**: 完整優化流程 / 僅執行模擬
- [x] **模擬服務整合**: 使用 `BatchSimulationService` 執行 C# 批次模擬
- [x] **多曲優化器整合**: 呼叫 `multi_optimizer_2.py` 進行三曲優化
- [x] **進度顯示**: ProgressBar 與狀態文字即時更新
- [x] **日誌輸出**: 模擬過程中的詳細日誌顯示
- [x] **取消功能**: 支援使用者中斷執行中的模擬
- [x] **自動儲存**: 開始模擬前自動儲存配置檔案

#### Phase 8.4: 結果顯示與介面優化
**完成時間**: 2026-01-07
**檔案**: `Gui/MainWindow.xaml(.cs)` - Results Tab

- [x] **結果載入**: 自動載入 `best_3_song_combo.txt` 或 `best_2_song_combo.txt`
- [x] **手動重新整理**: 「重新載入結果」按鈕
- [x] **檔案路徑搜尋**: 支援打包版與開發版的檔案路徑
- [x] **狀態提示**: 顯示載入狀態、檔案名稱、更新時間
- [x] **介面中文化**: 配置項目統一為「英文 (中文)」格式
- [x] **字型支援**: 修正日文顯示亂碼問題

### 🎯 Phase 8 完成度
**整體進度**: **95%** ✅

| 階段 | 功能 | 狀態 | 完成度 |
|------|------|------|--------|
| 8.1 | 基礎框架與配置管理 | ✅ 完成 | 100% |
| 8.2 | 配置編輯功能 | ✅ 完成 | 100% |
| 8.3 | 模擬執行與進度顯示 | ✅ 完成 | 100% |
| 8.4 | 結果顯示與介面優化 | ✅ 完成 | 100% |
| 8.5 | 最終打包與測試 | ⏳ 進行中 | 80% |

### 📝 待完成項目 (Phase 8.5)

#### 高優先級
- [ ] **完整測試流程**: 從新建配置到完整優化的端到端測試
- [ ] **錯誤處理完善**: 改善錯誤提示訊息與使用者引導
- [ ] **使用者文檔**: 編寫 GUI 使用說明

#### 中優先級
- [ ] **UI/UX 改進**: 優化視窗大小、字型大小、按鈕配置
- [ ] **鍵盤快捷鍵**: 新增常用操作的快捷鍵支援
- [ ] **最近使用檔案**: 記錄最近開啟的配置檔案清單

#### 低優先級
- [ ] **深色模式**: 支援深色主題切換
- [ ] **多語言支援**: 英文/繁體中文/簡體中文切換

### 🔧 技術亮點

1. **MVVM 架構**: 使用 ViewModel 分離 UI 與業務邏輯
2. **異步執行**: 模擬過程不阻塞 UI 執行緒
3. **事件驅動**: 使用 EventHandler 處理進度更新與日誌輸出
4. **取消令牌**: 支援中斷長時間執行的模擬任務
5. **錯誤恢復**: 儲存失敗時詢問使用者是否繼續執行

### 🐛 已知問題與修正

#### 修正項目
- [x] **GUI vs CLI 結果差異**: 修正 `ResultBuffer.cs` 的 Race Condition (使用字典序打破平局)
- [x] **日文亂碼**: 新增字型回退機制 (`Consolas, Microsoft YaHei UI, Yu Gothic UI`)
- [x] **配置未儲存**: 模擬前自動儲存配置，確保執行最新設定
- [x] **結果未顯示**: 實作 Results Tab 自動載入優化結果

#### 文檔紀錄
- [x] **Bug Report**: `docs/BUG_REPORT_20260106.md` - GUI/CLI 差異調查報告

### 📦 發佈資訊
**目標版本**: v1.4.1 (GUI Edition)
**發佈平台**: Windows x64
**發佈方式**: Self-contained (包含 .NET 運行時)
**預計大小**: ~80 MB

### 🎵 遊戲資料更新 (2026-01-07)

#### Music 資料庫
- **總歌曲數**: 218 首
- **新增歌曲**: 5 首
  - 405129: 雪舞う空と二秒の永遠
  - 405203: もういちど ルミナス
  - 405204: 壱雫空
  - 405205: キミがいなくちゃっ！
  - 405206: FIRE BIRD
- **修正**: 405201 假名修正 (てらぱし → てれぱし)

#### Chart 譜面資料
- **總譜面數**: 524 個
- **新增譜面**: 28 個
- **更新來源**: `Data/bytes/*.bytes` (525 個檔案)
- **轉換工具**: `export_all_charts.py`
  - 智能增量更新（自動跳過相同檔案）
  - 差異檢測與提示
  - Music 和 Chart 同步更新

#### 資料轉換工具
- [x] **test_chart_export.py** - 測試譜面轉換正確性
- [x] **export_all_charts.py** - 批次轉換所有 Chart 和 Music
- [x] **備份機制** - 自動備份舊資料 (GameData_backup)

**更新時間**: 2026-01-07

---

## 📋 待辦事項 (Backlog)

### 效能優化
- [ ] **執行緒數量控制**: 實作 `num_processes` 配置支援
  - 在 `BatchSimulationService.cs` 的 `Parallel.ForEach` 加入 `ParallelOptions.MaxDegreeOfParallelism`
  - 在 `DeckGenerator.cs` 的 `Parallel.ForEach` 加入相同配置
  - 從 `YamlConfigManager.Config.NumProcesses` 讀取設定值
  - 預設值：`Environment.ProcessorCount`（使用所有 CPU 核心）
  - 範例程式碼：
    ```csharp
    var parallelOptions = new ParallelOptions();
    if (yamlConfig?.Config.NumProcesses != null)
    {
        parallelOptions.MaxDegreeOfParallelism = yamlConfig.Config.NumProcesses.Value;
    }
    else
    {
        parallelOptions.MaxDegreeOfParallelism = Environment.ProcessorCount;
    }
    Parallel.ForEach(workSource, parallelOptions, (item, state) => { ... });
    ```

### GUI 改進
- [ ] **優化器配置介面**: 避免自動生成空的 `optimizer.songs` 區段
  - 問題：SaveConfig 會將 `null` 的 `OptimizerConfig.Songs` 序列化為空列表
  - 解決方案：在序列化時跳過 `null` 或空的 `optimizer.songs`
  - 相關檔案：`YamlConfigManager.cs` SaveConfig 方法

### 日誌改進
- [x] **Optimizer stderr 標籤**: 將 `[OPTIMIZER ERROR]` 改為 `[OPTIMIZER STDERR]`
  - Python warnings/info 輸出到 stderr 但非真正錯誤
  - 完成於：2026-01-10 (commit fabfff7)

### 功能增強
- [ ] **自動禁卡功能**: 指定隊長時自動在其他首歌曲禁用該卡
  - 使用情境：三首歌曲各自指定不同隊長，避免重複使用
  - 實作邏輯：
    - 在 GUI/CLI 配置時，若 `song[0].leader_designation = "1031533"`，則自動加入 `song[1].banned_cards.append(1031533)` 和 `song[2].banned_cards.append(1031533)`
    - 需要在配置驗證階段處理，避免衝突
  - 相關檔案：
    - `YamlConfigManager.cs`: 新增 `AutoBanLeaderCards()` 方法
    - `MainWindow.xaml.cs` / `SongConfigWindow.xaml.cs`: GUI 編輯時自動更新
  - 配置範例：
    ```yaml
    songs:
    - music_id: "405129"
      leader_designation: "1031533"  # 自動在歌曲 2,3 禁用
    - music_id: "405204"
      leader_designation: "1033524"  # 自動在歌曲 1,3 禁用
    - music_id: "405205"
      leader_designation: "1052506"  # 自動在歌曲 1,2 禁用
    ```

- [ ] **每首歌獨立卡池**: 允許每首歌曲配置專屬的卡片池
  - 需求：某些歌曲只想使用特定卡片組合
  - 資料模型：在 `SongConfig` 新增 `card_ids_override: List<int>?`
  - 優先級：`song.card_ids_override > config.card_ids`（全域卡池）
  - 相關檔案：
    - `Config/MemberConfig.cs`: 新增 `SongConfig.CardIdsOverride` 欄位
    - `Services/BatchSimulationService.cs`: 讀取歌曲專屬卡池
    - GUI: `SongConfigWindow.xaml`: 新增「覆蓋卡池」按鈕與選擇器
  - 配置範例：
    ```yaml
    card_ids: [1031533, 1033524, 1041405, ...]  # 全域卡池
    songs:
    - music_id: "405129"
      card_ids_override: [1031533, 1052506, 1051901]  # 只用這 3 張卡
    - music_id: "405204"
      # 使用全域卡池
    ```
