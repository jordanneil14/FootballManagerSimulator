using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Structures;

public class Manager : IPerson
{
    public int ID { get; set; }
    public string Name { get; set; } = "";
    public Team? Team { get; set; }
}
