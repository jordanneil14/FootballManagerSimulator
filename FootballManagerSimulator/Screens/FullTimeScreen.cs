using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class FullTimeScreen : BaseScreen
{
    private readonly IState State;
    private readonly IPlayerHelper PlayerHelper;

    public FullTimeScreen(
        IState state,
        IPlayerHelper playerHelper) : base(state)
    {
        State = state;
        PlayerHelper = playerHelper;
    }

    public override ScreenType Screen => ScreenType.FullTime;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                State.ScreenStack.Clear();
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Main
                });
                break;
            case "B":
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.PostMatchScores
                });
                break;
            case "C":
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.PostMatchLeagueTable
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
        var fixture = State.TodaysFixtures
            .SelectMany(p => p.Fixtures)
            .Where(p => p.HomeClub == State.MyClub || p.AwayClub == State.MyClub)
            .First();

        var homeClub = State.Clubs
            .Where(p => p == fixture.HomeClub)
            .First();

        var awayClub = State.Clubs
            .Where(p => p == fixture.AwayClub)
            .First();

        Console.WriteLine($"{homeClub,53}{fixture.GoalsHome,5} v {fixture.GoalsAway,-5}{awayClub,-53}\n{"** FULL TIME **",67}\n");

        var homeClubPlayers = State.Clubs
            .Where(p => p == homeClub)
            .First()
            .TacticSlots;

        var awayClubPlayers = State.Clubs
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
                var player = PlayerHelper.GetPlayerById(tacticSlotHome.PlayerId.Value)!;
                homePlayer = $"{player.Name,55}{player.ShirtNumber,3}";
            }

            var tacticSlotAway = homeClubPlayers.ElementAt(i);
            if (tacticSlotAway.PlayerId != null)
            {
                var player = PlayerHelper.GetPlayerById(tacticSlotAway.PlayerId.Value)!;
                awayPlayer = $"{player.ShirtNumber,-3}{player.Name,-55}";
            }

            Console.WriteLine($"{homePlayer}   {awayPlayer}");
        }
    }
}
