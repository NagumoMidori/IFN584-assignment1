namespace Lineup.Model;

internal readonly record struct MoveChoice(DiscType DiscType, int Column);

internal sealed record MoveFrame(string Message, string BoardText);

internal sealed record MoveResult(bool RenderBoardAtTurnStart, IReadOnlyList<MoveFrame> Frames);

internal class GameRules
{
    private readonly int winningLength;

    public GameRules(int winningLength)
    {
        if (winningLength < 4)
        {
            throw new ArgumentOutOfRangeException(nameof(winningLength), "Winning length must be at least 4.");
        }

        this.winningLength = winningLength;
    }

    public bool IsValidColumn(Board board, int column)
    {
        return column >= 1 && column <= board.Columns;
    }

    public IEnumerable<MoveChoice> GetValidMoves(
        Board board,
        Player player,
        IReadOnlyCollection<DiscType> enabledSpecialDiscTypes)
    {
        var discTypes = new List<DiscType>();
        if (player.CanPlayDisc(DiscType.Ordinary))
        {
            discTypes.Add(DiscType.Ordinary);
        }

        discTypes.AddRange(enabledSpecialDiscTypes.Where(player.CanPlaySpecialDisc));

        for (var column = 1; column <= board.Columns; column++)
        {
            if (board.IsColumnFull(column))
            {
                continue;
            }

            foreach (var discType in discTypes)
            {
                yield return new MoveChoice(discType, column);
            }
        }
    }

    public bool HasAnyValidMove(Board board, Player player, IReadOnlyCollection<DiscType> enabledSpecialDiscTypes)
    {
        return GetValidMoves(board, player, enabledSpecialDiscTypes).Any();
    }

    public bool MoveWinsImmediately(Board board, Player player, MoveChoice move)
    {
        var simulationBoard = CloneBoard(board);
        ApplyMove(simulationBoard, player, move.DiscType, move.Column, applyReturns: false, returnDiscToOwner: null, useInventory: false);
        return HasWinner(simulationBoard, player.Id);
    }

    public MoveResult ApplyMove(
        Board board,
        Player player,
        DiscType discType,
        int column,
        bool applyReturns,
        Action<Disc>? returnDiscToOwner,
        bool useInventory = true)
    {
        if (column < 1 || column > board.Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(column), $"Column must be between 1 and {board.Columns}.");
        }

        if (board.IsColumnFull(column))
        {
            throw new InvalidOperationException($"Column {column} is full.");
        }

        if (useInventory && !player.CanPlayDisc(discType))
        {
            throw new InvalidOperationException($"{player.Name} has no {discType} discs remaining.");
        }

        var frames = new List<MoveFrame>();
        var disc = useInventory ? player.UseDisc(discType) : new Disc(player.Id, discType);
        var row = board.DropDisc(column, disc.Symbol);

        if (discType != DiscType.Ordinary)
        {
            frames.Add(new MoveFrame(
                $"{player.Name} placed {discType} in column {column}.",
                board.Render()));
        }

        switch (discType)
        {
            case DiscType.Boring:
                ApplyBoringEffect(board, row, column, applyReturns, returnDiscToOwner, frames);
                break;
            case DiscType.Magnetic:
                ApplyMagneticEffect(board, player.Id, row, column, frames);
                break;
            case DiscType.Exploding:
                ApplyExplodingEffect(board, row, column, frames);
                break;
        }

        return new MoveResult(discType == DiscType.Ordinary, frames);
    }

    public bool HasWinner(Board board, PlayerId playerId)
    {
        for (var row = 1; row <= board.Rows; row++)
        {
            for (var column = 1; column <= board.Columns; column++)
            {
                if (!IsWinningDisc(board, row, column, playerId))
                {
                    continue;
                }

                if (CountDirection(board, row, column, 1, 0, playerId) >= winningLength ||
                    CountDirection(board, row, column, 0, 1, playerId) >= winningLength ||
                    CountDirection(board, row, column, 1, 1, playerId) >= winningLength ||
                    CountDirection(board, row, column, 1, -1, playerId) >= winningLength)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private void ApplyBoringEffect(
        Board board,
        int landedRow,
        int column,
        bool applyReturns,
        Action<Disc>? returnDiscToOwner,
        ICollection<MoveFrame> frames)
    {
        for (var row = 1; row <= board.Rows; row++)
        {
            if (row == landedRow)
            {
                continue;
            }

            var symbol = board.GetCell(row, column);
            if (!symbol.HasValue)
            {
                continue;
            }

            if (applyReturns && returnDiscToOwner is not null)
            {
                returnDiscToOwner(NormalizeReturnedDisc(symbol.Value));
            }

            board.ClearCell(row, column);
        }

        board.CollapseColumn(column);
        frames.Add(new MoveFrame("Boring disc effect activated.", board.Render()));
    }

    private void ApplyMagneticEffect(
        Board board,
        PlayerId playerId,
        int landedRow,
        int column,
        ICollection<MoveFrame> frames)
    {
        int? sourceRow = null;

        for (var row = landedRow - 1; row >= 1; row--)
        {
            var symbol = board.GetCell(row, column);
            if (!symbol.HasValue)
            {
                continue;
            }

            var disc = Disc.FromSymbol(symbol.Value);
            if (ActsAsOrdinaryOnBoard(disc) && disc.Owner == playerId)
            {
                sourceRow = row;
                break;
            }
        }

        if (sourceRow.HasValue && sourceRow.Value < landedRow - 1)
        {
            var movingSymbol = board.GetCell(sourceRow.Value, column);
            var destinationSymbol = board.GetCell(sourceRow.Value + 1, column);
            board.SetCell(sourceRow.Value + 1, column, movingSymbol);
            board.SetCell(sourceRow.Value, column, destinationSymbol);
        }

        frames.Add(new MoveFrame("Magnetic disc effect activated.", board.Render()));
    }

    private void ApplyExplodingEffect(
        Board board,
        int landedRow,
        int column,
        ICollection<MoveFrame> frames)
    {
        for (var row = Math.Max(1, landedRow - 1); row <= Math.Min(board.Rows, landedRow + 1); row++)
        {
            for (var targetColumn = Math.Max(1, column - 1); targetColumn <= Math.Min(board.Columns, column + 1); targetColumn++)
            {
                board.ClearCell(row, targetColumn);
            }
        }

        frames.Add(new MoveFrame("Exploding disc detonated.", board.Render()));

        board.CollapseAllColumns();
        frames.Add(new MoveFrame("Grid after the explosion settles.", board.Render()));
    }

    private int CountDirection(Board board, int startRow, int startColumn, int rowDelta, int columnDelta, PlayerId playerId)
    {
        var count = 0;
        var row = startRow;
        var column = startColumn;

        while (row >= 1 && row <= board.Rows && column >= 1 && column <= board.Columns)
        {
            if (!IsWinningDisc(board, row, column, playerId))
            {
                break;
            }

            count++;
            row += rowDelta;
            column += columnDelta;
        }

        return count;
    }

    private bool IsWinningDisc(Board board, int row, int column, PlayerId playerId)
    {
        var symbol = board.GetCell(row, column);
        if (!symbol.HasValue)
        {
            return false;
        }

        var disc = Disc.FromSymbol(symbol.Value);
        return disc.Owner == playerId && disc.CountsForWin;
    }

    private static Board CloneBoard(Board sourceBoard)
    {
        var clone = new Board(sourceBoard.Rows, sourceBoard.Columns);

        for (var row = 1; row <= sourceBoard.Rows; row++)
        {
            for (var column = 1; column <= sourceBoard.Columns; column++)
            {
                clone.SetCell(row, column, sourceBoard.GetCell(row, column));
            }
        }

        return clone;
    }

    private static Disc NormalizeReturnedDisc(char symbol)
    {
        var disc = Disc.FromSymbol(symbol);
        return ActsAsOrdinaryOnBoard(disc) ? disc.ToOrdinary() : disc;
    }

    private static bool ActsAsOrdinaryOnBoard(Disc disc)
    {
        return disc.Type != DiscType.Exploding;
    }
}
