using FootballManagerSimulator.Competitions.Services;
using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Competitions.Providers;

public class LeagueProvider(
    IOptions<SettingsModel> settings,
    LeagueService leagueService) : ICompetitionProvider
{
    private readonly SettingsModel Settings = settings.Value;
    private readonly LeagueService LeagueService = leagueService;

    public ICompetitionService CompetitionService => LeagueService;
    public CompetitionType Type => CompetitionType.League;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var clubs = Settings.Clubs
            .Where(p => p.LeagueId == competition.Id)
            .Select(p => new ClubModel
            {
                Id = p.Id,
                Name = p.Name
            });

        if (clubs == null || !clubs.Any())
            throw new Exception($"Unable to get clubs by leagueResourceId {competition.Id}");

        var league = new LeagueModel()
        {
            Id = competition.Id,
            Name = competition.Name,
            Rank = competition.Rank,
            Clubs = clubs.ToList()
        };

        CompetitionService.GenerateNextRoundOfFixtures(league);

        return league;
    }

    
}
