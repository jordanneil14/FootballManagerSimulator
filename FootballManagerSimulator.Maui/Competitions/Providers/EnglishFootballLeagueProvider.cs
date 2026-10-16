using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Providers;

public class EnglishFootballLeagueProvider(
	IState state,
	EnglishFootballLeagueService englishFootballLeagueService) : ICompetitionProvider
{
	private readonly IState State = state;
	private readonly EnglishFootballLeagueService EnglishFootballLeagueService = englishFootballLeagueService;

	public ICompetitionService CompetitionService => EnglishFootballLeagueService;
	public CompetitionType Type => CompetitionType.EnglishFootballLeague;

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
			Type = CompetitionType.EnglishFootballLeague,
			Rank = competition.Rank,
			Clubs = clubs.ToList(),
			CountryId = competition.CountryId,
			Year = State.Date.Year,
			AutomaticPromotionPlaces = competition.LeagueTable.AutomaticPromotionPlaces,
			PlayoffPlaces = competition.LeagueTable.PlayoffPlaces,
			RelegationPlaces = competition.LeagueTable.RelegationPlaces
		};

		CompetitionService.GenerateNextRoundOfFixtures(league);

		return league;
	}
}
