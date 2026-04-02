namespace Lineup.Model;

/// <summary>
/// 测试模式 — 解析走法序列并在 6×7 棋盘上自动执行
/// </summary>
public class TestModeRunner
{
    private readonly GameConsoleUi _ui;

    public TestModeRunner(GameConsoleUi ui)
    {
        _ui = ui;
    }

    /// <summary>运行测试模式</summary>
    public void Run(string sequence)
    {
        var moves = ParseSequence(sequence);

        // 创建 6×7 游戏，两个 HumanPlayer（测试模式不需要 AI）
        var board = new Board(6, 7);
        int winningLength = 4;
        var rules = new GameRules(winningLength);

        int discsPerPlayer = 6 * 7 / 2; // = 21
        int ordinaryCount = discsPerPlayer - 4; // 4 个特殊棋子（Boring×2 + Magnetic×2）

        var player1 = new HumanPlayer(PlayerId.Player1, "Player 1", ordinaryCount, 2, 2);
        var player2 = new HumanPlayer(PlayerId.Player2, "Player 2", ordinaryCount, 2, 2);

        var enabledSpecialTypes = new[] { DiscType.Boring, DiscType.Magnetic };
        Player currentPlayer = player1;

        _ui.ShowGameStart(board, winningLength);

        foreach (var (discType, col) in moves)
        {
            // 显示当前玩家
            Console.WriteLine($"{currentPlayer.Name} plays {discType} in column {col + 1}.");

            // 验证走法
            if (!rules.IsValidColumn(board, col))
                throw new InvalidOperationException($"Invalid column: {col + 1}.");
            if (board.IsColumnFull(col))
                throw new InvalidOperationException($"Column {col + 1} is full.");
            if (!currentPlayer.CanPlayDisc(discType))
                throw new InvalidOperationException($"{currentPlayer.Name} has no {discType} discs remaining.");

            // 扣减库存并落子
            Disc disc = currentPlayer.UseDisc(discType);
            int landedRow = board.DropDisc(col, disc.Symbol);

            // 特殊棋子分帧显示
            if (discType != DiscType.Ordinary)
            {
                _ui.ShowBoard(board); // 放置帧

                // 执行特殊效果
                Action<Disc> returnDisc = d =>
                {
                    if (d.Owner == PlayerId.Player1) player1.ReturnDisc(d);
                    else player2.ReturnDisc(d);
                };
                disc.ApplyEffect(board, landedRow, col, returnDisc);

                _ui.ShowEffectFrame($"{discType} disc effect activated.", board);
            }
            else
            {
                _ui.ShowBoard(board);
            }

            // 检查胜利
            if (rules.HasWinner(board, currentPlayer.Id))
            {
                _ui.ShowWinner(board, currentPlayer);
                return;
            }

            // 检查平局
            if (board.IsFull)
            {
                _ui.ShowTie(board);
                return;
            }

            // 切换玩家
            currentPlayer = currentPlayer == player1 ? player2 : player1;
        }

        // 所有步骤完成但无胜负
        Console.WriteLine("\nAll moves executed. No winner yet.");
        _ui.ShowBoard(board);
    }

    /// <summary>解析走法序列字符串</summary>
    private List<(DiscType discType, int col)> ParseSequence(string sequence)
    {
        var moves = new List<(DiscType, int)>();
        var tokens = sequence.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (tokens.Length == 0)
            throw new ArgumentException("Move sequence cannot be empty.");

        foreach (var token in tokens)
        {
            if (token.Length < 2)
                throw new ArgumentException($"Invalid move: '{token}'.");

            // 第一个字符是棋子类型
            DiscType discType = char.ToUpper(token[0]) switch
            {
                'O' => DiscType.Ordinary,
                'B' => DiscType.Boring,
                'M' => DiscType.Magnetic,
                _ => throw new ArgumentException($"Unknown disc type: '{token[0]}'.")
            };

            // 剩余部分是列号（1-based）
            if (!int.TryParse(token[1..], out int col1based))
                throw new ArgumentException($"Invalid column in move: '{token}'.");

            int col0based = col1based - 1; // 1-based → 0-based
            moves.Add((discType, col0based));
        }

        return moves;
    }
}
