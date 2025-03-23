using System;
using System.Diagnostics;
using Minesweeper;
using Range = Minesweeper.Range;

internal static class Program
{
    private static void Main()
    {
        Board board = new(16, 40);

        foreach (Cell c in board.Grid)
        {
            if (c.NearbyMines != 0) continue;
            c.Dig();
            break;
        }

        int turns = 0;
        int victories = 0;
        int defeats = 0;
        int stuck = 0;
        Stopwatch sw = new();
        long avgTime = 0;
        while (turns < 10000)
        {
            turns++;
            sw.Restart();
            GameResult res = Solve(board);
            sw.Stop();
            if (avgTime == 0) avgTime = sw.ElapsedMilliseconds;
            else avgTime += (sw.ElapsedMilliseconds - avgTime) / turns;
            switch (res)
            {
                case GameResult.Victory:
                    victories++;
                    Console.WriteLine(board);
                    Console.WriteLine("Victory! Total count: {0}", victories);
                    break;
                case GameResult.Defeat:
                    defeats++;
                    Console.WriteLine(board);
                    Console.WriteLine("Defeat! Total count: {0}", defeats);
                    break;
                case GameResult.Stuck:
                    stuck++;
                    Console.WriteLine(board);
                    Console.WriteLine("Stuck! Total count: {0}", stuck);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            Console.WriteLine("Stats:");
            Console.WriteLine($"  Turns: {turns}\n" +
                              $"  Victories: {victories}\n" +
                              $"  Defeats: {defeats}\n" +
                              $"  Stuck: {stuck}\n" +
                              $"  Time: {sw.ElapsedMilliseconds}ms\n" +
                              $"  Average Time: {avgTime}ms\n" +
                              $"  Percent victory: {victories / (double) (turns) * 100:F2}%\n" +
                              $"  Percent defeat: {defeats / (double) (turns) * 100:F2}%\n" +
                              $"  Percent stuck: {stuck / (double) (turns) * 100:F2}%\n");
            //Console.ReadKey();
            board = new Board(16, 40);
            
            foreach (Cell c in board.Grid)
            {
                if (c.NearbyMines != 0) continue;
                c.Dig();
                break;
            }
        }
        
        // while (!board.GameOver)
        // {
        //     Console.Write(board);
        //
        //     Console.Write("\n Enter a position (e.g., A1) followed by `f` to flag or `d` to dig: ");
        //     string input = (Console.ReadLine() ?? "").Trim();
        //     
        //     if (input.Length < 2)
        //     {
        //         Console.WriteLine("Invalid input. Please enter a position followed by 'f' or 'd'.");
        //         continue;
        //     }
        //     
        //     int column = GetAlphabetPosition(input[0]);
        //     int row = int.Parse(input[1..input.IndexOf(' ')]) - 1;
        //     char action = input[^1];
        //     if (row < 0 || row >= board.Size || column < 1 || column > board.Size)
        //     {
        //         Console.WriteLine("Invalid position. Please enter a valid position.");
        //         continue;
        //     }
        //     
        //     switch (action)
        //     {
        //         case 'f':
        //             board.Grid[row, column - 1].IsFlagged = !board.Grid[row, column - 1].IsFlagged;
        //             break;
        //         case 'd':
        //         {
        //             if (!board.Grid[row, column - 1].Dig())
        //             {
        //                 Console.WriteLine("Game Over! You hit a mine.");
        //                 board.GameOver = true;
        //             }
        //
        //             break;
        //         }
        //         default:
        //             Console.WriteLine("Invalid action. Please enter `f` to flag or `d` to dig.");
        //             break;
        //     }
        // }
        
        Console.Write(board);
    }
    
    private static int GetAlphabetPosition(char character)
    {
        character = char.ToUpper(character);
        return character - 'A' + 1;
    }
    
    public static char GetCharacterFromPosition(int position)
    {
        if (position < 1 || position > 26)
        {
            throw new ArgumentOutOfRangeException(nameof(position), "Position must be between 1 and 26.");
        }
        return (char)('A' + position - 1);
    }

    private static GameResult Solve(Board board)
    {
        while (!board.GameOver)
        {
            bool stuck = true;
            
            // loop for digging safe cells
            for (int r = 0; r < board.Size; r++)
            {
                for (int c = 0; c < board.Size; c++)
                {
                    if (!board.Grid[r, c].IsDug) continue;
                    // avoid duplicate iteration
                    if (board.Grid[r, c].IsSafe) continue;
                    // if the cell is safe
                    if (board.Grid[r, c].NearbyMines != 0 && !board.CheckSafe(r, c)) continue;

                    stuck = false;
                    board.Grid[r, c].IsSafe = true;
                    // Console.WriteLine("Cell {0}{1} is safe. Digging...", GetCharacterFromPosition(c + 1), r + 1);
                    foreach (Cell cell in board.GetRange(r, c).Cells)
                    {
                        if (cell.Dig())
                        {
                            board.GameOver = true;
                            return GameResult.Defeat;
                        }
                    }
                        
                    //Console.Write(board);
                    //Console.ReadLine();
                }
            }
            
            for (int r = 0; r < board.Size; r++)
            {
                for (int c = 0; c < board.Size; c++)
                {
                    if (board.Grid[r, c].IsSafe) continue;
                    if (!board.Grid[r, c].IsDug) continue;
                    // if the cell's range is 100% certain
                    Range range = board.GetRange(r, c);
                    if (range.Mines != range.Cells.Count()) continue;

                    stuck = false;
                    // Console.WriteLine("Cell {0}{1} has a 100% certain range. Flagging all cells...", GetCharacterFromPosition(c + 1), r + 1);
                    foreach (Cell cell in range.Cells)
                    {
                        cell.IsFlagged = true;
                    }
                        
                    //Console.Write(board);
                    //Console.ReadLine();


                }
            }
            
            bool breakToOuterLoop = false;
            for (int r = 0; r < board.Size; r++)
            {
                for (int c = 0; c < board.Size; c++)
                {
                    if (!board.Grid[r, c].IsDug) continue;
                    
                    Range range = board.GetRange(r, c);
                    // Console.WriteLine("Cell {0}{1} is being analyzed for range fusion.", GetCharacterFromPosition(c + 1), r + 1);
                    List<Range> ranges = [];
                    for (int or = 0; or < board.Size; or++)
                    {
                        for (int oc = 0; oc < board.Size; oc++)
                        {
                            if (or == r && oc == c) continue;
                            if (!board.Grid[or, oc].IsDug) continue;
                            if (board.Grid[or, oc].IsSafe || board.Grid[r, c].IsSafe) continue;
                            
                            Range other = board.GetRange(or, oc);
                            // this check will ensure that we only fuse ranges where one is a subset of the other
                            if (other.Cells.Intersect(range.Cells).Count() >= Math.Min(other.Cells.Count(), range.Cells.Count()))
                            {
                                ranges.Add(other);
                            }
                        }
                    }
                    
                    // if we have 2 or more ranges that intersect, fuse them
                    if (ranges.Count < 2) continue;
                    Range fused = Range.BinaryAnd(ranges.ToArray());
                    if (!fused.Cells.Any()) continue;
                    
                    
                    
                    //Console.WriteLine("Fused range around {2}{3} has {0} cells and {1} mines.", fused.Cells.Count(), fused.Mines, GetCharacterFromPosition(c + 1), r + 1);
                    if (fused.Mines != 0 && fused.Mines == fused.Cells.Count())
                    {
                        //Console.WriteLine("Fused range is 100% certain. Flagging all cells...");
                        breakToOuterLoop = true;
                        stuck = false;
                        foreach (Cell cell in fused.Cells)
                        {
                            cell.IsFlagged = true;
                        }
                    }
                    //Console.Write(board);
                    //Console.ReadLine();
                    if (breakToOuterLoop) break;
                    
                    // check for cells that didn't make it into any range
                    // these cells may be dug now because we aren't certain
                    foreach (Range oldRange in ranges)
                    {
                        foreach (Cell cell in oldRange.Cells)
                        {
                            if (!fused.Cells.Contains(cell))
                            {
                                stuck = false;
                                //Console.WriteLine("Cell {0}{1} is not part of the fused range. Digging...", GetCharacterFromPosition(c + 1), r + 1);
                                if (cell.Dig())
                                {
                                    // Console.WriteLine("Defeat at straggler check :(");
                                    board.GameOver = true;
                                    return GameResult.Defeat;
                                }
                            }
                        }
                    }
                }
                
            }

            
            
            if (board.CheckVictory())
            {
                board.GameOver = true;
                return GameResult.Victory;
            }
            

            
            if (stuck)
            {
                // Console.WriteLine("No more safe cells to dig. Stuck.");
                return GameResult.Stuck;
            }
        }

        return (GameResult)100;
    }
    
}