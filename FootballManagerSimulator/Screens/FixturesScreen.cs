using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

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

    public override void RenderOptions()
    {
        Console.WriteLine("Options:");
        Console.WriteLine("B) Back");
    }

    public override void RenderSubscreen()
    {
        Console.WriteLine("Fixtures & Results");

        foreach(var round in State.Competitions.SelectMany(p => p.Fixtures).GroupBy(x => x.WeekNumber))
        {
            Console.WriteLine($"\nRound {round.Key}");
            foreach(var fixture in round)
            {
                if (fixture.Concluded)
                {
                    Console.WriteLine($"{fixture.HomeTeam.Name,55}{fixture.GoalsHome!.Value,3} v {fixture.GoalsAway!.Value,-3}{fixture.AwayTeam.Name,-55}");
                }
                else
                {
                    Console.WriteLine($"{fixture.HomeTeam.Name,58} v {fixture.AwayTeam.Name,-58}");
                }
            }
        }
    }
}
