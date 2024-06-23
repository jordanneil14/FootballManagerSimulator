using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Interfaces;

public interface IGameCreator
{
    string ManagerName { get; set; }
    int LeagueId { get; set; }
    int ClubId { get; set; }
    IEnumerable<Club> Clubs { get; }
    IEnumerable<CompetitionModel> Competitions { get; }
}
