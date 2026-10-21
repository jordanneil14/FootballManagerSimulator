using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Support;

/// <summary>
/// Fake <see cref="INotificationFactory"/> that records notifications instead of writing to state.
/// (Deliberately not named "*Factory" so GameServices' convention scan never registers it.)
/// </summary>
public sealed class RecordingNotifications : INotificationFactory
{
    public List<NotificationModel> Sent { get; } = [];
    public DateOnly Now { get; set; }

    public void AddNotification(DateOnly date, string recipient, string subject, string message) =>
        Sent.Add(new NotificationModel { Date = date, Recipient = recipient, Subject = subject, Message = message });

    public void AddNotificationNow(string recipient, string subject, string message) =>
        AddNotification(Now, recipient, subject, message);
}
