using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class FixturesScreen(
    IState state,
    IPlayerHelper playerHelper) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.Fixtures;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                state.ScreenStack.Pop();
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

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Fixtures & Results");

        var parameters = state.ScreenStack.Peek().Parameters as FixturesScreenObj;

        var dates = state.Competitions
            .Where(p => p.Clubs.Select(p => p.Id).Contains(state.Clubs.First(p => p.Id == state.MyClubId).Id))
            .SelectMany(p => p.Fixtures)
            .GroupBy(p => p.Date)
            .Select(p => p.Key)
            .OrderBy(p => p);

        foreach (var date in dates)
        {
            var fixturesByCompetition = state.Competitions
                .Where(p => p.Clubs.Select(p => p.Id).Contains(state.Clubs.First(p => p.Id == state.MyClubId).Id));

            foreach (var f in fixturesByCompetition)
            {
                var fixturesOnDate = f.Fixtures.Where(p => p.Date == date);
                if (!fixturesOnDate.Any()) continue;

                var q = f.Fixtures.Where(p => p.Date == date);
                var round = q.First().Round;
                Console.WriteLine($"\n{f.Name} Round {round} - {date}");

                foreach (var fixture in q)
                {
                    if (!fixture.Concluded)
                    {
                        Console.WriteLine($"{fixture.HomeClub.Name,55}    v    {fixture.AwayClub.Name,-55}");
                        continue;
                    }

                    Console.WriteLine($"{fixture.HomeClub.Name,55}{fixture.GoalsHome!.Value,3} v {fixture.GoalsAway!.Value,-3}{fixture.AwayClub.Name,-55}");

                    var homeGoals = fixture.HomeScorers.GroupBy(p => p.PlayerId);
                    var awayGoals = fixture.AwayScorers.GroupBy(p => p.PlayerId);

                    for (int i = 0; i < Math.Max(homeGoals.Count(), awayGoals.Count()); i++)
                    {
                        var homeCaption = string.Empty;
                        var awayCaption = string.Empty;

                        var homeGroupedElement = homeGoals.ElementAtOrDefault(i);
                        if (homeGroupedElement != null)
                        {
                            var homePlayerName = playerHelper.GetPlayerById(homeGroupedElement.Key).Name;
                            homeCaption = $"{homePlayerName} ({string.Join(",", homeGroupedElement.Select(p => string.Format("{0}'", p.Minute)))})";
                        }

                        var awayGroupedElement = awayGoals.ElementAtOrDefault(i);
                        if (awayGroupedElement != null)
                        {
                            var awayPlayerName = playerHelper.GetPlayerById(awayGroupedElement.Key).Name;
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
