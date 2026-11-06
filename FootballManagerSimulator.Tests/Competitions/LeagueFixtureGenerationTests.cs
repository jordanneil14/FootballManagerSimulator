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
    [InlineData(nameof(EnglishPremierLeagueService))]
    [InlineData(nameof(EnglishFootballLeagueService))]
    [InlineData(nameof(EnglishNationalLeagueService))]
    public void EnglishLeaguesAvoidFACupDatesTooAndPlayTheMatchesOnTuesdays(string serviceName)
    {
        var (service, state, league) = Arrange(serviceName, 24);
        var faCupDates = Enumerable.Range(1, FACupRules.Rounds).Select(r => FACupRules.PlannedDate(r, 2026)).ToArray();
        state.Competitions.Add(new CupModel
        {
            Id = 20,
            Type = CompetitionType.EnglishFACup,
            DrawSettings = faCupDates.Select((d, i) => new DrawSettingsModel { Round = i + 1, FixtureDate = d }).ToList()
        });

        service.GenerateNextRoundOfFixtures(league);

        Assert.DoesNotContain(league.Fixtures, f => faCupDates.Contains(f.Date) || CupDates.Contains(f.Date));
        Assert.Equal(2 * 23, league.Fixtures.Select(f => f.Date).Distinct().Count());
        Assert.All(league.Fixtures, f => Assert.Contains(f.Date.DayOfWeek, new[] { DayOfWeek.Saturday, DayOfWeek.Tuesday }));
    }

    [Theory]
    [InlineData(nameof(LeagueService))]
    [InlineData(nameof(EnglishPremierLeagueService))]
    [InlineData(nameof(EnglishFootballLeagueService))]
    [InlineData(nameof(EnglishNationalLeagueService))]
    [InlineData(nameof(ScottishPremiershipService))]
    public void PreMatchReportNamesOppositionTheirLeaguePositionAndBestPlayers(string serviceName)
    {
        var state = new StateModel { Date = SeasonStart, MyClubId = 1 };
        state.Analysts.Add(new AnalystModel { Id = 1, Name = "Ann Alyst" });
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
            nameof(ScottishPremiershipService) => new ScottishPremiershipService(state, notifications),
            _ => new EnglishNationalLeagueService(state, notifications)
        };

        service.GeneratePreMatchReportForFixture(new FixtureModel { HomeClub = them, AwayClub = me });

        var report = Assert.Single(notifications.Sent);
        Assert.Equal(("Ann Alyst", "Pre-Match Report: Rivals (A)"), (report.Recipient, report.Subject));
        Assert.StartsWith("Boss, here's what I've got on Rivals ahead of tomorrow's away game.", report.Message);
        Assert.Contains("They're 1st in the league with 3 points, 1 place and 3 points above us.", report.Message);
        Assert.Contains("Player 5", report.Message);
        Assert.Contains("Player 3", report.Message);
        Assert.DoesNotContain("Player 2", report.Message); // only the top three
    }

    [Fact]
    public void PreMatchReport_WorksOutTheOppositionWhenIAmAtHome()
    {
        var state = new StateModel { Date = SeasonStart, MyClubId = 1 };
        state.Analysts.Add(new AnalystModel { Id = 1, Name = "Ann Alyst" });
        var me = TestData.Club(1, "Mine");
        var them = TestData.Club(2, "Rivals");
        state.Clubs.AddRange([me, them]);
        var league = TestData.League(1, state.Clubs);
        league.Fixtures.Add(TestData.Result(me, them, 2, 0));
        state.Competitions.Add(league);
        state.Players.Add(TestData.Player(1, them.Id));
        state.Players.Add(TestData.Player(2, me.Id));
        var notifications = new RecordingNotifications();

        new ScottishPremiershipService(state, notifications).GeneratePreMatchReportForFixture(new FixtureModel { HomeClub = me, AwayClub = them });

        var report = Assert.Single(notifications.Sent);
        Assert.Contains("got on Rivals ahead of tomorrow's home game", report.Message);
        Assert.Contains("They're 2nd in the league with 0 points, 1 place and 3 points below us.", report.Message);
        Assert.Contains("Player 1", report.Message);
        Assert.DoesNotContain("Player 2", report.Message);
    }

    [Theory]
    [InlineData(nameof(EnglishCommunityShieldService), true)]
    [InlineData(nameof(EnglishCommunityShieldService), false)]
    [InlineData(nameof(EnglishLeagueCupService), true)]
    [InlineData(nameof(EnglishLeagueCupService), false)]
    public void CupPreMatchReport_NamesTheCupTheOppositionAndTheirBestPlayers(string serviceName, bool iAmAtHome)
    {
        var state = new StateModel { Date = SeasonStart, MyClubId = 1 };
        state.Analysts.Add(new AnalystModel { Id = 1, Name = "Ann Alyst" });
        var me = TestData.Club(1, "Mine");
        var them = TestData.Club(2, "Rivals");
        state.Clubs.AddRange([me, them]);
        state.Players.AddRange(Enumerable.Range(1, 5).Select(i => TestData.Player(i, them.Id, rating: 60 + i)));
        state.Players.Add(TestData.Player(6, me.Id, rating: 99));
        var fixture = iAmAtHome ? new FixtureModel { HomeClub = me, AwayClub = them } : new FixtureModel { HomeClub = them, AwayClub = me };
        state.Competitions.Add(new CupModel { Id = 7, Name = "Big Cup", Fixtures = [fixture] });
        var notifications = new RecordingNotifications();
        ICompetitionService service = serviceName == nameof(EnglishCommunityShieldService)
            ? new EnglishCommunityShieldService(state, notifications)
            : new EnglishLeagueCupService(Microsoft.Extensions.Options.Options.Create(new SettingsModel()), notifications, state);

        service.GeneratePreMatchReportForFixture(fixture);

        var report = Assert.Single(notifications.Sent);
        Assert.Equal(("Ann Alyst", $"Pre-Match Report: Rivals ({(iAmAtHome ? "H" : "A")})", SeasonStart), (report.Recipient, report.Subject, report.Date));
        Assert.StartsWith($"Boss, here's what I've got on Rivals ahead of tomorrow's {(iAmAtHome ? "home" : "away")} game.", report.Message);
        Assert.Contains("It's a Big Cup tie.", report.Message);
        Assert.Contains("Player 5", report.Message);
        Assert.Contains("Player 3", report.Message);
        Assert.DoesNotContain("Player 2", report.Message);
        Assert.DoesNotContain("Player 6", report.Message);
    }

    [Fact]
    public void PreMatchReport_IsOnlySentWithAnAnalyst()
    {
        var state = new StateModel { Date = SeasonStart, MyClubId = 1 };
        var me = TestData.Club(1, "Mine");
        var them = TestData.Club(2, "Rivals");
        state.Clubs.AddRange([me, them]);
        var league = TestData.League(1, state.Clubs);
        state.Competitions.Add(league);
        var cupFixture = new FixtureModel { HomeClub = me, AwayClub = them };
        state.Competitions.Add(new CupModel { Id = 7, Name = "Big Cup", Fixtures = [cupFixture] });
        var notifications = new RecordingNotifications();

        new LeagueService(state, notifications).GeneratePreMatchReportForFixture(new FixtureModel { HomeClub = me, AwayClub = them });
        new EnglishCommunityShieldService(state, notifications).GeneratePreMatchReportForFixture(cupFixture);

        Assert.Empty(notifications.Sent);
    }

    [Fact]
    public void PreMatchReport_DescribesTheOppositionsFormationAndPlannedApproach()
    {
        var state = new StateModel { Date = SeasonStart, MyClubId = 1 };
        state.Analysts.Add(new AnalystModel { Id = 1, Name = "Ann Alyst" });
        var me = TestData.Club(1, "Mine");
        var them = TestData.Club(2, "Rivals", formation: "3-5-2");
        them.Instructions = new TeamInstructionsModel(); // last match's set-up, which the plan replaces
        state.Clubs.AddRange([me, them]);
        state.Competitions.Add(TestData.League(1, state.Clubs));
        var fixture = new FixtureModel
        {
            HomeClub = me,
            AwayClub = them,
            AwayPlannedInstructions = new TeamInstructionsModel { Mentality = Mentality.Defensive, Pressing = Pressing.High }
        };
        var notifications = new RecordingNotifications();

        new LeagueService(state, notifications).GeneratePreMatchReportForFixture(fixture);

        var report = Assert.Single(notifications.Sent).Message;
        Assert.Contains("HOW THEY'LL SET UP\n• 3-5-2\n• Defensive: they'll sit back", report);
        Assert.Contains("• High press:", report);
        Assert.Contains("we may need to be more attacking", report);
        Assert.Contains("hit them on the counter, or press them ourselves", report);
        Assert.DoesNotContain("gospel", report);
    }

    [Fact]
    public void PreMatchReport_GivesFormStrengthMissingPlayersGoalsAndTiredStarters()
    {
        var state = new StateModel { Date = SeasonStart, MyClubId = 1 };
        state.Analysts.Add(new AnalystModel { Id = 1, Name = "Ann Alyst" });
        var me = TestData.Club(1, "Mine");
        var them = TestData.Club(2, "Rivals");
        var third = TestData.Club(3, "Third");
        state.Clubs.AddRange([me, them, third]);
        var league = TestData.League(1, state.Clubs);
        var day = SeasonStart.AddDays(-10);
        foreach (var (home, away, gh, ga) in new[] { (them, third, 1, 0), (third, them, 2, 0), (them, third, 1, 1) })
        {
            var result = TestData.Result(home, away, gh, ga);
            result.Date = day = day.AddDays(1);
            league.Fixtures.Add(result);
        }
        state.Competitions.Add(league);

        // Their XI is far stronger than ours. Their best player is out injured.
        var theirs = TestData.AddSquad(state, them, 100, baseRating: 80);
        var injured = theirs.MaxBy(p => p.Rating)!;
        injured.InjuryEndDate = SeasonStart.AddDays(10);
        var striker = theirs.Where(p => p != injured).MaxBy(p => p.Rating)!;
        league.Fixtures[0].HomeScorers.Add(new GoalModel { Minute = 10, PlayerId = striker.Id });
        league.Fixtures[2].HomeScorers.Add(new GoalModel { Minute = 20, PlayerId = striker.Id });
        // A later friendly win with a goal from the striker counts towards neither the form nor the goals.
        var friendly = TestData.Result(them, third, 5, 0);
        friendly.Date = day.AddDays(1);
        friendly.HomeScorers.Add(new GoalModel { Minute = 30, PlayerId = striker.Id });
        state.Competitions.Add(new FriendlyModel { Id = 50, Fixtures = [friendly] });
        var mine = TestData.AddSquad(state, me, 200, baseRating: 60);
        mine[1].Rating = mine[2].Rating = 99; // sure to be in our XI
        mine[1].Fitness = 62.4;
        mine[2].Fitness = 70;
        var fixture = new FixtureModel { HomeClub = me, AwayClub = them, Date = SeasonStart.AddDays(1) };
        var notifications = new RecordingNotifications();

        new LeagueService(state, notifications).GeneratePreMatchReportForFixture(fixture);

        var report = Assert.Single(notifications.Sent).Message;
        Assert.Contains("Form: W L D.", report);
        Assert.Contains("On paper their likely XI is clearly stronger than ours.", report);
        Assert.Contains($"Not available: {injured.Name} ({injured.PreferredPosition}, injured).", report);
        Assert.Contains($"• {striker.Name} ({striker.PreferredPosition}): 2 goals this season", report);
        Assert.DoesNotContain($"• {injured.Name}", report);
        Assert.Contains($"{mine[1].Name} (62 fitness) and {mine[2].Name} (70) are still feeling the last game.", report);
    }
}
