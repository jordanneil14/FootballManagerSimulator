using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class ClubScreen : BaseScreen
{
    public override ScreenType Screen => ScreenType.Club;

    private readonly IState State;
    private readonly IPlayerHelper PlayerHelper;

    public ClubScreen(
        IState state, 
        IPlayerHelper utils) : base(state)
    {
        State = state;
        PlayerHelper = utils;
    }

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                var isInt = int.TryParse(input, out int value);
                if (!isInt) return;
                var player = PlayerHelper.GetPlayerById(value);
                if (player != null)
                {
                    State.ScreenStack.Push(PlayerScreen.CreateScreen(player));
                }
                break;
        }
    }

    public static Screen CreateScreen(Club club)
    {
        return new Screen
        {
            Type = ScreenType.Club,
            Parameters = new ClubScreenObj
            {
                Club = club
            }
        };
    }

    public class ClubScreenObj
    {
        public Club Club { get; set; } = new Club();
    }

    public override void RenderSubscreen()
    {
        var clubScreenObj = State.ScreenStack.Peek().Parameters as ClubScreenObj;

        Console.WriteLine($"{clubScreenObj!.Club.Name}");

        Console.WriteLine($"\nStadium:\n{clubScreenObj.Club.Stadium}\n");

        Console.WriteLine("Upcoming Fixtures:");
        var upcomingFixtures = State.Leagues
            .SelectMany(p => p.Fixtures)
            .Where(p => p.HomeClub == clubScreenObj.Club || p.AwayClub == clubScreenObj.Club).Take(5);
        foreach (var fixture in upcomingFixtures)
        {
            Console.WriteLine($"{fixture.HomeClub.Name} Vs {fixture.AwayClub.Name}");
        }

        Console.WriteLine("\nPlayers:");

        var players = State.Players.Where(p => p.Contract?.ClubId == clubScreenObj.Club.Id);

        Console.WriteLine($"{"ID",-10}{"Number",-10}{"Position",-10}{"Name",-40}{"Rating",-10}{"Weekly Wage", -20}");

        foreach (var player in players.OrderBy(p => p.Name))
        {
            Console.WriteLine($"{player.Id,-10}{player.ShirtNumber,-10}{player.PreferredPosition,-10}{player.Name,-40}{player.Rating,-10}");
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Player ID>) Go To Player");
    }
}
