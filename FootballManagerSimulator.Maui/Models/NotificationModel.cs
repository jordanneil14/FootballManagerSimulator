namespace FootballManagerSimulator.Maui.Models;

public class NotificationModel
{
    public DateOnly Date { get; set; }
	string DateFriendly => Date.ToString("dddd, dd MMMM yyyy");
	public string Recipient { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Message { get; set; } = "";
    public bool RequiresAction { get; set; }
    public int? BidPlayerId { get; set; }
    public int? BidAmount { get; set; }
    public int? BiddingClubId { get; set; }
    public int? JobOfferClubId { get; set; }
    public bool IsBoardWarning { get; set; }
    public override string ToString()
    {
        return $"From: {Recipient}\nDate: {DateFriendly}\nSubject: {Subject}\nMessage: {Message}";
    }
}
