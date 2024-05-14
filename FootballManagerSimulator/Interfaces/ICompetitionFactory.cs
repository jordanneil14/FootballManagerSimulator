using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface ICompetitionFactory
{
    ICompetition CreateCompetition(Settings.CompetitionModel competition);
    string Type { get; }
}

