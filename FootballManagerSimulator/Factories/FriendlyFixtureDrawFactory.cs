using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Services;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class FriendlyFixtureDrawFactory(
    IState state,
    FriendlyService friendlyService,
    INotificationFactory notificationFactory) : IEventFactory
{
    private readonly IState State = state;
    private readonly FriendlyService FriendlyService = friendlyService;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public EventType Type => EventType.FriendlyDrawFixture;

    public dynamic Data { get; set; } = new JObject();

    public void CompleteEvent(IEvent @event)
    {
        var cupFixtureDrawEvent = @event as FriendlyFixtureDrawEvent;

        var competition = State.Competitions.First(p => p.Type == CompetitionType.Friendly);

        FriendlyService.GenerateNextRoundOfFixtures(competition);

        var fixture = competition.Fixtures.First(p => (p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId) && p.Round == cupFixtureDrawEvent.Round);
        var oppositionClubName = fixture.HomeClub.Id == State.MyClubId ? fixture.AwayClub.Name : fixture.HomeClub.Name;
        var date = fixture.Date;

        var message = $"A friendly has been arranged against {oppositionClubName} on {date}";

        NotificationFactory.AddNotificationNow(
            "Chairman",
            "Friendly Arranged",
            message);
    }

    public void CreateEvent()
    {
        DateTime fd = Data.FixtureDate;
        DateOnly fixtureDate = DateOnly.FromDateTime(fd);
        State.Events.Add(new FriendlyFixtureDrawEvent(State.Date, fixtureDate, State.Date, (int)Data.Round));
    }
}
