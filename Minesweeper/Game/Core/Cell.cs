namespace Minesweeper.Game.Core;

/// <summary>
/// Defines an individual cell. Can be either a mine or not a mine; if it is not a mine, it can be dug or un-dug and it has a certain number 0-9 of adjacent mines.
/// </summary>
public class Cell
{
    public bool IsMine { get; set; } = false;
    public bool IsDug { get; private set; } = false;
    public bool IsFlagged { get; private set; } = false;
    public int Adjacent { get; set; } = 0; //must be set before the cell is dug.
    private List<float> Probabilities = new();
    public float Probability { get; private set; } = -1.0f;

    /// <summary>
    /// Attempt to dig the given cell.
    /// </summary>
    /// <returns>See DigResult.cs; the result of the dig operation.</returns>
    public DigResult Dig()
    {
        if (IsMine) return DigResult.Fail;
        return Adjacent == 0 ? DigResult.Open : DigResult.Success;
    }

    public void Flag()
    {
        if (IsDug) return;
        IsFlagged = !IsFlagged;
    }

    public void UpdateProbabilities(float prop)
    {
        if (IsDug | IsFlagged) return;
        
        // if the cell has 100% chance, it is locked
        if (Math.Abs(Probability - 100.0f) < 0.01) return;
        // if the given probability is 100%, lock it in
        if (Math.Abs(prop - 100.0f) < 0.01)
        {
            Probability = prop;
            Probabilities = Array.Empty<float>().ToList();
            return;
        }
        // if the probability is not yet set, set it.
        if (Math.Abs(Probability - (-1.0f)) < 0.01)
        {
            Probability = prop;
            Probabilities.Add(prop);
        }
        
        //averages!!
        Probabilities.Add(prop);
        Probability = Probabilities.Sum() / Probabilities.Count;
    }

    /// <summary>
    /// Reset the probabilities; to be called at the start of a new turn.
    /// </summary>
    public void Reset()
    {
        Probabilities = new List<float>();
    }
}