using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Providers;

public class LeagueProvider(
    IState state,
    LeagueService leagueService) : ICompetitionProvider
{
    private readonly IState State = state;
    private readonly LeagueService LeagueService = leagueService;

    public ICompetitionService CompetitionService => LeagueService;
    public CompetitionType Type => CompetitionType.League;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var clubs = State.Clubs
            .Where(p => p.LeagueId == competition.Id);

        if (clubs == null || !clubs.Any())
            throw new Exception($"Unable to get clubs by leagueResourceId {competition.Id}");

        var league = new LeagueModel()
        {
            Id = competition.Id,
            Name = competition.Name,
            Rank = competition.Rank,
            Clubs = clubs.ToList(),
            AutomaticPromotionPlaces = competition.LeagueTable.AutomaticPromotionPlaces,
            PlayoffPlaces = competition.LeagueTable.PlayoffPlaces
        };

        CompetitionService.GenerateNextRoundOfFixtures(league);

        return league;
    }

    
}
