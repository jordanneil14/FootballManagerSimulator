using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Providers;

public class EnglishPremierLeagueProvider(
    IState state,
    EnglishPremierLeagueService premierLeagueService) : ICompetitionProvider
{
    private readonly IState State = state;
    private readonly EnglishPremierLeagueService EnglishPremierLeagueService = premierLeagueService;
    public ICompetitionService CompetitionService => EnglishPremierLeagueService;

    public CompetitionType Type => CompetitionType.EnglishPremierLeague;

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
			Type = CompetitionType.EnglishPremierLeague,
			Rank = competition.Rank,
			Clubs = clubs.ToList()
		};

		CompetitionService.GenerateNextRoundOfFixtures(league);

		return league;
	}
}