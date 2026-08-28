namespace FootballManagerSimulator.Models;

public class Notification
{
    public DateOnly Date { get; set; }
	string DateFriendly => Date.ToString("dddd, dd MMMM yyyy");
	public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public override string ToString()
    {
        return $"From: {Recipient}\nDate: {DateFriendly}\nSubject: {Subject}\nMessage: {Message}";
    }
}
