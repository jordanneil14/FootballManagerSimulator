using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class MatchScreen(IState state,
    IMatchSimulatorHelper matchSimulator,
    IPlayerHelper playerHelper) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.Match;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                foreach(var comp in state.Competitions)
                {
                    var todaysFixtures = comp.Fixtures.Where(p => p.Date == state.Date);
                    foreach(var fixture in todaysFixtures)
                    {
                        matchSimulator.ProcessMatch(fixture, comp);
                    }
                }

                var myFixture = state.Competitions
                    .SelectMany(p => p.Fixtures)
                    .First(p => p.Date == state.Date && (p.HomeClub.Id == state.MyClubId || p.AwayClub.Id == state.MyClubId));
                if (myFixture.Concluded)
                {
                    foreach (var comp in state.Competitions)
                    {
                        var todaysFixtures = comp.Fixtures.Where(p => p.Date == state.Date && !p.Concluded);
                        foreach (var fixture in todaysFixtures)
                        {
                            matchSimulator.ProcessMatch(fixture, comp);
                        }
                    }

                    state.ScreenStack.Push(new Screen
                    {
                        Type = ScreenType.PostMatchScores
                    });
                }
                break;
            case "B":
                state.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Tactics
                });
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Continue Match");
        Console.WriteLine("B) Tactics");
    }

    private string GetDisplayCaption(Fixture fixture)
    {
        if (fixture.Minute == 45) return "** HALF TIME **";
        return "** EXTRA TIME REQUIRED **";
    }

    public override void RenderSubscreen()
    {
        var fixture = state.Competitions
            .SelectMany(p => p.Fixtures)
            .First(p => p.Date == state.Date && (p.HomeClub.Id == state.MyClubId || p.AwayClub.Id == state.MyClubId));
        var comp = state.Competitions.First(p => p.Fixtures.Contains(fixture));

        var homeClub = state.Clubs
            .Where(p => p.Id == fixture.HomeClub.Id)
            .First();

        var awayClub = state.Clubs
            .Where(p => p.Id == fixture.AwayClub.Id)
            .First();

        Console.WriteLine($"{homeClub.Name,53}{fixture.GoalsHome,5} v {fixture.GoalsAway,-5}{awayClub.Name,-53}\n{GetDisplayCaption(fixture),67}\n");

        var homeClubPlayers = state.Clubs
            .Where(p => p.Id == homeClub.Id)
            .First().TacticSlots;

        var awayClubPlayers = state.Clubs
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
                var player = playerHelper.GetPlayerById(tacticSlotHome.PlayerId.Value)!;

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
                var player = playerHelper.GetPlayerById(tacticSlotAway.PlayerId.Value)!;

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
