namespace Lineup.Model;

/// <summary>
/// 游戏主控类 — 持有棋盘、双方玩家、规则，负责游戏循环
/// </summary>
public class Game
{
    private readonly Board _board;
    private readonly Player _player1;
    private readonly Player _player2;
    private readonly GameRules _rules;
    private readonly GameConsoleUi _ui;
    private readonly DiscType[] _enabledSpecialTypes;
    private readonly Random _random;
    private readonly int _winningLength;

    private Player _currentPlayer;

    public Game(int rows, int cols, PlayerType player2Type, GameConsoleUi ui)
    {
        _ui = ui;
        _random = new Random();
        _enabledSpecialTypes = new[] { DiscType.Boring, DiscType.Magnetic };

        _board = new Board(rows, cols);
        _winningLength = Math.Max(4, rows * cols / 10);
        _rules = new GameRules(_winningLength);

        // 计算每人棋子数
        int discsPerPlayer = rows * cols / 2;
        int specialCount = _enabledSpecialTypes.Length * 2; // 每种特殊棋子各2个
        int ordinaryCount = discsPerPlayer - specialCount;

        _player1 = new HumanPlayer(PlayerId.Player1, "Player 1", ordinaryCount, 2, 2);
        _player2 = player2Type == PlayerType.Computer
            ? new ComputerPlayer(PlayerId.Player2, "Computer", ordinaryCount, 2, 2)
            : new HumanPlayer(PlayerId.Player2, "Player 2", ordinaryCount, 2, 2);

        _currentPlayer = _player1;
    }

    // 用于 Load Game 恢复状态的内部构造函数
    internal Game(Board board, Player player1, Player player2, GameRules rules,
        GameConsoleUi ui, DiscType[] enabledSpecialTypes, int winningLength, Player currentPlayer)
    {
        _board = board;
        _player1 = player1;
        _player2 = player2;
        _rules = rules;
        _ui = ui;
        _enabledSpecialTypes = enabledSpecialTypes;
        _winningLength = winningLength;
        _random = new Random();
        _currentPlayer = currentPlayer;
    }

    // 公开属性，存档时需要访问
    internal Board Board => _board;
    internal Player Player1 => _player1;
    internal Player Player2 => _player2;
    internal Player CurrentPlayer => _currentPlayer;
    internal int WinningLength => _winningLength;
    internal DiscType[] EnabledSpecialTypes => _enabledSpecialTypes;

    /// <summary>运行游戏主循环</summary>
    public void Run()
    {
        _ui.ShowGameStart(_board, _winningLength);
        PlayGameLoop();
    }

    /// <summary>游戏主循环</summary>
    private void PlayGameLoop()
    {
        while (true)
        {
            // 检查当前玩家是否有合法走法
            if (!_rules.HasAnyValidMove(_board, _currentPlayer, _enabledSpecialTypes))
            {
                _ui.ShowNoValidMoves(_currentPlayer.Name);
                var opponent = GetOpponent();
                if (!_rules.HasAnyValidMove(_board, opponent, _enabledSpecialTypes))
                {
                    // 双方都无合法走法 → 平局
                    _ui.ShowTie(_board);
                    return;
                }
                // 跳过当前玩家，轮到对方
                _currentPlayer = opponent;
                continue;
            }

            // 显示当前玩家回合信息（棋盘已在上一步或游戏开始时显示）
            if (_currentPlayer.Type == PlayerType.Human)
                _ui.ShowPlayerTurn(_currentPlayer);

            // 当前玩家执行回合
            var result = _currentPlayer.TakeTurn(_board, _ui, _rules, _enabledSpecialTypes, _random);

            // 处理 Save / Quit
            if (result.Action == TurnAction.Quit)
                return;

            if (result.Action == TurnAction.Save)
            {
                SaveGame("lineup_save.txt");
                _ui.ShowSaveSuccess("lineup_save.txt");
                continue;
            }

            // 执行走法（ExecuteMove 内统一负责显示棋盘）
            if (!ExecuteMove(_currentPlayer, result.DiscType, result.Column))
                continue; // 走法无效，重新提示

            // 检查胜利
            if (_rules.HasWinner(_board, _currentPlayer.Id))
            {
                _ui.ShowWinner(_board, _currentPlayer);
                return;
            }

            // 检查棋盘满
            if (_board.IsFull)
            {
                _ui.ShowTie(_board);
                return;
            }

            // 切换玩家
            _currentPlayer = GetOpponent();
        }
    }

    /// <summary>
    /// 执行一步走法：落子 + 特殊效果 + 分帧显示
    /// 返回 true 表示成功
    /// </summary>
    internal bool ExecuteMove(Player player, DiscType discType, int col)
    {
        // 验证
        if (!_rules.IsValidColumn(_board, col))
        {
            _ui.ShowInvalidColumn(_board.Columns);
            return false;
        }
        if (_board.IsColumnFull(col))
        {
            _ui.ShowColumnFull(col + 1);
            return false;
        }
        if (!player.CanPlayDisc(discType))
        {
            _ui.ShowNoDiscsRemaining(player.Name, discType);
            return false;
        }

        // 扣减库存并获取棋子
        Disc disc = player.UseDisc(discType);

        // 落子
        int landedRow = _board.DropDisc(col, disc.Symbol);

        if (discType != DiscType.Ordinary)
        {
            // 特殊棋子分帧显示：放置帧 → 效果帧
            _ui.ShowPlacementFrame($"{discType} disc", col + 1);
            _ui.ShowBoard(_board);

            disc.ApplyEffect(_board, landedRow, col, ReturnDiscToOwner);

            _ui.ShowEffectFrame($"{discType} disc effect activated.", _board);
        }
        else
        {
            // 普通棋子：显示落子后棋盘
            _ui.ShowBoard(_board);
        }

        return true;
    }

    /// <summary>归还棋子给对应玩家（Boring 效果回调）</summary>
    private void ReturnDiscToOwner(Disc disc)
    {
        if (disc.Owner == PlayerId.Player1)
            _player1.ReturnDisc(disc);
        else
            _player2.ReturnDisc(disc);
    }

    private Player GetOpponent()
    {
        return _currentPlayer == _player1 ? _player2 : _player1;
    }

    // ==================== 存档 / 读档 ====================

    /// <summary>保存游戏状态到文本文件</summary>
    public void SaveGame(string filePath)
    {
        using var writer = new StreamWriter(filePath);
        // 行1: 棋盘尺寸
        writer.WriteLine($"{_board.Rows},{_board.Columns}");
        // 行2: 胜利连线长度
        writer.WriteLine(_winningLength);
        // 行3: Player2 类型
        writer.WriteLine(_player2.Type);
        // 行4: 当前玩家 (1 or 2)
        writer.WriteLine(_currentPlayer == _player1 ? 1 : 2);
        // 行5: P1 库存
        writer.WriteLine($"{_player1.OrdinaryDiscsRemaining},{_player1.BoringDiscsRemaining},{_player1.MagneticDiscsRemaining}");
        // 行6: P2 库存
        writer.WriteLine($"{_player2.OrdinaryDiscsRemaining},{_player2.BoringDiscsRemaining},{_player2.MagneticDiscsRemaining}");
        // 行7+: 棋盘内容，每行一行，空位用 .
        for (int row = 0; row < _board.Rows; row++)
        {
            var line = "";
            for (int col = 0; col < _board.Columns; col++)
            {
                char? cell = _board.GetCell(row, col);
                line += cell ?? '.';
            }
            writer.WriteLine(line);
        }
    }

    /// <summary>从文本文件加载游戏状态</summary>
    public static Game LoadGame(string filePath, GameConsoleUi ui)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Save file not found: {filePath}");

        var lines = File.ReadAllLines(filePath);
        int lineIndex = 0;

        // 行1: 棋盘尺寸
        var sizeParts = lines[lineIndex++].Split(',');
        int rows = int.Parse(sizeParts[0]);
        int cols = int.Parse(sizeParts[1]);

        // 行2: 胜利连线长度
        int winningLength = int.Parse(lines[lineIndex++]);

        // 行3: Player2 类型
        var player2Type = Enum.Parse<PlayerType>(lines[lineIndex++]);

        // 行4: 当前玩家
        int currentPlayerNum = int.Parse(lines[lineIndex++]);

        // 行5: P1 库存
        var p1Parts = lines[lineIndex++].Split(',');
        int p1Ordinary = int.Parse(p1Parts[0]);
        int p1Boring = int.Parse(p1Parts[1]);
        int p1Magnetic = int.Parse(p1Parts[2]);

        // 行6: P2 库存
        var p2Parts = lines[lineIndex++].Split(',');
        int p2Ordinary = int.Parse(p2Parts[0]);
        int p2Boring = int.Parse(p2Parts[1]);
        int p2Magnetic = int.Parse(p2Parts[2]);

        // 重建棋盘
        var board = new Board(rows, cols);
        for (int row = 0; row < rows; row++)
        {
            string line = lines[lineIndex++];
            for (int col = 0; col < cols; col++)
            {
                char ch = line[col];
                if (ch != '.') board.SetCell(row, col, ch);
            }
        }

        // 重建玩家
        var player1 = new HumanPlayer(PlayerId.Player1, "Player 1", p1Ordinary, p1Boring, p1Magnetic);
        Player player2 = player2Type == PlayerType.Computer
            ? new ComputerPlayer(PlayerId.Player2, "Computer", p2Ordinary, p2Boring, p2Magnetic)
            : new HumanPlayer(PlayerId.Player2, "Player 2", p2Ordinary, p2Boring, p2Magnetic);

        var rules = new GameRules(winningLength);
        var enabledSpecialTypes = new[] { DiscType.Boring, DiscType.Magnetic };
        Player currentPlayer = currentPlayerNum == 1 ? player1 : player2;

        return new Game(board, player1, player2, rules, ui, enabledSpecialTypes, winningLength, currentPlayer);
    }
}
