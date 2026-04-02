# LineUp Game - Development Guide

IFN584 Assignment 1 (20%)，C# 控制台应用，.NET 8+。完整需求见 `req.md`。

---

## 核心原则

### 1. 不要过度设计

这是一个学校课程作业，不是企业项目。代码能清晰完成需求即可：
- 不用设计模式、工厂模式、依赖注入等复杂架构
- 不用泛型约束、接口隔离等高级技巧
- 不用过度抽象，能用具体类解决的不要加抽象层
- 方法够用就好，不需要"可扩展性"

### 2. 必须体现 OOP 概念

课程是 OOP，代码必须紧扣以下三个概念，即使实现简单也必须有：

- **封装 (Encapsulation)**：类的字段用 private，通过属性或方法对外暴露。Board 封装二维数组，外部不能直接访问 cells。Player 封装棋子库存。
- **继承 (Inheritance)**：Disc 作为基类，OrdinaryDisc / BoringDisc / MagneticDisc 作为子类继承。Player 作为基类，HumanPlayer / ComputerPlayer 继承。
- **多态 (Polymorphism)**：不同 Disc 子类重写 ApplyEffect() 方法实现各自特殊效果。不同 Player 子类重写 TakeTurn() 方法实现人类输入和电脑AI的不同行为。

### 3. 棋盘坐标系统 — 原始二维数组方式

棋盘用 `char?[,]` 二维数组存储，按照原始数组方式排布：

```
        col 0   col 1   col 2   col 3   col 4   col 5   col 6
row 0  |       |       |       |       |       |       |       |   ← 顶部（视觉顶行）
row 1  |       |       |       |       |       |       |       |
row 2  |       |       |       |       |       |       |       |
row 3  |       |       |       |       |       |       |       |
row 4  |       |       |       |       |       |       |       |
row 5  |       |       |       |       |       |       |       |   ← 底部（棋子先落到这里）
```

- `(0, 0)` = 左上角
- `(0, 1)` = 第一行第二列
- `(Rows-1, 0)` = 左下角（棋子因重力最先到达的位置）
- 行从上到下递增，列从左到右递增
- 重力方向：棋子从 row 0 往 row Rows-1 方向下落
- 渲染时直接按 row 0 → Rows-1 顺序输出即可，与视觉一致
- 用户输入的列号是 1-based（第1列 = col 0），GameConsoleUi 负责将用户输入的 1-based 列号转为 0-based 后传给 Game 层

---

## 项目结构

```
Lineup/
├── Program.cs                  # 入口，主菜单循环
└── model/
    ├── disc.cs                 # Disc 基类 + OrdinaryDisc/BoringDisc/MagneticDisc 子类，枚举
    ├── board.cs                # Board 类，封装二维数组，重力、渲染、塌陷
    ├── player.cs               # Player 基类 + HumanPlayer/ComputerPlayer 子类
    ├── gameRules.cs            # 胜负判定、走法验证
    ├── gameConsoleUi.cs        # 控制台输入输出、菜单、帮助
    ├── game.cs                 # Game 主控类，游戏循环、存档读档
    └── testModeRunner.cs       # 测试模式解析和执行
```

---

## 需求清单与实现要点

### A. 基本游戏功能

- [ ] 主菜单：New Game / Load Game / Testing Mode / Quit
- [ ] 新游戏设置：选择模式 (HvH / HvC)，设置行列数
- [ ] 棋盘尺寸限制：最小 6×7，行数 ≤ 列数
- [ ] 每人棋子数 = (rows × cols) / 2，其中每种特殊棋子各 2 个，剩余为普通棋子
- [ ] 胜利条件：连线长度 = `rows * cols / 10`（整数除法，等价于 ⌊rows×cols×0.1⌋），6×7 默认就是 4
- [ ] 回合交替，P1 先手
- [ ] 棋子因重力落到列中最低空位
- [ ] 列满不能再下
- [ ] 游戏结束的三种情况必须全部实现：
  1. **胜利**：某玩家达成连线，显示最终棋盘 + 胜者信息
  2. **平局（棋盘满）**：棋盘所有格子被填满但无人胜出，显示最终棋盘 + 平局信息
  3. **平局（无合法走法）**：双方都没有合法走法（比如棋子用完且所有列满），显示平局信息
- [ ] 每回合结束后必须依次检查：先检查是否有人获胜 → 再检查棋盘是否满 → 再检查对方是否有合法走法
- [ ] 游戏结束后显示最终棋盘和结果，然后返回主菜单（不是直接退出程序）

### B. 玩家类型

- [ ] HumanPlayer：从控制台读取输入，验证合法性
- [ ] ComputerPlayer：优先选能立即获胜的走法，否则随机选合法走法。电脑玩家走棋时不需要任何提示，立即执行并显示走法结果

### C. 棋子类型（选 Boring + Magnetic 两种特殊棋子）

**普通棋子 (Ordinary)**
- 落到最低空位，无特殊效果
- 符号：P1 = `@`，P2 = `#`

**钻孔棋 (Boring)**
- 落入后清空该列所有其他棋子，归还给各自玩家（通过棋盘符号识别归属：@/B/M→P1, #/b/m→P2）
- 归还时统一作为 Ordinary 棋子归还（因为已放置的特殊棋子已视为普通棋子，不会恢复特殊能力）
- 自身留在列底，之后视为普通棋子，可计入胜利
- 符号保持 B/b 不变（不需要转成 @/#），但功能上等同普通棋子
- 符号：P1 = `B`，P2 = `b`

**磁力棋 (Magnetic)**
- 落入后，在该列中从磁力棋位置往下搜索，寻找最近的己方普通棋子
- "普通棋子"包括：原始普通棋子（@/#）以及已放置的特殊棋子（B/b, M/m），因为它们放置后都视为普通棋子
- 若该棋子紧邻磁力棋正下方，则无效果
- 否则将该棋子上移一格（与上方棋子交换位置）
- 激活后视为普通棋子，可计入胜利
- 符号保持 M/m 不变，但功能上等同普通棋子
- 符号：P1 = `M`，P2 = `m`

**每人各 2 个特殊棋子。**

**重要：已放置的特殊棋子规则**
- 已放置在棋盘上的 B/b、M/m 在胜利判定中都算作该玩家的有效棋子（CountsForWin = true）
- 磁力棋寻找目标时，B/b 和 M/m 都算作"己方普通棋子"可以被吸引

### D. 特殊棋子显示帧

特殊棋子需要分帧展示（普通棋子只需显示落子后的棋盘）：
1. **放置帧**：棋子落到位置后的棋盘（效果尚未触发）
2. **效果帧**：特殊能力触发后的棋盘状态

帧生成方式：Disc.ApplyEffect() 只负责修改棋盘，Game 类在调用前后各 Render() 一次来生成两帧。这样 Disc 子类不需要关心显示逻辑。

### E. 存档 / 读档

- [ ] 游戏中输入 `save` 命令保存当前状态，游戏继续
- [ ] 主菜单 Load Game 选项可加载存档恢复游戏
- [ ] 存档内容：棋盘状态、双方棋子库存、当前回合、游戏模式、棋盘尺寸、特殊棋子类型
- [ ] 存档格式用简单文本即可，不需要 JSON/XML

### F. 帮助菜单

- [ ] 输入 `help` 显示可用命令和示例
- [ ] 包括：列号下棋、特殊棋子下法、save、help、quit

### G. Testing Mode（评分关键，必须正确实现！）

Testing Mode 是老师用来自动化测试游戏逻辑的，实现不正确会导致无法评分。

- [ ] 主菜单选择 Testing Mode 后，提示输入一行走法序列
- [ ] 输入格式：`O4,O5,B3,M6`（棋子类型字母 + 列号，逗号分隔）
- [ ] 棋子类型字母（不区分大小写）：`O` = Ordinary, `B` = Boring, `M` = Magnetic
- [ ] P1 先手，交替执行每一步
- [ ] 棋盘固定 6×7，胜利连线 = 4
- [ ] 每执行一步后显示当前棋盘状态
- [ ] 特殊棋子需要分帧显示（放置帧 + 效果帧）
- [ ] 遇到胜利或平局时立即停止并报告结果
- [ ] 所有步骤执行完毕但无胜负时，显示最终棋盘状态
- [ ] 走法非法（列满、棋子用完、列号越界）时抛出错误信息并停止

### H. 输入验证

- [ ] 无效输入不崩溃，提示重新输入
- [ ] 列号越界、列满、棋子用完等情况均需处理

---

## 类设计要点

### Disc 继承体系

```
Disc (abstract)              ← 基类：Owner, Symbol, CountsForWin
├── OrdinaryDisc             ← 无特殊效果
├── BoringDisc               ← override ApplyEffect(): 清空列
└── MagneticDisc             ← override ApplyEffect(): 磁力上移
```

- `Disc.ApplyEffect(Board, int row, int col, ...)` 是虚方法，OrdinaryDisc 什么都不做，子类各自重写 → **多态**
- Owner 和 Type 用 private 字段 + 公开属性 → **封装**

### Player 继承体系

```
Player (abstract)            ← 基类：Id, Name, 棋子库存管理
├── HumanPlayer              ← override TakeTurn(): 读取控制台输入
└── ComputerPlayer           ← override TakeTurn(): AI 选择走法
```

- `Player.TakeTurn(...)` 是虚方法，子类各自重写 → **多态**
- 棋子库存用 private 字段，通过方法 UseDisc/ReturnDisc 管理 → **封装**

### Board

- 封装 `char?[,] cells`，外部通过方法访问 → **封装**
- 提供：DropDisc, GetCell, SetCell, ClearCell, IsColumnFull, IsFull, CollapseColumn, Render

### GameRules

- 纯逻辑类：HasWinner, IsValidColumn, GetValidMoves, ApplyMove
- 不持有游戏状态，接收 Board 和 Player 作为参数

### Game

- 游戏主控：持有 Board, Player1, Player2, GameRules, 当前回合
- 负责游戏循环、存档读档
- 调用 Player.TakeTurn() 实现多态调度

### GameConsoleUi

- 所有 Console.ReadLine / Console.WriteLine 集中在此
- 菜单、提示、棋盘显示、帮助
- UI 风格：多用 `--------------------------------` 分隔线来区分不同区域（菜单选项、游戏状态、帮助信息等），让终端输出更清晰易读

### TestModeRunner

- 解析测试序列字符串，创建 6×7 游戏并执行

---

## 棋盘渲染格式

```
|   |   |   |   |   |   |   |
|   |   |   |   |   |   |   |
|   |   |   |   |   |   |   |
|   |   |   | @ | # |   |   |
|   |   |   | @ | # |   |   |
|   | @ | @ | @ | # | # |   |
```

每列 3 字符宽，用 `|` 分隔。有棋子时 ` X `（空格-符号-空格），无棋子时 `   `（三空格）。

---

## Build & Run

```bash
cd Lineup
dotnet clean
dotnet run
```
