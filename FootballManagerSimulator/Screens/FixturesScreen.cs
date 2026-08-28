using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace FootballManagerSimulator.Screens;

public class FixturesScreen(
    IState state,
    IPlayerHelper playerHelper) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly IPlayerHelper PlayerHelper = playerHelper;

    public override ScreenType Screen => ScreenType.Fixtures;

    public override Dictionary<string, string> Options => new() {
        { "B", "Back" }
    };

	public override string? OptionPrompt => null;

	public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    public static Screen CreateScreen(ICompetition competition)
    {
        return new Screen
        {
            Type = ScreenType.Fixtures,
            Parameters = new FixturesScreenObj
            {
                League = competition
            }
        };
    }

    public class FixturesScreenObj
    {
        public ICompetition League { get; set; }
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Fixtures & Results");

        var parameters = State.ScreenStack.Peek().Parameters as FixturesScreenObj;

        var dates = State.Competitions
            .Where(p => p.Clubs.Select(p => p.Id).Contains(State.Clubs.First(p => p.Id == State.MyClubId).Id))
            .SelectMany(p => p.Fixtures)
            .GroupBy(p => p.Date)
            .Select(p => p.Key)
            .OrderBy(p => p);

        foreach (var date in dates)
        {
            var fixturesByCompetition = State.Competitions
                .Where(p => p.Clubs.Select(p => p.Id).Contains(State.Clubs.First(p => p.Id == State.MyClubId).Id));

            foreach (var fixtureByCompetition in fixturesByCompetition)
            {
                var fixturesOnDate = fixtureByCompetition.Fixtures.Where(p => p.Date == date);
                if (!fixturesOnDate.Any()) continue;

                var todaysFixtures = fixtureByCompetition.Fixtures.Where(p => p.Date == date);
                var round = todaysFixtures.First().Round;
                Console.WriteLine($"\n{fixtureByCompetition.Name} Round {round} - {date.ToString("dddd, dd MMMM yyyy")}");

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
}
