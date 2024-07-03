namespace FootballManagerSimulator.Models;

public class TransferListItem
{
    public int PlayerId { get; set; }
    public int AskingPrice { get; set; }
    public string AskingPriceFriendly { get => $"£{AskingPrice:n}"; }
    public int ClubId { get; set; }
}
