using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class FixtureScreen(
    IState state,
    IMatchSimulatorHelper matchSimulator) : BaseScreen(state)
{
    public override ScreenType Screen => ScreenType.Fixture;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                HandleAdvanceInput();
                break;
            case "B":
                state.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    private void HandleAdvanceInput()
    {
        state.ScreenStack.Push(new Screen
        {
            Type = ScreenType.PreMatch
        });

        var todaysFixtures = state.TodaysFixtures.SelectMany(p => p.Fixtures);
        foreach (var fixture in todaysFixtures)
        {
            matchSimulator.PrepareMatch(fixture);
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
        var groupedFixtures = state.TodaysFixtures;
        foreach(var group in groupedFixtures)
        {
            var leagueName = state.Leagues.First(p => p.Id == group.LeagueId).Name;
            Console.WriteLine(leagueName);
            foreach (var fixture in group.Fixtures)
            {
                var homeClub = state.Clubs.Where(p => p.Id == fixture.HomeClub.Id).First();
                var awayClub = state.Clubs.Where(p => p.Id == fixture.AwayClub.Id).First();
                Console.WriteLine($"{homeClub.Name,48} v {awayClub.Name,-48}{"3PM KO",21}");
            }
            Console.WriteLine("\n");
        }
    }
}
