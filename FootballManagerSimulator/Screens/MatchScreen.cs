using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class MatchScreen(IState state,
    IMatchSimulatorHelper matchSimulator,
    IPlayerHelper playerHelper) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly IMatchSimulatorHelper MatchSimulator = matchSimulator;
    private readonly IPlayerHelper PlayerHelper = playerHelper;

    public override ScreenType Screen => ScreenType.Match;

    public override Dictionary<string, string> Options => new() {
        { "A", "Continue Match" },
        { "B", "Tactics" }
    };

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                foreach (var competition in State.Competitions)
                {
                    var todaysFixtures = competition.Fixtures.Where(p => p.Date == State.Date);
                    foreach (var fixture in todaysFixtures)
                    {
                        MatchSimulator.ProcessMatch(fixture, competition);
                    }
                }

                var myFixture = State.Competitions
                    .SelectMany(p => p.Fixtures)
                    .First(p => p.Date == State.Date && (p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId));
                if (myFixture.Concluded)
                {
                    foreach (var competition in State.Competitions)
                    {
                        var todaysFixtures = competition.Fixtures.Where(p => p.Date == State.Date && !p.Concluded);
                        foreach (var fixture in todaysFixtures)
                        {
                            MatchSimulator.ProcessMatch(fixture, competition);
                        }
                    }

                    State.ScreenStack.Push(new Screen
                    {
                        Type = ScreenType.FullTime
                    });
                }
                break;
            case "B":
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Tactics
                });
                break;
            default:
                break;
        }
    }

    private string GetDisplayCaption(Fixture fixture)
    {
        if (fixture.Minute == 45) return "** HALF TIME **";
        return "** EXTRA TIME REQUIRED **";
    }

    public override void RenderSubscreen()
    {
        var fixture = State.Competitions
            .SelectMany(p => p.Fixtures)
            .First(p => p.Date == State.Date && (p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId));
        var comp = State.Competitions.First(p => p.Fixtures.Contains(fixture));

        var homeClub = State.Clubs
            .Where(p => p.Id == fixture.HomeClub.Id)
            .First();

        var awayClub = State.Clubs
            .Where(p => p.Id == fixture.AwayClub.Id)
            .First();

        Console.WriteLine($"{homeClub.Name,53}{fixture.GoalsHome,5} v {fixture.GoalsAway,-5}{awayClub.Name,-53}\n{GetDisplayCaption(fixture),67}\n");

        var homeClubPlayers = State.Clubs
            .Where(p => p.Id == homeClub.Id)
            .First().TacticSlots;

        var awayClubPlayers = State.Clubs
            .Where(p => p.Id == awayClub.Id)
            .First().TacticSlots;

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
