using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IMatchSimulator
{
    void SimulateFirstHalf(Fixture fixture);

    void SimulateSecondHalf(Fixture fixture);
}
