using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetitionFactory
{
    ICompetition CreateCompetition(CompetitionModel competition);
    void GeneratePreMatchReportForFixture(Fixture fixture);
    void GenerateNextRoundOfFixtures(ICompetition competition);
    CompetitionType Type { get; }
}

