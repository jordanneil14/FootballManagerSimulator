using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Providers;

public class EnglishNationalLeagueProvider(
	IState state,
	EnglishNationalLeagueService nationalLeagueService) : ICompetitionProvider
{
	private readonly IState State = state;
	private readonly EnglishNationalLeagueService EnglishNationalLeagueService = nationalLeagueService;

	public ICompetitionService CompetitionService => EnglishNationalLeagueService;
	public CompetitionType Type => CompetitionType.EnglishNationalLeague;

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
			Type = CompetitionType.EnglishNationalLeague,
			Rank = competition.Rank,
			Clubs = clubs.ToList(),
			CountryId = competition.CountryId,
			Year = State.Date.Year,
			AutomaticPromotionPlaces = competition.LeagueTable.AutomaticPromotionPlaces,
			PlayoffPlaces = competition.LeagueTable.PlayoffPlaces,
			RelegationPlaces = competition.LeagueTable.RelegationPlaces,
			PlayoffFormat = competition.LeagueTable.PlayoffFormat
		};

		CompetitionService.GenerateNextRoundOfFixtures(league);

		return league;
	}
}
