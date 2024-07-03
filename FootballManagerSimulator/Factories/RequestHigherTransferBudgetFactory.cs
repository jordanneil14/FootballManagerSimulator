using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class RequestHigherTransferBudgetFactory(
    IState state,
    INotificationFactory notificationFactory) : IEventFactory
{
    public EventType Type => EventType.RequestHigherTransferBudget;

    public dynamic Data { get; set; } = new JObject();
    public DateOnly CompletionDate { get; set; }

    public void CompleteEvent(IEvent @event)
    {
        var expirationDate = state.Date.AddMonths(-3);
        var existsRecentRequest = state.Events
            .Any(p => p.Type == EventType.RequestHigherTransferBudget && p.StartDate >= expirationDate);
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

    public void CreateEvent()
    {
        state.Events.Add(new RequestHigherTransferBudgetEvent(state) { });
    }
}
