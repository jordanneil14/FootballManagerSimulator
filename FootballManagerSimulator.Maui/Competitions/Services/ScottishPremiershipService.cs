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

		var message = $"I've generate a pre-match report for the upcoming fixture against {oppositionClub.Name}.\n" +
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
		var league = (LeagueModel)competition;

		var firstRoundRobin = SingleRoundRobin(league.Clubs.OrderBy(_ => RandomNumberHelper.Next()).ToList());
		var returnRoundRobin = firstRoundRobin.Select(round => round.Select(p => (Home: p.Away, Away: p.Home)).ToList());

		// Shuffled afresh, so who gets the extra home game against whom is independent of the first two meetings.
		var thirdRoundRobin = SingleRoundRobin(league.Clubs.OrderBy(_ => RandomNumberHelper.Next()).ToList());

		var rounds = firstRoundRobin
			.Concat(returnRoundRobin)
			.Concat(thirdRoundRobin)
			.OrderBy(_ => RandomNumberHelper.Next())
			.ToList();

		var fixtureDates = GetFixtureDates(rounds.Count);

		competition.Fixtures = rounds
			.SelectMany((round, index) => round.Select(p => new FixtureModel
			{
				HomeClub = p.Home,
				AwayClub = p.Away,
				Round = index + 1,
				Date = fixtureDates[index],
				KickOffTime = new TimeOnly(15, 00)
			}))
			.OrderBy(p => p.Round)
			.ThenBy(p => p.HomeClub.Name)
			.ToList();
	}

	// Splits the table in half after the first three rounds of meetings. Each half plays one
	// more round amongst itself on the following Saturdays, at the ground of whichever club
	// has had fewer home games in that pairing so far. Returns the date of the last match.
	public DateOnly GenerateSplitFixtures(LeagueModel league)
	{
		var table = league.GenerateLeagueTable().Select(p => p.Club).ToList();
		var splitSize = league.AutomaticPromotionPlaces + league.PlayoffPlaces;

		league.TopSplitClubIds = table.Take(splitSize).Select(p => p.Id).ToList();

		var halves = new[] { table.Take(splitSize).ToList(), table.Skip(splitSize).ToList() };
		var splitRounds = halves.Select(SingleRoundRobin).ToList();
		var roundCount = splitRounds.Max(p => p.Count);

		var firstSaturday = State.Date;
		while (firstSaturday.DayOfWeek != DayOfWeek.Saturday)
			firstSaturday = firstSaturday.AddDays(1);

		var lastRound = league.Fixtures.Max(p => p.Round);

		for (var round = 0; round < roundCount; round++)
		{
			foreach (var pairing in splitRounds.Where(p => round < p.Count).SelectMany(p => p[round]))
			{
				var (home, away) = HomeGames(league, pairing.Home, pairing.Away) <= HomeGames(league, pairing.Away, pairing.Home)
					? (pairing.Home, pairing.Away)
					: (pairing.Away, pairing.Home);

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

	private static int HomeGames(LeagueModel league, ClubModel home, ClubModel away) =>
		league.Fixtures.Count(p => p.HomeClub.Id == home.Id && p.AwayClub.Id == away.Id);

	// Circle method for an even number of clubs: each meets every other once over n - 1 rounds.
	// A pairing is played at the lower-placed club's ground when the gap between the two
	// positions is odd, otherwise at the higher one's, giving everyone n/2 or n/2 - 1 home games.
	public static List<List<(ClubModel Home, ClubModel Away)>> SingleRoundRobin(List<ClubModel> clubs)
	{
		var count = clubs.Count;
		var rotating = Enumerable.Range(1, count - 1).ToList();
		var rounds = new List<List<(ClubModel Home, ClubModel Away)>>();

		for (var round = 0; round < count - 1; round++)
		{
			var order = new List<int> { 0 };
			order.AddRange(rotating.Skip(round).Concat(rotating.Take(round)));

			var matches = new List<(ClubModel Home, ClubModel Away)>();
			for (var i = 0; i < count / 2; i++)
			{
				var low = Math.Min(order[i], order[count - 1 - i]);
				var high = Math.Max(order[i], order[count - 1 - i]);

				matches.Add((high - low) % 2 == 1 ? (clubs[low], clubs[high]) : (clubs[high], clubs[low]));
			}

			rounds.Add(matches);
		}

		return rounds;
	}
}
