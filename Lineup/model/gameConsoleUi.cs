namespace Lineup.Model;

internal enum MainMenuSelection
{
    NewGame,
    Quit
}

internal enum HumanTurnCommandKind
{
    Move,
    Quit
}

internal readonly record struct HumanTurnCommand(HumanTurnCommandKind Kind, DiscType DiscType, int Column);

internal class GameConsoleUi
{
    private readonly DiscType[] enabledSpecialDiscTypes;

    public GameConsoleUi(IEnumerable<DiscType> enabledSpecialDiscTypes)
    {
        this.enabledSpecialDiscTypes = enabledSpecialDiscTypes.ToArray();
    }

    public void ShowWelcome()
    {
        Console.WriteLine("Welcome to LineUp.");
    }

    public MainMenuSelection PromptForMainMenuSelection()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("1. New game");
            Console.WriteLine("2. Quit");

            Console.Write("Select an option: ");
            var input = ReadTrimmedInput();

            if (input is "1" or "new")
            {
                return MainMenuSelection.NewGame;
            }

            if (input is "2" or "quit" or "q")
            {
                return MainMenuSelection.Quit;
            }

            Console.WriteLine("Invalid option. Enter 1 to start a new game or 2 to quit.");
        }
    }

    public void ShowSetupHeader()
    {
        Console.WriteLine();
        Console.WriteLine("Game setup");
    }

    public PlayerType PromptForGameMode()
    {
        while (true)
        {
            Console.Write("Game mode (1 = Human vs Human, 2 = Human vs Computer): ");
            var input = ReadTrimmedInput();

            if (input is "1" or "hvh")
            {
                return PlayerType.Human;
            }

            if (input is "2" or "hvc" or "ai")
            {
                return PlayerType.Computer;
            }

            Console.WriteLine("Invalid game mode. Enter 1 or 2.");
        }
    }

    public int PromptForRows()
    {
        return PromptForInt("Rows (minimum 6): ", value => value >= 6);
    }

    public int PromptForColumns(int rows)
    {
        return PromptForInt(
            $"Columns (minimum 7 and at least {rows}): ",
            value => value >= 7 && value >= rows);
    }

    public int PromptForInt(string prompt, Func<int, bool> validator)
    {
        while (true)
        {
            Console.Write(prompt);
            var input = ReadTrimmedInput();

            if (int.TryParse(input, out var value) && validator(value))
            {
                return value;
            }

            Console.WriteLine("Invalid number.");
        }
    }

    public void ShowGameSetup(Board board, int winningLength, IReadOnlyCollection<DiscType> activeSpecialDiscTypes)
    {
        Console.WriteLine();
        Console.WriteLine($"Board size: {board.Rows} x {board.Columns}");
        Console.WriteLine($"Winning length: {winningLength}");
        Console.WriteLine($"Enabled special discs: {string.Join(", ", activeSpecialDiscTypes)}");
        Console.WriteLine();
        Console.WriteLine(board.Render());
    }

    public void ShowBoard(Board board)
    {
        Console.WriteLine();
        Console.WriteLine(board.Render());
    }

    public void ShowPlayerStatus(Player player)
    {
        var status = new List<string>
        {
            $"ordinary={player.OrdinaryDiscsRemaining}"
        };

        foreach (var specialType in enabledSpecialDiscTypes)
        {
            status.Add(specialType switch
            {
                DiscType.Boring => $"boring={player.BoringDiscsRemaining}",
                DiscType.Magnetic => $"magnetic={player.MagneticDiscsRemaining}",
                DiscType.Exploding => $"exploding={player.ExplodingDiscsRemaining}",
                _ => string.Empty
            });
        }

        Console.WriteLine($"{player.Name}'s turn ({string.Join(", ", status.Where(text => text.Length > 0))})");
        Console.WriteLine($"Move format: {BuildMoveExamples()}");
    }

    public HumanTurnCommand PromptForHumanTurn()
    {
        while (true)
        {
            Console.Write("Enter a move or command: ");
            var input = ReadTrimmedInput();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Enter a column number or type 'help'.");
                continue;
            }

            if (input is "help" or "h" or "?")
            {
                ShowHelp();
                continue;
            }

            if (input is "quit" or "q" or "exit")
            {
                return new HumanTurnCommand(HumanTurnCommandKind.Quit, DiscType.Ordinary, 0);
            }

            if (TryParseMove(input, out var discType, out var column))
            {
                return new HumanTurnCommand(HumanTurnCommandKind.Move, discType, column);
            }

            Console.WriteLine($"Invalid move. Example inputs: {BuildMoveExamples()}.");
        }
    }

    public void ShowNoDiscsRemaining(Player player, DiscType discType)
    {
        Console.WriteLine($"{player.Name} has no {discType} discs remaining.");
    }

    public void ShowInvalidColumn(int columns)
    {
        Console.WriteLine($"Column must be between 1 and {columns}.");
    }

    public void ShowColumnFull(int column)
    {
        Console.WriteLine($"Column {column} is full.");
    }

    public void ShowComputerMove(Player player, MoveChoice move)
    {
        Console.WriteLine($"{player.Name} plays {move.DiscType} in column {move.Column}.");
    }

    public void ShowMoveFrames(IEnumerable<MoveFrame> frames)
    {
        foreach (var frame in frames)
        {
            Console.WriteLine();
            if (!string.IsNullOrWhiteSpace(frame.Message))
            {
                Console.WriteLine(frame.Message);
            }

            Console.WriteLine(frame.BoardText);
        }
    }

    public void ShowNoValidMoves(Player player)
    {
        Console.WriteLine();
        Console.WriteLine($"{player.Name} has no valid moves.");
    }

    public void ShowNoMoreMovesTie()
    {
        Console.WriteLine("No more moves are possible. The game ends in a tie.");
    }

    public void ShowWinner(Board board, Player player)
    {
        Console.WriteLine();
        Console.WriteLine(board.Render());
        Console.WriteLine($"{player.Name} wins.");
    }

    public void ShowTie(Board board)
    {
        Console.WriteLine();
        Console.WriteLine(board.Render());
        Console.WriteLine("The game ends in a tie.");
    }

    private void ShowHelp()
    {
        Console.WriteLine();
        Console.WriteLine("Commands");
        Console.WriteLine("  4              play an ordinary disc in column 4");
        Console.WriteLine("  o 4            play an ordinary disc in column 4");

        if (enabledSpecialDiscTypes.Contains(DiscType.Boring))
        {
            Console.WriteLine("  b 4            play a boring disc in column 4");
        }

        if (enabledSpecialDiscTypes.Contains(DiscType.Magnetic))
        {
            Console.WriteLine("  m 4            play a magnetic disc in column 4");
        }

        if (enabledSpecialDiscTypes.Contains(DiscType.Exploding))
        {
            Console.WriteLine("  e 4            play an exploding disc in column 4");
        }

        Console.WriteLine("  help           show this menu");
        Console.WriteLine("  quit           return to the main menu");
    }

    private string BuildMoveExamples()
    {
        var examples = new List<string> { "4", "o 4" };

        if (enabledSpecialDiscTypes.Contains(DiscType.Boring))
        {
            examples.Add("b 4");
        }

        if (enabledSpecialDiscTypes.Contains(DiscType.Magnetic))
        {
            examples.Add("m 4");
        }

        if (enabledSpecialDiscTypes.Contains(DiscType.Exploding))
        {
            examples.Add("e 4");
        }

        return string.Join(", ", examples);
    }

    private bool TryParseMove(string input, out DiscType discType, out int column)
    {
        discType = DiscType.Ordinary;
        column = 0;

        var parts = input
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length == 1)
        {
            return int.TryParse(parts[0], out column);
        }

        if (parts.Length != 2 || !TryParseDiscType(parts[0], out discType))
        {
            return false;
        }

        return int.TryParse(parts[1], out column);
    }

    private bool TryParseDiscType(string token, out DiscType discType)
    {
        discType = token.ToLowerInvariant() switch
        {
            "o" or "ord" or "ordinary" => DiscType.Ordinary,
            "b" or "boring" => DiscType.Boring,
            "m" or "magnetic" => DiscType.Magnetic,
            "e" or "exploding" => DiscType.Exploding,
            _ => (DiscType)(-1)
        };

        if (discType == (DiscType)(-1))
        {
            return false;
        }

        return discType == DiscType.Ordinary || enabledSpecialDiscTypes.Contains(discType);
    }

    private static string ReadTrimmedInput()
    {
        return (Console.ReadLine() ?? string.Empty).Trim();
    }
}
