using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetition
{
    CompetitionType Type { get; }
    bool IsLeague { get; }
    int Id { get; }
    string Name { get; }
    List<FixtureModel> Fixtures { get; set; }
    List<ClubModel> Clubs { get; set; }
    List<DrawSettingsModel> DrawSettings { get; set; }
}
