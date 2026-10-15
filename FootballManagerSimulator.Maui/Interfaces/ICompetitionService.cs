using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface ICompetitionService
{
    void GenerateNextRoundOfFixtures(ICompetition competition);
    void GeneratePreMatchReportForFixture(FixtureModel fixture);
}
