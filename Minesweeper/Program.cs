using Minesweeper.Game.Core;

namespace Minesweeper;

internal static class Program
{
    // This is just a simple wrapper to manage starting the application.
    // Takes arguments for width, height, and mines percent in that order.
    // defaults are 8, 8, and 16% respectively.
    private static void Main(string[] args)
    {
        int width;
        int height;
        float minesPercent;

        try
        {
            width = Convert.ToInt32(args[0]);
            height = Convert.ToInt32(args[1]);
            minesPercent = (float)Convert.ToDouble(args[2]);
        }
        catch (Exception)
        {
            Console.WriteLine("There was an error processing your arguments for width, height, and mines % of the board.\nThe defaults will be used (8, 8, 16%).");
            width = 8;
            height = 8;
            minesPercent = 0.16f;
        }

        Board board = new(width, height, minesPercent);
        // now let's solve ~
        Solver.Core.Solver.Start(board);
    }
}