using FootballManagerSimulator.Structures;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Interfaces;

public interface ILeague
{
    int Id { get; } 
    string Name { get; }
    List<Fixture> Fixtures { get; set; }
    List<Club> Clubs { get; set; }
}
