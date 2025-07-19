using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetition
{
    CompetitionType Type { get; }
    int Id { get; }
    string Name { get; }
    List<Fixture> Fixtures { get; set; }
    IEnumerable<Club> Clubs { get; set; }
    List<DrawDateModel> DrawDates { get; set; }
}
