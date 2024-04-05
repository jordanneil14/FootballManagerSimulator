using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class MainScreen : BaseScreen
{
    private readonly IState State;
    private readonly IProcessHelper Processor;

    public MainScreen(
        IState state,
        IProcessHelper processor) : base(state)
    {
        State = state;
        Processor = processor;
    }

    public override ScreenType Screen => ScreenType.Main;

    public override void HandleInput(string input)
    {
        switch (input.ToUpper())
        {
            case "A":
                Processor.Process();
                break;
            case "B":
                State.Notifications.RemoveRange(0, 1);
                break;
            case "C":
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.LeagueTable
                });
                break;
            case "D":
                var league = State.Leagues.First(p => p.Id == State.MyClub.LeagueId);
                State.ScreenStack.Push(FixturesScreen.CreateScreen(league));
                break;
            case "E":
                State.ScreenStack.Push(ClubScreen.CreateScreen(State.MyClub));
                break;
            case "F":
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.Scout
                });
                break;
            case "S":
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.SaveGame
                });
                break;
            case "G":
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.Tactics
                });
                break;
            case "H":
                State.ScreenStack.Push(new Structures.Screen
                { 
                    Type = ScreenType.Finances
                });
                break;
            case "Q":
                Environment.Exit(0);
                break;
            default:
                break;
        }
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Notifications");
        var unreadMessagesCount = State.Notifications.Where(p => p.Date <= State.Date).Count();
        Console.WriteLine($"You have {unreadMessagesCount} unread notifications\n");
        if (State.Notifications.Where(p => p.Date <= State.Date).Any())
        {
            Console.WriteLine(State.Notifications.Where(p => p.Date <= State.Date).First());
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Advance");
        if (State.Notifications.Where(p => p.Date <= State.Date).Any())
        {
            Console.WriteLine("B) Get next notification");
        }
        Console.WriteLine("C) League Table");
        Console.WriteLine("D) Fixtures & Results");
        Console.WriteLine("E) My Club");
        Console.WriteLine("F) Scout");
        Console.WriteLine("G) Tactics");
        Console.WriteLine("H) Finances");
        Console.WriteLine("S) Save Game");
        Console.WriteLine("Q) Quit Game");
    }
}
