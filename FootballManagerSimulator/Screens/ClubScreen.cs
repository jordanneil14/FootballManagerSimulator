using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class ClubScreen(
    IState state,
    IPlayerHelper playerHelper) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly IPlayerHelper PlayerHelper = playerHelper;

    public override ScreenType Screen => ScreenType.Club;

    public override Dictionary<string, string> Options => new() {};

	public override string? OptionPrompt => "Enter a player id: ";

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
			case "ESCAPE":
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

    public static ScreenModel CreateScreen(ClubModel club)
    {
        return new ScreenModel
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
        public ClubModel Club { get; set; } = new ClubModel();
    }

    public override void RenderSubscreen()
    {
        var clubScreenObj = State.ScreenStack.Peek().Parameters as ClubScreenObj;

        if (clubScreenObj.Club.Id != State.MyClubId)
            Console.WriteLine($"{clubScreenObj!.Club.Name}\n");

        Console.WriteLine($"Stadium:\n{clubScreenObj.Club.Stadium}\n");

        Console.WriteLine("Recent Fixtures & Results:");

        var recentResults = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Where(p => (p.HomeClub.Id == clubScreenObj.Club.Id || p.AwayClub.Id == clubScreenObj.Club.Id) && p.ClubWon != null)
            .OrderBy(p => p.Date)
            .TakeLast(2);

        foreach (var fixture in recentResults)
        {
            var competition = State.Competitions.First(p => p.Fixtures.Contains(fixture));
            Console.WriteLine($"{fixture.HomeClub.Name,55}{fixture.GoalsHome!.Value,3} v {fixture.GoalsAway!.Value,-3}{fixture.AwayClub.Name,-55}");
        }

        var upcomingFixtures = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Where(p => p.HomeClub.Id == clubScreenObj.Club.Id || p.AwayClub.Id == clubScreenObj.Club.Id && p.Date > State.Date)
            .OrderBy(p => p.Date)
            .Take(3);
        foreach (var fixture in upcomingFixtures)
        {
            var competition = State.Competitions.First(p => p.Fixtures.Contains(fixture));
			Console.WriteLine($"{fixture.HomeClub.Name,55}{"",3} v {"",-3}{fixture.AwayClub.Name,-55}");
		}

		Console.WriteLine("\nPlayers:");

        var players = State.Players.Where(p => p.Contract?.ClubId == clubScreenObj.Club.Id);

        Console.WriteLine($"{"Id",-10}{"Number",-10}{"Position",-10}{"Name",-40}{"Rating",-10}{"Transfer Value",-20}{"Contract Expiry Date",-15}");
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");

        foreach (var player in players.OrderBy(p => p.Name))
        {
            var transferValue = PlayerHelper.GetTransferValue(player);
            var transferValueFriendly = $"£{transferValue:n}";
            Console.WriteLine($"{player.Id,-10}{player.ShirtNumber,-10}{player.PreferredPosition,-10}{player.Name,-40}{player.Rating,-10}{transferValueFriendly,-20}{player.Contract.ExpiryDate,-15}");
        }
    }
}
