using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Factories;

public class NotificationFactory(IState state) : INotificationFactory
{
    private readonly IState State = state;

    public void AddNotification(DateOnly date, string recipient, string subject, string message)
    {
        var notification = new NotificationModel
        {
            Date = date,
            Recipient = recipient,
            Subject = subject,
            Message = message
        };
        State.Notifications.Add(notification);
    }

    public void AddNotificationNow(string recipient, string subject, string message)
    {
        AddNotification(State.Date, recipient, subject, message);
    }
}
