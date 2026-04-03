namespace Lineup.Model;


/// game class - main game loop, game state, and save/load methods.

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

        // calculate disc counts
        int discsPerPlayer = rows * cols / 2;
        int specialCount = _enabledSpecialTypes.Length * 2; // each special disc type has 2 discs
        int ordinaryCount = discsPerPlayer - specialCount;

        _player1 = new HumanPlayer(PlayerId.Player1, "Player 1", ordinaryCount, 2, 2);
        _player2 = player2Type == PlayerType.Computer
            ? new ComputerPlayer(PlayerId.Player2, "Computer", ordinaryCount, 2, 2)
            : new HumanPlayer(PlayerId.Player2, "Player 2", ordinaryCount, 2, 2);

        _currentPlayer = _player1;
    }

    // load game
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

    // save game
    internal Board Board => _board;
    internal Player Player1 => _player1;
    internal Player Player2 => _player2;
    internal Player CurrentPlayer => _currentPlayer;
    internal int WinningLength => _winningLength;
    internal DiscType[] EnabledSpecialTypes => _enabledSpecialTypes;

    /// run the game
    public void Run()
    {
        _ui.ShowGameStart(_board, _winningLength);
        PlayGameLoop();
    }

    /// game loop, returns when game ends (win/tie) or player quits
    private void PlayGameLoop()
    {
        while (true)
        {
            // check if current player has valid move.
            if (!_rules.HasAnyValidMove(_board, _currentPlayer, _enabledSpecialTypes))
            {
                _ui.ShowNoValidMoves(_currentPlayer.Name);
                var opponent = GetOpponent();
                if (!_rules.HasAnyValidMove(_board, opponent, _enabledSpecialTypes))
                {
                    // tie if both players have no valid moves
                    _ui.ShowTie(_board);
                    return;
                }
                // skip turn if current player has no valid moves but opponent can play
                _currentPlayer = opponent;
                continue;
            }

            // show current player's turn (only for human, computer move will be shown in TakeTurn)
            if (_currentPlayer.Type == PlayerType.Human)
                _ui.ShowPlayerTurn(_currentPlayer);

            // current player takes turn
            var result = _currentPlayer.TakeTurn(_board, _ui, _rules, _enabledSpecialTypes, _random);

            //  Save / Quit
            if (result.Action == TurnAction.Quit)
                return;

            if (result.Action == TurnAction.Save)
            {
                SaveGame("lineup_save.txt");
                _ui.ShowSaveSuccess("lineup_save.txt");
                continue;
            }

            // move
            if (!ExecuteMove(_currentPlayer, result.DiscType, result.Column))
                continue; // invalid move

            // check win
            if (_rules.HasWinner(_board, _currentPlayer.Id))
            {
                _ui.ShowWinner(_board, _currentPlayer);
                return;
            }

            // check tie
            if (_board.IsFull)
            {
                _ui.ShowTie(_board);
                return;
            }

            // switch player
            _currentPlayer = GetOpponent();
        }
    }


    /// execute a move, return false if move is invalid (e.g. column full), otherwise perform the move and return true.
    /// return true

    internal bool ExecuteMove(Player player, DiscType discType, int col)
    {
        // verify move
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

        // get disc
        Disc disc = player.UseDisc(discType);

        // put disc
        int landedRow = _board.DropDisc(col, disc.Symbol);

        if (discType != DiscType.Ordinary)
        {
            // spcial disc: show effect frame after applying effect
            _ui.ShowPlacementFrame($"{discType} disc", col + 1);
            _ui.ShowBoard(_board);

            disc.ApplyEffect(_board, landedRow, col, ReturnDiscToOwner);

            _ui.ShowEffectFrame($"{discType} disc effect activated.", _board);
        }
        else
        {
            // normal disc: just show the board after placement
            _ui.ShowBoard(_board);
        }

        return true;
    }

    /// return disc to owner
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

    // ===================== save and load ====================

    /// save game state to a text file
    public void SaveGame(string filePath)
    {
        using var writer = new StreamWriter(filePath);
        // row1: board size
        writer.WriteLine($"{_board.Rows},{_board.Columns}");
        // row2: winning length
        writer.WriteLine(_winningLength);
        // row3: Player2 type
        writer.WriteLine(_player2.Type);
        // row4: current player (1 or 2)
        writer.WriteLine(_currentPlayer == _player1 ? 1 : 2);
        // row5: P1 inventory
        writer.WriteLine($"{_player1.OrdinaryDiscsRemaining},{_player1.BoringDiscsRemaining},{_player1.MagneticDiscsRemaining}");
        // row6: P2 inventory
        writer.WriteLine($"{_player2.OrdinaryDiscsRemaining},{_player2.BoringDiscsRemaining},{_player2.MagneticDiscsRemaining}");
        // row7+: board content, one row per line, empty cells represented by .
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

    /// load game state from a text file, throws exception if file is invalid or not found
    public static Game LoadGame(string filePath, GameConsoleUi ui)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Save file not found: {filePath}");

        var lines = File.ReadAllLines(filePath);
        int lineIndex = 0;

        // row1: board size
        var sizeParts = lines[lineIndex++].Split(',');
        int rows = int.Parse(sizeParts[0]);
        int cols = int.Parse(sizeParts[1]);

        // row2: winning length
        int winningLength = int.Parse(lines[lineIndex++]);

        // row3: Player2 type
        var player2Type = Enum.Parse<PlayerType>(lines[lineIndex++]);

        // row4: current player
        int currentPlayerNum = int.Parse(lines[lineIndex++]);

        // row5: P1 inventory
        var p1Parts = lines[lineIndex++].Split(',');
        int p1Ordinary = int.Parse(p1Parts[0]);
        int p1Boring = int.Parse(p1Parts[1]);
        int p1Magnetic = int.Parse(p1Parts[2]);

        // row6: P2 inventory
        var p2Parts = lines[lineIndex++].Split(',');
        int p2Ordinary = int.Parse(p2Parts[0]);
        int p2Boring = int.Parse(p2Parts[1]);
        int p2Magnetic = int.Parse(p2Parts[2]);

        // rebuild board
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

        // rebuild players and game
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
