using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Interfaces;

public interface IGameCreator
{
    string ManagerName { get; set; }
    int LeagueId { get; set; }
    int ClubId { get; set; }
    IEnumerable<ClubModel> Clubs { get; }
    IEnumerable<CompetitionModel> Competitions { get; }
}
