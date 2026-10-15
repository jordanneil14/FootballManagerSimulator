using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface IMatchSimulatorHelper
{
    void ProcessMatch(FixtureModel fixture, ICompetition competition);
    void PrepareMatch(FixtureModel fixture);
    void ConcludeFixture(FixtureModel fixture, ICompetition competition);
}
