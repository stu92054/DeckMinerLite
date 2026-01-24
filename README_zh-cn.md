# SukuShow Deck Miner Lite

适用于 [Link！Like！LoveLive！](https://www.lovelive-anime.jp/hasunosora/system/) (リンクラ)
音游模式 **School Idol Show (スクショウ)** 的 **卡组模拟器（C# 高性能版）**。

本项目是 Python 版 [SukuShow Deck Miner](https://github.com/BlueNoBaka/SukuShow-Deck-Miner) 的 C# 实现，**性能更高**。
**仅实现了批量模拟**的功能，输出的 Log 与 Python 版兼容。

---

## 🎮 使用方式

### ▶ 运行主程序

本项目在 .Net 10 环境下开发，Windows 版本提供 **WPF 图形化界面 (GUI)**，Linux 版本采用 **NativeAOT** 构建的 CLI，使用时不需要额外安装 .Net 运行时。

#### Windows 版本（含 GUI）

**GUI 模式（推荐）**：
- 双击 `DeckMinerLite.exe` 启动图形化界面
- 通过界面加载 YAML 配置文件
- 可视化显示卡池、歌曲配置、模拟日志
- 适合一般用户和交互式操作

**CLI 模式（自动化）**：
```bash
# 传入参数时自动切换为 CLI 模式
DeckMinerLite.exe --config config/member-example.yaml
DeckMinerLite.exe --test-yaml
```

#### Linux 版本（纯 CLI）

Linux 版本仅提供命令行界面，采用 NativeAOT 优化：
```bash
chmod +x DeckMinerLite
./DeckMinerLite --config config/member-example.yaml
```

---

## 🖥 GUI 功能说明（Windows 专属）

### 主窗口界面

GUI 提供 4 个选项卡：

#### 1️⃣ Configuration（配置）
- 加载 YAML 配置文件（支持 `config/*.yaml`）
- 显示基本设置：成员名称、赛季模式、LGP 模式
- 显示卡池大小
- 列出所有歌曲配置（歌曲 ID、难度、熟练度）

#### 2️⃣ Simulation（模拟）
- 执行模式选择：完整优化（模拟 + 多曲优化）或仅模拟
- 开始/停止模拟按钮
- 进度条显示模拟进度
- 实时日志输出（与 CLI 相同格式）
- 清除日志按钮

#### 3️⃣ Results（结果）
- 显示多曲优化结果（`best_3_song_combo.txt` 或 `best_2_song_combo.txt`）
- 重新整理按钮可载入最新结果

#### 4️⃣ About（关于）
- 版本信息
- 功能说明
- CLI 模式提示

### 快速操作

- **加载配置**：点击「Load Config」选择 YAML 文件
- **重新加载**：点击「Reload」刷新配置
- **开始模拟**：加载配置后，「Start Simulation」按钮会启用
- **打开输出文件夹**：点击「Open Output Folder」快速打开结果目录

---

## ⚙ 配置说明

### 📋 YAML 配置（推荐）

支持使用 YAML 配置文件，与 Python 版完全兼容。详细说明请参考繁体中文版 README。

---

### 📄 JSONC 配置（旧版）

目前仅能通过 `cardConfig.jsonc` 和 `task.jsonc` 进行配置。

模拟器支持读取带注释的 Json，但是**注释内容需要以 `//` 开头**，而不是 Python 注释的 `#`。

* **卡牌等级配置**
  * 文件: `cardConfig.jsonc`
  * 功能与 Python 版的 `CardLevelConfig.py` 一致
  * 与 Python 版不同，练度中的卡牌 ID 需要带引号，例如 `"1021701": [140, 14, 11]`。

* **卡池配置**
  * 文件: `task.jsonc`
  * 字段: `CardPool`
  * 填写卡牌 ID 即可，与 Python 版一致。

* **模拟任务配置**
  * 文件: `task.jsonc`
  * 字段: `Task`
  * 单个任务的填写规则及用途与 Python 版基本一致，填写多个任务则会顺序执行。
  * 目前无法配置季度倍率，默认取满级的 6.6，如需重算 Pt 请使用 Python 版中的 `log_tool.py`。
  * 卡组的技能约束 `MustSkills` 需要填写技能类型的编号，具体参考下表。

#### 🎯 技能类型对照表

| 编号  | 枚举名                        | 说明 |
|------:|------------------------------|------|
| 1     | `APChange`                   | 回费/扣费 |
| 2     | `ScoreGain`                  | 分 |
| 3     | `VoltagePointChange`         | 加电/扣电 |
| 4     | `MentalRateChange`           | 回血/扣血 |
| 5     | `DeckReset`                  | 洗牌 |
| 6     | `CardExcept`                 | 除外 |
| 7     | `NextAPGainRateChange`       | 分加成 |
| 8     | `NextVoltageGainRateChange`  | 电加成 |

---

## ⚠ 与 Python 版的主要差异

### ✅ 已实现
- ✅ YAML 配置完全兼容
- ✅ 禁卡功能（三级合并）
- ✅ LGP 模式 / 日常模式切换
- ✅ PT 动态计算（Fan Level + Limitbreak）
- ✅ 输出目录隔离
- ✅ 卡牌练度自定义
- ✅ **WPF 图形化界面（Windows）**
- ✅ **GUI/CLI 双模式自动切换**
- ✅ **GUI 模拟执行集成**（支持完整优化流程或仅模拟）

### ⚠ 未实现
- ❌ 花火吟的延后 Miss（影响仰卧起坐精度）
- ❌ PT 重算工具（请使用 Python 版 `log_tool.py`）

---

## 📊 性能比较

| 项目 | C# (DeckMinerLite) | Python (MainBatch.py) |
|------|--------------------|-----------------------|
| 单曲模拟速度 | **极快** | 较慢 |
| 内存使用 | 低 | 中等 |
| 多曲优化 | ✅ (GUI 集成) | ✅ |
| YAML 配置 | ✅ | ✅ |
| **图形化界面** | **✅ (Windows)** | ❌ |
| 跨平台支持 | Windows (GUI+CLI) / Linux (CLI) | 全平台 CLI |

---

## 🛠 开发信息

- **语言**：C# (.NET 10)
- **构建架构**：
  - Windows: `net10.0-windows` (WPF GUI, 无 AOT)
  - Linux: `net10.0` (纯 CLI, NativeAOT)
- **GUI 框架**：WPF (Windows Presentation Foundation)
- **配置格式**：YAML（推荐）或 JSONC
- **依赖包**：
  - YamlDotNet 16.2.0（YAML 解析）
  - TqdmSharp 0.4.3（进度条）
  - CommunityToolkit.Mvvm 8.3.2（MVVM 支持，仅 Windows）

### 编译项目

```bash
cd DeckMinerLite

# 编译 Windows 版本（含 GUI）
dotnet build --framework net10.0-windows

# 编译 Linux 版本（纯 CLI）
dotnet build --framework net10.0

# 编译所有目标
dotnet build
```

### 运行开发版本

```bash
# Windows: CLI 模式（需传入参数）
dotnet run --framework net10.0-windows -- --config ../config/member-test.yaml

# Linux: CLI 模式
dotnet run --framework net10.0 -- --config ../config/member-test.yaml

# 测试 YAML 配置
dotnet run -- --test-yaml --config ../config/member-test.yaml
```

### 发布包

```bash
# 使用自动化脚本（推荐）
publish.bat

# 手动发布
dotnet publish -c Release --framework net10.0-windows -r win-x64 --self-contained
dotnet publish -c Release --framework net10.0 -r linux-x64 --self-contained
```