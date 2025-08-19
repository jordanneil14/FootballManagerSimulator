using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class FixtureScreen(
    IState state,
    IMatchSimulatorHelper matchSimulator) : BaseScreen(state)
{
    private readonly IState State = state;
    private readonly IMatchSimulatorHelper MatchSimulator = matchSimulator;

    public override ScreenType Screen => ScreenType.Fixture;

    public override Dictionary<string, string> Options => new() {
        { "A", "Advance" },
        { "B", "Back" }
    };

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
        var includesMyClub = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Any(p => p.Date == State.Date && (p.HomeClub.Id == State.MyClubId || p.AwayClub.Id == State.MyClubId));



        if (includesMyClub)
        {
            State.ScreenStack.Push(new Screen
            {
                Type = ScreenType.PreMatch
            });

            var todaysFixtures = State.Competitions
                .SelectMany(p => p.Fixtures)
                .Where(p => p.Date == State.Date);

            foreach (var fixture in todaysFixtures)
            {
                MatchSimulator.PrepareMatch(fixture);
            }
            return;
        }

        foreach (var comp in State.Competitions)
        {
            var todaysFixtures = comp.Fixtures.Where(p => p.Date == State.Date);
            foreach (var fixture in todaysFixtures)
            {
                MatchSimulator.PrepareMatch(fixture);
                MatchSimulator.ConcludeFixture(fixture, comp);
            }
        }

        State.ScreenStack.Push(new Screen
        {
            Type = ScreenType.PostMatchScores
        });
    }

    //public override void RenderOptions()
    //{
    //    Console.WriteLine("Options:");
    //    Console.WriteLine("A) Advance");
    //    Console.WriteLine("B) Back");
    //}

    public override void RenderSubscreen()
    {
        Console.WriteLine("Today's Fixtures\n");

        foreach (var comp in State.Competitions)
        {
            var todaysFixtures = comp.Fixtures.Where(p => p.Date == State.Date);
            if (!todaysFixtures.Any()) continue;
            Console.WriteLine(comp.Name);
            foreach (var fixture in todaysFixtures)
            {
                var homeClub = State.Clubs.Where(p => p.Id == fixture.HomeClub.Id).First();
                var awayClub = State.Clubs.Where(p => p.Id == fixture.AwayClub.Id).First();
                var kickOffTime = fixture.KickOffTime.ToString("HH:mm");

                Console.WriteLine($"{homeClub.Name,48} v {awayClub.Name,-48}{$"{kickOffTime} KO",21}");
            }
            Console.WriteLine("\n");
        }
    }
}
