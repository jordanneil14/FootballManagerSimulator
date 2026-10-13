using FootballManagerSimulator.Competitions.Services;
using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Competitions.Providers;

public class FriendlyProvider(
    IOptions<SettingsModel> settings,
    FriendlyService friendlyService) : ICompetitionProvider
{
    private readonly SettingsModel Settings = settings.Value;
    private readonly FriendlyService FriendlyService = friendlyService;
    public ICompetitionService CompetitionService => FriendlyService;

    public CompetitionType Type => CompetitionType.Friendly;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var friendy = new FriendlyModel
        {
            Id = competition.Id,
            Name = competition.Name,
            Clubs = Settings.Clubs.Select(p => new ClubModel
            {
                Id = p.Id,
                Name = p.Name,
                LeagueId = p.LeagueId
            }).ToList(),
            DrawSettings = competition.DrawSettings.Select(p => new DrawSettingsModel
            {
                Round = p.Round,
                FixtureDate = p.FixtureDate
            }).ToList()
        };

        return friendy;
    }
}
