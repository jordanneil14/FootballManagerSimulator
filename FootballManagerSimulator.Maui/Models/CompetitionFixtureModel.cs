using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Models;

public class CompetitionFixtureModel
{
    public ICompetition Competition { get; set; }
    public IEnumerable<FixtureModel> Fixtures { get; set; } = new List<FixtureModel>();
}
