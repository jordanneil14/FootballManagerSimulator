namespace FootballManagerSimulator.Maui.Models;

public class TransferListItemModel
{
    public int PlayerId { get; set; }
    public int AskingPrice { get; set; }
    public string AskingPriceFriendly { get => $"£{AskingPrice:n0}"; }
    public int ClubId { get; set; }
}
