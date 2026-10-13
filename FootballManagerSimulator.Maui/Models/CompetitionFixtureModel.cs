using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Structures;

public class CompetitionFixtureModel
{
    public ICompetition Competition { get; set; }
    public IEnumerable<FixtureModel> Fixtures { get; set; } = new List<FixtureModel>();
}
