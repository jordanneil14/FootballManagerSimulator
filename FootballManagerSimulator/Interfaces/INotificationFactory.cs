namespace FootballManagerSimulator.Interfaces;

public interface INotificationFactory
{
    void AddNotification(DateOnly date, string recipient, string subject, string message);
}