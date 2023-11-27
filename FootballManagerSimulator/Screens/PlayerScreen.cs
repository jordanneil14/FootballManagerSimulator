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
            default:
                break;
        }
    }

    public override void RenderSubscreen()
    {
        var screenParameters = State.ScreenStack.Peek().Parameters as PlayerScreenObj;
        var player = screenParameters.Player;

        Console.WriteLine($"{player.Name}\n\nGeneral Information:");

        Console.WriteLine(
            $"Age:{player.Age}\n" +
            $"Birth Date:{player.BirthDate}\n" +
            $"Height:{player.Height}\n" +
            $"Weight:{player.Weight}\n" +
            $"Rating:{player.Rating}\n" +
            $"Position:{player.Position}\n" +
            $"Nationality:{player.Nationality}\n");

        Console.WriteLine($"Playing Stats:");

        if (player.Position == "GK")
        {
            Console.WriteLine($"Positioning:{player.GKPositioning}Diving:{player.GKDiving}Kicking:{player.GKKicking}Handling:{player.GKHandling}Reflexes:{player.GKReflexes}");
            return;
        }

        Console.WriteLine($"Aggression:{player.Aggression,-10}Agility:{player.Agility,-10}Ball Control:{player.BallControl,-10}Dribbling:{player.Dribbling,-10}Marking:{player.Marking,-10}Sliding Tackle:{player.SlidingTackle,-10}");

        Console.WriteLine($"Crossing:{player.Crossing,-10}Short Passing:{player.ShortPass,-10}Long Passing:{player.LongPass,-10}Work Rate:{player.WorkRate,-10}Acceleration:{player.Acceleration,-10}Speed:{player.Speed,-10}");

        Console.WriteLine($"Stamina:{player.Stamina,-10}Strength:{player.Strength,-10}Balance:{player.Balance,-10}Skill Moves:{player.SkillMoves,-10}Jumping:{player.Jumping,-10}Heading:{player.Heading,-10}");

        Console.WriteLine($"Shot Power:{player.ShotPower,-10}Finishing:{player.Finishing,-10}Long Shots:{player.LongShots,-10}Curve:{player.Curve,-10}Freekick Accuracy:{player.FreekickAccuracy,-10}Volleys:{player.Volleys,-10}");

        Console.WriteLine($"Penalties:{player.Penalties,-10}Long Shots:{player.LongShots,-10}Curve:{player.Curve,-10}Freekick Accuracy:{player.FreekickAccuracy,-10}");
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
    }
}
