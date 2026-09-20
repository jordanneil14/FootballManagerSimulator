using FootballManagerSimulator.Helpers;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;

namespace FootballManagerSimulator.Services;

public class PremierLeagueService(
    IState state,
    INotificationFactory notificationFactory) : ICompetitionService
{
    private readonly IState State = state;
    private readonly INotificationFactory NotificationFactory = notificationFactory;

    public void GeneratePreMatchReportForFixture(Fixture fixture)
    {
        var oppositionClub = fixture.HomeClub.Id == State.Clubs.First(p => p.Id == State.MyClubId).Id
            ? State.Clubs.First(p => p.Id == fixture.AwayClub.Id)
            : State.Clubs.First(p => p.Id == fixture.HomeClub.Id);

        var league = State.Competitions.First(p => p.Id == State.Clubs.First(p => p.Id == State.MyClubId).LeagueId) as League;
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

    public class RandomFixture
    {
        public int WeekNumber { get; set; }
        public DateOnly Date { get; set; }
    }

    private IEnumerable<DateOnly> GetFixtureDates(int fixtureCount)
    {
        var startDate = new DateOnly(State.Date.Year, 8, 14);
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
        var league = (League)competition;

        var output = new List<Fixture>();

        var numRounds = league.Clubs.Count() - 1;
        var halfSize = league.Clubs.Count() / 2;

        var clubIndices = new List<Club>(league.Clubs);

        clubIndices.RemoveAt(0);

        var clubIdxSize = clubIndices.Count;

        //var date = new DateOnly(State.Date.Year, 8, 1);

        var fixtureDates = GetFixtureDates(numRounds * 2);

        var randomHelpers = new List<RandomFixture>();
        for (var i = 1; i <= numRounds * 2; i++)
        {
            randomHelpers.Add(new RandomFixture
            {
                WeekNumber = i,
                Date = fixtureDates.ElementAt(i - 1)
            });
        }
        randomHelpers = randomHelpers.OrderBy(p => RandomNumberHelper.Next()).ToList();

        for (var round = 0; round < numRounds; round++)
        {
            var clubIdx = round % clubIdxSize;

            var randomHelper = randomHelpers.First();
            randomHelpers.Remove(randomHelper);

            output.Add(new Fixture
            {
                HomeClub = league.Clubs.ElementAt(0),
                AwayClub = clubIndices[clubIdx],
                Round = randomHelper.WeekNumber,
                Date = randomHelper.Date,
                KickOffTime = new TimeOnly(15, 00)
            });

            for (var idx = 1; idx < halfSize; idx++)
            {
                var firstClubIdx = (round + idx) % clubIdxSize;
                var secondClubIdx = (round + clubIdxSize - idx) % clubIdxSize;

                output.Add(new Fixture
                {
                    HomeClub = clubIndices[firstClubIdx],
                    AwayClub = clubIndices[secondClubIdx],
                    Round = randomHelper.WeekNumber,
                    Date = randomHelper.Date,
                    KickOffTime = new TimeOnly(15, 00)
                });
            }
        }

        for (var round = 0; round < numRounds; round++)
        {
            var randomHelper = randomHelpers.First();
            randomHelpers.Remove(randomHelper);

            var clubIdx = round % clubIdxSize;

            output.Add(new Fixture
            {
                HomeClub = clubIndices[clubIdx],
                AwayClub = league.Clubs.ElementAt(0),
                Round = randomHelper.WeekNumber,
                Date = randomHelper.Date,
                KickOffTime = new TimeOnly(15, 00)
            });

            for (var idx = 1; idx < halfSize; idx++)
            {
                var firstClubIdx = (round + idx) % clubIdxSize;
                var secondClubIdx = (round + clubIdxSize - idx) % clubIdxSize;

                output.Add(new Fixture
                {
                    HomeClub = clubIndices[secondClubIdx],
                    AwayClub = clubIndices[firstClubIdx],
                    Round = randomHelper.WeekNumber,
                    Date = randomHelper.Date,
                    KickOffTime = new TimeOnly(15, 00)
                });
            }
        }

        competition.Fixtures = output
            .OrderBy(p => p.Round)
            .ThenBy(p => p.HomeClub.Name)
            .ToList();
    }
}
