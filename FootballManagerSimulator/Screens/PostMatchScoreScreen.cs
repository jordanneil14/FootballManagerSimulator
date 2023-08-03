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
            case "B":
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
        Console.WriteLine("B) Back");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Today's Fixtures\n");
        var groupedFixtures = State.TodaysFixtures;
        foreach (var group in groupedFixtures)
        {
            Console.WriteLine(group.Competition.Name);
            foreach (var fixture in group.Fixtures)
            {
                var homeTeam = State.Clubs.Where(p => p == fixture.HomeTeam).First();
                var awayTeam = State.Clubs.Where(p => p == fixture.AwayTeam).First();
                Console.WriteLine($"{homeTeam,48} v {awayTeam,-48}{"3PM KO",21}");
            }
            Console.WriteLine("\n");
        }
    }
}
