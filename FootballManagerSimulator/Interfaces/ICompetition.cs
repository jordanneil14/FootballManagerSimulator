using FootballManagerSimulator.Structures;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetition
{
    int ID { get; } 
    string Name { get; }
    List<Fixture> Fixtures { get; set; }
    IEnumerable<Club> Teams { get; set; }
    JObject SerialisableCompetition();
}
