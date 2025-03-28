using System.Diagnostics;

namespace Minesweeper;

internal static class Program
{
    private const bool Debug = false;
    private const int TestRuns = 5000;
    
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
        int randomDefeats = 0;
        int stuck = 0;
        Stopwatch sw = new();
        double avgTime = 0.0;
        const int timeout = 1000; // Timeout in milliseconds
        (int r, int c) initialDugCell = (-1, -1);

        Console.Write("Progress: 0% ...");
        
        while (turns < TestRuns)
        {
            turns++;
            sw.Restart();
            Task<GameResult> solveTask = Task.Run(() => Solve(board));
            solveTask.Wait();
            {
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
            }
            // else
            // {
            //     // Handle timeout
            //     Console.WriteLine("Solve method timed out.");
            //     stuck++;
            //     board.GameOver = true;
            //     Console.WriteLine(board);
            //     if (initialDugCell is not (-1, -1)) Console.WriteLine("First cell dug was {0}{1}", GetCharacterFromPosition(initialDugCell.c + 1), initialDugCell.r + 1);
            //     Console.ReadKey();
            // }

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
                    initialDugCell = (r0, r1);
                    found = true;
                    break;
                }
            }
        }
        
        Console.Write(" Done!\n\n");
        
        Console.WriteLine("Stats:");
        Console.WriteLine($"  Turns: {turns}\n" +
                          $"  Victories: {victories}\n" +
                          $"  Defeats: {defeats}\n" +
                          $"  Random Defeats: {randomDefeats}\n" +
                          $"  Stuck: {stuck}\n" +
                          $"  Time: {sw.ElapsedMilliseconds}ms\n" +
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
                if (Debug) Console.WriteLine("Cell {0}{1} is being analyzed for range fusion.", GetCharacterFromPosition(c + 1), r + 1);
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

                // bool didSomething = false;
                // foreach (Range oldRange in ranges)
                // {
                //     foreach (Cell cell in oldRange.Cells)
                //     {
                //         if (!fused.Cells.Contains(cell) && cell is { IsFlagged: false, IsDug: false })
                //         {
                //             didSomething = true;
                //             if (Debug) Console.WriteLine("Cell {0}{1} is not part of the fused range. Digging...", GetCharacterFromPosition(c + 1), r + 1);
                //             if (cell.Dig())
                //             {
                //                 if (Debug) Console.WriteLine("Defeat at straggler check :(");
                //                 board.GameOver = true;
                //                 return false;
                //             }
                //         }
                //     }
                // }
                // return didSomething;
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