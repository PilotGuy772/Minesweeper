namespace Minesweeper.Game.Core;

/// <summary>
/// Potential results of a dig operation.
/// </summary>
public enum DigResult
{
    Success, // returned when the dug cell is not a mine
    Fail, // returned when the dug cell was a mine; ends the game immediately.
    Open // returned when there are zero adjacent mines. When this is returned, all adjacent cells are also automatically dug.
}