using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IGameCreator
{
    string ManagerName { get; set; }
    int LeagueId { get; set; }
    int ClubId { get; set; }
    IEnumerable<Settings.ClubModel> Clubs { get; }
    IEnumerable<Settings.CompetitionModel> Leagues { get; }
}
