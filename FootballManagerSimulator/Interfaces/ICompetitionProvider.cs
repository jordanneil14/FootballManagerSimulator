using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetitionProvider
{
    ICompetition CreateCompetition(CompetitionModel competition);
    ICompetitionService CompetitionService { get; }
    CompetitionType Type { get; }
}

