using System.Net.Mime;
using Minesweeper.Game;
using Minesweeper.Game.Core;
using Console = Minesweeper.Game.Console;

namespace Minesweeper.Solver.Core;

/// <summary>
/// Manages starting and administration of the solving tasks.
/// </summary>
public class Solver
{
    /// <summary>
    /// Begin solving the board.
    /// </summary>
    /// <param name="board">The game board to solve.</param>
    public static void Start(Board board)
    {
        /*
         * This method only handles starting the solving tasks.
         * Other methods actually do the busy work.
         * PROCESS:
         *  I. Select starting cell to dig (one with no adjacent mines)
         *  II. Run a while loop to make turns until the game is over
         *      A. Iterate through all dug cells with adjacent mines
         *          1. Calculate the probability that every adjacent cell is a mine
         *          2. Update the probabilities of the adjacent cells
         *      B. Flag all cells with a 100% chance of being a mine
         *          1. For every cell adjacent to a flagged cell, subtract one from the "working" adjacent mine counter and remove its coordinates from the "working" list of adjacent cells.
         *      C. Dig all cells that have a 0% chance of being a mine
         *      D. If there are no cells with a 0% chance of being a mine, dig the cell with the lowest probability of being a mine
         *      E. If there are no more cells that are neither flagged nor dug and all mined cells have a flag, break the loop.
         *      F. Reset the probabilities of all cells using Cell.Reset(). 
         *      G. Pause and wait for permission to continue
         *  III. All done!
         */
        
        
        //I. Select starting cell to dig (one with no adjacent mines)
        PickStarter(ref board);
        
        //II. Run a while loop to make turns until the game is over
        while (true)
        {
            // first, calculate probabilities
            CalculateProbabilities(ref board);
            
            // second, flag all cells that are definitely mines
            FlagMines(ref board);
            
            // now, iterate through every cell that has a 0% chance to be a mine
            // if there are none, just dig whichever cell has the lowest chance to be a mine
            // to do this, iterate through every cell with a probability > -1
            // save the lowest recorded score and coordinates. Immediately dig any cell with a 0% chance;
            // if no cell was dug, dig the cell described by those variables.
            bool dugCell = false;
            float lowestScore = 101.0f;
            (int x, int y) lowestCoordinates = (0, 0);
            foreach (Cell cell in board)
            {
                //check if the cell has a calculated probability
                if (Math.Abs(cell.Probability - (-1.0f)) < 0.01f) continue;

                //if the probability is basically zero, dig it
                if (Math.Abs(cell.Probability) < 0.1f)
                {
                    bool result = board.AutoDig(cell.Coordinates.x, cell.Coordinates.y);
                    if (result == false) EndGame(false);
                    dugCell = true;
                }
                else
                {
                    if (!(cell.Probability < lowestScore)) continue;
                    
                    lowestScore = cell.Probability;
                    lowestCoordinates = cell.Coordinates;
                }
            }
            
            //now, if we haven't dug a cell, dig the lowest probability one
            if (!dugCell)
            {
                board.AutoDig(lowestCoordinates.x, lowestCoordinates.y);
            }
            
            //now print the board for the user to see
            board.PrintBoard();
            
            //now the turn is done. It's time to see if we won yet
            if (board.CheckVictory()) EndGame(true);
            
            // If the game was not yet won, wait for user input to continue
            System.Console.Write("\n\n The turn is complete. Press any key to resume...");
            System.Console.ReadKey();
        }
    }

    /// <summary>
    /// Takes a reference to a board and digs a random cell with no adjacent mines.
    /// </summary>
    /// <param name="board"></param>
    private static void PickStarter(ref Board board)
    {
        while (true)
        {
            (int x, int y) = GetRandomCell(board.Width, board.Height);
            
            if (board.Grid[x, y].AdjacentMines != 0) continue;
            board.Grid[x, y].Dig();
            return;
        }
        
        
        (int, int) GetRandomCell(int maxX, int maxY)
        {
            var rand = new Random();
            int x = rand.Next(maxX);
            int y = rand.Next(maxY);
            return (x, y);
        }
    }

    /// <summary>
    /// For every cell for which it is possible to do so, calculate the probability that each cell is a mine.
    /// </summary>
    /// <param name="board">Reference to the board to act on.</param>
    private static void CalculateProbabilities(ref Board board)
    {
        // iterate through all cells in the board
        foreach (Cell cell in board)
        {
            // Check to ensure that the cell is dug and that it has more than zero adjacent mines
            if (cell.IsDug == false) return;
            if (cell.AdjacentMines == 0) return;
            
            // Get the adjusted number of adjacent cells
            int adjacentCells = cell.WorkingAdjacentCells.Count;
            
            // Get the adjusted number of adjacent mines
            int adjacentMines = cell.WorkingAdjacentMines;
            
            // Calculate the probability for the adjacent cells (mines/cells)
            // ReSharper disable once PossibleLossOfFraction
            float probability = adjacentMines / adjacentCells;
            
            // Apply this probability to all adjacent cells according to the adjacent un-flagged cells
            foreach ((int x, int y) pair in cell.WorkingAdjacentCells)
            {
                board.Grid[pair.x, pair.y].UpdateProbabilities(probability * 100);
            }
        }
    }

    /// <summary>
    /// Flag every cell with a 100% chance of being a mine.
    /// </summary>
    /// <param name="board">A reference to the board to act on.</param>
    private static void FlagMines(ref Board board)
    {
        // flag all cells with a probability of 100
        // and update scratch values to remove flagged cells from the list of adjacent cells and from the count
        // of adjacent mines

        foreach (Cell cell in 
                 from Cell cell in board 
                 where !cell.IsDug 
                 where !(Math.Abs(cell.Probability - 100.0) > 0.1f) 
                 select cell)
        {
            //flag the cell
            cell.Flag();
            
            // for each adjacent cell, remove the current coordinates from the list of adjacent cells and subtract
            // one from the number of adjacent mines.
            foreach ((int x, int y) adjacent in cell.WorkingAdjacentCells)
            {
                board.Grid[adjacent.x, adjacent.y].WorkingAdjacentCells.Remove(cell.Coordinates);
                board.Grid[adjacent.x, adjacent.y].WorkingAdjacentMines--;
            }
        }
    }

    private static void EndGame(bool success)
    {
        Game.Console.ColorWrite(success ? "\n\n  The game was won!" : "\n\n  The game was LOST! :(",
            ConsoleColor.Magenta);

        Environment.Exit(0);
    }
}