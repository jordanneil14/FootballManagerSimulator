using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface ICompetitionProvider
{
    ICompetition CreateCompetition(CompetitionModel competition);
    ICompetitionService CompetitionService { get; }
    CompetitionType Type { get; }
}

