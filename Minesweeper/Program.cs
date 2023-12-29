using Minesweeper.Game.Core;

namespace Minesweeper;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Hello, World!\n");

        Board board = new(8, 8, 32);
        Game.Console.PrintBoard(board);
    }
}