namespace Lineup.Model;

public class Board
{
    private readonly char?[,] cells;
    public int Rows { get; }
    public int Columns { get; }

    public Board(int rows = 6, int columns = 7)
    {
        if (rows < 6)
        {
            throw new ArgumentOutOfRangeException(nameof(rows), "Board must have at least 6 rows.");
        }

        if (columns < 7)
        {
            throw new ArgumentOutOfRangeException(nameof(columns), "Board must have at least 7 columns.");
        }

        if (rows > columns)
        {
            throw new ArgumentException("Board cannot have more rows than columns.");
        }

        Rows = rows;
        Columns = columns;
        cells = new char?[rows, columns];
    }

    private int ColumnToIndex(int column)
    {
        if (column < 1 || column > Columns)
        {
            throw new ArgumentOutOfRangeException(nameof(column), $"Column must be between 1 and {Columns}.");
        }

        return column - 1;
    }


    public bool IsColumnFull(int column)
    {
        var columnIndex = ColumnToIndex(column);
        return cells[0, columnIndex].HasValue;
    }

    public bool IsFull
    {
        get
        {
            for (var column = 1; column <= Columns; column++)
            {
                if (!IsColumnFull(column))
                {
                    return false;
                }
            }

            return true;
        }
    }


    public int? FindLocation(int column)
    {
        var columnIndex = ColumnToIndex(column);

        for (var rowIndex = Rows-1; rowIndex >= 0; rowIndex--)
        {
            if (!cells[rowIndex, columnIndex].HasValue)
            {
                return Rows - rowIndex;
            }
        }

        return null;
    }

    public int DropDisc(int column, char disc)
    {
        var targetRow = FindLocation(column);

        if (!targetRow.HasValue)
        {
            throw new InvalidOperationException($"Column {column} is full.");
        }

        SetCell(targetRow.Value, column, disc);
        return targetRow.Value;
    }

    public char? GetCell(int row, int column)
    {
        var (rowIndex, columnIndex) = ToIndexes(row, column);
        return cells[rowIndex, columnIndex];
    }

    public void SetCell(int row, int column, char? disc)
    {
        var (rowIndex, columnIndex) = ToIndexes(row, column);
        cells[rowIndex, columnIndex] = disc;
    }

    public void ClearCell(int row, int column)
    {
        SetCell(row, column, null);
    }

    public void CollapseColumn(int column)
    {
        var columnIndex = ColumnToIndex(column);
        var discs = new List<char>();

        for (int row = 0; row < Rows; row++)
        {
            if (cells[row, columnIndex].HasValue)
            {
                discs.Add(cells[row, columnIndex]!.Value);
            }
        }

        for (int row = 0; row < Rows; row++)
        {
            cells[row, columnIndex] = null;
        }

        var targetRow = Rows - 1;
        foreach (var disc in discs)
        {
            cells[targetRow, columnIndex] = disc;
            targetRow--;
        }
    }

    public void CollapseAllColumns()
    {
        for (var column = 1; column <= Columns; column++)
        {
            CollapseColumn(column);
        }
    }

    private (int rowIndex, int columnIndex) ToIndexes(int row, int column)
    {
        if (row < 1 || row > Rows)
        {
            throw new ArgumentOutOfRangeException(nameof(row), $"Row must be between 1 and {Rows}.");
        }

        return (Rows-row, ColumnToIndex(column));
    }

    public string Render()
    {
        string result = "";
        for(int row = 0; row < Rows; row++)
        {
            result += "|";
            for(int column = 0; column < Columns; column++)
            {
                char? disc = cells[row, column];
                if (disc.HasValue)
                {
                    result+=" " + disc.Value + " |";
                }
                else
                {
                    result += "   |";
                }
            }
            result += "\n";
        }
        return result;
    }
}
