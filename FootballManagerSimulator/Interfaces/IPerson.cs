using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IPerson
{
    int ID { get; set; }
    string Name { get; set; }
    Team? Team { get; set; }
}
