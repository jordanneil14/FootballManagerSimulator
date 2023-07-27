using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class FullTimeScreen : BaseScreen
{
    private readonly IState State;
    private readonly IFixtureHelper FixtureHelper;

    public FullTimeScreen(IState state, IFixtureHelper fixtureHelper) : base(state)
    {
        State = state;
        FixtureHelper = fixtureHelper;
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
        var fixture = State.TodaysFixtures.Where(p => p.HomeTeam == State.MyTeam || p.AwayTeam == State.MyTeam).First();
        var homeTeam = State.Teams.Where(p => p == fixture.HomeTeam).First();
        var awayTeam = State.Teams.Where(p => p == fixture.AwayTeam).First();

        Console.WriteLine($"{homeTeam,53}{fixture.GoalsHome,5} v {fixture.GoalsAway,-5}{awayTeam,-53}\n{"** FULL TIME **",67}\n");

        var homeTeamPlayers = State.Teams.Where(p => p == homeTeam).First().TacticSlots;
        var awayTeamPlayers = State.Teams.Where(p => p == awayTeam).First().TacticSlots;

        for (int i = 0; i < 18; i++)
        {
            if (i == 11)
            {
                Console.WriteLine(string.Format("{0, 58}{1,-58}", "------------", "   ------------"));
            }

            var tacticSlotHome = homeTeamPlayers.ElementAt(i);
            var homePlayer = "EMPTY SLOT";
            if (tacticSlotHome.Player != null)
            {
                homePlayer = $"{tacticSlotHome.Player.Name,55}{tacticSlotHome.Player.ShirtNumber,3}";
            }

            var tacticSlotAway = awayTeamPlayers.ElementAt(i);
            var awayPlayer = "EMPTY SLOT";
            if (tacticSlotAway.Player != null)
            {
                awayPlayer = $"{tacticSlotAway.Player.ShirtNumber,-3}{tacticSlotAway.Player.Name,-55}";
            }

            Console.WriteLine($"{homePlayer}   {awayPlayer}");
        }
    }
}
