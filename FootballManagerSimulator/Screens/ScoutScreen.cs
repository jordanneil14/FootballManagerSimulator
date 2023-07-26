using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class ScoutScreen : BaseScreen
{
    private readonly IState State;

    public ScoutScreen(IState state) : base(state)
    {
        State = state;
    }

    public override ScreenType Screen => ScreenType.Scout;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Player Name>) Go To Player");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine(string.Format("{0,-35}{1,-10}{2,-25}", "Player", "Rating", "Team"));

        var batchOfPlayers = State.Players.OrderByDescending(p => p.Contract?.Team.Name).Take(600);
        foreach (var player in batchOfPlayers)
        {
            var team = player.Contract?.Team?.Name ?? "Unemployed";
            Console.WriteLine($"{player, -35}{player.Rating, -10}{team, -25}");
        }
    }
}
