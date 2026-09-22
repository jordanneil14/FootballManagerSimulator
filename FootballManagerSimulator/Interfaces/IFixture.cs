using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Interfaces;

public interface IFixture
{
    ClubModel HomeClub { get; set; }
    ClubModel AwayClub { get; set; }
    int Round { get; set; }
    int? GoalsHome { get; set; }
    int? GoalsAway { get; set; }
}
