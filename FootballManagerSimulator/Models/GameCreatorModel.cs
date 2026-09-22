using FootballManagerSimulator.Interfaces;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Models;

public class GameCreatorModel : IGameCreator
{
    private readonly SettingsModel Settings;

    public GameCreatorModel(
        IOptions<SettingsModel> settings)
    {
        Settings = settings.Value;
    }

    public string ManagerName { get; set; } = "";
    public int LeagueId { get; set; }
    public int ClubId { get; set; }
    public IEnumerable<ClubModel> Clubs => Settings.Clubs;
    public IEnumerable<CompetitionModel> Competitions => Settings.Competitions;
}
