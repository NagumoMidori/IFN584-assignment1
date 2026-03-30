using Lineup.Model;

var enabledSpecialDiscTypes = new[] { DiscType.Boring, DiscType.Magnetic };
var ui = new GameConsoleUi(enabledSpecialDiscTypes);

ui.ShowWelcome();

while (true)
{
    if (ui.PromptForMainMenuSelection() == MainMenuSelection.Quit)
    {
        return;
    }

    ui.ShowSetupHeader();

    var playerTwoType = ui.PromptForGameMode();
    var rows = ui.PromptForRows();
    var columns = ui.PromptForColumns(rows);

    var game = new Game(rows, columns, playerTwoType, enabledSpecialDiscTypes);
    game.Run();
}
