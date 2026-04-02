namespace Lineup.Model;

public class GameConsoleUi
{
    // ==================== main menu ====================

    /// display main menu（1=New, 2=Load, 3=Test, 4=Quit)
    public int ShowMainMenu()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("--------------------------------");
            Console.WriteLine("  LineUp - Main Menu");
            Console.WriteLine("--------------------------------");
            Console.WriteLine("  1. New Game");
            Console.WriteLine("  2. Load Game");
            Console.WriteLine("  3. Testing Mode");
            Console.WriteLine("  4. Quit");
            Console.WriteLine("--------------------------------");
            Console.Write("Select an option: ");

            string input = ReadInput();
            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= 4)
                return choice;

            Console.WriteLine("Invalid option. Please enter 1-4.");
        }
    }

    // ==================== new game ====================

    public PlayerType PromptGameMode()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("--------------------------------");
            Console.Write("Game mode (1 = Human vs Human, 2 = Human vs Computer): ");

            string input = ReadInput();
            if (input == "1") return PlayerType.Human;
            if (input == "2") return PlayerType.Computer;

            Console.WriteLine("Invalid option. Please enter 1 or 2.");
        }
    }

    /// input rows
    public int PromptRows()
    {
        while (true)
        {
            Console.Write("Enter number of rows (minimum 6): ");
            string input = ReadInput();
            if (int.TryParse(input, out int rows) && rows >= 6)
                return rows;
            Console.WriteLine("Invalid. Rows must be at least 6.");
        }
    }

    /// input columns
    public int PromptColumns(int rows)
    {
        while (true)
        {
            Console.Write($"Enter number of columns (minimum 7, at least {rows}): ");
            string input = ReadInput();
            if (int.TryParse(input, out int cols) && cols >= 7 && cols >= rows)
                return cols;
            Console.WriteLine($"Invalid. Columns must be at least 7 and at least {rows}.");
        }
    }

    // ==================== during game ====================

    /// board size and win condition
    public void ShowGameStart(Board board, int winningLength)
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"  Board: {board.Rows} x {board.Columns}");
        Console.WriteLine($"  Win condition: {winningLength} in a row");
        Console.WriteLine("--------------------------------");
        Console.WriteLine(board.Render());
    }

    /// show board
    public void ShowBoard(Board board)
    {
        Console.WriteLine();
        Console.WriteLine(board.Render());
    }

    /// show current turn and inventory
    public void ShowPlayerTurn(Player player)
    {
        Console.WriteLine("--------------------------------");
        Console.Write($"{player.Name}'s turn  |  ");
        Console.Write($"ordinary: {player.OrdinaryDiscsRemaining}");
        Console.Write($", boring: {player.BoringDiscsRemaining}");
        Console.Write($", magnetic: {player.MagneticDiscsRemaining}");
        Console.WriteLine();
        Console.WriteLine("--------------------------------");
    }

    /// show special disc placement frame
    public void ShowPlacementFrame(string discName, int col1based)
    {
        Console.WriteLine($"  {discName} placed in column {col1based}.");
    }

    /// show special disc effect frame
    public void ShowEffectFrame(string effectMessage, Board board)
    {
        Console.WriteLine($"  {effectMessage}");
        Console.WriteLine(board.Render());
    }

    /// show computer move
    public void ShowComputerMove(string playerName, DiscType discType, int col1based)
    {
        Console.WriteLine($"{playerName} plays {discType} in column {col1based}.");
    }

    // ==================== human move input ====================

    /// return TurnResult
    /// format: "4"(normal)"b 4"（Boring col4）、"m 4"（Magnetic col4）
    /// or save / help / quit
    
    public TurnResult PromptHumanMove()
    {
        while (true)
        {
            Console.Write("Enter move (or help/save/quit): ");
            string input = ReadInput();

            if (string.IsNullOrWhiteSpace(input))
            {
                Console.WriteLine("Please enter a command. Type 'help' for options.");
                continue;
            }

            // options
            if (input == "help" || input == "h" || input == "?")
            {
                ShowHelp();
                continue;
            }
            if (input == "save" || input == "s")
                return new TurnResult { Action = TurnAction.Save };
            if (input == "quit" || input == "q" || input == "exit")
                return new TurnResult { Action = TurnAction.Quit };

            // parse move
            if (TryParseMove(input, out DiscType discType, out int col0based))
            {
                return new TurnResult
                {
                    Action = TurnAction.Move,
                    DiscType = discType,
                    Column = col0based
                };
            }

            Console.WriteLine("Invalid move. Examples: 4, b 4, m 4. Type 'help' for details.");
        }
    }

    /// parse move input, return disc type and location
    private bool TryParseMove(string input, out DiscType discType, out int col0based)
    {
        discType = DiscType.Ordinary;
        col0based = 0;

        string[] parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length == 1)
        {
            // pure number, means ordinary disc
            if (int.TryParse(parts[0], out int col1))
            {
                col0based = col1 - 1; // 1-based → 0-based
                return true;
            }
            return false;
        }

        if (parts.Length == 2)
        {
            // "type column"
            string typeStr = parts[0].ToLower();
            if (!int.TryParse(parts[1], out int col2)) return false;

            col0based = col2 - 1; // 1-based → 0-based

            switch (typeStr)
            {
                case "o": case "ordinary":
                    discType = DiscType.Ordinary; return true;
                case "b": case "boring":
                    discType = DiscType.Boring; return true;
                case "m": case "magnetic":
                    discType = DiscType.Magnetic; return true;
                default:
                    return false;
            }
        }

        return false;
    }

    // ==================== help ====================


    public void ShowHelp()
    {
        Console.WriteLine();
        Console.WriteLine("--------------------------------");
        Console.WriteLine("  Commands:");
        Console.WriteLine("--------------------------------");
        Console.WriteLine("  4          - play ordinary disc in column 4");
        Console.WriteLine("  o 4        - play ordinary disc in column 4");
        Console.WriteLine("  b 4        - play boring disc in column 4");
        Console.WriteLine("  m 4        - play magnetic disc in column 4");
        Console.WriteLine("  save       - save the current game");
        Console.WriteLine("  help       - show this menu");
        Console.WriteLine("  quit       - quit the game");
        Console.WriteLine("--------------------------------");
    }

    // ==================== error ====================

    public void ShowInvalidColumn(int maxCol1based)
    {
        Console.WriteLine($"Invalid column. Please enter 1-{maxCol1based}.");
    }

    public void ShowColumnFull(int col1based)
    {
        Console.WriteLine($"Column {col1based} is full. Choose another column.");
    }

    public void ShowNoDiscsRemaining(string playerName, DiscType discType)
    {
        Console.WriteLine($"{playerName} has no {discType} discs remaining.");
    }

    public void ShowError(string message)
    {
        Console.WriteLine($"Error: {message}");
    }

    // ==================== result ====================

    public void ShowWinner(Board board, Player winner)
    {
        Console.WriteLine();
        Console.WriteLine(board.Render());
        Console.WriteLine("================================");
        Console.WriteLine($"  {winner.Name} wins!");
        Console.WriteLine("================================");
    }

    public void ShowTie(Board board)
    {
        Console.WriteLine();
        Console.WriteLine(board.Render());
        Console.WriteLine("================================");
        Console.WriteLine("  The game ends in a tie.");
        Console.WriteLine("================================");
    }

    public void ShowNoValidMoves(string playerName)
    {
        Console.WriteLine($"{playerName} has no valid moves. Skipping turn.");
    }

    // ==================== save and load ====================

    public void ShowSaveSuccess(string filePath)
    {
        Console.WriteLine($"Game saved to: {filePath}");
    }

    public string PromptLoadFilePath()
    {
        while (true)
        {
            Console.Write("Enter save file path (or 'cancel' to go back): ");
            string input = ReadInput();
            if (!string.IsNullOrWhiteSpace(input))
                return input;
            Console.WriteLine("Please enter a file path.");
        }
    }

    // ==================== testing mode ====================

    public string PromptTestSequence()
    {
        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("--------------------------------");
            Console.WriteLine("  Testing Mode (6x7 board)");
            Console.WriteLine("--------------------------------");
            Console.Write("Enter move sequence (e.g. O4,O5,B3,M6): ");
            string input = ReadInput();
            if (!string.IsNullOrWhiteSpace(input))
                return input;
            Console.WriteLine("Sequence cannot be empty.");
        }
    }

    // ==================== utils ====================

    private static string ReadInput()
    {
        return (Console.ReadLine() ?? "").Trim();
    }
}
