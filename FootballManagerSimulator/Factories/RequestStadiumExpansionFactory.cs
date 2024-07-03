using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class RequestStadiumExpansionFactory(
    IState state,
    INotificationFactory notificationFactory) : IEventFactory
{
    public EventType Type => EventType.RequestStadiumExpansion;

    public dynamic Data { get; set; } = new JObject();

    public void CompleteEvent(IEvent @event)
    {
        var ev = new StadiumExpansionEvent(state);

        state.Events.Add(ev);

        notificationFactory.AddNotification(
            state.Date,
            "Chairman",
            "Stadium Expansion Request",
            $"Your stadium expansion request has been accepted by the owner. Work will begin immediately and finish on {ev.CompletionDate}");
    }

    public void CreateEvent()
    {
        state.Events.Add(new RequestStadiumExpansionEvent(state));
    }
}
