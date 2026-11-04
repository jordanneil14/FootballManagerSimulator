namespace FootballManagerSimulator.Maui.Models;

public class GoalModel
{
    public int Minute { get; set; }
    public int PlayerId { get; set; }
    // A penalty scored during play; shootout goals (after minute 120) are not marked.
    public bool IsPenalty { get; set; }
}
