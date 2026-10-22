namespace FootballManagerSimulator.Maui.Models;


public class LeaguePositionModel
{
    public ClubModel Club { get; set; } = new ClubModel();
    public int Points { get; set; }
    public int GoalsScored { get; set; }
    public int GoalsConceded { get; set; }
    public int Played { get; set; }
    public string GoalDifference => FormatGoalDifference();

    public string FormatGoalDifference()
    {
        var goalDifference = GoalsScored - GoalsConceded;
        return goalDifference > 0 ? $"+{goalDifference}" : goalDifference.ToString();
    }
}
