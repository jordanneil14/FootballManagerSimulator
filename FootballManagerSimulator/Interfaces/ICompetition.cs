using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetition
{
    CompetitionType Type { get; }
    bool IsLeague { get; }
    int Id { get; }
    string Name { get; }
    List<Fixture> Fixtures { get; set; }
    List<Club> Clubs { get; set; }
    List<DrawDateModel> DrawDates { get; set; }
}
