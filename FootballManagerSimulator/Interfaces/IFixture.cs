using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IFixture
{
    Team HomeTeam { get; set; }
    Team AwayTeam { get; set; }
    int WeekNumber { get; set; }
    int GoalsHome { get; set; }
    int GoalsAway { get; set; }
}
