using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class FixtureScreen : BaseScreen
{
    private readonly IState State;
    private readonly ITacticHelper TacticHelper;
    private readonly IMatchSimulator MatchSimulator;

    public FixtureScreen(
        IState state, 
        ITacticHelper tacticHelper,
        IMatchSimulator matchSimulator) : base(state)
    {
        State = state;
        TacticHelper = tacticHelper;
        MatchSimulator = matchSimulator;
    }

    public override ScreenType Screen => ScreenType.Fixture;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.PreMatch
                });

                var todaysFixtures = State.TodaysFixtures.SelectMany(p => p.Fixtures);
                foreach(var fixture in todaysFixtures)
                {
                    MatchSimulator.PrepareMatch(fixture);
                }
                break;
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("A) Advance");
        Console.WriteLine("B) Back");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Today's Fixtures\n");
        var groupedFixtures = State.TodaysFixtures;
        foreach(var group in groupedFixtures)
        {
            var leagueName = State.Leagues.First(p => p.Id == group.LeagueId).Name;
            Console.WriteLine(leagueName);
            foreach (var fixture in group.Fixtures)
            {
                var homeClub = State.Clubs.Where(p => p.Id == fixture.HomeClub.Id).First();
                var awayClub = State.Clubs.Where(p => p.Id == fixture.AwayClub.Id).First();
                Console.WriteLine($"{homeClub.Name,48} v {awayClub.Name,-48}{"3PM KO",21}");
            }
            Console.WriteLine("\n");
        }
    }
}
