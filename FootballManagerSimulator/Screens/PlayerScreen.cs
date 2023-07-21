using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class PlayerScreen : BaseScreen
{
    public override ScreenType Screen => ScreenType.Player;

    private readonly IState State;

    public PlayerScreen(IState state) : base(state)
    {
        State = state;
    }

    public static CurrentScreen CreateScreen(Player player)
    {
        return new CurrentScreen
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
        throw new NotImplementedException();
    }

    public override void RenderSubscreen()
    {
        var screenParameters = State.CurrentScreen.Parameters as PlayerScreenObj;

        Console.WriteLine($"{screenParameters.Player}");
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Back");
        Console.WriteLine("<Enter Player Name>) Go To Player");
    }
}
