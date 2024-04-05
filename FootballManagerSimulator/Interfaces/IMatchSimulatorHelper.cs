using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IMatchSimulatorHelper
{
    void ProcessMatch(Fixture fixture);
    void PrepareMatch(Fixture fixture);
}
