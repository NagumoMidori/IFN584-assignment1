# IFN584 Assignment 1 Video Outline

## Opening

- Show webcam, system clock, your name, and `IFN584 Assignment1`.
- Say the video will cover completed features, a short gameplay demo, save/load, testing mode, and key OOP design choices.
- Briefly state what is completed, partially completed, or not attempted.

## Part 1: Main Menu And Basic Flow

- Start the program and show the main menu.
- Point out the available options:
  - New Game
  - Load Game
  - Testing Mode
  - Quit
- Mention that the project is a C# console application on .NET.

## Part 2: Normal Game Demo

- Start a new game.
- Choose `Human vs Human`.
- Use the default `6 x 7` board.
- Play a short sequence using only ordinary discs.
- Show that discs fall to the lowest available row.
- Continue until one player wins.
- Point out that the board is rendered clearly in the terminal and the game announces the result.

## Part 3: Input Validation And Help

- Start another short game or continue from a fresh one.
- Enter one invalid command.
- Enter a full-column move or invalid column if easy to demonstrate.
- Show that the program rejects bad input and asks again instead of crashing.
- Enter `help` and show the in-game command list.
- Mention that this is part of usability and robustness.

## Part 4: Custom Board Size

- Start a new game with a non-default valid board size.
- Example: `6 x 8` or `7 x 9`.
- Mention the rule that rows cannot exceed columns.
- Show that the game starts normally with the custom board.
- Briefly mention that the winning length changes with board size.

## Part 5: Special Disc Demo

- Explain that this implementation supports two special disc types:
  - Boring Disc
  - Magnetic Disc
- Use either normal play or testing mode to reach a quick demonstration state.

### Boring Disc

- Show a column with existing discs in it.
- Play a Boring Disc into that column.
- Point out the separate frames:
  - placement frame
  - effect frame
- Explain that discs in that column are removed and returned to the owners' inventories, while the boring disc remains on the board.

### Magnetic Disc

- Set up a situation where the magnetic effect is visible.
- Play a Magnetic Disc.
- Point out the placement frame and the effect frame.
- Explain which disc is pulled upward and how the board changes after the effect.

## Part 6: Testing Mode

- Return to the main menu and open `Testing Mode`.
- Enter one prepared sequence.
- Show that the program parses a comma-separated move list and executes moves automatically.
- Mention that this mode is important because the assignment specification says it will be used for marking.
- If useful, use testing mode to quickly demonstrate one of the special-disc effects again.

## Part 7: Save And Load

- Start or continue a game in a non-terminal state.
- Use `save`.
- Show that the save succeeds.
- Return to the main menu.
- Choose `load`.
- Load the saved file.
- Point out that the board state and current game progress are restored.

## Part 8: Human Vs Computer

- Start a new game in `Human vs Computer` mode.
- Make one or two moves.
- Show that the computer responds automatically.
- Mention that the computer checks for an immediate winning move; otherwise it selects a valid move.

## Part 9: Code Design And OOP

- Open only a few key files, not the whole project.
- Suggested files to show:
  - `model/disc.cs`
  - `model/player.cs`
  - `model/game.cs`
  - `model/board.cs`
  - `model/gameRules.cs`

- Explain OOP points briefly:
  - Encapsulation: board state, player inventory, and game state are stored inside dedicated classes.
  - Abstraction: the game loop, UI, rules, and board logic are separated into different classes.
  - Inheritance: `Player` has `HumanPlayer` and `ComputerPlayer`; `Disc` has ordinary and special-disc subclasses.
  - Polymorphism: different disc types use their own `ApplyEffect` implementations.
  - Exception handling: invalid input and file loading problems are caught and shown to the user.
  - File operations: save and load are implemented through text-file persistence.

## Closing

- Summarise what was demonstrated:
  - basic gameplay
  - custom board size
  - two special discs
  - testing mode
  - save/load
  - human vs computer
  - OOP structure
- End the recording cleanly within 10 minutes.
