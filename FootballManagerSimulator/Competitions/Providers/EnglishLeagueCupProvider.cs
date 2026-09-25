using FootballManagerSimulator.Competitions.Services;
using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Competitions.Providers;

public class EnglishLeagueCupProvider(
    IOptions<SettingsModel> settings,
    IState state,
    CupFixtureDrawFactory cupFixtureDrawFactory,
    EnglishLeagueCupService englishLeagueCupService) : ICompetitionProvider
{
    private readonly SettingsModel Settings = settings.Value;
    private readonly EnglishLeagueCupService EnglishLeagueCupService = englishLeagueCupService;
    public ICompetitionService CompetitionService => EnglishLeagueCupService;
    private readonly IState State = state;
    private readonly CupFixtureDrawFactory CupFixtureDrawFactory = cupFixtureDrawFactory;

    private const int NUMBER_OF_ROUNDS = 7;
    private readonly IEnumerable<string> LeaguesInvolved = [ "Premier League", "EFL Championship", "EFL League One", "EFL League Two" ];

    public CompetitionType Type => CompetitionType.Cup;

    private DateOnly GetFixtureDateForRound(int round)
    {
		var date = new DateOnly(State.Date.Year, 8, 1);
		var daysUntilTuesday = ((int)DayOfWeek.Tuesday - (int)date.DayOfWeek + 7) % 7;
		var roundOneFixtureDate = date.AddDays(daysUntilTuesday);

        if (round == 1)
            return roundOneFixtureDate;

        if (round < 7)
            return roundOneFixtureDate.AddDays((round - 1) * 14);

        return roundOneFixtureDate.AddDays(((round - 1) * 14) + 4);
	}

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var leagues = Settings.Competitions.Where(p => LeaguesInvolved.Contains(p.Name));
        var leagueIds = leagues.Select(p => p.Id);

        var clubs = Settings.Clubs
            .Where(p => leagueIds.Contains(p.LeagueId))
            .ToList();

        var cup = new CupModel
        {
            Id = competition.Id,
            Name = competition.Name,
            Clubs = clubs,
            DrawSettings = []
        };

        for(var i = 1; i <= NUMBER_OF_ROUNDS; i++)
        {
            var roundSettings = competition.DrawSettings.FirstOrDefault(p => p.Round == i);
            var drawSettings = new DrawSettingsModel
            {
                Round = i,
                IntroducedClubIds = roundSettings == null ? [] : roundSettings.IntroducedClubIds,
                FixtureDate = GetFixtureDateForRound(i)
            };

            if (i == 1)
            {
				drawSettings.DrawDate = new DateOnly(State.Date.Year, 7, 13);
			}
			else
            {
                drawSettings.DrawDate = cup.DrawSettings.Last().FixtureDate.AddDays(1);
            }

			cup.DrawSettings.Add(drawSettings);

			var cupDrawFixtureEvent = CupFixtureDrawFactory;
            cupDrawFixtureEvent.Data.DrawDate = new DateTime(drawSettings.DrawDate.Year, drawSettings.DrawDate.Month, drawSettings.DrawDate.Day);
			cupDrawFixtureEvent.Data.FixtureDate = new DateTime(drawSettings.FixtureDate.Year, drawSettings.FixtureDate.Month, drawSettings.FixtureDate.Day);
			cupDrawFixtureEvent.Data.Round = drawSettings.Round;
			cupDrawFixtureEvent.Data.CompetitionId = competition.Id;
			cupDrawFixtureEvent.CreateEvent();
		}

        return cup;
    }
}
