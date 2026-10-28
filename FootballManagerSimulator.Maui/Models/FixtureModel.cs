using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;

namespace FootballManagerSimulator.Maui.Models;

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
    public List<CardModel> HomeCards { get; set; } = new List<CardModel>();
    public List<CardModel> AwayCards { get; set; } = new List<CardModel>();
    public TimeOnly KickOffTime { get; set; }
    public FixtureType FixtureType { get; set; } = FixtureType.Base;
    public int? FirstLegGoalsHome { get; set; }
    public int? FirstLegGoalsAway { get; set; }
    public List<InjuryModel> HomeInjuries { get; set; } = new List<InjuryModel>();
    public List<InjuryModel> AwayInjuries { get; set; } = new List<InjuryModel>();
    // Set once an English Cup tie's prize money has gone to both clubs, so it is never paid twice.
    public bool PrizeMoneyPaid { get; set; }
}
