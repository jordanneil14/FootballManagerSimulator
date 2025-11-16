using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class TransferPlayerScreen(
    IState state,
    ITransferListHelper transferListHelper,
    IPlayerHelper playerHelper) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly ITransferListHelper TransferListHelper = transferListHelper;
    private readonly IPlayerHelper PlayerHelper = playerHelper;

    public override ScreenType Screen => ScreenType.TransferPlayer;

    public override IDictionary<string, string> Options => GetOptions();

    public override void HandleInput(string input)
    {
        var screenParameters = State.ScreenStack.Peek().Parameters as TransferPlayerScreenObj;
        var player = screenParameters.Player;

        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            case "C":
                TransferListHelper.RemovePlayerFromTransferList(player.Id);
                break;
            default:
                var inputIsInt = int.TryParse(input, out int inputAsInt);
                if (!inputIsInt) return;
                TransferListHelper.AddPlayerToTransferList(player.Id, inputAsInt);
                break;
        }
    }

    public Dictionary<string, string> GetOptions()
    {
        var dictionary = new Dictionary<string, string>();
        dictionary.Add("B", "Back");

        var screenParameters = State.ScreenStack.Peek().Parameters as TransferPlayerScreenObj;
        var player = screenParameters.Player;
        if (TransferListHelper.IsPlayerTransferListed(player.Id))
            dictionary.Add("C", "Remove From Transfer List");
        else
            dictionary.Add("<Enter Amount>", "Add To Transfer List");

        return dictionary;
    }

    public override void RenderSubscreen()
    {
        var screenParameters = State.ScreenStack.Peek().Parameters as TransferPlayerScreenObj;
        var player = screenParameters.Player;

        Console.WriteLine($"{player.Name}\n");

        var transferListItem = TransferListHelper.GetTransferListItemByPlayerId(player.Id);
        if (transferListItem == null)
        {
            Console.WriteLine($"Transfer Status: Not Set");
        }
        else
        {
            var askingPriceFriendly = $"£{transferListItem.AskingPrice:n}";
            Console.WriteLine($"Transfer Status: Transfer Listed For {askingPriceFriendly}");
        }

        var transferValue = PlayerHelper.GetTransferValue(player);
        var transferValueFriendly = $"£{transferValue:n}";
        Console.WriteLine($"Transfer Value: {transferValueFriendly}");
    }

    public static Screen CreateScreen(Player player)
    {
        return new Screen
        {
            Type = ScreenType.TransferPlayer,
            Parameters = new TransferPlayerScreenObj
            {
                Player = player,
            }
        };
    }

    public class TransferPlayerScreenObj
    {
        public Player Player { get; set; } = new Player();
    }
}
