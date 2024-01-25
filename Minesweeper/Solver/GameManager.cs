using Minesweeper.Game.Core;

namespace Minesweeper.Solver;

/// <summary>
/// Simple class to manage ops like digging etc.
/// </summary>
public static class GameManager
{
    /// <summary>
    /// Dig a cell, but also handle all return cases.
    /// </summary>
    /// <param name="board">The board to act on.</param>
    /// <param name="x">The x coordinate of the cell to dig.</param>
    /// <param name="y">The y coordinate of the cell to dig.</param>
    /// <returns>True if the game was not lost, false if the game was lost.</returns>
    public static bool AutoDig(this Board board, int x, int y)
    {
        if (board.Grid[x, y].IsDug) return true;
        switch (board.Grid[x, y].Dig())
        {
            case DigResult.Success:
                return true;
            case DigResult.Fail:
                return false;
            case DigResult.Disallowed:
                return true;
            case DigResult.Open:
                // in this case we have to call this method again for every adjacent.
                // this Linq statement calls AutoDig on each adjacent cell and returns false if any process itself returned false.
                //return board.Grid[x, y].AdjacentCells.All(adjacent => board.AutoDig(adjacent.Item1, adjacent.Item2));
                foreach ((int x, int y) adjacent in board.Grid[x, y].WorkingAdjacentCells)
                {
                    if (!board.AutoDig(adjacent.x, adjacent.y)) return false;
                }

                return true;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    /// <summary>
    /// Checks if the state of the given board indicates a victory.
    /// A victory condition means that every cell with a mine is flagged and every cell without a mine is dug.
    /// </summary>
    /// <param name="board">The board to check.</param>
    /// <returns>Returns true if the board is in a victory state, false otherwise.</returns>
    public static bool CheckVictory(this Board board)
    {
        // first, check that all mines are flagged
        if ((from Cell cell in board  // if any cell
                where cell.IsMine // is mined
                where !cell.IsFlagged // but is not flagged
                select cell).Any())
            return false;
        
        // now, check that every un-mined cell is dug
        // ReSharper disable once ConvertIfStatementToReturnStatement
        if ((from Cell cell in board // if any cell
                where !cell.IsMine // is not mined
                where !cell.IsDug // and is not dug
                select cell).Any())
            return false;
        
        //if neither of the above conditions fail, the board wins.
        return true;
    }
}