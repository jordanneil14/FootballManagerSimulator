using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Services;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.CompetitionProviders;

public class PremierLeagueProvider(
    IOptions<Settings> settings,
    PremierLeagueService premierLeagueService) : ICompetitionProvider
{
	private readonly Settings Settings = settings.Value;
    private readonly PremierLeagueService PremierLeagueService = premierLeagueService;
    public ICompetitionService CompetitionService => PremierLeagueService;

    public CompetitionType Type => CompetitionType.PremierLeague;

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