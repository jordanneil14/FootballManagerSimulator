namespace FootballManagerSimulator.Models;

public class Notification
{
    public DateOnly Date { get; set; }
    public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public override string ToString()
    {
        return $"From: {Recipient}\nDate: {Date}\nSubject: {Subject}\nMessage: {Message}";
    }
}
