namespace Minesweeper.Game.Core;

/// <summary>
/// Defines an individual cell. Can be either a mine or not a mine; if it is not a mine, it can be dug or un-dug and it has a certain number 0-9 of adjacent mines.
/// </summary>
public class Cell
{
    /// <summary>
    /// The coordinates of the current cell on the game board.
    /// </summary>
    public (int x, int y) Coordinates { get; set; }
    
    /// <summary>
    /// A coordinate list of cells that affect or are affected by this cell (adjacent cells).
    /// This includes cells above below, to the side, and diagonal to this cell.
    /// </summary>
    public List<(int, int)> AdjacentCells { get; set; } = new(); // a list of adjacent cells
    
    /// <summary>
    /// Whether the current cell is mined. If true, digging this cell constitutes failure.
    /// </summary>
    public bool IsMine { get; set; } = false;
    
    /// <summary>
    /// Whether the current cell has been revealed (dug). This value is mutually exclusive with IsFlagged.
    /// </summary>
    public bool IsDug { get; private set; } = false;
    
    /// <summary>
    /// Whether the current cell is flagged. This value and IsDug are mutually exclusive.
    /// </summary>
    public bool IsFlagged { get; private set; } = false;
    
    /// <summary>
    /// The number of adjacent mined cells. Should only be read if the current cell is dug.
    /// </summary>
    public int AdjacentMines { get; set; } = 0; //must be set before the cell is dug.
    
    /* SCRATCH VALUES */
    
    /// <summary>
    /// A list of recorded probabilities given by the UpdateProbabilities() method.
    /// This list is used to average out probability values when updating the value.
    /// Flushed every turn.
    /// </summary>
    private List<float> _probabilities = new();

    /// <summary>
    /// Scratch value used by the solver.
    /// Stores the number of adjacent mines minus the number of adjacent flagged cells.
    /// </summary>
    public int WorkingAdjacentMines { get; set; } = 0; // a pencil value that stores the number of adjacent mines minus flagged cells
    
    /// <summary>
    /// Scratch value used by the solver.
    /// This list stores the coordinate list of adjacent mines minus the coordinates of adjacent flagged cells.
    /// This is to isolate the flagged cells from the solving algorithm.
    /// </summary>
    public List<(int, int)> WorkingAdjacentCells { get; set; } = new(); // scratch value for adjacent cells minus adjacent flagged cells
    
    /// <summary>
    /// Scratch value used by the solver.
    /// This value stores the probability 0-1 that the cell is mined.
    /// </summary>
    public float Probability { get; private set; } = -1.0f; // scratch value that represents the probability 0-1 that the current cell is a mine
    
    /* METHODS */
    
    /// <summary>
    /// Attempt to dig the given cell.
    /// </summary>
    /// <returns>See DigResult.cs; the result of the dig operation.</returns>
    public DigResult Dig()
    {
        if (IsFlagged || IsDug) return DigResult.Disallowed;
        if (IsMine) return DigResult.Fail;
        return AdjacentMines == 0 ? DigResult.Open : DigResult.Success;
    }

    public void Flag()
    {
        if (IsDug) return;
        IsFlagged = !IsFlagged;
    }

    /// <summary>
    /// Update the probability that this cell is a mine.
    /// If this cell's probability is not yet set, this will set the probability to the given value.
    /// If this cell's probability is set, it will add the given probability to a list of recorded probabilities and set
    /// the cell's probability to the average of those values.
    /// Finally, if the given value is 100 or 0, the cell's probability is locked at 100.
    /// </summary>
    /// <param name="prop">The probability to update. Float 0-100</param>
    public void UpdateProbabilities(float prop)
    {
        if (IsDug | IsFlagged) return;
        
        // if the cell has 100% or a 0% chance, it is locked
        if (Math.Abs(Probability - 100.0f) < 0.1 || Math.Abs(Probability - 0.0f) < 0.1) return;
        // if the given probability is 100% or 0%, lock it in
        if (Math.Abs(prop - 100.0f) < 0.1 || Math.Abs(prop - 0.0f) < 0.1)
        {
            Probability = prop;
            _probabilities = Array.Empty<float>().ToList();
            return;
        }
        // if the probability is not yet set, set it.
        if (Math.Abs(Probability - (-1.0f)) < 0.01)
        {
            Probability = prop;
            _probabilities.Add(prop);
        }
        
        //averages!!
        _probabilities.Add(prop);
        Probability = _probabilities.Sum() / _probabilities.Count;
    }

    /// <summary>
    /// Reset the probabilities; to be called at the start of a new turn.
    /// </summary>
    public void Reset()
    {
        _probabilities = new List<float>();
        Probability = -1.0f;
    }

    /// <summary>
    /// Call this for each cell once the board is done being built.
    /// This method sets the preliminary values for WorkingAdjacentCells and WorkingAdjacentMines.
    /// </summary>
    public void Init()
    {
        WorkingAdjacentCells = AdjacentCells;
        WorkingAdjacentMines = AdjacentMines;
    }
}