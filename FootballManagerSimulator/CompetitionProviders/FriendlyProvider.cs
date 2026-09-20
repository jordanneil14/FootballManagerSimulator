using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Services;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.CompetitionProviders;

public class FriendlyProvider(
    IOptions<Settings> settings,
    FriendlyService friendlyService) : ICompetitionProvider
{
    private readonly Settings Settings = settings.Value;
    private readonly FriendlyService FriendlyService = friendlyService;
    public ICompetitionService CompetitionService => FriendlyService;

    public CompetitionType Type => CompetitionType.Friendly;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var friendy = new Friendly
        {
            Id = competition.Id,
            Name = competition.Name,
            Clubs = Settings.Clubs.Select(p => new Club
            {
                Id = p.Id,
                Name = p.Name,
                LeagueId = p.LeagueId
            }).ToList(),
            DrawDates = competition.DrawDates.Select(p => new DrawDateModel
            {
                Round = p.Round,
                FixtureDate = p.FixtureDate
            }).ToList()
        };

        return friendy;
    }
}
