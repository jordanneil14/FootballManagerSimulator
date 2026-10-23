using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Events;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Maui.Competitions.Providers;

public class EnglishCommunityShieldProvider(
    IOptions<SettingsModel> settings,
    EnglishCommunityShieldService englishCommunityShieldService,
    IState state,
    IEventManager eventManager,
    IServiceProvider serviceProvider) : ICompetitionProvider
{
    private readonly SettingsModel Settings = settings.Value;
    private readonly EnglishCommunityShieldService EnglishCommunityShieldService = englishCommunityShieldService;
    private readonly IState State = state;
    private readonly IEventManager EventManager = eventManager;
    private readonly IServiceProvider ServiceProvider = serviceProvider;

    public ICompetitionService CompetitionService => EnglishCommunityShieldService;

    public CompetitionType Type => CompetitionType.EnglishCommunityShield;

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var clubIds = competition.DrawSettings.First().IntroducedClubIds;
        var clubs = clubIds.Select(id => State.Clubs.First(p => p.Id == id)).ToList();

        var premierLeague = State.Competitions.First(p => p.Id == 1);
        var firstLeagueFixtureDate = premierLeague.Fixtures.Min(f => f.Date);
        var shieldDate = firstLeagueFixtureDate.AddDays(-7);
        var daysSinceSaturday = ((int)shieldDate.DayOfWeek - (int)DayOfWeek.Saturday + 7) % 7;
        shieldDate = shieldDate.AddDays(-daysSinceSaturday);

        var drawSettings = new DrawSettingsModel
        {
            Round = 1,
            DrawDate = State.Date.AddDays(-1),
            FixtureDate = shieldDate,
            IntroducedClubIds = clubIds
        };

        var cup = new CupModel
        {
            Id = competition.Id,
            Name = competition.Name,
            Type = CompetitionType.EnglishCommunityShield,
            Year = State.Date.Year,
            Clubs = clubs,
            DrawSettings = [ drawSettings ]
        };

        var @event = ActivatorUtilities.CreateInstance<EnglishCommunityShieldFixtureDrawEvent>(ServiceProvider, drawSettings.DrawDate, false);
        EventManager.ValidateAndAddEvent(@event);

        return cup;
    }
}
