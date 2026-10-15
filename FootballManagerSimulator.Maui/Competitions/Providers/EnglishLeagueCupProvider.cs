using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Maui.Competitions.Providers;

public class EnglishLeagueCupProvider(
    IOptions<SettingsModel> settings,
    IState state,
    EnglishLeagueCupService englishLeagueCupService,
    IServiceProvider serviceProvider,
    IEventManager eventManager) : ICompetitionProvider
{
    private readonly SettingsModel Settings = settings.Value;
    private readonly EnglishLeagueCupService EnglishLeagueCupService = englishLeagueCupService;
    public ICompetitionService CompetitionService => EnglishLeagueCupService;
    private readonly IState State = state;
    private readonly IServiceProvider ServiceProvider = serviceProvider;
    private readonly IEventManager EventManager = eventManager;

    private const int NUMBER_OF_ROUNDS = 7;

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
        var leagueIds = Settings.Competitions
            .Where(p => p.IsLeague && p.CountryId == 1)
            .Select(p => p.Id);

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

            var @event = ActivatorUtilities.CreateInstance<EnglishLeagueCupFixtureDrawEvent>(ServiceProvider, drawSettings.DrawDate, false);
            EventManager.ValidateAndAddEvent(@event);
        }

        return cup;
    }
}
