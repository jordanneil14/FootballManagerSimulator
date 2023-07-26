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
                foreach(var fixture in State.TodaysFixtures)
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
        if (State.MyTeam.TacticSlots
            .Where(p => new[] { TacticSlotType.GK, TacticSlotType.FWD, TacticSlotType.MID, TacticSlotType.DEF}.Contains(p.TacticSlotType))
            .Any(p => p.Player == null))
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
        var fixture = State.TodaysFixtures.Where(p => p.HomeTeam == State.MyTeam || p.AwayTeam == State.MyTeam).First();
        var homeTeam = State.Teams.Where(p => p == fixture.HomeTeam).First();
        var awayTeam = State.Teams.Where(p => p == fixture.AwayTeam).First();

        Console.WriteLine($"{homeTeam, 40} v {awayTeam, -40}\n");

        var homeTeamPlayers = State.Teams.Where(p => p == homeTeam).First().TacticSlots;
        var awayTeamPlayers = State.Teams.Where(p => p == awayTeam).First().TacticSlots;

        for (int i = 0; i < 11; i++)
        {
            var homePlayer = homeTeamPlayers.ElementAt(i).Player?.ToString() ?? "EMPTY SLOT";
            var awayPlayer = awayTeamPlayers.ElementAt(i).Player?.ToString() ?? "EMPTY SLOT";
            Console.WriteLine($"{homePlayer + " " + (i+1), 40}   {i+1 + " " + awayPlayer, -40}");
        }
    }
}
