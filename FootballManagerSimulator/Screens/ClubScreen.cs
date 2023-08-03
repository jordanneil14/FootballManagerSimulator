using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class ClubScreen : BaseScreen
{
    public override ScreenType Screen => ScreenType.Club;

    private readonly IState State;
    private readonly IUtils Utils;

    public ClubScreen(
        IState state, 
        IUtils utils) : base(state)
    {
        State = state;
        Utils = utils;
    }

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                int.TryParse(input, out int value);
                if (value == 0) return;
                var clubScreenObj = State.ScreenStack.Peek().Parameters as ClubScreenObj;
                var player = Utils.GetPlayerByClubAndShirtNumber(clubScreenObj.Club, value);
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

        Console.WriteLine($"{clubScreenObj!.Club}");

        Console.WriteLine($"\nStadium:\n{clubScreenObj.Club.Stadium}\n");

        Console.WriteLine("Upcoming Fixtures");
        var upcomingFixtures = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Where(p => p.HomeTeam == clubScreenObj.Club || p.AwayTeam == clubScreenObj.Club).Take(5);
        foreach (var fixture in upcomingFixtures)
        {
            Console.WriteLine($"{fixture.HomeTeam} Vs {fixture.AwayTeam}");
        }

        Console.WriteLine("\nPlayers");

        var players = State.Players.Where(p => p.Contract?.Club == clubScreenObj.Club);

        Console.WriteLine(string.Format("{0,-10}{1,-10}{2,-40}{3,-10}{4,-10}", "Number", "Position", "Name", "Rating", "Weekly Wage"));

        foreach (var player in players.OrderBy(p => p.Name))
        {
            Console.WriteLine($"{player.ShirtNumber,-10}{player.Position,-10}{player.Name,-40}{player.Rating,-10}{player.Contract!.WeeklyWageFriendly}");
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
        Console.WriteLine("<Enter Player Number>) Go To Player");
    }
}
