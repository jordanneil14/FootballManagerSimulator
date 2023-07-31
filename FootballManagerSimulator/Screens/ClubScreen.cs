using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class ClubScreen : BaseScreen
{
    public override ScreenType Screen => ScreenType.Club;

    private readonly IState State;
    private readonly IUtils HelperFunction;

    public ClubScreen(
        IState state, 
        IUtils helperFunction) : base(state)
    {
        State = state;
        HelperFunction = helperFunction;
    }

    public override void HandleInput(string input)
    {
        switch (input)
        {
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
            Parameters = new ClubScreenObj
            {
                Team = team
            }
        };
    }

    public class ClubScreenObj
    {
        public Team Team { get; set; } = new Team();
    }

    public override void RenderSubscreen()
    {
        var playerScreenObj = State.ScreenStack.Peek().Parameters as ClubScreenObj;

        Console.WriteLine($"{playerScreenObj!.Team} Players");
        Console.WriteLine("\n");

        var players = State.Players.Where(p => p.Contract?.Team == playerScreenObj.Team);

        Console.WriteLine(string.Format("{0,-10}{1,-10}{2,-40}{3,-10}{4,-10}", "Number", "Position", "Name", "Rating", "Weekly Wage"));

        foreach (var player in players.OrderBy(p => p.Name))
        {
            Console.WriteLine($"{player.ShirtNumber,-10}{player.Position,-10}{player,-40}{player.Rating,-10}{player.Contract!.WeeklyWageFriendly}");
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Player Number>) Go To Player");
    }
}
