using System.Collections;

namespace Minesweeper.Game.Core;

/// <summary>
/// A class to represent a board object. Contains a 2d array or cells representing the grid. Initialized by providing the grid size as well as a number of mines.
/// </summary>
public class Board : IEnumerable
{
    public Cell[,] Grid { get; private set; }
    public readonly int Width;
    public readonly int Height;
    

    public Board(int x, int y, int mines)
    {
        Grid = MakeBoard(x, y, mines);
        Width = x;
        Height = y;
    }

    public Board(int x, int y, float minesPercent)
    {
        Grid = MakeBoard(x, y, (int)Math.Floor(minesPercent * (x * y)));
        Width = x;
        Height = y;
    }

    private static Cell[,] MakeBoard(int x, int y, int mines)
    {
        var grid = new Cell[x, y]; //initialize the grid
        for (int i = 0; i < x; i++) //populate the grid
        {
            for (int j = 0; j < y; j++)
            {
                grid[i, j] = new Cell();
            }
        }

        for (int i = 0; i < mines; i++) //select the given number of mines from the grid
        {
            (int x, int y) coords = GetRandomCell(x, y);
            if (grid[coords.x, coords.y].IsMine)
            {
                i--;
                continue;
            }

            grid[coords.x, coords.y].IsMine = true;
        }
        
        //count up adjacent mines
        /*
         * PROCESS:
         * I. Iterate through all cells that are mines
         * II. Add 1 to the adjacent counter of every adjacent cell; add to:
         *  A. (x+1, y)
         *  B. (x, y+1)
         *  C. (x-1, y)
         *  D. (x, y-1)
         *  E. (x+1, y+1)
         *  F. (x-1, y-1)
         *  G. (x-1, y+1)
         *  H. (x+1, y-1)
         */

        for (int i = 0; i < x; i++)
            for (int j = 0; j < y; j++)
            {
                if (!grid[i, j].IsMine) continue;

                (int, int)[] adjacents = new[]
                {
                    (i+1,j),
                    (i,j+1),
                    (i-1,j),
                    (i,j-1),
                    (i+1,j+1),
                    (i-1,j-1),
                    (i-1,j+1),
                    (i+1,j-1)
                };

                foreach (var adjacent in adjacents)
                {
                    try //at the edges, this creates IOORE. Fix it simply.
                    {
                        grid[adjacent.Item1, adjacent.Item2].Adjacent++;
                    }
                    catch (Exception e)
                    {
                        if (e is not IndexOutOfRangeException)
                            throw;
                    }
                    
                }
            }

        return grid;
        

        (int, int) GetRandomCell(int maxX, int maxY)
        {
            var rand = new Random();
            int x = rand.Next(maxX);
            int y = rand.Next(maxY);
            return (x, y);
        }
    }

    public IEnumerator GetEnumerator()
    {
        return Grid.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}