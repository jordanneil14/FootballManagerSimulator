using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Maui.Competitions.Services;

// Every club plays every other three times (twice at home and once away, or the reverse).
// The table then splits in half and each half plays a further round amongst itself.
public class ScottishPremiershipService(IState state, INotificationFactory notificationFactory) : ICompetitionService
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

	// Saturdays from August to the end of March, with Tuesdays only if there are too few.
	private List<DateOnly> GetFixtureDates(int fixtureCount)
	{
		var startDate = new DateOnly(State.Date.Year, 8, 1);
		var endDate = new DateOnly(State.Date.Year + 1, 3, 31);

		var availableSaturdays = new List<DateOnly>();
		var availableTuesdays = new List<DateOnly>();

		for (var date = startDate; date <= endDate; date = date.AddDays(1))
		{
			if (date.DayOfWeek == DayOfWeek.Saturday)
				availableSaturdays.Add(date);

			if (date.DayOfWeek == DayOfWeek.Tuesday)
				availableTuesdays.Add(date);
		}

		var saturdays = availableSaturdays
			.OrderBy(_ => RandomNumberHelper.Next())
			.Take(fixtureCount)
			.ToList();

		var tuesdays = availableTuesdays
			.OrderBy(_ => RandomNumberHelper.Next())
			.Take(fixtureCount - saturdays.Count);

		return [.. saturdays.Concat(tuesdays).OrderBy(p => p)];
	}

	public void GenerateNextRoundOfFixtures(ICompetition competition)
	{
		competition.Fixtures = LeagueScheduler.CreateFixtures(competition.Clubs, 3, GetFixtureDates);
	}

	// Splits the table in half after the first three rounds of meetings. Each half plays one
	// more round amongst itself on the following Saturdays. No club plays more than two home or
	// away games in a row; within that, each pairing is levelled at two home games each where
	// possible. Returns the date of the last match.
	public DateOnly GenerateSplitFixtures(LeagueModel league)
	{
		var table = league.GenerateLeagueTable().Select(p => p.Club).ToList();
		var splitSize = league.AutomaticPromotionPlaces + league.PlayoffPlaces;

		league.TopSplitClubIds = table.Take(splitSize).Select(p => p.Id).ToList();

		var played = league.Fixtures.OrderBy(p => p.Round).ToList();
		var halves = new[] { table.Take(splitSize).ToList(), table.Skip(splitSize).ToList() };
		var splitRounds = halves.Select(half => SplitRounds(half, played)).ToList();
		var roundCount = splitRounds.Max(p => p.Count);

		var firstSaturday = State.Date;
		while (firstSaturday.DayOfWeek != DayOfWeek.Saturday)
			firstSaturday = firstSaturday.AddDays(1);

		var lastRound = league.Fixtures.Max(p => p.Round);

		for (var round = 0; round < roundCount; round++)
		{
			foreach (var (home, away) in splitRounds.Where(p => round < p.Count).SelectMany(p => p[round]))
			{
				league.Fixtures.Add(new FixtureModel
				{
					HomeClub = home,
					AwayClub = away,
					Round = lastRound + round + 1,
					Date = firstSaturday.AddDays(7 * round),
					KickOffTime = new TimeOnly(15, 00)
				});
			}
		}

		return firstSaturday.AddDays(7 * (roundCount - 1));
	}

	private static List<List<(ClubModel Home, ClubModel Away)>> SplitRounds(List<ClubModel> half, List<FixtureModel> played)
	{
		var history = half
			.Select(club => (IReadOnlyList<bool>)played
				.Where(p => p.HomeClub.Id == club.Id || p.AwayClub.Id == club.Id)
				.Select(p => p.HomeClub.Id == club.Id)
				.ToList())
			.ToList();

		int Hosted(int a, int b) => played.Count(p => p.HomeClub.Id == half[a].Id && p.AwayClub.Id == half[b].Id);

		var rounds = LeagueScheduler.Continue(history, Hosted, RandomNumberHelper.Next)
			?? throw new InvalidOperationException("No split schedule keeps every club within two home or away games in a row.");

		return rounds.Select(r => r.Select(m => (half[m.Home], half[m.Away])).ToList()).ToList();
	}
}
