using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Factories;

public class NotificationFactory : INotificationFactory
{
    private readonly IState State;

    public NotificationFactory(
        IState state)
    {
        State = state;
    }

    public void AddNotification(DateOnly date, string recipient, string subject, string message)
    {
        var notification = new Notification
        {
            Date = date,
            Recipient = recipient,
            Subject = subject,
            Message = message
        };
        State.Notifications.Add(notification);
    }
}
