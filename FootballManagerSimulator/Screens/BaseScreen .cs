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

        Console.WriteLine($"{State.MyClub.Name,-100}{State.Date,20}");
        Console.WriteLine($"{State.ManagerName,-100}{State.Weather,20}");
        Console.WriteLine(nextMatchCaption);
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");
    }

    private string GetNextMatchCaption()
    {
        var nextFixture = State.Competitions.SelectMany(p => p.Fixtures)
            .Where(p => p.Date >= State.Date && (p.HomeClub == State.MyClub || p.AwayClub == State.MyClub))
            .OrderBy(p => p.WeekNumber)
            .FirstOrDefault();

        if (nextFixture == null) return "Season Complete";
        var clubAgainst = nextFixture.HomeClub == State.MyClub ? nextFixture.AwayClub : nextFixture.HomeClub;
        if (nextFixture.Date == State.Date && nextFixture.Concluded)
        {
            return $"Last Match: Today {nextFixture.HomeClub} {nextFixture.GoalsHome} v {nextFixture.GoalsAway} {nextFixture.AwayClub}";
        }
        if (nextFixture.Date == State.Date) return $"Next Match: Today Vs {clubAgainst}";
        return $"Next Match: {(nextFixture.Date.DayNumber - State.Date.DayNumber)} days Vs {clubAgainst}";

    }
}
