namespace Minesweeper;

public class Range
{
    public IEnumerable<Cell> Cells { get; set; } = [];
    public int Mines { get; set; }

    /// <summary>
    /// Fuse ranges using an operation like binary AND-- keep only the cells that are common to all ranges.
    /// </summary>
    /// <param name="ranges"></param>
    /// <returns></returns>
    public static Range BinaryAnd(IEnumerable<Range> ranges)
    {
        if (ranges.Count() == 0)
            return new Range();
        
        Range result = new()
        {
            Cells = ranges.First().Cells
        };

        foreach (Range range in ranges)
        {
            result.Cells = result.Cells.Intersect(range.Cells);
        }

        result.Mines = ranges.Min(range => range.Mines);
        return result;
        
    }

    public float Probability() => Mines / (float)Cells.Count();
}