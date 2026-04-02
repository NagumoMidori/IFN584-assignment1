using Lineup.Model;

var ui = new GameConsoleUi();

while (true)
{
    int choice = ui.ShowMainMenu();

    switch (choice)
    {
        case 1: // New Game
            try
            {
                var mode = ui.PromptGameMode();
                int rows = ui.PromptRows();
                int cols = ui.PromptColumns(rows);
                var game = new Game(rows, cols, mode, ui);
                game.Run();
            }
            catch (Exception ex)
            {
                ui.ShowError(ex.Message);
            }
            break;

        case 2: // Load Game
            try
            {
                string path = ui.PromptLoadFilePath();
                if (path.ToLower() == "cancel") break;
                var loadedGame = Game.LoadGame(path, ui);
                loadedGame.Run();
            }
            catch (Exception ex)
            {
                ui.ShowError(ex.Message);
            }
            break;

        case 3: // Testing Mode
            try
            {
                string sequence = ui.PromptTestSequence();
                var testRunner = new TestModeRunner(ui);
                testRunner.Run(sequence);
            }
            catch (Exception ex)
            {
                ui.ShowError(ex.Message);
            }
            break;

        case 4: // Quit
            Console.WriteLine("Goodbye!");
            return;
    }
}
