using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class MainScreen : BaseScreen
{
    private readonly IState State;

    public MainScreen(IState state) : base(state)
    {
        State = state;
    }

    public override ScreenType Screen => ScreenType.Main;

    public override void HandleInput(string input)
    {
        switch (input.ToUpper())
        {
            case "A":
                Process();
                break;
            case "B":
                State.Notifications.RemoveRange(0, 1);
                break;
            case "C":
                State.CurrentScreen.Type = ScreenType.LeagueTable;
                break;
            case "D":
                State.CurrentScreen.Type = ScreenType.Fixtures;
                break;
            case "E":
                State.CurrentScreen = ClubScreen.CreateScreen(State.MyTeam);
                break;
            case "F":
                State.CurrentScreen.Type = ScreenType.Scout;
                break;
            case "S":
                State.CurrentScreen.Type = ScreenType.SaveGame;
                break;
            case "G":
                State.CurrentScreen.Type = ScreenType.Tactics;
                break;
            case "Q":
                Environment.Exit(0);
                break;
            default:
                break;
        }
    }

    private void Process()
    {
        State.UserFeedbackUpdates.Clear();

        if (State.TodaysFixtures.Any(p => !p.Concluded))
        {
            State.CurrentScreen.Type = ScreenType.Fixture;
        }
        else
        {
            State.Date = State.Date.AddDays(1);
        }
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Notifications");
        Console.WriteLine($"You have {State.Notifications.Where(p => p.Date <= State.Date).Count()} unread notifications\n");
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
        Console.WriteLine("S) Save Game");
        Console.WriteLine("Q) Quit Game");
    }
}
