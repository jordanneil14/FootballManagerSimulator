using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class HalfTimeScreen : BaseScreen
{
    private readonly IState State;
    private readonly IMatchSimulator MatchSimulator;

    public HalfTimeScreen(IState state, IMatchSimulator matchSimulator) : base(state)
    {
        State = state;
        MatchSimulator = matchSimulator;
    }

    public override ScreenType Screen => ScreenType.HalfTime;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                foreach (var fixture in State.TodaysFixtures)
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
        var fixture = State.TodaysFixtures.Where(p => p.HomeTeam == State.MyTeam || p.AwayTeam == State.MyTeam).First();
        var homeTeam = State.Teams.Where(p => p == fixture.HomeTeam).First();
        var awayTeam = State.Teams.Where(p => p == fixture.AwayTeam).First();

        Console.WriteLine($"{homeTeam,40}{fixture.GoalsHome,5} v {fixture.GoalsAway,-5}{awayTeam,-40}\n");

        var homeTeamPlayers = State.Teams.Where(p => p == homeTeam).First().TacticSlots;
        var awayTeamPlayers = State.Teams.Where(p => p == awayTeam).First().TacticSlots;

        for (int i = 0; i < 11; i++)
        {
            var homePlayer = homeTeamPlayers.ElementAt(i).Player?.ToString() ?? "EMPTY SLOT";
            var awayPlayer = awayTeamPlayers.ElementAt(i).Player?.ToString() ?? "EMPTY SLOT";
            Console.WriteLine($"{homePlayer + " " + (i+1),45}   {i+1 + " " + awayPlayer,-45}");
        }
    }
}
