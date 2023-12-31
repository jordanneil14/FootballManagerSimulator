﻿namespace FootballManagerSimulator.Structures;

public class Notification
{
    public DateOnly Date { get; set; }
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public override string ToString()
    {
        return $"From: {Recipient}\nSubject: {Subject}\nMessage: {Message}";
    }
}
