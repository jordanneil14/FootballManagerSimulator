using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class MainScreen(
    IState state,
    IProcessHelper processor) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.Main;

    public override void HandleInput(string input)
    {
        switch (input.ToUpper())
        {
            case "A":
                processor.Process();
                break;
            case "B":
                state.Notifications.RemoveRange(0, 1);
                break;
            case "C":
                state.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.LeagueTable
                });
                break;
            case "D":
                var league = state.Competitions.First(p => p.Id == state.MyClub.LeagueId);
                state.ScreenStack.Push(FixturesScreen.CreateScreen(league));
                break;
            case "E":
                state.ScreenStack.Push(ClubScreen.CreateScreen(state.MyClub));
                break;
            case "F":
                state.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.Scout
                });
                break;
            case "S":
                state.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.SaveGame
                });
                break;
            case "G":
                state.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.Tactics
                });
                break;
            case "H":
                state.ScreenStack.Push(new Structures.Screen
                { 
                    Type = ScreenType.Finances
                });
                break;
            case "I":
                state.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.TransferList
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
        var unreadMessagesCount = state.Notifications.Where(p => p.Date <= state.Date).Count();
        Console.WriteLine($"You have {unreadMessagesCount} unread notifications\n");
        if (state.Notifications.Where(p => p.Date <= state.Date).Any())
        {
            Console.WriteLine(state.Notifications.Where(p => p.Date <= state.Date).First());
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Advance");
        if (state.Notifications.Where(p => p.Date <= state.Date).Any())
        {
            Console.WriteLine("B) Get next notification");
        }
        Console.WriteLine("C) League Table");
        Console.WriteLine("D) Fixtures & Results");
        Console.WriteLine("E) My Club");
        Console.WriteLine("F) Scout");
        Console.WriteLine("G) Tactics");
        Console.WriteLine("H) Finances");
        Console.WriteLine("I) Transfer List");
        Console.WriteLine("S) Save Game");
        Console.WriteLine("Q) Quit Game");
    }
}
