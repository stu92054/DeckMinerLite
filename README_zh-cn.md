# SukuShow Deck Miner Lite

适用于 [Link！Like！LoveLive！](https://www.lovelive-anime.jp/hasunosora/system/) (リンクラ)
音游模式 **School Idol Show (スクショウ)** 的 **卡组模拟器（C# 高性能版）**。

本项目是 Python 版 [SukuShow Deck Miner](https://github.com/BlueNoBaka/SukuShow-Deck-Miner) 的 C# 实现，**性能更高**。  
**仅实现了批量模拟**的功能，输出的 Log 与 Python 版兼容。

---

## 🎮 使用方式

### ▶ 运行主程序

本项目在 .Net 10 环境下开发，采用 **NativeAOT** 构建，使用时不需要额外安装 .Net 运行时。

双击 `DeckMinerLite.exe` 即可运行，练度、卡池、模拟任务通过配置文件修改。

---

### ⚙ 配置说明

目前仅能通过 `cardConfig.jsonc` 和 `task.jsonc` 进行配置，后续也许会增加命令行参数用于指定其他配置文件。  

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

* 未实现花火吟的延后 Miss (影响仰卧起坐精度)。  
* 无法配置季度等级的倍率。
* 剩下忘了