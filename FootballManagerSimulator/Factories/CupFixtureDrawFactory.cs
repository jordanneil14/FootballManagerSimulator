using FootballManagerSimulator.Competitions.Services;
using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class CupFixtureDrawFactory(
    IState State,
    EnglishLeagueCupService englishLeagueCupService,
    INotificationFactory notificationFactory) : IEventFactory
{
    private readonly IState state = State;
    private readonly EnglishLeagueCupService EnglishLeagueCupService = englishLeagueCupService;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public EventType Type => EventType.CupDrawFixture;

    public dynamic Data { get; set; } = new JObject();
    public DateOnly CompletionDate { get; set; }

    public void CompleteEvent(IEvent @event)
    {
        var cupFixtureDrawEvent = @event as CupFixtureDrawEvent;

        var competition = state.Competitions.First(p => p.Id == cupFixtureDrawEvent.CompetitionId);

        EnglishLeagueCupService.GenerateNextRoundOfFixtures(competition);

        var clubIds = competition.Clubs.Select(p => p.Id);
        if (clubIds.Any() && clubIds.Contains(state.MyClubId.GetValueOrDefault()))
        {
            var fixtures = competition.Fixtures.Where(p => p.Round == cupFixtureDrawEvent.Round);

            var message = $"Fixtures have been drawn for the next round of the {competition.Name}";
            var fixtureString = fixtures.Select(p => $"{p.HomeClub.Name} v {p.AwayClub.Name}");
            message += "\n" + string.Join("\n", fixtureString);

            NotificationFactory.AddNotification(
                state.Date,
                "Assistant",
                $"{competition.Name} Round {cupFixtureDrawEvent.Round}",
                message);
        }
    }

    public void CreateEvent()
    {
        DateTime dd = Data.DrawDate;
        DateTime fd = Data.FixtureDate;
        DateOnly drawDate = DateOnly.FromDateTime(dd);
        DateOnly fixtureDate = DateOnly.FromDateTime(fd);
        var cupFixtureDrawEvent = new CupFixtureDrawEvent(drawDate, fixtureDate, (int)Data.CompetitionId, state.Date, (int)Data.Round);
        state.Events.Add(cupFixtureDrawEvent);
    }
}
