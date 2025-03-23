namespace Minesweeper;

public class Board
{
    public Cell[,] Grid { get; set; }
    public int Size { get; }
    public bool GameOver { get; set; }

    public Board(int size, int mines)
    {
        Size = size;
        Grid = new Cell[size, size];
        (int, int)[] mineLocations = new (int, int)[mines];
        Random rand = new();
        
        for (int i = 0; i < mines; i++)
        {
            int c = rand.Next(size);
            int r = rand.Next(size);
            if (mineLocations.Contains((r, c)))
            {
                i--;
                continue;
            }

            mineLocations[i] = (r, c);
        }

        for (int r = 0; r < size; r++)
        {
            for (int c = 0; c < size; c++)
            {
                Grid[r, c] = new Cell(mineLocations.Contains((r, c)));
            }
        }
        
        UpdateAdjacentMines();
    }

    public bool CheckVictory()
    {
        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                if ((Grid[r, c].IsFlagged && Grid[r, c].IsMine) || Grid[r, c].IsDug) continue;
                return false;
            }
        }

        return true;
    }
    
    private void UpdateAdjacentMines()
    {
        for (int r = 0; r < Size; r++)
        {
            for (int c = 0; c < Size; c++)
            {
                int count = 0;

                if (r - 1 >= 0 && Grid[r - 1, c].IsMine) count++;
                if (c - 1 >= 0 && Grid[r, c - 1].IsMine) count++;
                
                if (r + 1 < Size && Grid[r + 1, c].IsMine) count++;
                if (c + 1 < Size && Grid[r, c + 1].IsMine) count++;
                
                if (r - 1 >= 0 && c - 1 >= 0 && Grid[r - 1, c - 1].IsMine) count++;
                if (r + 1 < Size && c - 1 >= 0 && Grid[r + 1, c - 1].IsMine) count++;
                if (r - 1 >= 0 && c + 1 < Size && Grid[r - 1, c + 1].IsMine) count++;
                if (r + 1 < Size && c + 1 < Size && Grid[r + 1, c + 1].IsMine) count++;

                Grid[r, c].NearbyMines = count;

            }
        }
    }

    public override string ToString()
    {
        string ret = "";
        int maxDigits = (int)Math.Log10(Size);
        ret += new string(' ', maxDigits + 4); 
        for (int c = 0; c < Size; c++)
        {
            ret += Program.GetCharacterFromPosition(c + 1) + " ";
        }

        ret += "\n";
        for (int r = 0; r < Size; r++)
        {
            ret += (r + 1) + new string(' ', maxDigits - (int)Math.Log10(r + 1)) + " | ";
            for (int c = 0; c < Size; c++)
            {
                
                if (GameOver && Grid[r, c].IsMine)
                {
                    if (Grid[r, c].IsFlagged) ret += "\u2713"; // checkmark
                    else ret += "\u2736"; // star
                }
                else if (Grid[r, c].IsFlagged) ret += "\u2691"; // flag
                else if (Grid[r, c].IsDug) ret += Grid[r, c].NearbyMines == 0 ? " " : Grid[r, c].NearbyMines;
                else ret += "-";

                ret += " ";
            }

            ret += "\n";
        }

        return ret;
    }
    
    public Range GetRange(int row, int column, bool includeFlags = false)
    {
        if (!Grid[row, column].IsDug) throw new UnauthorizedAccessException("Attempted to access a cell that has not been dug: (" + Program.GetCharacterFromPosition(column + 1) + "" + (row + 1) + ")");
        List<Cell> cells = new();
        int mineCount = 0;
        
        for (int r = Math.Max(0, row - 1); r <= Math.Min(Size - 1, row + 1); r++)
        {
            for (int c = Math.Max(0, column - 1); c <= Math.Min(Size - 1, column + 1); c++)
            {
                if (!includeFlags && Grid[r, c].IsFlagged) continue;
                if (Grid[r,c].IsDug) continue;
                
                cells.Add(Grid[r, c]);
                if (Grid[r, c].IsMine) mineCount++;
            }
        }

        return new Range
        {
            Cells = cells,
            Mines = mineCount
        };
    }
    
    /// <summary>
    /// Check if a given cell is safe; i.e., it has exactly the same number of adjacent mines as adjacent flags.
    /// </summary>
    /// <param name="row"></param>
    /// <param name="column"></param>
    /// <returns></returns>
    public bool CheckSafe(int row, int column)
    {
        Range range = GetRange(row, column, true);
        return range.Cells.Count(c => c.IsFlagged) == range.Mines;
    }
}