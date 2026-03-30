namespace Lineup.Model;

public class Game
{
    private readonly Random random;
    private readonly DiscType[] enabledSpecialDiscTypes;
    private readonly GameConsoleUi ui;
    private readonly Board board;
    private readonly Player player1;
    private readonly Player player2;
    private readonly GameRules rules;
    private readonly int winningLength;

    private Player currentPlayer;
    private bool quitRequested;
    private bool renderBoardAtTurnStart;

    public Game(int rows, int columns, PlayerType playerTwoType, params DiscType[] enabledSpecialDiscTypes)
    {
        if (rows < 6)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), "Rows must be at least 6.");
        }

        if (columns < 7 || columns < rows)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), $"Columns must be at least 7 and at least {rows}.");
        }

        random = new Random();
        this.enabledSpecialDiscTypes = NormalizeSpecialDiscTypes(enabledSpecialDiscTypes);
        ui = new GameConsoleUi(this.enabledSpecialDiscTypes);

        board = new Board(rows, columns);
        winningLength = DetermineWinningLength(rows, columns);
        rules = new GameRules(winningLength);

        var discsPerPlayer = rows * columns / 2;
        var specialDiscCount = this.enabledSpecialDiscTypes.Length * 2;
        var ordinaryDiscCount = discsPerPlayer - specialDiscCount;

        if (ordinaryDiscCount < 0)
        {
            throw new InvalidOperationException("Board is too small for the configured number of special discs.");
        }

        player1 = CreatePlayer(PlayerId.Player1, PlayerType.Human, "Player 1", ordinaryDiscCount);
        player2 = CreatePlayer(
            PlayerId.Player2,
            playerTwoType,
            playerTwoType == PlayerType.Computer ? "Computer" : "Player 2",
            ordinaryDiscCount);

        currentPlayer = player1;
        quitRequested = false;
        renderBoardAtTurnStart = false;
    }

    public void Run()
    {
        ui.ShowGameSetup(board, winningLength, enabledSpecialDiscTypes);
        PlayGame();
    }

    private static DiscType[] NormalizeSpecialDiscTypes(DiscType[] enabledSpecialDiscTypes)
    {
        if (enabledSpecialDiscTypes.Length == 0)
        {
            return new[] { DiscType.Boring, DiscType.Magnetic };
        }

        var normalized = enabledSpecialDiscTypes
            .Where(type => type != DiscType.Ordinary)
            .Distinct()
            .ToArray();

        if (normalized.Length != 2)
        {
            throw new ArgumentException("LineUp requires exactly two special disc types to be enabled.");
        }

        return normalized;
    }

    private static int DetermineWinningLength(int rows, int columns)
    {
        if (rows == 6 && columns == 7)
        {
            return 4;
        }

        var calculatedLength = (int)Math.Ceiling(rows * columns * 0.1);
        return Math.Min(columns, Math.Max(4, calculatedLength));
    }

    private Player CreatePlayer(PlayerId id, PlayerType type, string name, int ordinaryDiscCount)
    {
        return new Player(
            id,
            type,
            name,
            ordinaryDiscCount,
            enabledSpecialDiscTypes.Contains(DiscType.Boring) ? 2 : 0,
            enabledSpecialDiscTypes.Contains(DiscType.Magnetic) ? 2 : 0,
            enabledSpecialDiscTypes.Contains(DiscType.Exploding) ? 2 : 0);
    }

    private void PlayGame()
    {
        while (!quitRequested)
        {
            if (!rules.HasAnyValidMove(board, currentPlayer, enabledSpecialDiscTypes))
            {
                ui.ShowNoValidMoves(currentPlayer);

                var opponent = GetOpponent(currentPlayer.Id);
                if (!rules.HasAnyValidMove(board, opponent, enabledSpecialDiscTypes))
                {
                    ui.ShowNoMoreMovesTie();
                    return;
                }

                currentPlayer = opponent;
                renderBoardAtTurnStart = true;
                continue;
            }

            if (renderBoardAtTurnStart)
            {
                ui.ShowBoard(board);
            }

            ui.ShowPlayerStatus(currentPlayer);

            if (currentPlayer.IsComputer)
            {
                PerformComputerTurn(currentPlayer);
            }
            else
            {
                var movePlayed = HandleHumanTurn(currentPlayer);
                if (!movePlayed)
                {
                    if (quitRequested)
                    {
                        return;
                    }

                    continue;
                }
            }

            if (rules.HasWinner(board, currentPlayer.Id))
            {
                ui.ShowWinner(board, currentPlayer);
                return;
            }

            if (board.IsFull)
            {
                ui.ShowTie(board);
                return;
            }

            currentPlayer = GetOpponent(currentPlayer.Id);
        }
    }

    private bool HandleHumanTurn(Player player)
    {
        while (true)
        {
            var command = ui.PromptForHumanTurn();
            if (command.Kind == HumanTurnCommandKind.Quit)
            {
                quitRequested = true;
                return false;
            }

            if (!player.CanPlayDisc(command.DiscType))
            {
                ui.ShowNoDiscsRemaining(player, command.DiscType);
                continue;
            }

            if (!rules.IsValidColumn(board, command.Column))
            {
                ui.ShowInvalidColumn(board.Columns);
                continue;
            }

            if (board.IsColumnFull(command.Column))
            {
                ui.ShowColumnFull(command.Column);
                continue;
            }

            var moveResult = rules.ApplyMove(
                board,
                player,
                command.DiscType,
                command.Column,
                applyReturns: true,
                returnDiscToOwner: ReturnDiscToOwner);

            ui.ShowMoveFrames(moveResult.Frames);
            renderBoardAtTurnStart = moveResult.RenderBoardAtTurnStart;
            return true;
        }
    }

    private void PerformComputerTurn(Player player)
    {
        var validMoves = rules.GetValidMoves(board, player, enabledSpecialDiscTypes).ToList();
        if (validMoves.Count == 0)
        {
            quitRequested = true;
            return;
        }

        var selectedMove = validMoves.FirstOrDefault(move => rules.MoveWinsImmediately(board, player, move));
        if (selectedMove == default)
        {
            selectedMove = validMoves[random.Next(validMoves.Count)];
        }

        ui.ShowComputerMove(player, selectedMove);

        var moveResult = rules.ApplyMove(
            board,
            player,
            selectedMove.DiscType,
            selectedMove.Column,
            applyReturns: true,
            returnDiscToOwner: ReturnDiscToOwner);

        ui.ShowMoveFrames(moveResult.Frames);
        renderBoardAtTurnStart = moveResult.RenderBoardAtTurnStart;
    }

    private Player GetOpponent(PlayerId playerId)
    {
        return playerId == PlayerId.Player1 ? player2 : player1;
    }

    private void ReturnDiscToOwner(Disc disc)
    {
        if (disc.Owner == PlayerId.Player1)
        {
            player1.ReturnDisc(disc);
            return;
        }

        player2.ReturnDisc(disc);
    }
}
