namespace Lineup.Model;

public enum PlayerType
{
    Human,
    Computer
}

public class Player
{
    public PlayerId Id { get; }
    public PlayerType Type { get; }
    public string Name { get; }

    public int OrdinaryDiscsRemaining { get; private set; }
    public int BoringDiscsRemaining { get; private set; }
    public int MagneticDiscsRemaining { get; private set; }
    public int ExplodingDiscsRemaining { get; private set; }

    public Player(
        PlayerId id,
        PlayerType type,
        string name,
        int ordinaryDiscCount,
        int boringDiscCount = 2,
        int magneticDiscCount = 2,
        int explodingDiscCount = 0)
    {
        if (ordinaryDiscCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ordinaryDiscCount), "Ordinary disc count cannot be negative.");
        }

        if (boringDiscCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(boringDiscCount), "Boring disc count cannot be negative.");
        }

        if (magneticDiscCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(magneticDiscCount), "Magnetic disc count cannot be negative.");
        }

        if (explodingDiscCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(explodingDiscCount), "Exploding disc count cannot be negative.");
        }

        Id = id;
        Type = type;
        Name = name;
        OrdinaryDiscsRemaining = ordinaryDiscCount;
        BoringDiscsRemaining = boringDiscCount;
        MagneticDiscsRemaining = magneticDiscCount;
        ExplodingDiscsRemaining = explodingDiscCount;
    }

    public bool IsComputer => Type == PlayerType.Computer;

    public bool CanPlayDisc(DiscType discType)
    {
        return discType switch
        {
            DiscType.Ordinary => OrdinaryDiscsRemaining > 0,
            DiscType.Boring => BoringDiscsRemaining > 0,
            DiscType.Magnetic => MagneticDiscsRemaining > 0,
            DiscType.Exploding => ExplodingDiscsRemaining > 0,
            _ => false
        };
    }

    public bool CanPlaySpecialDisc(DiscType discType)
    {
        return discType != DiscType.Ordinary && CanPlayDisc(discType);
    }

    public Disc UseDisc(DiscType discType)
    {
        if (!CanPlayDisc(discType))
        {
            throw new InvalidOperationException($"{Name} cannot play {discType}.");
        }

        switch (discType)
        {
            case DiscType.Ordinary:
                OrdinaryDiscsRemaining--;
                break;
            case DiscType.Boring:
                BoringDiscsRemaining--;
                break;
            case DiscType.Magnetic:
                MagneticDiscsRemaining--;
                break;
            case DiscType.Exploding:
                ExplodingDiscsRemaining--;
                break;
        }

        return new Disc(Id, discType);
    }

    public void UseSpecialDisc(DiscType discType)
    {
        if (discType == DiscType.Ordinary)
        {
            throw new InvalidOperationException("Ordinary disc is not a special disc.");
        }

        UseDisc(discType);
    }

    public void ReturnDisc(Disc disc)
    {
        if (disc.Owner != Id)
        {
            throw new InvalidOperationException($"{Name} cannot receive a disc owned by another player.");
        }

        switch (disc.Type)
        {
            case DiscType.Ordinary:
                OrdinaryDiscsRemaining++;
                break;
            case DiscType.Boring:
                BoringDiscsRemaining++;
                break;
            case DiscType.Magnetic:
                MagneticDiscsRemaining++;
                break;
            case DiscType.Exploding:
                ExplodingDiscsRemaining++;
                break;
        }
    }
}
