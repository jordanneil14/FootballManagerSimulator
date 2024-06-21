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
        var includesMyClub = state.Competitions
            .SelectMany(p => p.Fixtures)
            .Any(p => p.Date == state.Date && (p.HomeClub.Id == state.MyClubId || p.AwayClub.Id == state.MyClubId));

        

        if (includesMyClub)
        {
            state.ScreenStack.Push(new Screen
            {
                Type = ScreenType.PreMatch
            });

            var todaysFixtures = state.Competitions
                .SelectMany(p => p.Fixtures)
                .Where(p => p.Date == state.Date);

            foreach (var fixture in todaysFixtures)
            {
                matchSimulator.PrepareMatch(fixture);
            }
            return;
        }

        foreach(var comp in state.Competitions)
        {
            var todaysFixtures = comp.Fixtures.Where(p => p.Date == state.Date);
            foreach (var fixture in todaysFixtures)
            {
                matchSimulator.PrepareMatch(fixture);
                matchSimulator.ConcludeFixture(fixture, comp);
            }
        }

        state.ScreenStack.Push(new Structures.Screen
        {
            Type = ScreenType.PostMatchScores
        });
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

        foreach(var comp in state.Competitions)
        {
            var todaysFixtures = comp.Fixtures.Where(p => p.Date == state.Date);
            if (!todaysFixtures.Any()) continue;
            Console.WriteLine(comp.Name);
            foreach (var fixture in todaysFixtures)
            {
                var homeClub = state.Clubs.Where(p => p.Id == fixture.HomeClub.Id).First();
                var awayClub = state.Clubs.Where(p => p.Id == fixture.AwayClub.Id).First();
                var kickOffTime = fixture.KickOffTime.ToString("HH:mm");

                Console.WriteLine($"{homeClub.Name,48} v {awayClub.Name,-48}{$"{kickOffTime} KO",21}");
            }
            Console.WriteLine("\n");
        }
    }
}
