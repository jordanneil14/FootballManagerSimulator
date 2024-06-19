using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public abstract class BaseScreen(IState state) : IBaseScreen
{
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
        foreach(var update in state.UserFeedbackUpdates)
        {
            Console.WriteLine($"** {update} **");
        }
        if (state.UserFeedbackUpdates.Any())
        {
            Console.WriteLine("\n");
        }
    }

    public abstract void RenderOptions();

    public void RenderTop()
    {
        var nextMatchCaption = GetNextMatchCaption();

        Console.WriteLine($"{state.Clubs.First(p => p.Id == state.MyClubId).Name,-100}{state.Date,20}");
        Console.WriteLine($"{state.ManagerName,-100}{state.Weather,20}");
        Console.WriteLine(nextMatchCaption);
        Console.WriteLine("------------------------------------------------------------------------------------------------------------------------");
    }

    private string GetNextMatchCaption()
    {
        var nextFixture = state.Competitions.SelectMany(p => p.Fixtures)
            .Where(p => p.Date >= state.Date && (p.HomeClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id || p.AwayClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id))
            .OrderBy(p => p.Date)
            .FirstOrDefault();

        if (nextFixture == null) return "Season Complete";
        var clubAgainst = nextFixture.HomeClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id ? nextFixture.AwayClub : nextFixture.HomeClub;

        if (nextFixture.Date == state.Date && nextFixture.Concluded)
        {
            return $"Last Match: Today {nextFixture.HomeClub.Name} {nextFixture.GoalsHome} v {nextFixture.GoalsAway} {nextFixture.AwayClub.Name}";
        }

        if (nextFixture.Date == state.Date) return $"Next Match: Today Vs {clubAgainst.Name}";

        var diff = nextFixture.Date.DayNumber - state.Date.DayNumber;
        if (diff == 1)
            return $"Next Match: Tomorrow Vs {clubAgainst.Name}";       
                
        return $"Next Match: {nextFixture.Date.DayNumber - state.Date.DayNumber} days Vs {clubAgainst.Name}";
    }
}
