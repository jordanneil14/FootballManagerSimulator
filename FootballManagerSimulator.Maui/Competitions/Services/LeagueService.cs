using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

public class LeagueService(IState state, INotificationFactory notificationFactory) : ICompetitionService
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public void GeneratePreMatchReportForFixture(FixtureModel fixture)
    {
        LeaguePreMatchReport.Send(State, NotificationFactory, fixture);
    }


    private IEnumerable<DateOnly> GetFixtureDates(int fixtureCount)
    {
        var startDate = new DateOnly(State.Date.Year, 8, 1);
        var endDate = new DateOnly(State.Date.Year + 1, 4, 22);

        //var excludedDates = new List<DateOnly>() { new( state.Date.Year, 12, 25) };

        var availableSaturdays = new List<DateOnly>();
        var availableTuesdays = new List<DateOnly>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            if (date.DayOfWeek == DayOfWeek.Saturday)
                availableSaturdays.Add(date);

            if (date.DayOfWeek == DayOfWeek.Tuesday)
                availableTuesdays.Add(date);
        }

        var remainingFixtureCount = fixtureCount - availableSaturdays.Count();

        var random = new Random();

        var randomDates = availableTuesdays
            .OrderBy(_ => random.Next())
            .Take(remainingFixtureCount)
            .ToList();

        return availableSaturdays.Concat(randomDates).OrderBy(p => p);
    }

	public void GenerateNextRoundOfFixtures(ICompetition competition)
	{
		competition.Fixtures = LeagueScheduler.CreateFixtures(competition.Clubs, 2, GetFixtureDates);
	}
}
