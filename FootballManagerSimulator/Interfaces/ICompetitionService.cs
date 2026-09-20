using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetitionService
{
    void GenerateNextRoundOfFixtures(ICompetition competition);
    void GeneratePreMatchReportForFixture(Fixture fixture);
}
