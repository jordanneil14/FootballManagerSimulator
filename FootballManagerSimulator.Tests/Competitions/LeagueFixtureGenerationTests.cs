using FootballManagerSimulator.Maui.Competitions.Services;
using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Competitions;

/// <summary>
/// The four league services share the same double round-robin algorithm but differ in their
/// calendars, so each is checked against the same invariants.
/// </summary>
public class LeagueFixtureGenerationTests
{
    private static readonly DateOnly SeasonStart = new(2026, 7, 1);
    private static readonly DateOnly[] CupDates = [new(2026, 8, 4), new(2026, 8, 18), new(2026, 9, 1), new(2026, 9, 15)];

    public static TheoryData<string, int> Cases => new()
    {
        { nameof(LeagueService), 20 },
        { nameof(EnglishPremierLeagueService), 20 },
        { nameof(EnglishFootballLeagueService), 24 },
        { nameof(EnglishNationalLeagueService), 24 },
        { nameof(LeagueService), 18 },
    };

    private static (ICompetitionService service, StateModel state, LeagueModel league) Arrange(string serviceName, int clubCount)
    {
        var state = new StateModel { Date = SeasonStart, MyClubId = 1 };
        state.Clubs.AddRange(Enumerable.Range(1, clubCount).Select(i => TestData.Club(i)));
        state.Competitions.Add(TestData.LeagueCup(CupDates));
        var notifications = new RecordingNotifications();

        ICompetitionService service = serviceName switch
        {
            nameof(LeagueService) => new LeagueService(state, notifications),
            nameof(EnglishPremierLeagueService) => new EnglishPremierLeagueService(state, notifications),
            nameof(EnglishFootballLeagueService) => new EnglishFootballLeagueService(state, notifications),
            nameof(EnglishNationalLeagueService) => new EnglishNationalLeagueService(state, notifications),
            _ => throw new ArgumentOutOfRangeException(nameof(serviceName))
        };

        var league = TestData.League(1, state.Clubs);
        return (service, state, league);
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryClubPlaysEveryOtherClubHomeAndAwayExactlyOnce(string serviceName, int clubCount)
    {
        var (service, _, league) = Arrange(serviceName, clubCount);

        service.GenerateNextRoundOfFixtures(league);

        Assert.Equal(clubCount * (clubCount - 1), league.Fixtures.Count);
        var pairings = league.Fixtures.Select(f => (f.HomeClub.Id, f.AwayClub.Id)).ToList();
        Assert.Equal(pairings.Count, pairings.Distinct().Count());
        Assert.DoesNotContain(pairings, p => p.Item1 == p.Item2);
        foreach (var club in league.Clubs)
        {
            Assert.Equal(clubCount - 1, league.Fixtures.Count(f => f.HomeClub.Id == club.Id));
            Assert.Equal(clubCount - 1, league.Fixtures.Count(f => f.AwayClub.Id == club.Id));
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void EachRoundIsOneMatchdayInWhichEveryClubPlaysOnce(string serviceName, int clubCount)
    {
        var (service, _, league) = Arrange(serviceName, clubCount);

        service.GenerateNextRoundOfFixtures(league);

        var rounds = league.Fixtures.GroupBy(f => f.Round).ToList();
        Assert.Equal(Enumerable.Range(1, 2 * (clubCount - 1)), rounds.Select(r => r.Key).OrderBy(r => r));
        foreach (var round in rounds)
        {
            Assert.Single(round.Select(f => f.Date).Distinct());
            var clubIds = round.SelectMany(f => new[] { f.HomeClub.Id, f.AwayClub.Id }).ToList();
            Assert.Equal(clubCount, clubIds.Count);
            Assert.Equal(clubCount, clubIds.Distinct().Count());
        }
        Assert.Equal(rounds.Count, league.Fixtures.Select(f => f.Date).Distinct().Count());
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void NoClubPlaysMoreThanTwoHomeOrAwayGamesInARow(string serviceName, int clubCount)
    {
        for (var season = 0; season < 25; season++)
        {
            var (service, _, league) = Arrange(serviceName, clubCount);

            service.GenerateNextRoundOfFixtures(league);

            Assert.True(FixtureRuns.LongestHomeOrAwayRun(league.Fixtures) <= 2);
        }
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void MatchdaysAreSaturdaysOrTuesdaysWithinTheSeason(string serviceName, int clubCount)
    {
        var (service, _, league) = Arrange(serviceName, clubCount);

        service.GenerateNextRoundOfFixtures(league);

        var earliest = serviceName == nameof(EnglishPremierLeagueService) ? new DateOnly(2026, 8, 14) : new DateOnly(2026, 8, 1);
        Assert.All(league.Fixtures, f =>
        {
            Assert.Contains(f.Date.DayOfWeek, new[] { DayOfWeek.Saturday, DayOfWeek.Tuesday });
            Assert.InRange(f.Date, earliest, new DateOnly(2027, 4, 22));
            Assert.Equal(new TimeOnly(15, 0), f.KickOffTime);
            Assert.Equal(FixtureType.Base, f.FixtureType);
            Assert.False(f.Concluded);
        });
    }

    [Theory]
    [InlineData(nameof(EnglishPremierLeagueService))]
    [InlineData(nameof(EnglishFootballLeagueService))]
    [InlineData(nameof(EnglishNationalLeagueService))]
    public void EnglishLeaguesAvoidLeagueCupDates(string serviceName)
    {
        var (service, _, league) = Arrange(serviceName, 20);

        service.GenerateNextRoundOfFixtures(league);

        Assert.DoesNotContain(league.Fixtures, f => CupDates.Contains(f.Date));
    }

    [Theory]
    [InlineData(nameof(LeagueService))]
    [InlineData(nameof(EnglishPremierLeagueService))]
    [InlineData(nameof(EnglishFootballLeagueService))]
    [InlineData(nameof(EnglishNationalLeagueService))]
    public void PreMatchReportNamesOppositionTheirLeaguePositionAndBestPlayers(string serviceName)
    {
        var state = new StateModel { Date = SeasonStart, MyClubId = 1 };
        var me = TestData.Club(1, "Mine");
        var them = TestData.Club(2, "Rivals");
        var third = TestData.Club(3, "Third");
        state.Clubs.AddRange([me, them, third]);
        var league = TestData.League(1, state.Clubs);
        league.Fixtures.Add(TestData.Result(them, third, 2, 0)); // Rivals top the table
        state.Competitions.Add(league);
        state.Players.AddRange(Enumerable.Range(1, 5).Select(i => TestData.Player(i, them.Id, rating: 60 + i)));
        var notifications = new RecordingNotifications();
        ICompetitionService service = serviceName switch
        {
            nameof(LeagueService) => new LeagueService(state, notifications),
            nameof(EnglishPremierLeagueService) => new EnglishPremierLeagueService(state, notifications),
            nameof(EnglishFootballLeagueService) => new EnglishFootballLeagueService(state, notifications),
            _ => new EnglishNationalLeagueService(state, notifications)
        };

        service.GeneratePreMatchReportForFixture(new FixtureModel { HomeClub = them, AwayClub = me });

        var report = Assert.Single(notifications.Sent);
        Assert.Equal("Pre-Match Report", report.Subject);
        Assert.Contains("Rivals", report.Message);
        Assert.Contains("1st", report.Message);
        Assert.Contains("Player 5", report.Message);
        Assert.Contains("Player 3", report.Message);
        Assert.DoesNotContain("Player 2", report.Message); // only the top three
    }
}
