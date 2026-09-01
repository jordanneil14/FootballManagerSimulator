using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class FullTimeScreen(
    IState state,
    IPlayerHelper playerHelper) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly IPlayerHelper PlayerHelper = playerHelper;

    public override ScreenType Screen => ScreenType.FullTime;

    public override Dictionary<string, string> Options => new() {
        { "A", "Continue" }
    };

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
			case "ESCAPE":
				State.ScreenStack.Pop();
				OptionIndex = 0;
				break;
			case "ENTER":
                State.ScreenStack.Clear();
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.PostMatchScores
                });
                break;
            default:
                break;
        }
    }

    public override void RenderSubscreen()
    {
        var fixture = State.Competitions
            .SelectMany(p => p.Fixtures)
            .First(p => p.Date == State.Date && (p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId));

        var homeClub = State.Clubs
            .Where(p => p.Id == fixture.HomeClub.Id)
            .First();

        var awayClub = State.Clubs
            .Where(p => p.Id == fixture.AwayClub.Id)
            .First();

        Console.WriteLine($"{homeClub.Name,53}{fixture.GoalsHome,5} v {fixture.GoalsAway,-5}{awayClub.Name,-53}\n{"** FULL TIME **",67}\n");

        var homeClubPlayers = State.Clubs
            .Where(p => p.Id == homeClub.Id)
            .First()
            .TacticSlots;

        var awayClubPlayers = State.Clubs
            .Where(p => p.Id == awayClub.Id)
            .First()
            .TacticSlots;

        for (var i = 0; i < 18; i++)
        {
            if (i == 11)
                Console.WriteLine($"{"------------",58}{"   ------------",-58}");

            var homePlayer = "EMPTY SLOT";
            var awayPlayer = "EMPTY SLOT";

            var tacticSlotHome = homeClubPlayers.ElementAt(i);
            if (tacticSlotHome.PlayerId != null)
            {
                var player = PlayerHelper.GetPlayerById(tacticSlotHome.PlayerId.Value)!;

                var goalCaption = string.Empty;
                var goals = fixture.HomeScorers.Where(p => p.PlayerId == player.Id).Select(p => p.Minute);
                if (goals.Any())
                {
                    var q = string.Join(", ", goals.Select(x => string.Format("{0}'", x)));
                    goalCaption = $"({q})";
                }

                homePlayer = $"{goalCaption + " " + player.Name,55}{player.ShirtNumber,3}";
            }

            var tacticSlotAway = awayClubPlayers.ElementAt(i);
            if (tacticSlotAway.PlayerId != null)
            {
                var player = PlayerHelper.GetPlayerById(tacticSlotAway.PlayerId.Value)!;

                var goalCaption = string.Empty;
                var goals = fixture.AwayScorers.Where(p => p.PlayerId == player.Id).Select(p => p.Minute);
                if (goals.Any())
                {
                    var q = string.Join(", ", goals.Select(x => string.Format("{0}'", x)));
                    goalCaption = $"({q})";
                }

                awayPlayer = $"{player.ShirtNumber,-3}{player.Name + " " + goalCaption,-55}";
            }

            Console.WriteLine($"{homePlayer}   {awayPlayer}");
        }
    }
}
