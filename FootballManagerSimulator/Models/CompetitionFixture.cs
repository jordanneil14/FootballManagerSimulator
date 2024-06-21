using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class CompetitionFixture
{
    public ICompetition Competition { get; set; }
    public IEnumerable<Fixture> Fixtures { get; set; } = new List<Fixture>();
}
