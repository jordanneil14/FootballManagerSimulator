using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Events;
using FootballManagerSimulator.Interfaces;
using Newtonsoft.Json.Linq;

namespace FootballManagerSimulator.Factories;

public class RequestHigherTransferBudgetFactory(
    IState state,
    INotificationFactory notificationFactory) : IEventFactory
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    private const int INCREASED_AMOUNT = 10000000;

    public EventType Type => EventType.RequestHigherTransferBudget;
    public dynamic Data { get; set; } = new JObject();
    public DateOnly CompletionDate { get; set; }

    public void CompleteEvent(IEvent @event)
    {
        var expirationDate = State.Date.AddMonths(-3);
        var existsRecentRequest = State.Events
            .Any(p => p.Type == EventType.RequestHigherTransferBudget && p.StartDate >= expirationDate);
        if (existsRecentRequest)
        {
            NotificationFactory.AddNotification(
                State.Date,
                "Chairman",
                "Transfer Budget Request",
                $"The chairman has rejected your transfer budget request.");
            return;
        }

        State.Clubs.First(p => p.Id == State.MyClubId).TransferBudget += INCREASED_AMOUNT;

        NotificationFactory.AddNotification(
            State.Date,
            "Chairman",
            "Transfer Budget Request",
            $"The chairman has responded to your transfer budget request and has granted you an extra £{INCREASED_AMOUNT:n} to spend.");
    }

    public void CreateEvent()
    {
        State.Events.Add(new RequestHigherTransferBudgetEvent(State) { });
    }
}
