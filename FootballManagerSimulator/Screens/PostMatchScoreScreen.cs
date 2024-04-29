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
        Console.WriteLine("Today's Results\n");
        var groupedFixtures = State.TodaysFixtures;
        foreach (var group in groupedFixtures)
        {
            var leagueName = State.Leagues.First(p => p.Id == group.LeagueId).Name;
            Console.WriteLine(leagueName);
            foreach (var fixture in group.Fixtures)
            {
                var homeClub = State.Clubs
                    .Where(p => p == fixture.HomeClub)
                    .First();

                var awayClub = State.Clubs
                    .Where(p => p == fixture.AwayClub
                    ).First();

                Console.WriteLine($"{homeClub.Name,45}{fixture.GoalsHome,3} v {fixture.GoalsAway,-3}{awayClub.Name,-35}{(fixture.Concluded ? "" : "(Latest)"),-5}{"3PM KO",21}");
            }
            Console.WriteLine("\n");
        }
    }
}
