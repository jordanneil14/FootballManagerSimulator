using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IMatchSimulatorHelper
{
    void ProcessMatch(Fixture fixture, ICompetition competition);
    void PrepareMatch(Fixture fixture);
    void ConcludeFixture(Fixture fixture, ICompetition competition);
}
