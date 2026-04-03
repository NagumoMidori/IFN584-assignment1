namespace Lineup.Model;


public enum PlayerId { Player1, Player2 }


public enum DiscType { Ordinary, Boring, Magnetic }


public abstract class Disc
{
    private readonly PlayerId owner;
    private readonly DiscType type;

    protected Disc(PlayerId owner, DiscType type)
    {
        this.owner = owner;
        this.type = type;
    }

    public PlayerId Owner => owner;
    public DiscType Type => type;


    public abstract char Symbol { get; }

    public virtual bool CountsForWin => true;

    /// <summary>
    /// the disc effect when placed on the board:
    /// OrdinaryDisc no effect, BoringDisc/MagneticDisc needs override to implement
    /// </summary>
    /// <param name="board">board</param>
    /// <param name="row">row</param>
    /// <param name="col">column</param>
    /// <param name="returnDisc">return disc callback</param>
    public abstract void ApplyEffect(Board board, int row, int col, Action<Disc>? returnDisc);

    /// <summary>
    /// reconstruct a Disc instance from its symbol on the board (e.g. '@' → OrdinaryDisc(Player1))
    /// </summary>
    public static Disc FromSymbol(char symbol)
    {
        return symbol switch
        {
            '@' => new OrdinaryDisc(PlayerId.Player1),
            '#' => new OrdinaryDisc(PlayerId.Player2),
            'B' => new BoringDisc(PlayerId.Player1),
            'b' => new BoringDisc(PlayerId.Player2),
            'M' => new MagneticDisc(PlayerId.Player1),
            'm' => new MagneticDisc(PlayerId.Player2),
            _ => throw new ArgumentException($"Unknown disc symbol: {symbol}")
        };
    }
}


public class OrdinaryDisc : Disc
{
    public OrdinaryDisc(PlayerId owner) : base(owner, DiscType.Ordinary) { }

    public override char Symbol => Owner == PlayerId.Player1 ? '@' : '#';

    public override void ApplyEffect(Board board, int row, int col, Action<Disc>? returnDisc)
    {
        // no effect for ordinary disc
    }
}

/// <summary>
/// Boring disc: remove all other discs in the same column, then collapse the column so that the Boring disc falls to the bottom
/// </summary>
public class BoringDisc : Disc
{
    public BoringDisc(PlayerId owner) : base(owner, DiscType.Boring) { }

    public override char Symbol => Owner == PlayerId.Player1 ? 'B' : 'b';

    public override void ApplyEffect(Board board, int row, int col, Action<Disc>? returnDisc)
    {
        // remove all other discs in the same column (except the Boring disc just placed)
        for (int r = 0; r < board.Rows; r++)
        {
            if (r == row) continue;

            char? cell = board.GetCell(r, col);
            if (cell.HasValue)
            {
                // return the removed disc to its owner
                if (returnDisc != null)
                {
                    var originalDisc = Disc.FromSymbol(cell.Value);
                    returnDisc(new OrdinaryDisc(originalDisc.Owner));
                }
                board.ClearCell(r, col);
            }
        }

        // drop the Boring disc to the bottom of the column
        board.CollapseColumn(col);
    }
}

/// <summary>
/// Magnetic disc: after placing, if there is a contiguous stack of own discs directly below, pull the top one up by one cell (swap with the cell above it)
/// </summary>
public class MagneticDisc : Disc
{
    public MagneticDisc(PlayerId owner) : base(owner, DiscType.Magnetic) { }

    public override char Symbol => Owner == PlayerId.Player1 ? 'M' : 'm';

    public override void ApplyEffect(Board board, int row, int col, Action<Disc>? returnDisc)
    {
        // from the cell bellow, find the first disc that belongs to the same player
        int? targetRow = null;
        for (int r = row + 1; r < board.Rows; r++)
        {
            char? cell = board.GetCell(r, col);
            if (cell.HasValue)
            {
                var disc = Disc.FromSymbol(cell.Value);
                if (disc.Owner == Owner)
                {
                    targetRow = r;
                    break;
                }
            }
        }

        // if found, swap it up by one cell (simulate being pulled up by the magnetic disc)
        if (targetRow.HasValue && targetRow.Value != row + 1)
        {
            char? targetSymbol = board.GetCell(targetRow.Value, col);
            char? aboveSymbol = board.GetCell(targetRow.Value - 1, col);
            board.SetCell(targetRow.Value - 1, col, targetSymbol);
            board.SetCell(targetRow.Value, col, aboveSymbol);
        }
    }
}
