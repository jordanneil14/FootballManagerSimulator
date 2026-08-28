using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class MainScreen(
    IState state,
    IProcessHelper processor) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly IProcessHelper Processor = processor;

    public override ScreenType Screen => ScreenType.Main;

    public override IDictionary<string, string> Options => GetOptions();

	public override string? OptionPrompt => null;

	public override void HandleInput(string input)
    {
        switch (input)
        {
			case "UPARROW":
				if (base.OptionIndex > 0)
					base.OptionIndex -= 1;
				break;
			case "DOWNARROW":
				if (Options.Count > 1 && base.OptionIndex < Options.Count - 1)
					base.OptionIndex += 1;
				break;
            case "ENTER":
				HandleEnterPress();
				break;
            default:
                break;
        }
    }

    private void HandleEnterPress()
    {
		var option = Options.ElementAt(base.OptionIndex).Key;
		switch (option)
		{
			case "A":
				Processor.Process();
				break;

			case "B":
				State.Notifications.RemoveRange(0, 1);
				break;
			case "C":
				State.ScreenStack.Push(new Screen
				{
					Type = ScreenType.LeagueTable
				});
				break;
			case "D":
				var league = State.Competitions.First(p => p.Id == State.Clubs.First(p => p.Id == State.MyClubId).LeagueId);
				State.ScreenStack.Push(FixturesScreen.CreateScreen(league));
				break;
			case "E":
				State.ScreenStack.Push(ClubScreen.CreateScreen(State.Clubs.First(p => p.Id == State.MyClubId)));
				break;
			case "F":
				State.ScreenStack.Push(new Screen
				{
					Type = ScreenType.Scout
				});
				break;
			case "S":
				State.ScreenStack.Push(new Screen
				{
					Type = ScreenType.SaveGame
				});
				break;
			case "G":
				State.ScreenStack.Push(new Screen
				{
					Type = ScreenType.Tactics
				});
				break;
			case "H":
				State.ScreenStack.Push(new Screen
				{
					Type = ScreenType.Finances
				});
				break;
			case "I":
				State.ScreenStack.Push(new Screen
				{
					Type = ScreenType.TransferList
				});
				break;
			case "Q":
				Environment.Exit(0);
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

    public Dictionary<string, string> GetOptions()
    {
        var dict = new Dictionary<string, string>();
        dict.Add("A", "Advance");
        if (State.Notifications.Where(p => p.Date <= State.Date).Any())
            dict.Add("B", "Get Next Notification");
        dict.Add("C", "League Table");
        dict.Add("D", "Fixtures & Results");
        dict.Add("E", "My Club");
        dict.Add("F", "Scout");
        dict.Add("G", "Tactics");
        dict.Add("H", "Finances");
        dict.Add("I", "Transfer List");
        dict.Add("S", "Save Game");
        dict.Add("Q", "Quit Game");
        return dict;
    }
}
