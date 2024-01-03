using System.Diagnostics;
using System.Xml;
using Minesweeper.Game.Core;

namespace Minesweeper.Game;

/// <summary>
/// Handles interactions with the console: printing the game board, taking player input, and handling menus.
/// </summary>
public static class Console
{
    /// <summary>
    /// Extension method for Minesweeper.Game.Core.Board to print the grid to the console.
    /// </summary>
    /// <param name="board">Self.</param>
    public static void PrintBoard(this Board board)
    {
        //first, print the top bar
        System.Console.Write("_");
        for (int i = 0; i < board.Width * 3; i++)
            System.Console.Write("_");
        System.Console.Write("_\n"); //and append a newline
        
        //now iterate left to right then top to bottom
        for (int y = 0; y < board.Height; y++)
        {
            //print the sidebar
            System.Console.Write("|");
            for (int x = 0; x < board.Width; x++)
            {
                //now we have selected a single cell
                //for this we must print a single 
                //character of a certain color according to
                //whether it is dug and whether it has
                //adjacent cells
                
                /*
                 * CONSOLE COLORS:
                 * -> 1-2 adj: BLUE
                 * -> 3-4 adj: YELLOW
                 * -> 5-6 adj: RED
                 * -> 7+ adj: PURPLE
                 */

                //if the cell is not dug, print a white 0 and continue
                if (!board.Grid[x, y].IsDug)
                {
                    // BUT if it is flagged, print a pink F
                    if (board.Grid[x, y].IsFlagged)
                    {
                        ColorWrite(" F ", ConsoleColor.Magenta);
                        continue;
                    }
                    System.Console.Write(" 0 ");
                    continue;
                }
                
                //if the cell is dug and has zero adjacent, print three spaces and continue
                if (board.Grid[x, y].AdjacentMines == 0)
                {
                    System.Console.Write("   ");
                    continue;
                }
                
                //switch/case to choose color
                ColorWrite($" {board.Grid[x,y].AdjacentMines} ", board.Grid[x, y].AdjacentMines switch
                {
                    1 or 2 => ConsoleColor.Blue,
                    3 or 4 => ConsoleColor.Yellow,
                    5 or 6 => ConsoleColor.Red,
                    > 7 => ConsoleColor.DarkMagenta,
                    _ => ConsoleColor.White
                });


            }
            System.Console.Write("|\n");
        }
    }

    public static void ColorWrite(string input, ConsoleColor color)
    {
        ConsoleColor orig = System.Console.ForegroundColor;
        System.Console.ForegroundColor = color;
        System.Console.Write(input);
        System.Console.ForegroundColor = orig;
    }
}