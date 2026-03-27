using Lineup.Model;

var board = new Board();
board.SetCell(1, 1, 'X');
board.SetCell(1, 2, '#');
board.SetCell(2, 3, '@');
Console.WriteLine($"Board initialized: {board.Rows} rows x {board.Columns} columns.");
board.CollapseAllColumns();
Console.WriteLine(board.Render());
