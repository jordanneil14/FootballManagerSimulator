using FootballManagerSimulator.Interfaces;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Structures;

public class GameCreator : IGameCreator
{
    private readonly Settings Settings;

    public GameCreator(
        IOptions<Settings> settings)
    {
        Settings = settings.Value;
    }

    public string ManagerName { get; set; } = string.Empty;
    public int LeagueId { get; set; }
    public int ClubId { get; set; }
    public IEnumerable<Settings.ClubModel> Clubs => Settings.Clubs;
    public IEnumerable<Settings.LeagueModel> Leagues => Settings.Leagues;
}
