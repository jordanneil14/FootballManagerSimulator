using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;
using static FootballManagerSimulator.Screens.TransferPlayerScreen;

namespace FootballManagerSimulator.Screens;

public class PlayerScreen : BaseScreen
{
    public override ScreenType Screen => ScreenType.Player;

    private readonly IState State;
    private readonly IPlayerHelper PlayerHelper;
    private readonly ITransferListHelper TransferListHelper;

    public PlayerScreen(
        IState state,
        IPlayerHelper playerHelper,
        ITransferListHelper transferListHelper) : base(state)
    {
        State = state;
        PlayerHelper = playerHelper;
        TransferListHelper = transferListHelper;
    }

    public static Screen CreateScreen(Player player)
    {
        return new Screen
        {
            Type = ScreenType.Player,
            Parameters = new PlayerScreenObj
            {
                Player = player,
            }
        };
    }

    public class PlayerScreenObj
    {
        public Player Player { get; set; } = new Player();
    }

    public override void HandleInput(string input)
    {
        switch(input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            case "C":
                HandleTransferOptionsInput();
                break;
            case "D":
                HandleSignFreeAgentInput();
                break;
            default:
                break;
        }
    }

    private void HandleSignFreeAgentInput()
    {
        var screenParameters = State.ScreenStack.Peek().Parameters as PlayerScreenObj;
        var player = PlayerHelper.GetPlayerById(screenParameters.Player.Id);

        TransferListHelper.SignFreeAgentByPlayerId(player.Id);

        State.UserFeedbackUpdates.Add($"{player.Name} has been signed");
    }

    private void HandleTransferOptionsInput()
    {
        var screenParameters = State.ScreenStack.Peek().Parameters as PlayerScreenObj;
        var player = screenParameters.Player;

        State.ScreenStack.Push(new Screen
        {
            Type = ScreenType.TransferPlayer,
            Parameters = new TransferPlayerScreenObj
            {
                Player = player,
            }
        });
    }

    public override void RenderSubscreen()
    {
        var screenParameters = State.ScreenStack.Peek().Parameters as PlayerScreenObj;
        var player = screenParameters.Player;

        Console.WriteLine($"{player.Name}\n\nGeneral Information");

        var transferValue = PlayerHelper.GetTransferValue(player);
        var transferValueFriendly = $"£{transferValue:n}";

        Console.WriteLine(
            $"Age:{player.Age}\n" +
            $"Birth Date:{player.BirthDate}\n" +
            $"Height:{player.Height}\n" +
            $"Weight:{player.Weight}\n" +
            $"Rating:{player.Rating}\n" +
            $"Position:{player.PreferredPosition}\n" +
            $"Nationality:{player.Nationality}\n" +
            $"Transfer Value:{transferValueFriendly}\n");

        if (player.PreferredPosition == "GK")
        {
            Console.WriteLine($"Goalkeeper Stats");

            Console.WriteLine(string.Format("{0,-25}{1,-25}{2,-25}{3,-25}", $"Positioning:{player.GKPositioning}", $"Diving:{player.GKDiving}", $"Kicking:{player.GKKicking}", $"Handling:{player.GKHandling}"));
            Console.WriteLine(string.Format("{0,-25}", $"Reflexes:{player.GKReflexes}"));
            return;
        }

        Console.WriteLine($"Playing Stats");
        Console.WriteLine(string.Format("{0,-25}{1,-25}{2,-25}{3,-25}", $"Aggression:{player.Aggression}", $"Agility:{player.Agility}", $"Ball Control:{player.BallControl}", $"Dribbling:{player.Dribbling}"));
        Console.WriteLine(string.Format("{0,-25}{1,-25}{2,-25}{3,-25}", $"Marking:{player.Marking}", $"Sliding Tackle:{player.SlidingTackle}", $"Crossing:{player.Crossing}", $"Short Passing:{player.ShortPass}"));
        Console.WriteLine(string.Format("{0,-25}{1,-25}{2,-25}{3,-25}", $"Long Passing:{player.LongPass}", $"Work Rate:{player.WorkRate}", $"Acceleration:{player.Acceleration}", $"Speed:{player.Speed}"));
        Console.WriteLine(string.Format("{0,-25}{1,-25}{2,-25}{3,-25}", $"Stamina:{player.Stamina}", $"Strength:{player.Strength}", $"Balance:{player.Balance}", $"Skill Moves:{player.SkillMoves}"));
        Console.WriteLine(string.Format("{0,-25}{1,-25}{2,-25}{3,-25}", $"Jumping:{player.Jumping}", $"Heading:{player.Heading}", $"Shot Power:{player.ShotPower}", $"Finising:{player.Finishing}"));
        Console.WriteLine(string.Format("{0,-25}{1,-25}{2,-25}{3,-25}", $"Long Shots:{player.LongShots}", $"Curve:{player.Curve}", $"FK Accuracy:{player.FreekickAccuracy}", $"Volleys:{player.Volleys}"));
        Console.WriteLine(string.Format("{0,-25}", $"Penalties:{player.Penalties}"));
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");

        var screenParameters = State.ScreenStack.Peek().Parameters as PlayerScreenObj;
        var player = PlayerHelper.GetPlayerById(screenParameters.Player.Id);

        var playerPlaysForMyClub = PlayerHelper.PlayerPlaysForClub(player.Id, State.MyClub.Id);
        var playerIsFreeAgent = player.Contract == null;

        if (playerPlaysForMyClub)
        {
            Console.WriteLine("C) Transfer Options");
        }
        else if (playerIsFreeAgent)
        {
            Console.WriteLine("D) Sign Free Agent");
        }
    }
}
