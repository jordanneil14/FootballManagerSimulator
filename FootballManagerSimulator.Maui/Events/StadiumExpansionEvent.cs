using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Events;

public class StadiumExpansionEvent(
    IState state,
    INotificationFactory notificationFactory,
    DateOnly triggerDate,
    bool isCompleted) : Event(triggerDate, isCompleted)
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public override void Execute()
    {
        var myClub = State.Clubs.First(p => p.Id == State.MyClubId);
        var seatsAdded = StadiumRules.SeatsAdded(myClub.StadiumSize);
        myClub.StadiumSize += seatsAdded;

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "Stadium Expansion",
            $"Work on {myClub.Stadium} is complete. Capacity has increased by {seatsAdded:n0} to {myClub.StadiumSize:n0}.");
    }

    public override (bool success, string errorMessage) ValidateAdd()
    {
        var inProgress = State.Events.OfType<StadiumExpansionEvent>().Any(p => !p.IsCompleted);
        return (!inProgress, "A stadium expansion is already underway");
    }
}
