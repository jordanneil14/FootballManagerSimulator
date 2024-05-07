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

    public static Screen CreateScreen(ILeague competition)
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
        public ILeague League { get; set; }
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

        var rounds = state.Leagues
            .Where(p => p.Id == parameters.League.Id)
            .SelectMany(p => p.Fixtures)
            .GroupBy(x => x.WeekNumber);

        foreach (var round in rounds)
        {
            Console.WriteLine($"\nRound {round.Key} - {round.First().Date}");
            foreach(var fixture in round)
            {
                if (fixture.Concluded)
                {
                    Console.WriteLine($"{fixture.HomeClub.Name,55}{fixture.GoalsHome!.Value,3} v {fixture.GoalsAway!.Value,-3}{fixture.AwayClub.Name,-55}");


                    var homeGoals = fixture.HomeScorers.GroupBy(p => p.PlayerId);
                    var awayGoals = fixture.AwayScorers.GroupBy(p => p.PlayerId);

                    for(int i = 0; i < Math.Max(homeGoals.Count(), awayGoals.Count()); i++) 
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
                    continue;
                }

                Console.WriteLine($"{fixture.HomeClub.Name,58} v {fixture.AwayClub.Name,-58}");
            }
        }
    }
}
