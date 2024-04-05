using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IMatchSimulator
{
    void ProcessMatch(Fixture fixture);
    void PrepareMatch(Fixture fixture);
}
