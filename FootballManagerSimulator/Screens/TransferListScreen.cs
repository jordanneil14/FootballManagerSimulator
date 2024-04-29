using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Helpers;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class TransferListScreen : BaseScreen
{
    private readonly IState State;
    private readonly IPlayerHelper PlayerHelper;
    private readonly ITransferListHelper TransferListHelper;

    public TransferListScreen(
        IState state,
        IPlayerHelper playerHelper,
        ITransferListHelper transferListHelper) : base( state )
    {
        State = state;
        PlayerHelper = playerHelper;
        TransferListHelper = transferListHelper;
    }

    public override ScreenType Screen => ScreenType.TransferList;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                var inputIsInt = int.TryParse(input, out int inputAsInt);
                if (!inputIsInt) return;
                var transferListItem = TransferListHelper.GetTransferListItemByPlayerId(inputAsInt);
                if (transferListItem == null)
                {
                    State.UserFeedbackUpdates.Add("This player is not on the transfer list");
                    return;
                }
                var isFundsAvailable = transferListItem.AskingPrice <= State.MyClub.TransferBudget;
                if (!isFundsAvailable)
                {
                    State.UserFeedbackUpdates.Add("Insufficent funds to purchase this player");
                    return;
                }
                TransferListHelper.TransferContractedPlayerByPlayerIdAndClubId(transferListItem.PlayerId, State.MyClub.Id);
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter PlayerId>) Buy Player for asking price");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Transfer List\n");

        Console.WriteLine($"Funds Available: {State.MyClub.TransferBudgetFriendly}\n");

        Console.WriteLine($"{"Id",-10}{"Name",-30}{"Club",-20}{"Position",-10}{"Rating",-10}{"Asking Price",-20}{"Value",-20}");

        foreach(var transferListItem in State.TransferListItems.OrderBy(p => p.PlayerId))
        {
            var player = PlayerHelper.GetPlayerById(transferListItem.PlayerId);
            if (player.Contract.ClubId == State.MyClub.Id) continue;

            var playerValue = PlayerHelper.GetTransferValue(player);

            var askingPriceFriendly = $"£{transferListItem.AskingPrice:n}";
            var playerValueFriendly = $"£{playerValue:n}";

            Console.WriteLine($"{transferListItem.PlayerId,-10}{player.Name,-30}{player.Contract!.ClubName,-20}{player.PreferredPosition,-10}{player.Rating,-10}{askingPriceFriendly,-20}{playerValueFriendly, -20}");
        }
    }
}
