using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetitionFactory
{
    ICompetition CreateCompetition(Settings.CompetitionModel competition);
    void GeneratePreMatchReportForFixture(Fixture fixture);
    void GenerateNextRoundOfFixtures(ICompetition competition, DateOnly fixtureDate);
    string Type { get; }
}

