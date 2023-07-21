using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public abstract class BaseScreen : IBaseScreen
{
    public abstract ScreenType Screen { get; }

    private readonly IState State;

    public BaseScreen(IState state)
    {
        State = state;
    }

    public abstract void HandleInput(string input);

    public abstract void RenderSubscreen();

    public void RenderScreen()
    {
        RenderTop();
        RenderUserFeedbackUpdates();
        RenderSubscreen();
        Console.WriteLine("\n");
        RenderOptions();
    }

    public void RenderUserFeedbackUpdates()
    {
        foreach(var update in State.UserFeedbackUpdates)
        {
            Console.WriteLine($"** {update} **");
        }
        if (State.UserFeedbackUpdates.Any())
        {
            Console.WriteLine("\n");
        }
    }

    public abstract void RenderOptions();

    public void RenderTop()
    {
        var nextMatchCaption = GetNextMatchCaption();

        Console.WriteLine($"{State.MyTeam.Name, -80}{State.Date, -15}");
        Console.WriteLine($"{State.ManagerName, -80}{State.Weather, -15}");
        Console.WriteLine(nextMatchCaption);
        Console.WriteLine("-------------------------------------------------------------------------------------------------");
    }

    private string GetNextMatchCaption()
    {
        var nextFixture = State.Competitions.SelectMany(p => p.Fixtures)
            .Where(p => p.Date >= State.Date && (p.HomeTeam == State.MyTeam || p.AwayTeam == State.MyTeam))
            .OrderBy(p => p.WeekNumber)
            .FirstOrDefault();

        if (nextFixture == null) return "Season Complete";
        var teamAgainst = nextFixture.HomeTeam == State.MyTeam ? nextFixture.AwayTeam : nextFixture.HomeTeam;
        if (nextFixture.Date == State.Date) return $"Next Match In: Today Vs {teamAgainst}";
        return $"Next Match In: {(nextFixture.Date.DayNumber - State.Date.DayNumber)} days Vs {teamAgainst}";

    }
}
