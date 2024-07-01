using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Events;

public class RequestHigherTransferBudgetEvent(
    IState state, 
    INotificationFactory notificationFactory) : BaseEvent(state)
{
    private DateOnly completionDate;
    public override DateOnly CompletionDate => completionDate;
    public override EventType Event => EventType.RequestHigherTransferBudget;

    public override void Complete()
    {
        var expirationDate = state.Date.AddMonths(-3);
        var existsRecentRequest = state.Events
            .Any(p => p.Event == EventType.RequestHigherTransferBudget && p.StartDate >= expirationDate);
        if (existsRecentRequest)
        {
            notificationFactory.AddNotification(
                state.Date,
                "Chairman",
                "Transfer Budget Request",
                $"The chairman has rejected your transfer budget request.");
            return;
        }

        state.Clubs.First(p => p.Id == state.MyClubId).TransferBudget += 10000000;

        notificationFactory.AddNotification(
            state.Date,
            "Chairman",
            "Transfer Budget Request",
            $"The chairman has responded to your transfer budget request and has granted you an extra £{10000000:n} to spend.");
    }

    public override void Start()
    {
        state.UserFeedbackUpdates.Add("Transfer budget request has been submitted");
        completionDate = state.Date.AddDays(2);
        base.Initialise();
    }
}
