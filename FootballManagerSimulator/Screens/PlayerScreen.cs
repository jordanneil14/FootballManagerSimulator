using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using static FootballManagerSimulator.Screens.TransferPlayerScreen;

namespace FootballManagerSimulator.Screens;

public class PlayerScreen(
    IState state,
    IPlayerHelper playerHelper,
    ITransferListHelper transferListHelper,
    IClubHelper clubHelper) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly IPlayerHelper PlayerHelper = playerHelper;
    private readonly ITransferListHelper TransferListHelper = transferListHelper;
    private readonly IClubHelper ClubHelper = clubHelper;

    public override ScreenType Screen => ScreenType.Player;

    public override IDictionary<string, string> Options => GetOptions();

	public override string? OptionPrompt => null;

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
        switch (input)
        {
			case "UPARROW":
				if (base.OptionIndex > 0)
					base.OptionIndex -= 1;
				break;
			case "DOWNARROW":
				if (Options.Count > 1 && base.OptionIndex < Options.Count - 1)
					base.OptionIndex += 1;
				break;
			case "ESCAPE":
                State.ScreenStack.Pop();
				OptionIndex = 0;
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

        var club = player.Contract == null ? "Free Agent" : ClubHelper.GetClubById(player.Contract.ClubId).Name;

        Console.WriteLine(
            $"Age:{player.Age}\n" +
            $"Birth Date:{player.BirthDate}\n" +
            $"Height:{player.Height}\n" +
            $"Weight:{player.Weight}\n" +
            $"Rating:{player.Rating}\n" +
            $"Position:{player.PreferredPosition}\n" +
            $"Nationality:{player.Nationality}\n" +
            $"Club:{club}\n" +
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

    public Dictionary<string, string> GetOptions()
    {
        var screenParameters = State.ScreenStack.Peek().Parameters as PlayerScreenObj;
        var player = PlayerHelper.GetPlayerById(screenParameters.Player.Id);

        var doesPlayerPlaysForMyClub = PlayerHelper.DoesPlayerPlaysForClub(player.Id, State.Clubs.First(p => p.Id == State.MyClubId).Id);
        var playerIsFreeAgent = player.Contract == null;

        var dictionary = new Dictionary<string, string>();
        dictionary.Add("B", "Back");
        if (doesPlayerPlaysForMyClub) 
            dictionary.Add("C", "Transfer Options");
        else if (playerIsFreeAgent)
            dictionary.Add("D", "Sign Free Agent");

        return dictionary;
    }
}
