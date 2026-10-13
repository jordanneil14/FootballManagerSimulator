using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Models;

public class FixtureModel : IFixture
{
    public int Id { get; set; }
    public ClubModel HomeClub { get; set; } = new ClubModel();
    public ClubModel AwayClub { get; set; } = new ClubModel();
    public int Round { get; set; }
    public int? GoalsHome { get; set; }
    public int? GoalsAway { get; set; }
    public bool Concluded { get; set; }
    public DateOnly Date { get; set; }
    public int Minute { get; set; }
    public ClubModel? ClubWon { get; set; } = null;

    public List<GoalModel> HomeScorers { get; set; } = new List<GoalModel>();
    public List<GoalModel> AwayScorers { get; set; } = new List<GoalModel>();
    public TimeOnly KickOffTime { get; set; }
}
