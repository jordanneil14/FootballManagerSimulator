using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class StadiumExpansionFactory(
    IState state,
    INotificationFactory notificationFactory) : IEventFactory
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public EventType Type => EventType.StadiumExpansion;

    public dynamic Data { get; set; } = new JObject();
    public DateOnly CompletionDate { get; set; }

    public void CompleteEvent(IEvent @event)
    {
        var stadiumSizeIncrease = (int)(State.Clubs.First(p => p.Id == State.MyClubId).StadiumSize * 0.2);
        State.Clubs.First(p => p.Id == State.MyClubId).StadiumSize += stadiumSizeIncrease;

        var myClub = State.Clubs.First(p => p.Id == State.MyClubId);

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "Stadium Expansion",
            $"{myClub.Name} capactity has been increased by {stadiumSizeIncrease} to {myClub.StadiumSize}");
    }

    public void CreateEvent()
    {
        State.Events.Add(new StadiumExpansionEvent(State));
    }
}
