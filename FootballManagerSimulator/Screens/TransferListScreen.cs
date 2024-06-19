using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class TransferListScreen(
    IState state,
    IPlayerHelper playerHelper,
    ITransferListHelper transferListHelper) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.TransferList;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                state.ScreenStack.Pop();
                break;
            default:
                var inputIsInt = int.TryParse(input, out int inputAsInt);
                if (!inputIsInt) return;
                var transferListItem = transferListHelper.GetTransferListItemByPlayerId(inputAsInt);
                if (transferListItem == null)
                {
                    state.UserFeedbackUpdates.Add("This player is not on the transfer list");
                    return;
                }
                var isFundsAvailable = transferListItem.AskingPrice <= state.Clubs.First(p => p.Id == state.MyClubId).TransferBudget;
                if (!isFundsAvailable)
                {
                    state.UserFeedbackUpdates.Add("Insufficent funds to purchase this player");
                    return;
                }
                transferListHelper.TransferContractedPlayerByPlayerIdAndClubId(transferListItem.PlayerId, state.Clubs.First(p => p.Id == state.MyClubId).Id);
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

        Console.WriteLine($"Funds Available: {state.Clubs.First(p => p.Id == state.MyClubId).TransferBudgetFriendly}\n");

        Console.WriteLine($"{"Id",-10}{"Name",-30}{"Club",-20}{"Position",-10}{"Rating",-10}{"Asking Price",-20}{"Value",-20}");

        foreach(var transferListItem in state.TransferListItems.OrderBy(p => p.PlayerId))
        {
            var player = playerHelper.GetPlayerById(transferListItem.PlayerId);
            if (player.Contract.ClubId == state.Clubs.First(p => p.Id == state.MyClubId).Id) continue;

            var playerValue = playerHelper.GetTransferValue(player);

            var askingPriceFriendly = $"£{transferListItem.AskingPrice:n}";
            var playerValueFriendly = $"£{playerValue:n}";

            Console.WriteLine($"{transferListItem.PlayerId,-10}{player.Name,-30}{player.Contract!.ClubName,-20}{player.PreferredPosition,-10}{player.Rating,-10}{askingPriceFriendly,-20}{playerValueFriendly, -20}");
        }
    }
}
