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

    /// Returns all valid moves as disc-type and column pairs.
    public List<(DiscType discType, int col)> GetValidMoves(
        Board board, Player player, DiscType[] enabledSpecialTypes)
    {
        var moves = new List<(DiscType, int)>();

        for (int col = 0; col < board.Columns; col++)
        {
            if (board.IsColumnFull(col)) continue;

            // Ordinary disc.
            if (player.CanPlayDisc(DiscType.Ordinary))
                moves.Add((DiscType.Ordinary, col));

            // Special discs.
            foreach (var type in enabledSpecialTypes)
            {
                if (player.CanPlayDisc(type))
                    moves.Add((type, col));
            }
        }

        return moves;
    }

    /// Checks whether the player has any valid move.
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

    /// Checks whether the specified player has a winning line of length winningLength.
    public bool HasWinner(Board board, PlayerId playerId)
    {
        for (int row = 0; row < board.Rows; row++)
        {
            for (int col = 0; col < board.Columns; col++)
            {
                if (!IsPlayerDisc(board, row, col, playerId)) continue;

                // Check four directions: right, down, down-right, and down-left.
                if (CountInDirection(board, row, col, 0, 1, playerId) >= _winningLength) return true;  // Horizontal
                if (CountInDirection(board, row, col, 1, 0, playerId) >= _winningLength) return true;  // Vertical
                if (CountInDirection(board, row, col, 1, 1, playerId) >= _winningLength) return true;  // Diagonal down-right
                if (CountInDirection(board, row, col, 1, -1, playerId) >= _winningLength) return true; // Diagonal down-left
            }
        }
        return false;
    }

    /// Simulates a move to see whether it wins immediately for the computer player.
    public bool MoveWinsImmediately(Board board, Player player, DiscType discType, int col)
    {
        var clonedBoard = CloneBoard(board);

        // Simulate dropping the disc.
        var disc = new OrdinaryDisc(player.Id); // Only the symbol matters here.
        switch (discType)
        {
            case DiscType.Boring: disc = null!; break;
            case DiscType.Magnetic: disc = null!; break;
        }

        // Simplified simulation: drop the matching symbol directly.
        char symbol = discType switch
        {
            DiscType.Ordinary => player.Id == PlayerId.Player1 ? '@' : '#',
            DiscType.Boring => player.Id == PlayerId.Player1 ? 'B' : 'b',
            DiscType.Magnetic => player.Id == PlayerId.Player1 ? 'M' : 'm',
            _ => throw new ArgumentException("Unknown disc type")
        };

        int landedRow = clonedBoard.DropDisc(col, symbol);

        // Simulate special-disc effects.
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

    /// Counts consecutive scoring discs for the player from the start position in one direction.
    private int CountInDirection(Board board, int startRow, int startCol, int rowDelta, int colDelta, PlayerId playerId)
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

    /// Checks whether the specified cell contains a scoring disc owned by the player.
    private bool IsPlayerDisc(Board board, int row, int col, PlayerId playerId)
    {
        char? cell = board.GetCell(row, col);
        if (!cell.HasValue) return false;

        var disc = Disc.FromSymbol(cell.Value);
        return disc.Owner == playerId && disc.CountsForWin;
    }

    /// deep clone the board for simulation
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
