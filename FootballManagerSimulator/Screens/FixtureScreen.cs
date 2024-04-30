using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class FixtureScreen : BaseScreen
{
    private readonly IState State;
    private readonly IMatchSimulatorHelper MatchSimulator;

    public FixtureScreen(
        IState state, 
        IMatchSimulatorHelper matchSimulator) : base(state)
    {
        State = state;
        MatchSimulator = matchSimulator;
    }

    public override ScreenType Screen => ScreenType.Fixture;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                HandleAdvanceInput();
                break;
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    private void HandleAdvanceInput()
    {
        State.ScreenStack.Push(new Screen
        {
            Type = ScreenType.PreMatch
        });

        var todaysFixtures = State.TodaysFixtures.SelectMany(p => p.Fixtures).Where(p => p.HomeClub.Id != State.MyClub.Id && p.AwayClub.Id != State.MyClub.Id);
        foreach (var fixture in todaysFixtures)
        {
            MatchSimulator.PrepareMatch(fixture);
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
