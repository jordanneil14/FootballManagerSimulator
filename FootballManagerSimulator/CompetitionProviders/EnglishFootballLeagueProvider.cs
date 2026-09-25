using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Services;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.CompetitionProviders;

public class EnglishFootballLeagueProvider(
	IOptions<SettingsModel> settings,
	EnglishFootballLeagueService englishFootballLeagueService) : ICompetitionProvider
{
	private readonly SettingsModel Settings = settings.Value;
	private readonly EnglishFootballLeagueService EnglishFootballLeagueService = englishFootballLeagueService;

	public ICompetitionService CompetitionService => EnglishFootballLeagueService;
	public CompetitionType Type => CompetitionType.EnglishFootballLeague;

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
