using FootballManagerSimulator.Enums;
using FootballManagerSimulator.Helpers;
using FootballManagerSimulator.Interfaces;
using FootballManagerSimulator.Models;
using FootballManagerSimulator.Structures;
using Microsoft.Extensions.Options;

namespace FootballManagerSimulator.Factories;

public class LeagueFactory(
    IOptions<Settings> settings,
    IState state,
    INotificationFactory notificationFactory) : ICompetitionFactory
{
    private readonly Settings Settings = settings.Value;

    public CompetitionType Type => CompetitionType.League;

    public class RandomFixture
    {
        public int WeekNumber { get; set; }
        public DateOnly Date { get; set; }
    }

    public ICompetition CreateCompetition(CompetitionModel competition)
    {
        var clubs = Settings.Clubs
            .Where(p => p.LeagueId == competition.Id)
            .Select(p => new Club
            {
                Id = p.Id,
                Name = p.Name
            });

        if (clubs == null || !clubs.Any())
            throw new Exception($"Unable to get clubs by leagueResourceId {competition.Id}");

        var league = new League()
        {
            Id = competition.Id,
            Name = competition.Name,
            Rank = competition.Rank,
            Clubs = clubs.ToList()
        };

        GenerateNextRoundOfFixtures(league);

        return league;
    }

    public void GeneratePreMatchReportForFixture(Fixture fixture)
    {
        var oppositionClub = fixture.HomeClub.Id == state.Clubs.First(p => p.Id == state.MyClubId).Id
            ? state.Clubs.First(p => p.Id == fixture.AwayClub.Id)
            : state.Clubs.First(p => p.Id == fixture.HomeClub.Id);

        var league = state.Competitions.First(p => p.Id == state.Clubs.First(p => p.Id == state.MyClubId).LeagueId) as League;
        var leagueTable = league.GenerateLeagueTable().ToList();

        var leaguePosition = leagueTable.First(p => p.Club.Id == oppositionClub.Id);
        var leaguePositionIndex = leagueTable.IndexOf(leaguePosition) + 1;

        var oppositionPlayers = state.Players
            .Where(p => p.Contract != null && p.Contract.ClubId == oppositionClub.Id)
            .OrderByDescending(p => p.Rating)
            .Take(3)
            .Select(p => p.Name);

        var message = $"I've generate a pre-match report for the upcoming fixture against {oppositionClub.Name}.\n" +
            $"They sit {NumberHelper.AddOrdinal(leaguePositionIndex)} in the league and have numerous players who can cause a threat:\n" +
            $"\t{string.Join("\n\t", oppositionPlayers)}";

        notificationFactory.AddNotification(
            state.Date,
            "Club Analyst",
            "Pre-Match Report",
            message);
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

        var date = new DateOnly(2016, 08, 05);

        var randomHelpers = new List<RandomFixture>();
        for (int i = 1; i <= numRounds * 2; i++)
        {
            randomHelpers.Add(new RandomFixture
            {
                WeekNumber = i,
                Date = date
            });
            date = date.AddDays(7);
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

            for (int idx = 1; idx < halfSize; idx++)
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

            date = date.AddDays(7);
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

            date = date.AddDays(7);
        }

        competition.Fixtures = output.OrderBy(p => p.Round).ThenBy(p => p.HomeClub.Name).ToList();
    }
}
