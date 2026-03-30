namespace Lineup.Model;

public class Game
{
    private readonly Random random;
    private readonly DiscType[] enabledSpecialDiscTypes;
    private readonly GameConsoleUi ui;

    private Board? board;
    private Player? player1;
    private Player? player2;
    private Player? currentPlayer;
    private GameRules? rules;
    private int winningLength;
    private bool quitRequested;
    private bool renderBoardAtTurnStart;

    public Game(Random? random = null, params DiscType[] enabledSpecialDiscTypes)
    {
        this.random = random ?? new Random();

        if (enabledSpecialDiscTypes.Length == 0)
        {
            this.enabledSpecialDiscTypes = new[] { DiscType.Boring, DiscType.Magnetic };
        }
        else
        {
            var normalized = enabledSpecialDiscTypes
                .Where(type => type != DiscType.Ordinary)
                .Distinct()
                .ToArray();

            if (normalized.Length != 2)
            {
                throw new ArgumentException("LineUp requires exactly two special disc types to be enabled.");
            }

            this.enabledSpecialDiscTypes = normalized;
        }

        ui = new GameConsoleUi(this.enabledSpecialDiscTypes);
    }

    private Board CurrentBoard => board ?? throw new InvalidOperationException("Game has not been initialized.");
    private Player PlayerOne => player1 ?? throw new InvalidOperationException("Player 1 has not been initialized.");
    private Player PlayerTwo => player2 ?? throw new InvalidOperationException("Player 2 has not been initialized.");
    private Player CurrentPlayer => currentPlayer ?? throw new InvalidOperationException("Current player has not been initialized.");
    private GameRules Rules => rules ?? throw new InvalidOperationException("Game rules have not been initialized.");

    public void Run()
    {
        ui.ShowWelcome();

        while (true)
        {
            if (ui.PromptForMainMenuSelection() == MainMenuSelection.Quit)
            {
                return;
            }

            SetupNewGame();
            PlayGame();
        }
    }

    private void SetupNewGame()
    {
        ui.ShowSetupHeader();

        var playerTwoType = ui.PromptForGameMode();
        var rows = ui.PromptForInt("Rows (minimum 6): ", value => value >= 6);
        var columns = ui.PromptForInt(
            $"Columns (minimum 7 and at least {rows}): ",
            value => value >= 7 && value >= rows);

        board = new Board(rows, columns);
        winningLength = DetermineWinningLength(rows, columns);
        rules = new GameRules(winningLength);

        var discsPerPlayer = rows * columns / 2;
        var specialDiscCount = enabledSpecialDiscTypes.Length * 2;
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

        ui.ShowGameSetup(CurrentBoard, winningLength, enabledSpecialDiscTypes);
    }

    private int DetermineWinningLength(int rows, int columns)
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
            if (!Rules.HasAnyValidMove(CurrentBoard, CurrentPlayer, enabledSpecialDiscTypes))
            {
                ui.ShowNoValidMoves(CurrentPlayer);

                var opponent = GetOpponent(CurrentPlayer.Id);
                if (!Rules.HasAnyValidMove(CurrentBoard, opponent, enabledSpecialDiscTypes))
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
                ui.ShowBoard(CurrentBoard);
            }

            ui.ShowPlayerStatus(CurrentPlayer);

            if (CurrentPlayer.IsComputer)
            {
                PerformComputerTurn(CurrentPlayer);
            }
            else
            {
                var movePlayed = HandleHumanTurn(CurrentPlayer);
                if (!movePlayed)
                {
                    if (quitRequested)
                    {
                        return;
                    }

                    continue;
                }
            }

            if (Rules.HasWinner(CurrentBoard, CurrentPlayer.Id))
            {
                ui.ShowWinner(CurrentBoard, CurrentPlayer);
                return;
            }

            if (CurrentBoard.IsFull)
            {
                ui.ShowTie(CurrentBoard);
                return;
            }

            currentPlayer = GetOpponent(CurrentPlayer.Id);
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

            if (!Rules.IsValidColumn(CurrentBoard, command.Column))
            {
                ui.ShowInvalidColumn(CurrentBoard.Columns);
                continue;
            }

            if (CurrentBoard.IsColumnFull(command.Column))
            {
                ui.ShowColumnFull(command.Column);
                continue;
            }

            var moveResult = Rules.ApplyMove(
                CurrentBoard,
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
        var validMoves = Rules.GetValidMoves(CurrentBoard, player, enabledSpecialDiscTypes).ToList();
        if (validMoves.Count == 0)
        {
            quitRequested = true;
            return;
        }

        var selectedMove = validMoves.FirstOrDefault(move => Rules.MoveWinsImmediately(CurrentBoard, player, move));
        if (selectedMove == default)
        {
            selectedMove = validMoves[random.Next(validMoves.Count)];
        }

        ui.ShowComputerMove(player, selectedMove);

        var moveResult = Rules.ApplyMove(
            CurrentBoard,
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
        return playerId == PlayerId.Player1 ? PlayerTwo : PlayerOne;
    }

    private void ReturnDiscToOwner(Disc disc)
    {
        if (disc.Owner == PlayerId.Player1)
        {
            PlayerOne.ReturnDisc(disc);
            return;
        }

        PlayerTwo.ReturnDisc(disc);
    }
}
