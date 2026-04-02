namespace Lineup.Model;

// games rules
public class GameRules
{
    private readonly int _winningLength;

    public int WinningLength => _winningLength;

    public GameRules(int winningLength)
    {
        if (winningLength < 4)
            throw new ArgumentOutOfRangeException(nameof(winningLength), "Winning length must be at least 4.");
        _winningLength = winningLength;
    }

    /// check if column is valid
    public bool IsValidColumn(Board board, int col)
    {
        return col >= 0 && col < board.Columns;
    }

    /// <summary>获取所有合法走法（列号 + 棋子类型）</summary>
    public List<(DiscType discType, int col)> GetValidMoves(
        Board board, Player player, DiscType[] enabledSpecialTypes)
    {
        var moves = new List<(DiscType, int)>();

        for (int col = 0; col < board.Columns; col++)
        {
            if (board.IsColumnFull(col)) continue;

            // 普通棋子
            if (player.CanPlayDisc(DiscType.Ordinary))
                moves.Add((DiscType.Ordinary, col));

            // 特殊棋子
            foreach (var type in enabledSpecialTypes)
            {
                if (player.CanPlayDisc(type))
                    moves.Add((type, col));
            }
        }

        return moves;
    }

    /// <summary>检查玩家是否有任何合法走法</summary>
    public bool HasAnyValidMove(Board board, Player player, DiscType[] enabledSpecialTypes)
    {
        for (int col = 0; col < board.Columns; col++)
        {
            if (board.IsColumnFull(col)) continue;

            if (player.CanPlayDisc(DiscType.Ordinary)) return true;

            foreach (var type in enabledSpecialTypes)
            {
                if (player.CanPlayDisc(type)) return true;
            }
        }
        return false;
    }

    /// <summary>检查指定玩家是否获胜（在棋盘上有连续 winningLength 个己方棋子）</summary>
    public bool HasWinner(Board board, PlayerId playerId)
    {
        for (int row = 0; row < board.Rows; row++)
        {
            for (int col = 0; col < board.Columns; col++)
            {
                if (!IsPlayerDisc(board, row, col, playerId)) continue;

                // 检查四个方向：右、下、右下、左下
                if (CountInDirection(board, row, col, 0, 1, playerId) >= _winningLength) return true;  // 水平→
                if (CountInDirection(board, row, col, 1, 0, playerId) >= _winningLength) return true;  // 垂直↓
                if (CountInDirection(board, row, col, 1, 1, playerId) >= _winningLength) return true;  // 对角线↘
                if (CountInDirection(board, row, col, 1, -1, playerId) >= _winningLength) return true; // 对角线↙
            }
        }
        return false;
    }

    /// <summary>模拟一步走法，检查是否立即获胜（用于电脑 AI）</summary>
    public bool MoveWinsImmediately(Board board, Player player, DiscType discType, int col)
    {
        var clonedBoard = CloneBoard(board);

        // 模拟落子
        var disc = new OrdinaryDisc(player.Id); // 只需要符号，用对应类型创建
        switch (discType)
        {
            case DiscType.Boring: disc = null!; break;
            case DiscType.Magnetic: disc = null!; break;
        }

        // 简化：用对应符号落子
        char symbol = discType switch
        {
            DiscType.Ordinary => player.Id == PlayerId.Player1 ? '@' : '#',
            DiscType.Boring => player.Id == PlayerId.Player1 ? 'B' : 'b',
            DiscType.Magnetic => player.Id == PlayerId.Player1 ? 'M' : 'm',
            _ => throw new ArgumentException("Unknown disc type")
        };

        int landedRow = clonedBoard.DropDisc(col, symbol);

        // 模拟特殊效果
        switch (discType)
        {
            case DiscType.Boring:
                var boringDisc = new BoringDisc(player.Id);
                boringDisc.ApplyEffect(clonedBoard, landedRow, col, null);
                break;
            case DiscType.Magnetic:
                var magneticDisc = new MagneticDisc(player.Id);
                magneticDisc.ApplyEffect(clonedBoard, landedRow, col, null);
                break;
        }

        return HasWinner(clonedBoard, player.Id);
    }

    /// <summary>从起点沿指定方向计数连续的己方棋子</summary>
    private int CountInDirection(Board board, int startRow, int startCol,
        int rowDelta, int colDelta, PlayerId playerId)
    {
        int count = 0;
        int row = startRow;
        int col = startCol;

        while (row >= 0 && row < board.Rows && col >= 0 && col < board.Columns)
        {
            if (!IsPlayerDisc(board, row, col, playerId)) break;
            count++;
            row += rowDelta;
            col += colDelta;
        }
        return count;
    }

    /// <summary>检查指定位置是否是指定玩家的可计分棋子</summary>
    private bool IsPlayerDisc(Board board, int row, int col, PlayerId playerId)
    {
        char? cell = board.GetCell(row, col);
        if (!cell.HasValue) return false;

        var disc = Disc.FromSymbol(cell.Value);
        return disc.Owner == playerId && disc.CountsForWin;
    }

    /// <summary>深拷贝棋盘</summary>
    private Board CloneBoard(Board source)
    {
        var clone = new Board(source.Rows, source.Columns);
        for (int row = 0; row < source.Rows; row++)
        {
            for (int col = 0; col < source.Columns; col++)
            {
                clone.SetCell(row, col, source.GetCell(row, col));
            }
        }
        return clone;
    }
}
