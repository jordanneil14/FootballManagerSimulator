using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Helpers;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class HalfTimeScreen : BaseScreen
{
    private readonly IState State;
    private readonly IMatchSimulator MatchSimulator;
    private readonly IUtils Utils;

    public HalfTimeScreen(IState state, 
        IMatchSimulator matchSimulator,
        IUtils utils) : base(state)
    {
        State = state;
        MatchSimulator = matchSimulator;
        Utils = utils;
    }

    public override ScreenType Screen => ScreenType.HalfTime;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                foreach (var fixture in State.TodaysFixtures.SelectMany(p => p.Fixtures))
                {
                    MatchSimulator.SimulateSecondHalf(fixture);
                    fixture.Concluded = true;
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
        var fixture = State.TodaysFixtures.SelectMany(p => p.Fixtures).Where(p => p.HomeClub == State.MyClub || p.AwayClub == State.MyClub).First();
        var homeClub = State.Clubs.Where(p => p == fixture.HomeClub).First();
        var awayClub = State.Clubs.Where(p => p == fixture.AwayClub).First();

        Console.WriteLine($"{homeClub,53}{fixture.GoalsHome,5} v {fixture.GoalsAway,-5}{awayClub,-53}\n{"** HALF TIME **",67}\n");

        var homeClubPlayers = State.Clubs.Where(p => p == homeClub).First().TacticSlots;
        var awayClubPlayers = State.Clubs.Where(p => p == awayClub).First().TacticSlots;

        for (var i = 0; i < 18; i++)
        {
            if (i == 11)
                Console.WriteLine($"{"------------",58}{"   ------------",-58}");

            var homePlayer = "EMPTY SLOT";
            var awayPlayer = "EMPTY SLOT";

            var tacticSlotHome = homeClubPlayers.ElementAt(i);
            if (tacticSlotHome.PlayerID != null)
            {
                var player = Utils.GetPlayerById(tacticSlotHome.PlayerID.Value)!;
                homePlayer = $"{player.Name,55}{player.ShirtNumber,3}";
            }

            var tacticSlotAway = awayClubPlayers.ElementAt(i);
            if (tacticSlotAway.PlayerID != null)
            {
                var player = Utils.GetPlayerById(tacticSlotAway.PlayerID.Value)!;
                awayPlayer = $"{player.ShirtNumber,-3}{player.Name,-55}";
            }

            Console.WriteLine($"{homePlayer}   {awayPlayer}");
        }
    }
}
