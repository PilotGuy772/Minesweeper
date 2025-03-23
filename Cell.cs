namespace Minesweeper;

public class Cell
{
    public bool IsDug { get; private set; }
    public int NearbyMines { get; set;  }
    public bool IsMine { get; }
    public bool IsFlagged { get; set; }
    public bool IsSafe { get; set; }

    /// <summary>
    /// Dig this cell.
    /// </summary>
    /// <returns>True if the cell is a mine (game over), false otherwise.</returns>
    public bool Dig()
    {
        if (IsMine) return true;
        IsDug = true;
        return false;
    }

    public Cell(bool isMine)
    {
        IsMine = isMine;
        IsDug = false;
        IsFlagged = false;
        NearbyMines = 0;
    }
}