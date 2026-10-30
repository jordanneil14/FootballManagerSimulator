using FootballManagerSimulator.Maui.Competitions.Providers;
using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Events;

public class EnglishFACupFixtureDrawEvent(
    IState state,
    INotificationFactory notificationFactory,
    DateOnly triggerDate,
    bool isCompleted,
    EnglishFACupService englishFACupService,
    EnglishFACupProvider englishFACupProvider) : Event(triggerDate, isCompleted)
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;
    private readonly EnglishFACupService EnglishFACupService = englishFACupService;
    private readonly EnglishFACupProvider EnglishFACupProvider = englishFACupProvider;

    public override void Execute()
    {
        if (State.Competitions.FirstOrDefault(p => p.Type == CompetitionType.EnglishFACup) is not CupModel cup)
            return;

        var round = cup.Round;
        EnglishFACupProvider.PopulateIntroducedClubs(cup);
        EnglishFACupService.GenerateNextRoundOfFixtures(cup);
        cup.Round += 1;

        // My tie, if I have one, leads the list.
        var ties = cup.Fixtures
            .Where(p => p.Round == round)
            .OrderBy(p => p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId ? 0 : 1)
            .Select(p => $"{p.HomeClub.Name} v {p.AwayClub.Name}");

        var drawSettings = cup.DrawSettings.First(p => p.Round == round);
        var stage = CupPrizeMoneyRules.RoundName(round, FACupRules.Rounds);
        NotificationFactory.AddNotification(
            State.Date,
            "Assistant",
            $"{cup.Name} Round {round}",
            $"The draw has been made for {stage} of the {cup.Name}, to be played on {drawSettings.FixtureDate:d MMMM yyyy}:\n{string.Join("\n", ties)}");
    }

    public override (bool success, string errorMessage) ValidateAdd() => (true, "");
}
