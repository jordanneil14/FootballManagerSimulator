using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Interfaces;

public interface IMatchSimulatorHelper
{
    void ProcessMatch(FixtureModel fixture, ICompetition competition);
    void PrepareMatch(FixtureModel fixture);
    void ConcludeFixture(FixtureModel fixture, ICompetition competition);
}
