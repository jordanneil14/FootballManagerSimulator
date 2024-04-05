using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class HalfTimeScreen : BaseScreen
{
    private readonly IState State;
    private readonly IMatchSimulator MatchSimulator;
    private readonly IPlayerHelper PlayerHelper;

    public HalfTimeScreen(IState state, 
        IMatchSimulator matchSimulator,
        IPlayerHelper playerHelper) : base(state)
    {
        State = state;
        MatchSimulator = matchSimulator;
        PlayerHelper = playerHelper;
    }

    public override ScreenType Screen => ScreenType.HalfTime;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                foreach (var fixture in State.TodaysFixtures.SelectMany(p => p.Fixtures))
                {
                    MatchSimulator.ProcessMatch(fixture);
                }
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.FullTime
                });
                break;
            case "B":
                State.ScreenStack.Push(new Structures.Screen
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

    public override void RenderSubscreen()
    {
        var fixture = State.TodaysFixtures.SelectMany(p => p.Fixtures).Where(p => p.HomeClub == State.MyClub || p.AwayClub == State.MyClub).ToList().First();
        var homeClub = State.Clubs
            .Where(p => p == fixture.HomeClub)
            .First();

        var awayClub = State.Clubs
            .Where(p => p == fixture.AwayClub)
            .First();

        Console.WriteLine($"{homeClub.Name,53}{fixture.GoalsHome,5} v {fixture.GoalsAway,-5}{awayClub.Name,-53}\n{"** HALF TIME **",67}\n");

        var homeClubPlayers = State.Clubs
            .Where(p => p == homeClub)
            .First().TacticSlots;

        var awayClubPlayers = State.Clubs
            .Where(p => p == awayClub)
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
                homePlayer = $"{player.Name,55}{player.ShirtNumber,3}";
            }

            var tacticSlotAway = awayClubPlayers.ElementAt(i);
            if (tacticSlotAway.PlayerId != null)
            {
                var player = PlayerHelper.GetPlayerById(tacticSlotAway.PlayerId.Value)!;
                awayPlayer = $"{player.ShirtNumber,-3}{player.Name,-55}";
            }

            Console.WriteLine($"{homePlayer}   {awayPlayer}");
        }
    }
}
