using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class RequestStadiumExpansionFactory(
    IState state,
    INotificationFactory notificationFactory) : IEventFactory
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public EventType Type => EventType.RequestStadiumExpansion;

    public dynamic Data { get; set; } = new JObject();

    public void CompleteEvent(IEvent @event)
    {
        var stadiumExpansionEvent = new StadiumExpansionEvent(State);

        State.Events.Add(stadiumExpansionEvent);

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "Stadium Expansion Request",
            $"Your stadium expansion request has been accepted by the owner. Work will begin immediately and finish on {stadiumExpansionEvent.CompletionDate}");
    }

    public void CreateEvent()
    {
        State.Events.Add(new RequestStadiumExpansionEvent(State));
    }
}
