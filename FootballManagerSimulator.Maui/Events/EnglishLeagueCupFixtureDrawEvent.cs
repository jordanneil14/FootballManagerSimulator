using FootballManagerSimulator.Maui.Competitions.Providers;
using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Events;

public class EnglishLeagueCupFixtureDrawEvent(
    IState state,
    INotificationFactory notificationFactory,
    DateOnly triggerDate,
    bool isCompleted,
    EnglishLeagueCupService englishLeagueCupService,
    EnglishLeagueCupProvider englishLeagueCupProvider) : Event(triggerDate, isCompleted)
{
    private readonly IState State = state;
    private readonly EnglishLeagueCupService EnglishLeagueCupService = englishLeagueCupService;
    private readonly EnglishLeagueCupProvider EnglishLeagueCupProvider = englishLeagueCupProvider;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public override void Execute()
    {
        var competition = State.Competitions.First(p => p.Type == CompetitionType.Cup);
		var cup = (CupModel)competition;
        var round = cup.Round;

		EnglishLeagueCupProvider.PopulateIntroducedClubs(cup);
		EnglishLeagueCupService.GenerateNextRoundOfFixtures(competition);

        cup.Round += 1;

        var clubIds = competition.Clubs.Select(p => p.Id);
        if (clubIds.Any())
        {
            var fixtures = competition.Fixtures.Where(p => p.Round == round);

            var message = $"Fixtures have been drawn for the next round of the {competition.Name}";
            var fixtureString = fixtures.Select(p => $"{p.HomeClub.Name} v {p.AwayClub.Name}");
            message += "\n" + string.Join("\n", fixtureString);

            NotificationFactory.AddNotification(
                State.Date,
                "Assistant",
                $"{competition.Name} Round {round}",
                message);
        }
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        return (true, "");
    }
}
