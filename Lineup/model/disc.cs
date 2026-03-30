namespace Lineup.Model;

public enum PlayerId
{
    Player1,
    Player2
}

public enum DiscType
{
    Ordinary,
    Boring,
    Magnetic,
    Exploding
}

public class Disc
{
    public PlayerId Owner { get; }
    public DiscType Type { get; }

    public Disc(PlayerId owner, DiscType type)
    {
        Owner = owner;
        Type = type;
    }

    public char Symbol =>
        (Owner, Type) switch
        {
            (PlayerId.Player1, DiscType.Ordinary) => '@',
            (PlayerId.Player2, DiscType.Ordinary) => '#',
            (PlayerId.Player1, DiscType.Boring) => 'B',
            (PlayerId.Player2, DiscType.Boring) => 'b',
            (PlayerId.Player1, DiscType.Magnetic) => 'M',
            (PlayerId.Player2, DiscType.Magnetic) => 'm',
            (PlayerId.Player1, DiscType.Exploding) => 'E',
            (PlayerId.Player2, DiscType.Exploding) => 'e',
            _ => throw new InvalidOperationException("Unknown disc.")
        };

    public bool CountsForWin => Type != DiscType.Exploding;

    public bool IsSpecial => Type != DiscType.Ordinary;

    public Disc ToOrdinary()
    {
        return new Disc(Owner, DiscType.Ordinary);
    }

    public static Disc FromSymbol(char symbol)
    {
        return symbol switch
        {
            '@' => new Disc(PlayerId.Player1, DiscType.Ordinary),
            '#' => new Disc(PlayerId.Player2, DiscType.Ordinary),
            'B' => new Disc(PlayerId.Player1, DiscType.Boring),
            'b' => new Disc(PlayerId.Player2, DiscType.Boring),
            'M' => new Disc(PlayerId.Player1, DiscType.Magnetic),
            'm' => new Disc(PlayerId.Player2, DiscType.Magnetic),
            'E' => new Disc(PlayerId.Player1, DiscType.Exploding),
            'e' => new Disc(PlayerId.Player2, DiscType.Exploding),
            _ => throw new ArgumentException($"Invalid disc symbol: {symbol}")
        };
    }

    public static bool IsValidSymbol(char symbol)
    {
        return symbol is '@' or '#' or 'B' or 'b' or 'M' or 'm' or 'E' or 'e';
    }
}
