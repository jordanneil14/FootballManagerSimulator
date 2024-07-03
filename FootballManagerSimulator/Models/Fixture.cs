using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Models;

public class Fixture : IFixture
{
    public int Id { get; set; }
    public Club HomeClub { get; set; } = new Club();
    public Club AwayClub { get; set; } = new Club();
    public int Round { get; set; }
    public int? GoalsHome { get; set; }
    public int? GoalsAway { get; set; }
    public bool Concluded { get; set; }
    public DateOnly Date { get; set; }
    public int Minute { get; set; }
    public Club? ClubWon { get; set; } = null;

    public List<GoalModel> HomeScorers { get; set; } = new List<GoalModel>();
    public List<GoalModel> AwayScorers { get; set; } = new List<GoalModel>();
    public TimeOnly KickOffTime { get; set; }
}
