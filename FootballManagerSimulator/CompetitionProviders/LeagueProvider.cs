using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Services;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.CompetitionProviders;

public class LeagueProvider(
    IOptions<Settings> settings,
    LeagueService leagueService) : ICompetitionProvider
{
    private readonly Settings Settings = settings.Value;
    private readonly LeagueService LeagueService = leagueService;

    public ICompetitionService CompetitionService => LeagueService;
    public CompetitionType Type => CompetitionType.League;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var clubs = Settings.Clubs
            .Where(p => p.LeagueId == competition.Id)
            .Select(p => new Club
            {
                Id = p.Id,
                Name = p.Name
            });

        if (clubs == null || !clubs.Any())
            throw new Exception($"Unable to get clubs by leagueResourceId {competition.Id}");

        var league = new League()
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
