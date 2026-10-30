using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

public class EnglishFootballLeagueService(IState state, INotificationFactory notificationFactory) : ICompetitionService
{
	private readonly IState State = state;
	private readonly INotificationFactory NotificationFactory = notificationFactory;

	public void GeneratePreMatchReportForFixture(FixtureModel fixture)
	{
		var oppositionClub = fixture.HomeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id
			? State.Clubs.First(p => p.Id == fixture.AwayClub.Id)
			: State.Clubs.First(p => p.Id == fixture.HomeClub.Id);

		var league = (LeagueModel)State.Competitions.First(p => p.Id == State.Clubs.First(p => p.Id == State.MyClubId).LeagueId);
		var leagueTable = league.GenerateLeagueTable().ToList();

		var leaguePosition = leagueTable.First(p => p.Club.Id == oppositionClub.Id);
		var leaguePositionIndex = leagueTable.IndexOf(leaguePosition) + 1;

		var oppositionPlayers = State.Players
			.Where(p => p.Contract != null && p.Contract.ClubId == oppositionClub.Id)
			.OrderByDescending(p => p.Rating)
			.Take(3)
			.Select(p => p.Name);

		var message = $"I've generated a pre-match report for the upcoming fixture against {oppositionClub.Name}.\n" +
			$"They sit {NumberHelper.AddOrdinal(leaguePositionIndex)} in the league and have numerous players who can cause a threat:\n" +
			$"\t{string.Join("\n\t", oppositionPlayers)}";

		NotificationFactory.AddNotification(
			State.Date,
			"Club Analyst",
			"Pre-Match Report",
			message);
	}


	private IEnumerable<DateOnly> EnglishCupFixtureDates => State.Competitions
		.Where(p => p.Type.IsSeasonLongEnglishCup())
		.SelectMany(p => p.DrawSettings)
		.Select(p => p.FixtureDate);

	private IEnumerable<DateOnly> GetFixtureDates(int fixtureCount)
	{
		var startDate = new DateOnly(State.Date.Year, 8, 1);
		var endDate = new DateOnly(State.Date.Year + 1, 4, 22);

		var availableSaturdays = new List<DateOnly>();
		var availableTuesdays = new List<DateOnly>();

		for (var date = startDate; date <= endDate; date = date.AddDays(1))
		{
			if (date.DayOfWeek == DayOfWeek.Saturday)
				availableSaturdays.Add(date);

			if (date.DayOfWeek == DayOfWeek.Tuesday)
				availableTuesdays.Add(date);
		}

		availableSaturdays = [.. availableSaturdays.Except(EnglishCupFixtureDates)];
		availableTuesdays = [.. availableTuesdays.Except(EnglishCupFixtureDates)];

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
