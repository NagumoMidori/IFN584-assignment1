namespace Lineup.Model;

public class Board
{
    private readonly char?[,] cells;

    public int Rows { get; }
    public int Columns { get; }

    public Board(int rows, int columns)
    {
        if (rows < 6)
            throw new ArgumentOutOfRangeException(nameof(rows), "Rows must be at least 6.");
        if (columns < 7)
            throw new ArgumentOutOfRangeException(nameof(columns), "Columns must be at least 7.");
        if (rows > columns)
            throw new ArgumentException("Rows cannot exceed columns.");

        Rows = rows;
        Columns = columns;
        cells = new char?[rows, columns];
    }

    public char? GetCell(int row, int col)
    {
        return cells[row, col];
    }


    public void SetCell(int row, int col, char? value)
    {
        cells[row, col] = value;
    }


    public void ClearCell(int row, int col)
    {
        cells[row, col] = null;
    }

    public int DropDisc(int col, char symbol)
    {
        for (int row = Rows - 1; row >= 0; row--)
        {
            if (!cells[row, col].HasValue)
            {
                cells[row, col] = symbol;
                return row;
            }
        }
        throw new InvalidOperationException($"Column {col} is full.");
    }


    public bool IsColumnFull(int col)
    {
        return cells[0, col].HasValue;
    }

    public bool IsFull
    {
        get
        {
            for (int col = 0; col < Columns; col++)
            {
                if (!IsColumnFull(col))
                    return false;
            }
            return true;
        }
    }

    public void CollapseColumn(int col)
    {

        var discs = new List<char>();
        for (int row = Rows - 1; row >= 0; row--)
        {
            if (cells[row, col].HasValue)
            {
                discs.Add(cells[row, col]!.Value);
            }
        }


        for (int row = 0; row < Rows; row++)
        {
            cells[row, col] = null;
        }


        int targetRow = Rows - 1;
        foreach (var disc in discs)
        {
            cells[targetRow, col] = disc;
            targetRow--;
        }
    }

    public string Render()
    {
        string consoleBoard = "";

        for (int row = 0; row < Rows; row++)
        {
            consoleBoard += "|";

            for (int col = 0; col < Columns; col++)
            {
                char? cell = cells[row, col];
                consoleBoard += cell.HasValue ? $" {cell.Value} |" : "   |";
            }

            if (row < Rows - 1)
                consoleBoard += Environment.NewLine;
        }

        return consoleBoard;
    }
}
