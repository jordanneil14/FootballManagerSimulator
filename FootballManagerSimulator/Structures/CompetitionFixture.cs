namespace FootballManagerSimulator.Structures;

public class CompetitionFixture
{
    public int LeagueId { get; set; }
    public IEnumerable<Fixture> Fixtures { get; set; } = new List<Fixture>();
}
