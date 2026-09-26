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

    public void CompleteEvent(IEvent @event)
    {
        
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
