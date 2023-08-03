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

        Console.WriteLine($"{player.Name}\n\nInformation:");

        Console.WriteLine($"Age:{player.Age} Birth Date:{player.BirthDate} Height:{player.Height} Weight:{player.Weight} Rating:{player.Rating} " +
            $"Position:{player.Position} Nationality:{player.Nationality}\n");

        Console.WriteLine($"Outfield Stats:");

        Console.WriteLine(string.Format("Aggression:{0,-6}Agility:{1,-6}Ball Control:{2,-6}Dribbling:{3,-6}Marking:{4,-6}Sliding Tackle:{5,-6}", 
            player.Aggression, player.Agility, player.BallControl, player.Dribbling, player.Marking, player.SlidingTackle));

        Console.WriteLine(string.Format("Crossing:{0,-6}Short Passing:{1,-6}Long Passing:{2,-6}Work Rate:{3,-6}Acceleration:{4,-6}Speed:{5,-6}",
            player.Crossing, player.ShortPass, player.LongPass, player.WorkRate, player.Acceleration, player.Speed));

        Console.WriteLine(string.Format("Stamina:{0,-6}Strength:{1,-6}Balance:{2,-6}Skill Moves:{3,-6}Jumping:{4,-6}Heading:{5,-6}",
            player.Stamina, player.Strength, player.Balance, player.SkillMoves, player.Jumping, player.Heading));

        Console.WriteLine(string.Format("Shot Power:{0,-6}Finishing:{1,-6}Long Shots:{2,-6}Curve:{3,-6}Freekick Accuracy:{4,-6}Volleys:{5,-6}",
           player.ShotPower, player.Finishing, player.LongShots, player.Curve, player.FreekickAccuracy, player.Volleys));

        Console.WriteLine(string.Format("Penalties:{0,-6}Long Shots:{1,-6}Curve:{2,-6}Freekick Accuracy:{3,-6}",
           player.Penalties, player.LongShots, player.Curve, player.FreekickAccuracy));

        Console.WriteLine($"\nGoalKeeper Stats:");
        Console.WriteLine(string.Format("Positioning:{0,-6}Diving:{1,-6}Kicking:{2,-6}Handling:{3,-6}Reflexes:{4,-6}",
           player.GKPositioning, player.GKDiving, player.GKKicking, player.GKHandling, player.GKReflexes));

        //Console.WriteLine($"{player.SlidingTackle} {player.Aggression} {player.Agility} {player.FreekickAccuracy} {player.Finishing} {player.Dribbling} {player.Crossing} {} {} {} {} {}")
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
    }
}
