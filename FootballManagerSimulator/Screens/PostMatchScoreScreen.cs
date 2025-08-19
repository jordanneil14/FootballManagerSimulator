using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Screens;

public class PostMatchScoreScreen(IState state) : BaseScreen(state)
{
    private readonly IState State = state;

    public override ScreenType Screen => ScreenType.PostMatchScores;

    public override Dictionary<string, string> Options => new() {
        { "A", "Continue" }
    };

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "A":
                State.ScreenStack.Push(new Screen
                {
                    Type = ScreenType.Main
                });
                break;
            default:
                break;
        }
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Today's Results\n");
        foreach (var comp in State.Competitions)
        {

            var todaysFixtures = comp.Fixtures
                .Where(p => p.Date == State.Date);

            if (!todaysFixtures.Any()) continue;
            Console.WriteLine(comp.Name);
            foreach (var fixture in todaysFixtures)
            {
                var homeClub = State.Clubs
                    .Where(p => p.Id == fixture.HomeClub.Id)
                    .First();

                var awayClub = State.Clubs
                    .Where(p => p.Id == fixture.AwayClub.Id)
                    .First();

                var kickOffTime = fixture.KickOffTime.ToString("HH:mm");
                Console.WriteLine($"{homeClub.Name,45}{fixture.GoalsHome,3} v {fixture.GoalsAway,-3}{awayClub.Name,-35}{(fixture.Concluded ? "" : "(Latest)"),-5}{$"{kickOffTime} KO",21}");

            }
            Console.WriteLine("\n");
        }
    }
}
