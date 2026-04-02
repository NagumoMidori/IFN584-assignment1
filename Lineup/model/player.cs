namespace Lineup.Model;

// type
public enum PlayerType { Human, Computer }

// options
public enum TurnAction { Move, Save, Quit }


public class TurnResult
{
    public TurnAction Action { get; set; }
    public DiscType DiscType { get; set; }
    public int Column { get; set; } // 0-based
}

/// <summary>
/// player class - stores info and inventory, and TakeTurn() method.
/// </summary>
public abstract class Player
{
    private int _ordinaryDiscs;
    private int _boringDiscs;
    private int _magneticDiscs;

    public PlayerId Id { get; }
    public string Name { get; }
    public PlayerType Type { get; }

    // 公开只读属性暴露库存数量
    public int OrdinaryDiscsRemaining => _ordinaryDiscs;
    public int BoringDiscsRemaining => _boringDiscs;
    public int MagneticDiscsRemaining => _magneticDiscs;

    protected Player(PlayerId id, PlayerType type, string name,
        int ordinaryDiscs, int boringDiscs, int magneticDiscs)
    {
        if (ordinaryDiscs < 0 || boringDiscs < 0 || magneticDiscs < 0)
            throw new ArgumentOutOfRangeException("Disc counts cannot be negative.");

        Id = id;
        Type = type;
        Name = name;
        _ordinaryDiscs = ordinaryDiscs;
        _boringDiscs = boringDiscs;
        _magneticDiscs = magneticDiscs;
    }

    /// check if the player can play this kind of disc
    public bool CanPlayDisc(DiscType type)
    {
        return type switch
        {
            DiscType.Ordinary => _ordinaryDiscs > 0,
            DiscType.Boring => _boringDiscs > 0,
            DiscType.Magnetic => _magneticDiscs > 0,
            _ => false
        };
    }

    /// use disc of the specified type, reduce inventory and return a Disc instance
    public Disc UseDisc(DiscType type)
    {
        if (!CanPlayDisc(type))
            throw new InvalidOperationException($"{Name} has no {type} discs remaining.");

        switch (type)
        {
            case DiscType.Ordinary:
                _ordinaryDiscs--;
                return new OrdinaryDisc(Id);
            case DiscType.Boring:
                _boringDiscs--;
                return new BoringDisc(Id);
            case DiscType.Magnetic:
                _magneticDiscs--;
                return new MagneticDisc(Id);
            default:
                throw new ArgumentException($"Unknown disc type: {type}");
        }
    }

    /// return disc to player, used for Boring disc effect
    public void ReturnDisc(Disc disc)
    {
        if (disc.Owner != Id)
            throw new InvalidOperationException("Cannot return disc to wrong player.");

        _ordinaryDiscs++;
    }

    
    public abstract TurnResult TakeTurn(
        Board board, GameConsoleUi ui, GameRules rules,
        DiscType[] enabledSpecialTypes, Random random);
}

// human player - input from console
public class HumanPlayer : Player
{
    public HumanPlayer(PlayerId id, string name,
        int ordinaryDiscs, int boringDiscs, int magneticDiscs)
        : base(id, PlayerType.Human, name, ordinaryDiscs, boringDiscs, magneticDiscs) { }

    public override TurnResult TakeTurn(
        Board board, GameConsoleUi ui, GameRules rules,
        DiscType[] enabledSpecialTypes, Random random)
    {
        // 循环直到玩家输入有效走法或选择 save/quit
        return ui.PromptHumanMove();
    }
}

// computer player - simple AI: prioritize winning move, otherwise random valid move
public class ComputerPlayer : Player
{
    public ComputerPlayer(PlayerId id, string name,
        int ordinaryDiscs, int boringDiscs, int magneticDiscs)
        : base(id, PlayerType.Computer, name, ordinaryDiscs, boringDiscs, magneticDiscs) { }

    public override TurnResult TakeTurn(
        Board board, GameConsoleUi ui, GameRules rules,
        DiscType[] enabledSpecialTypes, Random random)
    {
        var validMoves = rules.GetValidMoves(board, this, enabledSpecialTypes);
        if (validMoves.Count == 0)
            return new TurnResult { Action = TurnAction.Quit };

        // check if any move wins immediately, if so take it
        foreach (var (discType, col) in validMoves)
        {
            if (rules.MoveWinsImmediately(board, this, discType, col))
            {
                ui.ShowComputerMove(Name, discType, col + 1);
                return new TurnResult { Action = TurnAction.Move, DiscType = discType, Column = col };
            }
        }

        // without winning move, pick a random valid move
        var (randomType, randomCol) = validMoves[random.Next(validMoves.Count)];
        ui.ShowComputerMove(Name, randomType, randomCol + 1);
        return new TurnResult { Action = TurnAction.Move, DiscType = randomType, Column = randomCol };
    }
}
