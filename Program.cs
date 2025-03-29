using System.Diagnostics;

namespace Minesweeper;

internal static class Program
{
    private static bool Debug = false;
    private static int TestRuns = 1000;
    
    private static void Main(string[] args)
    {
        if (args.Length == 2)
        {
            TestRuns = int.Parse(args[0]);
            Debug = true;
        }
        else if (args.Length == 1)
        {
            TestRuns = int.Parse(args[0]);
        }
        else if (args.Length != 0)
        {
            Console.WriteLine("This program accepts zero, one, or two arguments:");
            Console.WriteLine("  (minesweeper) [<number of test runs>] [<debug>]");
            Console.WriteLine("  <number of test runs> - The number of test runs to perform. Default is 5000.");
            Console.WriteLine("  <debug> - If specified, enables debug mode. This allows you to step through one action at a time.");
            return;
        }
        
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
        int randomDefeats = 0;
        int stuck = 0;
        Stopwatch sw = new();
        Stopwatch overallTime = new();
        double avgTime = 0.0;

        Console.Write("Progress: 0% ...");

        overallTime.Start();
        while (turns < TestRuns)
        {
            turns++;
            sw.Restart();
            Task<GameResult> solveTask = Task.Run(() => Solve(board));
            solveTask.Wait();
            
            GameResult res = solveTask.Result;
            sw.Stop();
            if (avgTime == 0.0) avgTime = sw.ElapsedMilliseconds;
            else avgTime += (sw.ElapsedMilliseconds - avgTime) / turns;
            switch (res)
            {
                case GameResult.Victory:
                    victories++;
                    //Console.WriteLine(board);
                    //Console.WriteLine("Victory! Total count: {0}", victories);
                    break;
                case GameResult.Defeat:
                    defeats++;
                    //Console.WriteLine(board);
                    //Console.WriteLine("Defeat! Total count: {0}", defeats);
                    break;
                case GameResult.RandomDefeat:
                    randomDefeats++;
                    //Console.WriteLine(board);
                    //Console.WriteLine("Random Defeat! Total count: {0}", randomDefeats);
                    break;
                case GameResult.Stuck:
                    stuck++;
                    //Console.WriteLine(board);
                    //Console.WriteLine("Stuck! Total count: {0}", stuck);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            
            }


            double progress = (turns / (double)TestRuns) * 100;
            if (progress > 0 && progress % 10.0 == 0)
            {
                Console.Write(" {0}% ({1}/{2}) ...", progress, turns, TestRuns);
            }

            bool found = false;
            int initial = board.Size / 4;
            while (found == false)
            {
                board = new Board(16, 40);

                for (int r0 = initial; r0 < initial + board.Size / 2; r0++)
                for (int r1 = initial; r1 < initial + board.Size / 2; r1++)
                {
                    if (board.Grid[r0, r1].NearbyMines != 0) continue;
                    if (board.Grid[r0, r1].Dig()) continue;
                    found = true;
                    break;
                }
            }
        }
        overallTime.Stop();
        Console.Write(" Done!\n\n");
        
        Console.WriteLine("Stats:");
        Console.WriteLine($"  Turns: {turns}\n" +
                          $"  Victories: {victories}\n" +
                          $"  Defeats: {defeats}\n" +
                          $"  Random Defeats: {randomDefeats}\n" +
                          $"  Stuck: {stuck}\n" +
                          $"  Time: {overallTime.Elapsed}\n" +
                          $"  Average Time: {avgTime:F4}ms\n" +
                          $"  Percent victory: {victories / (double) (turns) * 100:F2}%\n" +
                          $"  Percent defeat: {defeats / (double) (turns) * 100:F2}%\n" +
                          $"  Percent random defeat: {randomDefeats / (double) (turns) * 100:F2}%\n" +
                          $"  Percent stuck: {stuck / (double) (turns) * 100:F2}%\n");
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
            // ReSharper disable once ReplaceWithSingleAssignment.True
            bool stuck = true;

            if (Debug) Console.WriteLine("Dig safe cells");
            if (DigSafeCells(board)) stuck = false;
            if (Debug)
            {
                Console.WriteLine(board);
                Console.Write(stuck);
                Console.ReadKey();
            }

            if (board.GameOver) return GameResult.Defeat;
            
            if (Debug) Console.WriteLine("Flag certain ranges");
            if (FlagCertainRanges(board)) stuck = false;
            if (Debug)
            {
                Console.WriteLine(board);
                Console.Write(stuck);
                Console.ReadKey();
            }
            
            if (board.GameOver) return GameResult.Defeat;
            
            if (Debug) Console.WriteLine("Analyze ranges");
            if (AnalyzeAndFuseRanges(board)) stuck = false;
            if (Debug)
            {
                Console.WriteLine(board);
                Console.Write(stuck);
                Console.ReadKey();
            }

            if (board.GameOver) return GameResult.Defeat;
            
            if (stuck)
            {
                if (!Guess(board)) return GameResult.Stuck;
                if (Debug)
                {
                    Console.WriteLine(board);
                    Console.WriteLine("Guessed");
                    Console.ReadKey();
                }
                if (board.GameOver) return GameResult.RandomDefeat;
                continue;
            }

            if (board.CheckVictory())
            {
                board.GameOver = true;
                return GameResult.Victory;
            }
        }
        
        return GameResult.Defeat;
    }
    
    private static bool DigSafeCells(Board board)
    {
        bool dugACell = false;
        for (int r = 0; r < board.Size; r++)
        {
            for (int c = 0; c < board.Size; c++)
            {
                // conditions //
                // only check around already dug cells
                if (!board.Grid[r, c].IsDug) continue;
                // skip cells marked as `safe` (avoid duplicate iteration)
                if (board.Grid[r, c].IsSafe) continue;
                // if the cell has zero nearby mines or it is safe, dig its adjacent cells
                if (board.Grid[r, c].NearbyMines != 0 && !board.CheckSafe(r, c)) continue;
                
                board.Grid[r, c].IsSafe = true;
                if (Debug) Console.WriteLine("Cell {0}{1} is safe. Digging...", GetCharacterFromPosition(c + 1), r + 1);
                foreach (Cell cell in board.GetRange(r, c).Cells)
                {
                    if (cell.Dig())
                    {
                        board.GameOver = true;
                        return false;
                    }
                }

                dugACell = true;
            }
        }
        return dugACell;
    }
    
    private static bool FlagCertainRanges(Board board)
    {
        for (int r = 0; r < board.Size; r++)
        {
            for (int c = 0; c < board.Size; c++)
            {
                if (board.Grid[r, c].IsSafe) continue;
                if (!board.Grid[r, c].IsDug) continue;
    
                Range range = board.GetRange(r, c);
                if (range.Mines != range.Cells.Count()) continue;
    
                if (Debug) Console.WriteLine("Cell {0}{1} has a 100% certain range. Flagging all cells...", GetCharacterFromPosition(c + 1), r + 1);
                foreach (Cell cell in range.Cells)
                {
                    cell.IsFlagged = true;
                }
                return true;
            }
        }
        return false;
    }
    
    private static bool AnalyzeAndFuseRanges(Board board)
    {
        for (int r = 0; r < board.Size; r++)
        {
            for (int c = 0; c < board.Size; c++)
            {
                if (!board.Grid[r, c].IsDug || board.Grid[r, c].IsSafe) continue;
    
                
                Range range = board.GetRange(r, c);
                Range largestRange = range;
                if (Debug) Console.WriteLine("Cell {0}{1} is being analyzed for range fusion.", GetCharacterFromPosition(c + 1), r + 1);
                List<Range> ranges = [range];
                
                for (int or = 0; or < board.Size; or++)
                {
                    for (int oc = 0; oc < board.Size; oc++)
                    {
                        if (or == r && oc == c) continue;
                        if (!board.Grid[or, oc].IsDug) continue;
                        if (board.Grid[or, oc].IsSafe) continue;
    
                        Range other = board.GetRange(or, oc);
                        if (other.Cells.Intersect(range.Cells).Count() >= Math.Min(other.Cells.Count(), range.Cells.Count()))
                        {
                            // keep track of which range is the largest-- largestRange will effectively be the superset of the others.
                            if (other.Cells.Count() > largestRange.Cells.Count()) largestRange = other;
                            ranges.Add(other);
                        }
                    }
                }
    
                if (ranges.Count < 2) continue;
                
                // We now have a set of ranges such that some range in the set is a superset of all other ranges.
                Range fused = Range.BinaryAnd(ranges);
                if (!fused.Cells.Any()) continue;
    
                if (Debug) Console.WriteLine("Fused range around {2}{3} has {0} cells and {1} mines.", fused.Cells.Count(), fused.Mines, GetCharacterFromPosition(c + 1), r + 1);
                if (fused.Mines != 0 && fused.Mines == fused.Cells.Count())
                {
                    if (Debug) Console.WriteLine("Fused range is 100% certain. Flagging all cells...");
                    foreach (Cell cell in fused.Cells)
                    {
                        cell.IsFlagged = true;
                    }
                    return true;
                }

                // now, we can use the difference method to find the cells that are part of the superset but not any other ranges
                // this can give us slightly more information
                // the number of mines in this range must be the number of mines in the superset minus the number of mines in the fused range
                Range differenceRange = new()
                {
                    Cells = largestRange.Cells.Except(fused.Cells), // cells in the superset but not in the fused range
                    Mines = largestRange.Mines - fused.Mines
                };
                
                if (differenceRange.Cells.Any())
                {
                    // if the difference range has cells, we can analyze it further
                    if (Debug) Console.WriteLine("Difference range around {2}{3} has {0} cells and {1} mines.", differenceRange.Cells.Count(), differenceRange.Mines, GetCharacterFromPosition(c + 1), r + 1);
                    
                    // if the difference range is 100% certain, flag those cells as well
                    if (differenceRange.Mines == differenceRange.Cells.Count())
                    {
                        if (Debug) Console.WriteLine("Difference range is 100% certain. Flagging all cells...");
                        foreach (Cell cell in differenceRange.Cells)
                        {
                            cell.IsFlagged = true;
                        }
                        return true;
                    }
                
                    if (differenceRange.Mines == 0)
                    {
                        foreach (Cell differenceRangeCell in differenceRange.Cells)
                        {
                            if (differenceRangeCell.Dig())
                            {
                                board.GameOver = true;
                                return false;
                            }
                        }
                
                        return true;
                    }
                }
                
                
                
                return false;
            }
        }
        return false;
    }

    private static bool Guess(Board board)
    {
        Range best = new() { Cells = new List<Cell> { new Cell(false) }, Mines = int.MaxValue };
        
        for (int r = 0; r < board.Size; r++)
        {
            for (int c = 0; c < board.Size; c++)
            {
                if (!board.Grid[r, c].IsDug || board.Grid[r, c].IsSafe) continue;
    
                Range range = board.GetRange(r, c);
                if (Debug) Console.WriteLine("Cell {0}{1} is being analyzed for range fusion to guess out of stuck state.", GetCharacterFromPosition(c + 1), r + 1);
                List<Range> ranges = new() { range };
                for (int or = 0; or < board.Size; or++)
                {
                    for (int oc = 0; oc < board.Size; oc++)
                    {
                        if (or == r && oc == c) continue;
                        if (!board.Grid[or, oc].IsDug) continue;
                        if (board.Grid[or, oc].IsSafe) continue;
    
                        Range other = board.GetRange(or, oc);
                        if (other.Cells.Intersect(range.Cells).Count() >= Math.Min(other.Cells.Count(), range.Cells.Count()))
                        {
                            ranges.Add(other);
                        }
                    }
                }
    
                if (ranges.Count < 2) continue;
                Range fused = Range.BinaryAnd(ranges.ToArray());
                if (Debug) Console.WriteLine("Fused range around {2}{3} has {0} cells and {1} mines, probability {4}", fused.Cells.Count(), fused.Mines, GetCharacterFromPosition(c + 1), r + 1, range.Probability());

                if (!fused.Cells.Any()) continue;
                
                if (range.Probability() < best.Probability())
                {
                    best = range;
                }
            }
        }

        // if no ranges are viable, blindly choose the first range that exists
        if (best.Mines == int.MaxValue)
        {
            for (int r = 0; r < board.Size; r++)
            {
                for (int c = 0; c < board.Size; c++)
                {
                    if (!board.Grid[r, c].IsDug || board.Grid[r, c].IsSafe) continue;
    
                    Range range = board.GetRange(r, c);
                    if (range.Cells.Any())
                    {
                        best = range;
                        break;
                    }
                }
            }
        }
        
        //finally, if no ranges exist, literally just choose any random cell. This can happen if
        // a cell is sandwiched in a corner or an edge surrounded by mines. (i.e., no adjacent non-mine cells)
        if (best.Mines == int.MaxValue)
        {
            List<Cell> cells = [];
            for (int r = 0; r < board.Size; r++)
            {
                for (int c = 0; c < board.Size; c++)
                {
                    if (board.Grid[r, c].IsDug) continue;
                    cells.Add(board.Grid[r, c]);
                }
            }
            best = new Range { Cells = cells, Mines = 0 };
        }
        
        Random rand = new();
        int index = rand.Next(0, best.Cells.Count());
        
        if (best.Cells.ElementAt(index).Dig())
        {
            board.GameOver = true;
            return true;
        }
        
        return true;
    }
}