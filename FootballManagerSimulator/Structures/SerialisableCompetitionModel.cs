using static FootballManagerSimulator.Structures.Fixture;

namespace FootballManagerSimulator.Structures;

public class SerialisableCompetitionModel
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string CompetitionType { get; set; } = "";
    public List<SerialisableFixtureModel> Fixtures { get; set; } = new List<SerialisableFixtureModel>();
}
