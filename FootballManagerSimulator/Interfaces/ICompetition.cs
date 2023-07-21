using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetition
{
    string Name { get; }
    List<Fixture> Fixtures { get; set; }
}
