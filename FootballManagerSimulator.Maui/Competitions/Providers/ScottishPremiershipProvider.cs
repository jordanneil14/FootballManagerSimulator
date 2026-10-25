using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;

namespace FootballManagerSimulator.Maui.Competitions.Providers;

public class ScottishPremiershipProvider(
	IState state,
	ScottishPremiershipService scottishPremiershipService,
	IServiceProvider serviceProvider,
	IEventManager eventManager) : ICompetitionProvider
{
	private readonly IState State = state;
	private readonly ScottishPremiershipService ScottishPremiershipService = scottishPremiershipService;
	private readonly IServiceProvider ServiceProvider = serviceProvider;
	private readonly IEventManager EventManager = eventManager;

	public ICompetitionService CompetitionService => ScottishPremiershipService;
	public CompetitionType Type => CompetitionType.ScottishPremiership;

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
			Type = CompetitionType.ScottishPremiership,
			Rank = competition.Rank,
			Clubs = clubs.ToList(),
			CountryId = competition.CountryId,
			Year = State.Date.Year,
			AutomaticPromotionPlaces = competition.LeagueTable.AutomaticPromotionPlaces,
			PlayoffPlaces = competition.LeagueTable.PlayoffPlaces,
			RelegationPlaces = competition.LeagueTable.RelegationPlaces
		};

		CompetitionService.GenerateNextRoundOfFixtures(league);

		var splitEvent = ActivatorUtilities.CreateInstance<ScottishPremiershipSplitEvent>(
			ServiceProvider, league.Fixtures.Max(p => p.Date).AddDays(1), false, league.Id);
		EventManager.ValidateAndAddEvent(splitEvent);

		return league;
	}
}
