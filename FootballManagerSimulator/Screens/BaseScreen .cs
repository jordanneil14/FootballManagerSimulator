using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public abstract class BaseScreen(IState state) : IBaseScreen
{
    private readonly IState State = state;

    public abstract ScreenType Screen { get; }

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
        foreach (var update in State.UserFeedbackUpdates)
        {
            Console.WriteLine($"** {update} **");
        }
        if (State.UserFeedbackUpdates.Count != 0)
        {
            Console.WriteLine("\n");
        }
    }

    public abstract void RenderOptions();

    public void RenderTop()
    {
        var nextMatchCaption = GetNextMatchCaption();

        Console.WriteLine($"{State.Clubs.First(p => p.Id == State.MyClubId).Name,-95}{State.DateFriendly,25}");
        Console.WriteLine($"{State.ManagerName,-100}{State.Weather,20}");
        Console.WriteLine(nextMatchCaption);
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");
    }

    private string GetNextMatchCaption()
    {
        var nextFixture = State.Competitions.SelectMany(p => p.Fixtures)
            .Where(p => p.Date >= State.Date && (p.HomeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id || p.AwayClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id))
            .OrderBy(p => p.Date)
        .FirstOrDefault();

        if (nextFixture == null) return "Season Complete";

        var comp = State.Competitions.First(p => p.Fixtures.Contains(nextFixture));

        var clubAgainst = nextFixture.HomeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id ? nextFixture.AwayClub : nextFixture.HomeClub;

        if (nextFixture.Date == State.Date && nextFixture.Concluded)
        {
            return $"Last Match: Today {nextFixture.HomeClub.Name} {nextFixture.GoalsHome} v {nextFixture.GoalsAway} {nextFixture.AwayClub.Name}";
        }

        if (nextFixture.Date == State.Date) return $"Next Match: {comp.Name} Vs {clubAgainst.Name} today";

        var diff = nextFixture.Date.DayNumber - State.Date.DayNumber;
        if (diff == 1)
            return $"Next Match: {comp.Name} Vs {clubAgainst.Name} tomorrow";

        return $"Next Match: {comp.Name} vs {clubAgainst.Name} in {nextFixture.Date.DayNumber - State.Date.DayNumber} days";
    }
}
