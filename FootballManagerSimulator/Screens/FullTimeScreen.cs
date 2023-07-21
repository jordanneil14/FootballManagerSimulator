using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

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
                State.CurrentScreen.Type = ScreenType.Main;
                break;
            case "B":
                State.CurrentScreen.Type = ScreenType.PostMatchScores;
                break;
            case "C":
                State.CurrentScreen.Type = ScreenType.PostMatchLeagueTable;
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

        Console.WriteLine($"{homeTeam,40}{fixture.GoalsHome,5} v {fixture.GoalsAway,-5}{awayTeam,-40}\n");

        var homeTeamPlayers = State.Teams.Where(p => p == homeTeam).First().TacticSlots;
        var awayTeamPlayers = State.Teams.Where(p => p == awayTeam).First().TacticSlots;

        for (int i = 0; i < 11; i++)
        {
            var homePlayer = homeTeamPlayers.ElementAt(i).Player?.ToString() ?? "EMPTY SLOT";
            var awayPlayer = awayTeamPlayers.ElementAt(i).Player?.ToString() ?? "EMPTY SLOT";
            Console.WriteLine($"{homePlayer + " " + (i + 1),45}   {i + 1 + " " + awayPlayer,-45}");
        }
    }
}
