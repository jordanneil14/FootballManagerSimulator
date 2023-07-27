using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public class PostMatchScoreScreen : BaseScreen
{
    private readonly IState State;

    public PostMatchScoreScreen(IState state) : base(state)
    {
        State = state;
    }

    public override ScreenType Screen => ScreenType.PostMatchScores;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                State.ScreenStack.Push(new Structures.Screen
                {
                    Type = ScreenType.FullTime
                });
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Back");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Todays Fixtures\n");
        foreach (var fixture in State.TodaysFixtures)
        {
            var homeTeam = State.Teams.Where(p => p == fixture.HomeTeam).First();
            var awayTeam = State.Teams.Where(p => p == fixture.AwayTeam).First();
            Console.WriteLine($"{homeTeam,53}{fixture.GoalsHome.GetValueOrDefault(),5} v {fixture.GoalsAway!.GetValueOrDefault(),-5}{awayTeam,-53}");
        }
    }
}
