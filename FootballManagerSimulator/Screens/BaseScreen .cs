using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Interfaces;

namespace FootballManagerSimulator.Screens;

public abstract class BaseScreen(IState state) : IBaseScreen
{
    private readonly IState State = state;
    public abstract ScreenType Screen { get; }
    public abstract IDictionary<string, string> Options { get; }

    public int OptionIndex { get; set; }
	public abstract string? OptionPrompt { get; }
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

    public void RenderOptions()
    {
		for (var i = 0; i < Options.Count(); i++)
        {
            if (OptionIndex == i)
			{
				Console.WriteLine($"> {Options.ElementAt(i).Value.ToUpper()}");
			}
			else 
            {
				Console.WriteLine($"{Options.ElementAt(i).Value}");
			}
		}

		if (!string.IsNullOrWhiteSpace(OptionPrompt))
			Console.Write(OptionPrompt);
	}

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
        var nextFixture = State.Competitions
            .SelectMany(p => p.Fixtures)
            .Where(p => p.Date >= State.Date && (p.HomeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id || p.AwayClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id))
            .OrderBy(p => p.Date)
            .FirstOrDefault();

        if (nextFixture == null) return "Season Complete";

        var competition = State.Competitions.First(p => p.Fixtures.Contains(nextFixture));

        var clubAgainst = nextFixture.HomeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id ? nextFixture.AwayClub : nextFixture.HomeClub;

        if (nextFixture.Date == State.Date && nextFixture.Concluded)
            return $"Last Match: Today {nextFixture.HomeClub.Name} {nextFixture.GoalsHome} v {nextFixture.GoalsAway} {nextFixture.AwayClub.Name}";

        if (nextFixture.Date == State.Date) return $"Next Match: {competition.Name} Vs {clubAgainst.Name} today";

        var diff = nextFixture.Date.DayNumber - State.Date.DayNumber;
        if (diff == 1)
            return $"Next Match: {competition.Name} Vs {clubAgainst.Name} tomorrow";

        return $"Next Match: {competition.Name} vs {clubAgainst.Name} in {nextFixture.Date.DayNumber - State.Date.DayNumber} days";
    }
}
