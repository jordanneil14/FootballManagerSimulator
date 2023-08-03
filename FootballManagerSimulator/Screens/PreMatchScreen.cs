using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class PreMatchScreen : BaseScreen
{
    private readonly IState State;
    private readonly IMatchSimulator MatchSimulator;

    public PreMatchScreen(IState state, IMatchSimulator matchSimulator) : base(state)
    {
        State = state;
        MatchSimulator = matchSimulator;
    }

    public override ScreenType Screen => ScreenType.PreMatch;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                var result = ValidateStartMatch();
                if (result != null)
                {
                    State.UserFeedbackUpdates.Add(result);
                    return;
                }
                foreach(var fixture in State.TodaysFixtures.SelectMany(p => p.Fixtures))
                {
                    MatchSimulator.SimulateFirstHalf(fixture);
                }
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.HalfTime
                });
                break;
            case "B":
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.Tactics
                });
                break;
            case "C":
                State.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    private string? ValidateStartMatch()
    {
        var positions = State.MyClub.TacticSlots.Where(p => p.TacticSlotType != TacticSlotType.SUB && p.TacticSlotType != TacticSlotType.RES);
        if (positions.Where(p => p.Player == null).Any())
        {
            return "Unable to start game. Your team has not been fully selected";
        }
        return null;
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Start Match");
        Console.WriteLine("B) Tactics");
        Console.WriteLine("C) Back");
    }

    public override void RenderSubscreen()
    {
        var fixture = State.TodaysFixtures.SelectMany(p => p.Fixtures).Where(p => p.HomeTeam == State.MyClub || p.AwayTeam == State.MyClub).First();
        var homeTeam = State.Clubs.Where(p => p == fixture.HomeTeam).First();
        var awayTeam = State.Clubs.Where(p => p == fixture.AwayTeam).First();

        Console.WriteLine($"{homeTeam, 58} v {awayTeam, -58}\n");

        var homeTeamPlayers = State.Clubs.Where(p => p == homeTeam).First().TacticSlots;
        var awayTeamPlayers = State.Clubs.Where(p => p == awayTeam).First().TacticSlots;

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
                homePlayer = $"{tacticSlotHome.Player.Name, 55}{tacticSlotHome.Player.ShirtNumber, 3}";
            }

            var tacticSlotAway = awayTeamPlayers.ElementAt(i);
            var awayPlayer = "EMPTY SLOT";
            if (tacticSlotAway.Player != null)
            {
                awayPlayer = $"{tacticSlotAway.Player.ShirtNumber,-3}{tacticSlotAway.Player.Name, -55}";
            }

            Console.WriteLine($"{homePlayer,58}   {awayPlayer,-58}");
        }
    }
}
