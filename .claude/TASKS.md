# LineUp 开发任务清单

---

## Phase 1: 数据模型层（Disc + Board + Player）

这一阶段完成所有核心数据类，不涉及游戏逻辑和 UI。完成后应该能编译通过。

### 1.1 disc.cs — 枚举 + Disc 继承体系

- [ ] 定义 `PlayerId` 枚举：Player1, Player2
- [ ] 定义 `DiscType` 枚举：Ordinary, Boring, Magnetic
- [ ] 实现 `Disc` 抽象基类
  - private 字段 + 公开属性：Owner (PlayerId), Type (DiscType)
  - 抽象属性 `Symbol` (char)，子类各自返回自己的符号
  - 属性 `CountsForWin`：默认 true（所有棋子都计入胜利）
  - 抽象方法 `ApplyEffect(Board board, int row, int col, Action<Disc>? returnDisc)`：修改棋盘实现特殊效果，不负责显示（帧由 Game 层在调用前后 Render 生成）
  - 静态方法 `FromSymbol(char symbol)`：根据棋盘符号还原 Disc 对象（用于 Boring 归还棋子时识别归属）
- [ ] 实现 `OrdinaryDisc` 子类
  - Symbol：P1 = `@`, P2 = `#`
  - ApplyEffect：空实现，无特殊效果
- [ ] 实现 `BoringDisc` 子类
  - Symbol：P1 = `B`, P2 = `b`
  - ApplyEffect：清空该列所有其他棋子（统一作为 Ordinary 归还各自玩家），自身落到列底
- [ ] 实现 `MagneticDisc` 子类
  - Symbol：P1 = `M`, P2 = `m`
  - ApplyEffect：寻找下方最近己方普通棋子，若不紧邻则上移一格交换

### 1.2 board.cs — Board 类

- [ ] private `char?[,] cells` 二维数组，(0,0) = 左上角
- [ ] 构造函数：接收 rows, cols，验证 rows ≥ 6, cols ≥ 7, rows ≤ cols
- [ ] 公开只读属性 Rows, Columns
- [ ] `GetCell(int row, int col)` → char?（0-based）
- [ ] `SetCell(int row, int col, char? value)`（0-based）
- [ ] `ClearCell(int row, int col)`
- [ ] `DropDisc(int col, char symbol)` → int：棋子因重力落到最低空位，返回落到的行号
- [ ] `IsColumnFull(int col)` → bool：检查 row 0 是否有棋子
- [ ] `IsFull` 属性 → bool：所有列是否都满
- [ ] `CollapseColumn(int col)`：消除空洞，从底部往上收集棋子重新排列（保持相对顺序）
- [ ] `Render()` → string：按格式渲染棋盘，row 0 到 Rows-1 从上到下输出

### 1.3 player.cs — Player 继承体系

- [ ] 定义 `PlayerType` 枚举：Human, Computer
- [ ] 实现 `Player` 抽象基类
  - private 字段 + 公开属性：Id, Name, Type
  - private 棋子库存字段：OrdinaryDiscsRemaining, BoringDiscsRemaining, MagneticDiscsRemaining
  - 公开只读属性暴露库存数量
  - `CanPlayDisc(DiscType)` → bool
  - `UseDisc(DiscType)` → Disc：扣减库存，返回对应子类实例
  - `ReturnDisc(Disc)` → void：归还棋子（Boring 效果用）
  - 抽象方法 `TakeTurn(...)` → 子类实现
- [ ] 实现 `HumanPlayer` 子类
  - override TakeTurn：从控制台读取输入，验证后执行走法
- [ ] 实现 `ComputerPlayer` 子类
  - override TakeTurn：优先找立即获胜走法，否则随机选

### Phase 1 验收

- [ ] `dotnet build` 编译通过，无报错

---

## Phase 2: 游戏规则层（GameRules）

### 2.1 gameRules.cs — 胜负判定与走法验证

- [ ] 构造函数接收 winningLength（最小 4）
- [ ] `IsValidColumn(Board, int col)` → bool：列号 0-based 范围检查
- [ ] `GetValidMoves(Board, Player, DiscType[])` → 所有合法走法列表
- [ ] `HasAnyValidMove(Board, Player, DiscType[])` → bool
- [ ] `HasWinner(Board, PlayerId)` → bool：检查四个方向（水平、垂直、对角线×2）
- [ ] `MoveWinsImmediately(Board, Player, move)` → bool：模拟走法后检查是否获胜（用于电脑 AI）
- [ ] 模拟走法需要 CloneBoard 深拷贝

### Phase 2 验收

- [ ] 编译通过
- [ ] 可以手动在 Main 里创建 Board + GameRules 测试胜负判定逻辑

---

## Phase 3: UI 层（GameConsoleUi）

### 3.1 gameConsoleUi.cs — 控制台交互

- [ ] 主菜单显示与输入：New Game / Load Game / Testing Mode / Quit
- [ ] 新游戏设置提示：游戏模式 (1=HvH, 2=HvC)、行数、列数
- [ ] 游戏中显示棋盘：调用 Board.Render()
- [ ] 显示玩家状态：当前玩家名 + 各类棋子剩余数量
- [ ] 人类走法输入与解析：`4`（普通棋子列4）、`b 4`（Boring 列4）、`m 4`（Magnetic 列4）
- [ ] 用户输入的列号是 1-based，UI 层负责转为 0-based 后传给 Game 层
- [ ] 帮助菜单：列出所有命令（下棋、save、help、quit），用分隔线区分
- [ ] 存档相关提示：save 成功、load 文件路径输入
- [ ] 各类错误提示：无效输入、列满、棋子用完、列号越界
- [ ] 游戏结果显示：胜利信息 / 平局信息 + 最终棋盘
- [ ] 特殊棋子分帧显示：放置帧 + 效果帧
- [ ] 测试模式提示：输入走法序列

### Phase 3 验收

- [ ] 编译通过
- [ ] UI 方法可以独立调用测试输出格式

---

## Phase 4: 游戏主控（Game + Program.cs）

### 4.1 game.cs — Game 类

- [ ] 构造函数：接收行列数、游戏模式、特殊棋子类型，初始化 Board + 两个 Player + GameRules
- [ ] 计算胜利连线长度：`rows * cols / 10`（整数除法），6×7 = 4
- [ ] 计算每人棋子数：总格子 / 2，其中特殊棋子各 2 个，剩余为普通棋子
- [ ] 游戏主循环 `Run()`：
  - 显示初始棋盘
  - 循环：显示棋盘 → 当前玩家 TakeTurn()（电脑玩家立即执行不提示） → 检查胜利 → 检查棋盘满 → 检查对方有无合法走法 → 切换玩家
  - 特殊棋子走法：调用前 Render 生成放置帧，ApplyEffect 后 Render 生成效果帧，两帧都输出
  - 三种结束情况都要处理（胜利 / 棋盘满平局 / 无合法走法平局）
  - 游戏结束返回主菜单
- [ ] `ReturnDiscToOwner(Disc)` 回调方法：Boring 效果归还棋子用

### 4.2 Program.cs — 入口与主菜单循环

- [ ] 主菜单循环：根据选择调用 New Game / Load Game / Testing Mode / Quit
- [ ] New Game：提示设置 → 创建 Game → 运行
- [ ] Quit：退出程序
- [ ] 异常处理：catch 非法输入，显示错误后继续

### Phase 4 验收

- [ ] 可以完整进行一局 HvH 普通棋子游戏（胜利 + 平局）
- [ ] 可以完整进行一局 HvC 普通棋子游戏
- [ ] 游戏结束后回到主菜单

---

## Phase 5: 特殊棋子效果

### 5.1 Boring 效果完整测试

- [ ] BoringDisc.ApplyEffect 正确清空列、归还棋子、自身落底
- [ ] 分帧显示：放置帧 → 效果帧
- [ ] Boring 棋子落底后计入胜利判定

### 5.2 Magnetic 效果完整测试

- [ ] MagneticDisc.ApplyEffect 正确找到下方最近己方棋子（包括已放置的 B/b、M/m，因为它们已视为普通棋子）
- [ ] 紧邻正下方时无效果
- [ ] 非紧邻时上移一格（与上方棋子交换）
- [ ] 分帧显示：放置帧 → 效果帧
- [ ] Magnetic 棋子激活后计入胜利判定

### Phase 5 验收

- [ ] HvH 模式下可以正确使用 Boring 和 Magnetic 棋子
- [ ] 特殊棋子用完后不能再选
- [ ] 特殊效果后胜负判定正确

---

## Phase 6: Testing Mode

### 6.1 testModeRunner.cs — 测试模式

- [ ] 解析输入序列字符串：按逗号分割，每个 token 解析为棋子类型 + 列号
- [ ] 棋子类型字母不区分大小写：O / B / M
- [ ] 列号是 1-based（和正常游戏一致），解析时转为 0-based
- [ ] 创建固定 6×7 游戏，P1 先手交替执行
- [ ] 每步执行后显示棋盘（特殊棋子分帧显示）
- [ ] 遇到胜利或平局立即停止并报告
- [ ] 所有步骤完成但无胜负时显示最终棋盘
- [ ] 非法走法时显示错误信息并停止

### 6.2 接入主菜单

- [ ] Program.cs 中 Testing Mode 选项调用 TestModeRunner

### Phase 6 验收

用 req.md 中提供的示例序列逐一验证：

- [ ] 基本游戏：`O4,O5,O3,O6,O2,O5,O4,O5,O4,O5` → P2 在第5列连4获胜
- [ ] Boring 棋子：构造一个序列使 P2 在第4列使用 Boring 棋子，验证清空列 + 归还棋子 + 自身落底 + 胜负判定正确
- [ ] Magnetic 棋子：构造一个序列使 P2 使用 Magnetic 棋子，验证磁力上移效果 + 紧邻时无效果 + 胜负判定正确
- [ ] 测试非法输入序列（列号越界、棋子用完）时正确报错并停止

---

## Phase 7: 存档 / 读档

### 7.1 存档功能

- [ ] 人类回合中输入 `save` 触发存档
- [ ] 序列化游戏状态到文本文件：棋盘内容、双方库存、当前回合玩家、游戏模式、棋盘尺寸、启用的特殊棋子类型
- [ ] 存档后游戏继续，不中断

### 7.2 读档功能

- [ ] 主菜单 Load Game 选项提示输入存档文件路径
- [ ] 反序列化文件内容，重建 Game 对象
- [ ] 从存档状态继续游戏
- [ ] 文件不存在或格式错误时提示错误，返回主菜单

### Phase 7 验收

- [ ] 游戏中途存档 → 退出 → 重新启动 → Load → 从存档状态继续完成游戏
- [ ] 存档保留了游戏模式（HvH/HvC）、棋子库存、当前回合

---

## Phase 8: 收尾与打磨

- [ ] 完整走一遍所有游戏流程：New Game (HvH) → 玩到胜利 → 返回主菜单
- [ ] 完整走一遍：New Game (HvC) → 玩到平局 → 返回主菜单
- [ ] 完整走一遍：New Game → save → quit → Load Game → 继续玩到结束
- [ ] 完整走一遍：Testing Mode → 输入序列 → 正确输出结果
- [ ] 验证自定义棋盘尺寸（如 8×10）胜利连线长度计算正确
- [ ] 验证各种非法输入不崩溃
- [ ] 检查代码注释是否清晰
- [ ] `dotnet clean && dotnet run` 确认干净编译和运行
