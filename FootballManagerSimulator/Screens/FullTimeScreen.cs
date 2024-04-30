using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class FullTimeScreen(
    IState state,
    IPlayerHelper playerHelper) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.FullTime;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                state.ScreenStack.Clear();
                state.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Main
                });
                break;
            case "B":
                state.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.PostMatchScores
                });
                break;
            case "C":
                state.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.LeagueTable
                });
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Leave Match");
        Console.WriteLine("B) Full-time Scores");
        Console.WriteLine("C) League Table");
    }

    public override void RenderSubscreen()
    {
        var fixture = state.TodaysFixtures
            .SelectMany(p => p.Fixtures)
            .Where(p => p.HomeClub == state.MyClub || p.AwayClub == state.MyClub)
            .First();

        var homeClub = state.Clubs
            .Where(p => p == fixture.HomeClub)
            .First();

        var awayClub = state.Clubs
            .Where(p => p == fixture.AwayClub)
            .First();

        Console.WriteLine($"{homeClub.Name,53}{fixture.GoalsHome,5} v {fixture.GoalsAway,-5}{awayClub.Name,-53}\n{"** FULL TIME **",67}\n");

        var homeClubPlayers = state.Clubs
            .Where(p => p == homeClub)
            .First()
            .TacticSlots;

        var awayClubPlayers = state.Clubs
            .Where(p => p == awayClub)
            .First()
            .TacticSlots;

        for (var i = 0; i < 18; i++)
        {
            if (i == 11)
                Console.WriteLine($"{"------------", 58}{"   ------------",-58}");

            var homePlayer = "EMPTY SLOT";
            var awayPlayer = "EMPTY SLOT";

            var tacticSlotHome = homeClubPlayers.ElementAt(i);
            if (tacticSlotHome.PlayerId != null)
            {
                var player = playerHelper.GetPlayerById(tacticSlotHome.PlayerId.Value)!;
                homePlayer = $"{player.Name,55}{player.ShirtNumber,3}";
            }

            var tacticSlotAway = awayClubPlayers.ElementAt(i);
            if (tacticSlotAway.PlayerId != null)
            {
                var player = playerHelper.GetPlayerById(tacticSlotAway.PlayerId.Value)!;
                awayPlayer = $"{player.ShirtNumber,-3}{player.Name,-55}";
            }

            Console.WriteLine($"{homePlayer}   {awayPlayer}");
        }
    }
}
