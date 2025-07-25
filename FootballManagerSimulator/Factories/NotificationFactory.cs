﻿using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Factories;

public class NotificationFactory(
    IState state) : INotificationFactory
{
    public void AddNotification(DateOnly date, string recipient, string subject, string message)
    {
        var notification = new Notification
        {
            Date = date,
            Recipient = recipient,
            Subject = subject,
            Message = message
        };
        state.Notifications.Add(notification);
    }

    public void AddNotificationNow(string recipient, string subject, string message)
    {
        AddNotification(state.Date, recipient, subject, message);
    }
}
