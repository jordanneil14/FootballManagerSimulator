using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FootballManagerSimulator.Events;

public class RequestStadiumExpansionEvent(
    IState state,
    INotificationFactory notificationFactory) : BaseEvent(state)
{
    public override EventType Event => EventType.RequestStadiumExpansion;

    private DateOnly completionDate;
    public override DateOnly CompletionDate => completionDate;

    public override void Complete()
    {
        var stadiumExpansionEvent = new StadiumExpansionEvent(state, notificationFactory);
        stadiumExpansionEvent.Start();
        state.Events.Add(stadiumExpansionEvent);

        notificationFactory.AddNotification(
            state.Date,
            "Chairman",
            "Stadium Expansion Request",
            $"Your stadium expansion request has been accepted by the owner. Work will begin immediately and finish on {stadiumExpansionEvent.CompletionDate}");
    }

    public override void Start()
    {
        state.UserFeedbackUpdates.Add("Stadium Expansion request has been submitted");
        completionDate = state.Date.AddDays(2);
        base.Initialise();
    }
}
