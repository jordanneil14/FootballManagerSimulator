using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IPerson
{
    int ID { get; set; }
    string Name { get; set; }
    Club? Club { get; set; }
    Contract? Contract { get; set; }

}
