using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IFixture
{
    Club HomeClub { get; set; }
    Club AwayClub { get; set; }
    int WeekNumber { get; set; }
    int? GoalsHome { get; set; }
    int? GoalsAway { get; set; }
}
