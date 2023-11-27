using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Factories;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Structures;

namespace FootballManagerSimulator.Screens;

public class FixturesScreen : BaseScreen
{
    private readonly IState State;

    public FixturesScreen(IState state) : base(state)
    {
        State = state;
    }

    public override ScreenType Screen => ScreenType.Fixtures;

    public override void HandleInput(string input)
    {
        switch (input)
        {
            case "B":
                State.ScreenStack.Pop();
                break;
            default:
                break;
        }
    }

    public static Screen CreateScreen(ICompetition competition)
    {
        return new Screen
        {
            Type = ScreenType.Fixtures,
            Parameters = new FixturesScreenObj
            {
                Competition = competition 
            }
        };
    }

    public class FixturesScreenObj
    {
        public ICompetition Competition { get; set; }
    }

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Fixtures & Results");

        var parameters = State.ScreenStack.Peek().Parameters as FixturesScreenObj;

        var rounds = State.Competitions
            .Where(p => p.ID == parameters.Competition.ID)
            .SelectMany(p => p.Fixtures)
            .GroupBy(x => x.WeekNumber);

        foreach (var round in rounds)
        {
            Console.WriteLine($"\nRound {round.Key}");
            foreach(var fixture in round)
            {
                if (fixture.Concluded)
                {
                    Console.WriteLine($"{fixture.HomeClub.Name,55}{fixture.GoalsHome!.Value,3} v {fixture.GoalsAway!.Value,-3}{fixture.AwayClub.Name,-55}");
                    continue;
                }

                Console.WriteLine($"{fixture.HomeClub.Name,58} v {fixture.AwayClub.Name,-58}");
            }
        }
    }
}
