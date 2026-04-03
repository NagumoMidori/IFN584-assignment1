namespace Lineup.Model;

/// <summary>
/// Test mode that parses a move sequence and executes it automatically on a 6x7 board.
/// </summary>
public class TestModeRunner
{
    private readonly GameConsoleUi _ui;

    public TestModeRunner(GameConsoleUi ui)
    {
        _ui = ui;
    }

    /// <summary>Runs test mode.</summary>
    public void Run(string sequence)
    {
        var moves = ParseSequence(sequence);

        // Create a 6x7 game with two human players. Test mode does not need AI.
        var board = new Board(6, 7);
        int winningLength = 4;
        var rules = new GameRules(winningLength);

        int discsPerPlayer = 6 * 7 / 2; // = 21
        int ordinaryCount = discsPerPlayer - 4; // Four special discs total: Boring x2 and Magnetic x2.

        var player1 = new HumanPlayer(PlayerId.Player1, "Player 1", ordinaryCount, 2, 2);
        var player2 = new HumanPlayer(PlayerId.Player2, "Player 2", ordinaryCount, 2, 2);

        var enabledSpecialTypes = new[] { DiscType.Boring, DiscType.Magnetic };
        Player currentPlayer = player1;

        _ui.ShowGameStart(board, winningLength);

        foreach (var (discType, col) in moves)
        {
            // Show the current player.
            Console.WriteLine($"{currentPlayer.Name} plays {discType} in column {col + 1}.");

            // Validate the move.
            if (!rules.IsValidColumn(board, col))
                throw new InvalidOperationException($"Invalid column: {col + 1}.");
            if (board.IsColumnFull(col))
                throw new InvalidOperationException($"Column {col + 1} is full.");
            if (!currentPlayer.CanPlayDisc(discType))
                throw new InvalidOperationException($"{currentPlayer.Name} has no {discType} discs remaining.");

            // Consume the disc and place it on the board.
            Disc disc = currentPlayer.UseDisc(discType);
            int landedRow = board.DropDisc(col, disc.Symbol);

            // Render special-disc placement and effect as separate frames.
            if (discType != DiscType.Ordinary)
            {
                _ui.ShowBoard(board); // Placement frame.

                // Apply the special effect.
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

            // Check for a win.
            if (rules.HasWinner(board, currentPlayer.Id))
            {
                _ui.ShowWinner(board, currentPlayer);
                return;
            }

            // Check for a draw.
            if (board.IsFull)
            {
                _ui.ShowTie(board);
                return;
            }

            // Switch players.
            currentPlayer = currentPlayer == player1 ? player2 : player1;
        }

        // All moves were executed without a winner.
        Console.WriteLine("\nAll moves executed. No winner yet.");
        _ui.ShowBoard(board);
    }

    /// <summary>Parses the move-sequence string.</summary>
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

            // The first character indicates the disc type.
            DiscType discType = char.ToUpper(token[0]) switch
            {
                'O' => DiscType.Ordinary,
                'B' => DiscType.Boring,
                'M' => DiscType.Magnetic,
                _ => throw new ArgumentException($"Unknown disc type: '{token[0]}'.")
            };

            // The remaining characters represent the 1-based column number.
            if (!int.TryParse(token[1..], out int col1based))
                throw new ArgumentException($"Invalid column in move: '{token}'.");

            int col0based = col1based - 1; // 1-based → 0-based
            moves.Add((discType, col0based));
        }

        return moves;
    }
}
