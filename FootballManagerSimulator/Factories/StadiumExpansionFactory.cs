using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class StadiumExpansionFactory(
    IState state,
    INotificationFactory notificationFactory) : IEventFactory
{
    public EventType Type => EventType.StadiumExpansion;

    public dynamic Data { get; set; } = new JObject();
    public DateOnly CompletionDate { get; set; }

    public void CompleteEvent(IEvent @event)
    {
        var stadiumSizeIncrease = (int)(state.Clubs.First(p => p.Id == state.MyClubId).StadiumSize * 0.2);
        state.Clubs.First(p => p.Id == state.MyClubId).StadiumSize += stadiumSizeIncrease;

        var myClub = state.Clubs.First(p => p.Id == state.MyClubId);

        notificationFactory.AddNotification(
            state.Date,
            "Chairman",
            "Stadium Expansion",
            $"{myClub.Name} capactity has been increased by {stadiumSizeIncrease} to {myClub.StadiumSize}");
    }

    public void CreateEvent()
    {
        state.Events.Add(new StadiumExpansionEvent(state));
    }
}
