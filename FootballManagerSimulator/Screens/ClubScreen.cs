using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class ClubScreen : BaseScreen
{
    public override ScreenType Screen => ScreenType.Club;

    private readonly IState State;
    private readonly IHelperFunction HelperFunction;

    public ClubScreen(
        IState state, 
        IHelperFunction helperFunction) : base(state)
    {
        State = state;
        HelperFunction = helperFunction;
    }

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.LeagueTable
                });
                break;
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                var player = HelperFunction.GetPlayerByName(input);
                if (player != null)
                {
                    State.ScreenStack.Push(PlayerScreen.CreateScreen(player));
                }
                break;
        }
    }

    public static Screen CreateScreen(Team team)
    {
        return new Screen
        {
            Type = ScreenType.Club,
            Parameters = new PlayerScreenObj
            {
                Team = team
            }
        };
    }

    public class PlayerScreenObj
    {
        public Team Team { get; set; } = new Team();
    }

    public override void RenderSubscreen()
    {
        var playerScreenObj = State.ScreenStack.Peek().Parameters as PlayerScreenObj;

        Console.WriteLine($"{playerScreenObj!.Team}");
        Console.WriteLine("\n");
        Console.WriteLine("Players");

        var players = State.Players.Where(p => p.Contract?.Team == playerScreenObj.Team);
        Console.WriteLine(string.Format("{0, -10}{1, -40}{2, -10}{3,-10}", "Position", "Name", "Rating", "Weekly Wage"));

        foreach (var player in players)
        {
            Console.WriteLine($"{player.Position,-10}{player,-40}{player.Rating,-10}{player.Contract!.WeeklyWageFriendly}");
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Back");
        Console.WriteLine("<Enter Player Name>) Go To Player");
    }
}
