using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Helpers;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class PostMatchScoreScreen(
	IState state,
	IPlayerHelper playerHelper) : BaseScreen(state)
{
    private readonly IState State = state;
	private readonly IPlayerHelper PlayerHelper = playerHelper;

	public override ScreenType Screen => ScreenType.PostMatchScores;

    public override Dictionary<string, string> Options => new() { { "A", "Continue" } };


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
                State.ScreenStack.Push(new ScreenModel
                {
                    Type = ScreenType.Main
                });
				break;
            default:
                break;
        }
    }

	public override void RenderSubscreen()
    {
        Console.WriteLine("Today's Results\n");

		var fixturesByCompetition = State.Competitions
				.Where(p => p.Clubs.Select(p => p.Id).Contains(State.Clubs.First(p => p.Id == State.MyClubId).Id));

		foreach (var fixtureByCompetition in fixturesByCompetition)
		{
			var todaysFixtures = fixtureByCompetition.Fixtures.Where(p => p.Date == State.Date);
			if (!todaysFixtures.Any()) continue;

			var round = todaysFixtures.First().Round;
			Console.WriteLine($"\n{fixtureByCompetition.Name} Round {round}");

			foreach (var todaysFixture in todaysFixtures)
			{
				if (!todaysFixture.Concluded)
				{
					Console.WriteLine($"{todaysFixture.HomeClub.Name,55}    v    {todaysFixture.AwayClub.Name,-55}");
					continue;
				}

				Console.WriteLine($"{todaysFixture.HomeClub.Name,55}{todaysFixture.GoalsHome!.Value,3} v {todaysFixture.GoalsAway!.Value,-3}{todaysFixture.AwayClub.Name,-55}");

				var homeGoals = todaysFixture.HomeScorers.GroupBy(p => p.PlayerId);
				var awayGoals = todaysFixture.AwayScorers.GroupBy(p => p.PlayerId);

				for (var i = 0; i < Math.Max(homeGoals.Count(), awayGoals.Count()); i++)
				{
					var homeCaption = string.Empty;
					var awayCaption = string.Empty;

					var homeGroupedElement = homeGoals.ElementAtOrDefault(i);
					if (homeGroupedElement != null)
					{
						var homePlayerName = PlayerHelper.GetPlayerById(homeGroupedElement.Key).Name;
						homeCaption = $"{homePlayerName} ({string.Join(",", homeGroupedElement.Select(p => string.Format("{0}'", p.Minute)))})";
					}

					var awayGroupedElement = awayGoals.ElementAtOrDefault(i);
					if (awayGroupedElement != null)
					{
						var awayPlayerName = PlayerHelper.GetPlayerById(awayGroupedElement.Key).Name;
						awayCaption = $"{awayPlayerName} ({string.Join(",", awayGroupedElement.Select(p => string.Format("{0}'", p.Minute)))})";
					}

					Console.WriteLine($"{homeCaption,58}   {awayCaption,-58}");
				}

				Console.WriteLine("");
			}
		}
	}
}
