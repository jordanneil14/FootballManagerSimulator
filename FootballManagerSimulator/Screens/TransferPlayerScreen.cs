using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class TransferPlayerScreen : BaseScreen
{
    private readonly IState State;
    private readonly ITransferListHelper TransferListHelper;
    private readonly IPlayerHelper PlayerHelper;

    public TransferPlayerScreen(
        IState state,
        ITransferListHelper transferListHelper,
        IPlayerHelper playerHelper) : base(state)
    {
        State = state;
        TransferListHelper = transferListHelper;
        PlayerHelper = playerHelper;
    }

    public override ScreenType Screen => ScreenType.TransferPlayer;

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

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");

        var screenParameters = State.ScreenStack.Peek().Parameters as TransferPlayerScreenObj;
        var player = screenParameters.Player;

        if (TransferListHelper.IsPlayerTransferListed(player.Id))
        {
            Console.WriteLine("C) Remove From Transfer List");
        }
        else
        {
            Console.WriteLine("<Enter Amount>) Add To Transfer List");
        }
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
