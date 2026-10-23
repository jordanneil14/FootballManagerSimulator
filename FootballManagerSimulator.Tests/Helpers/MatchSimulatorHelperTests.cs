using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Interfaces;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class MatchSimulatorHelperTests
{
    private const int Iterations = 40;

    private readonly StateModel _state = TestData.StateWithClubs(3, myClubId: 3);
    private readonly TacticHelper _tactics;
    private readonly MatchSimulatorHelper _simulator;
    private ClubModel Home => _state.Clubs[0];
    private ClubModel Away => _state.Clubs[1];

    public MatchSimulatorHelperTests()
    {
        var players = new PlayerHelper(_state);
        _tactics = new TacticHelper(_state, players);
        _simulator = new MatchSimulatorHelper(new ClubHelper(_state), _tactics, players, _state);
        foreach (var club in _state.Clubs)
        {
            _tactics.ResetTacticForClub(club);
            _tactics.FillEmptyTacticSlotsByClubId(club.Id);
        }
    }

    private (FixtureModel fixture, ICompetition competition) NewFixture(CompetitionType type, FixtureType fixtureType = FixtureType.Base)
    {
        var fixture = new FixtureModel { HomeClub = Home, AwayClub = Away, Date = _state.Date, FixtureType = fixtureType };
        ICompetition competition = type switch
        {
            CompetitionType.Friendly => new FriendlyModel { Id = 50 },
            CompetitionType.Cup or CompetitionType.EnglishCommunityShield => new CupModel { Id = 50, Type = type },
            _ => TestData.League(50, _state.Clubs, type)
        };
        competition.Fixtures.Add(fixture);
        _state.Competitions.Clear();
        _state.Competitions.Add(competition);
        return (fixture, competition);
    }

    private HashSet<int?> StarterIds(ClubModel club) => club.TacticSlots
        .Where(s => s.TacticSlotType is not (TacticSlotType.SUB or TacticSlotType.RES))
        .Select(s => s.PlayerId)
        .ToHashSet();

    [Fact]
    public void ConcludeFixture_LeagueMatch_ScoreMatchesScorersAndWinnerIsConsistent()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var (fixture, competition) = NewFixture(CompetitionType.League);

            _simulator.ConcludeFixture(fixture, competition);

            Assert.True(fixture.Concluded);
            Assert.Equal(90, fixture.Minute);
            Assert.Equal(fixture.HomeScorers.Count, fixture.GoalsHome);
            Assert.Equal(fixture.AwayScorers.Count, fixture.GoalsAway);
            Assert.All(fixture.HomeScorers.Concat(fixture.AwayScorers), g => Assert.InRange(g.Minute, 1, 90));
            var expectedWinner = fixture.GoalsHome > fixture.GoalsAway ? Home : fixture.GoalsAway > fixture.GoalsHome ? Away : null;
            Assert.Same(expectedWinner, fixture.ClubWon);
            Assert.All(fixture.HomeScorers, g => Assert.Contains(g.PlayerId, StarterIds(Home)));
            Assert.All(fixture.AwayScorers, g => Assert.Contains(g.PlayerId, StarterIds(Away)));
        }
    }

    [Theory]
    [InlineData(CompetitionType.Cup)]
    [InlineData(CompetitionType.EnglishCommunityShield)]
    public void ConcludeFixture_KnockoutMatchAlwaysProducesAWinner(CompetitionType type)
    {
        for (var i = 0; i < Iterations; i++)
        {
            var (fixture, competition) = NewFixture(type);

            _simulator.ConcludeFixture(fixture, competition);

            Assert.True(fixture.Concluded);
            Assert.NotNull(fixture.ClubWon);
            Assert.NotEqual(fixture.GoalsHome, fixture.GoalsAway);
        }
    }

    [Fact]
    public void ConcludeFixture_ExtraTimeRunsTheFullThirtyMinutes()
    {
        var extraTimeMatches = 0;
        var lateGoals = 0;

        for (var i = 0; i < 400; i++)
        {
            var (fixture, competition) = NewFixture(CompetitionType.Cup);
            _simulator.ConcludeFixture(fixture, competition);

            if (fixture.Minute != 120)
                continue;

            var goals = fixture.HomeScorers.Concat(fixture.AwayScorers).ToList();
            Assert.All(goals, g => Assert.InRange(g.Minute, 1, 120));
            extraTimeMatches++;
            if (goals.Any(g => g.Minute > 100))
                lateGoals++;
        }

        // Extra time used to stop after a single step (minute 99 at most). Over the full 30 minutes,
        // roughly half of all extra times have a goal after minute 100.
        Assert.True(extraTimeMatches > 0);
        Assert.True(lateGoals > extraTimeMatches * 0.2, $"{lateGoals} of {extraTimeMatches} extra times had a goal after minute 100");
    }

    [Fact]
    public void ProcessMatch_AdvancesHalfByHalf()
    {
        var (fixture, competition) = NewFixture(CompetitionType.League);

        _simulator.ProcessMatch(fixture, competition);
        Assert.Equal(45, fixture.Minute);
        Assert.False(fixture.Concluded);
        Assert.All(fixture.HomeScorers.Concat(fixture.AwayScorers), g => Assert.InRange(g.Minute, 1, 45));

        _simulator.ProcessMatch(fixture, competition);
        Assert.Equal(90, fixture.Minute);
        Assert.True(fixture.Concluded);
    }

    [Fact]
    public void ConcludeFixture_WhenAwaySideHasNoPlayersEveryGoalGoesToTheHomeSide()
    {
        Away.TacticSlots.ForEach(s => s.PlayerId = null);

        for (var i = 0; i < Iterations; i++)
        {
            var (fixture, competition) = NewFixture(CompetitionType.League);
            _simulator.ConcludeFixture(fixture, competition);
            Assert.Equal(0, fixture.GoalsAway);
            Assert.Empty(fixture.AwayScorers);
        }
    }

    [Fact]
    public void PreGenerateMatch_GeneratesRegulationTimeEventsWithoutConcluding()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.League);

            _simulator.PreGenerateMatch(fixture);

            Assert.False(fixture.Concluded);
            Assert.Equal(0, fixture.GoalsHome);
            Assert.Equal(0, fixture.GoalsAway);
            Assert.All(fixture.HomeScorers.Concat(fixture.AwayScorers), g => Assert.InRange(g.Minute, 1, 90));
            Assert.All(fixture.HomeCards.Concat(fixture.AwayCards), c => Assert.InRange(c.Minute, 1, 90));
            Assert.All(fixture.HomeCards, c => Assert.Contains(c.PlayerId, StarterIds(Home)));
        }
    }

    [Fact]
    public void GenerateExtraTimeAndPenaltyEvents_AreAfterNinetyMinutes()
    {
        var (fixture, _) = NewFixture(CompetitionType.Cup);

        _simulator.GenerateExtraTimeEvents(fixture);
        _simulator.GeneratePenaltyEvents(fixture);

        var goals = fixture.HomeScorers.Concat(fixture.AwayScorers).ToList();
        Assert.All(goals, g => Assert.InRange(g.Minute, 91, 121));
        Assert.Single(goals, g => g.Minute == 121); // exactly one penalty-shootout winner
    }

    [Fact]
    public void ReGenerateFromMinute_KeepsEventsUpToThatMinuteAndRegeneratesTheRest()
    {
        var (fixture, _) = NewFixture(CompetitionType.League);
        var early = new GoalModel { Minute = 20, PlayerId = Home.TacticSlots[9].PlayerId!.Value };
        var late = new GoalModel { Minute = 80, PlayerId = Home.TacticSlots[9].PlayerId!.Value };
        fixture.HomeScorers.AddRange([early, late]);
        fixture.HomeCards.Add(new CardModel { Minute = 85, PlayerId = Home.TacticSlots[1].PlayerId!.Value });

        _simulator.ReGenerateFromMinute(fixture, 60);

        Assert.Contains(early, fixture.HomeScorers);
        Assert.DoesNotContain(late, fixture.HomeScorers);
        Assert.All(fixture.HomeScorers.Concat(fixture.AwayScorers).Where(g => g != early), g => Assert.InRange(g.Minute, 61, 90));
        Assert.All(fixture.HomeCards.Concat(fixture.AwayCards), c => Assert.InRange(c.Minute, 61, 90));
    }

    [Fact]
    public void ReGenerateFromMinute_RegeneratesAPenaltyWinnerIfThereWasOne()
    {
        var (fixture, _) = NewFixture(CompetitionType.Cup);
        fixture.AwayScorers.Add(new GoalModel { Minute = 121, PlayerId = Away.TacticSlots[9].PlayerId!.Value });

        _simulator.ReGenerateFromMinute(fixture, 30);

        Assert.Single(fixture.HomeScorers.Concat(fixture.AwayScorers), g => g.Minute == 121);
    }

    [Fact]
    public void FinaliseMatch_StraightRedBansForThreeMatchesAndSecondYellowForOne()
    {
        var (fixture, competition) = NewFixture(CompetitionType.League);
        var straightRed = _state.Players.First(p => p.Id == Home.TacticSlots[1].PlayerId);
        var twoYellows = _state.Players.First(p => p.Id == Away.TacticSlots[1].PlayerId);
        fixture.GoalsHome = 1;
        fixture.GoalsAway = 0;
        fixture.HomeCards.Add(new CardModel { Minute = 10, PlayerId = straightRed.Id, IsRed = true });
        fixture.AwayCards.AddRange(
        [
            new CardModel { Minute = 20, PlayerId = twoYellows.Id },
            new CardModel { Minute = 70, PlayerId = twoYellows.Id },
            new CardModel { Minute = 70, PlayerId = twoYellows.Id, IsRed = true }
        ]);

        _simulator.FinaliseMatch(fixture, competition);

        Assert.True(fixture.Concluded);
        Assert.Same(Home, fixture.ClubWon);
        Assert.Equal(3, straightRed.SuspensionMatches);
        Assert.Equal(1, twoYellows.SuspensionMatches);
    }

    [Fact]
    public void FinaliseMatch_ServesExistingBansButFriendliesDoNotCount()
    {
        var banned = _state.Players.First(p => p.Contract?.ClubId == Home.Id);
        banned.SuspensionMatches = 2;
        var elsewhere = _state.Players.First(p => p.Contract?.ClubId == 3);
        elsewhere.SuspensionMatches = 2;

        var (friendly, friendlyComp) = NewFixture(CompetitionType.Friendly);
        friendly.GoalsHome = friendly.GoalsAway = 0;
        friendly.HomeCards.Add(new CardModel { Minute = 5, PlayerId = Home.TacticSlots[2].PlayerId!.Value, IsRed = true });
        _simulator.FinaliseMatch(friendly, friendlyComp);
        Assert.Equal(2, banned.SuspensionMatches);
        Assert.Equal(0, _state.Players.First(p => p.Id == Home.TacticSlots[2].PlayerId).SuspensionMatches);

        var (league, leagueComp) = NewFixture(CompetitionType.League);
        league.GoalsHome = league.GoalsAway = 0;
        _simulator.FinaliseMatch(league, leagueComp);
        Assert.Equal(1, banned.SuspensionMatches);
        Assert.Equal(2, elsewhere.SuspensionMatches); // club 3 did not play
        Assert.Null(league.ClubWon);
    }

    [Theory]
    [InlineData(2, 0, 53)]
    [InlineData(1, 1, 50)]
    [InlineData(0, 2, 47)]
    public void FinaliseMatch_MyCompetitiveResultMovesBoardConfidence(int homeGoals, int awayGoals, int expected)
    {
        _state.MyClubId = Home.Id;
        var (fixture, competition) = NewFixture(CompetitionType.League);
        fixture.GoalsHome = homeGoals;
        fixture.GoalsAway = awayGoals;

        _simulator.FinaliseMatch(fixture, competition);
        _simulator.FinaliseMatch(fixture, competition); // a second finalise must not count twice

        Assert.Equal(expected, _state.BoardConfidence);
    }

    [Fact]
    public void FinaliseMatch_UpdatesBothClubsJobSecurityButNotForFriendlies()
    {
        var (friendly, friendlyComp) = NewFixture(CompetitionType.Friendly);
        friendly.GoalsHome = 1;
        friendly.GoalsAway = 0;
        _simulator.FinaliseMatch(friendly, friendlyComp);
        Assert.Equal((50, 50), (Home.JobSecurity, Away.JobSecurity));

        var (league, leagueComp) = NewFixture(CompetitionType.League);
        league.GoalsHome = 2;
        league.GoalsAway = 0;
        _simulator.FinaliseMatch(league, leagueComp);
        _simulator.FinaliseMatch(league, leagueComp);

        Assert.Equal((53, 47), (Home.JobSecurity, Away.JobSecurity));
    }

    [Fact]
    public void FinaliseMatch_AiClubDroppingBelowTenSacksItsManagerAndGoesVacant()
    {
        Away.JobSecurity = 12;

        for (var i = 0; i < 2; i++)
        {
            var (fixture, competition) = NewFixture(CompetitionType.League); // my club (3) is not playing
            fixture.GoalsHome = 1;
            fixture.GoalsAway = 0;
            _simulator.FinaliseMatch(fixture, competition);
        }

        Assert.Equal(6, Away.JobSecurity); // not reset while the job is vacant
        Assert.InRange(Away.VacantUntil!.Value.DayNumber - _state.Date.DayNumber,
            BoardConfidenceRules.VacancyMinDays, BoardConfidenceRules.VacancyMaxDays);
        var news = Assert.Single(_state.Notifications); // sacked once, not again while vacant
        Assert.Equal($"{Away.Name} sack their manager", news.Subject);
        Assert.False(news.RequiresAction);
    }

    [Fact]
    public void FinaliseMatch_MyClubReachingZeroIsNotTreatedAsAnAiSacking()
    {
        _state.MyClubId = Away.Id;
        Away.JobSecurity = 3;
        var (fixture, competition) = NewFixture(CompetitionType.League);
        fixture.GoalsHome = 1;
        fixture.GoalsAway = 0;

        _simulator.FinaliseMatch(fixture, competition);

        Assert.Equal(0, Away.JobSecurity);
        Assert.Null(Away.VacantUntil);
        Assert.DoesNotContain(_state.Notifications, n => n.Subject.EndsWith("sack their manager"));
    }

    [Fact]
    public void FinaliseMatch_DroppingIntoAtRiskSendsOneBlockingWarning()
    {
        _state.MyClubId = Home.Id;
        _state.BoardConfidence = BoardConfidenceRules.UnderPressureFrom + 1;

        for (var i = 0; i < 2; i++)
        {
            var (fixture, competition) = NewFixture(CompetitionType.League);
            fixture.GoalsHome = 0;
            fixture.GoalsAway = 1;
            _simulator.FinaliseMatch(fixture, competition);
        }

        var warning = Assert.Single(_state.Notifications);
        Assert.True(warning.IsBoardWarning);
        Assert.True(warning.RequiresAction);
        Assert.Equal("At risk", BoardConfidenceRules.Label(_state.BoardConfidence));
    }

    [Fact]
    public void FinaliseMatch_FriendliesAndOtherClubsMatchesLeaveBoardConfidence()
    {
        var (otherClubs, otherComp) = NewFixture(CompetitionType.League); // my club is 3, not playing
        otherClubs.GoalsHome = 3;
        otherClubs.GoalsAway = 0;
        _simulator.FinaliseMatch(otherClubs, otherComp);

        _state.MyClubId = Home.Id;
        var (friendly, friendlyComp) = NewFixture(CompetitionType.Friendly);
        friendly.GoalsHome = 0;
        friendly.GoalsAway = 4;
        _simulator.FinaliseMatch(friendly, friendlyComp);

        Assert.Equal(BoardConfidenceRules.Starting, _state.BoardConfidence);
    }

    [Fact]
    public void FinaliseMatch_RecordsMyCompetitiveResultsOnceInTheManagerHistory()
    {
        _state.MyClubId = Home.Id;
        _state.ManagerHistory.StartJob(Home, _state.Date);

        var (friendly, friendlyComp) = NewFixture(CompetitionType.Friendly);
        friendly.GoalsHome = 5;
        friendly.GoalsAway = 0;
        _simulator.FinaliseMatch(friendly, friendlyComp);

        var (league, leagueComp) = NewFixture(CompetitionType.League);
        league.GoalsHome = 2;
        league.GoalsAway = 0;
        _simulator.FinaliseMatch(league, leagueComp);
        _simulator.FinaliseMatch(league, leagueComp);

        var job = _state.ManagerHistory.CurrentJob!;
        Assert.Equal((1, 1, 0, 0), (job.Played, job.Won, job.Drawn, job.Lost));
        Assert.Equal((2, 0, 1), (job.GoalsFor, job.GoalsAgainst, job.CleanSheets));
        Assert.Empty(_state.ManagerHistory.Trophies); // a league win is not a trophy
    }

    [Fact]
    public void FinaliseMatch_WinningACupFinalOnPenaltiesAddsATrophyButNotTheShootoutGoal()
    {
        _state.MyClubId = Away.Id;
        _state.ManagerHistory.StartJob(Away, _state.Date);
        var (fixture, competition) = NewFixture(CompetitionType.Cup);
        competition.DrawSettings.AddRange([new DrawSettingsModel { Round = 1 }, new DrawSettingsModel { Round = 2 }]);
        fixture.Round = 2;
        fixture.AwayScorers.Add(new GoalModel { Minute = 121, PlayerId = Away.TacticSlots[9].PlayerId!.Value });
        fixture.GoalsHome = 1;
        fixture.GoalsAway = 2; // 1-1 after extra time, won on penalties

        _simulator.FinaliseMatch(fixture, competition);

        var job = _state.ManagerHistory.CurrentJob!;
        Assert.Equal((1, 1, 1), (job.Won, job.GoalsFor, job.GoalsAgainst));
        var trophy = Assert.Single(_state.ManagerHistory.Trophies);
        Assert.Equal((competition.Name, Away.Name), (trophy.Name, trophy.ClubName));
    }

    [Fact]
    public void FinaliseMatch_WinningAnEarlierCupRoundIsNotATrophy()
    {
        _state.MyClubId = Home.Id;
        _state.ManagerHistory.StartJob(Home, _state.Date);
        var (fixture, competition) = NewFixture(CompetitionType.Cup);
        competition.DrawSettings.AddRange([new DrawSettingsModel { Round = 1 }, new DrawSettingsModel { Round = 2 }]);
        fixture.Round = 1;
        fixture.GoalsHome = 3;
        fixture.GoalsAway = 0;

        _simulator.FinaliseMatch(fixture, competition);

        Assert.Empty(_state.ManagerHistory.Trophies);
    }

    [Fact]
    public void ManagerHistory_TotalsAcrossJobsAndHandlesNoGames()
    {
        var history = new ManagerHistoryModel();
        Assert.Equal((0d, 0d), (history.WinPercentage, history.PointsPerGame));

        history.StartJob(Home, new DateOnly(2026, 7, 1));
        history.RecordResult(2, 1, true);
        history.RecordResult(0, 0, null);
        history.StartJob(Away, new DateOnly(2027, 1, 1));
        history.RecordResult(0, 3, false);
        history.RecordResult(1, 0, true);

        Assert.Equal(2, history.Jobs.Count);
        Assert.Equal((4, 2, 1, 1), (history.Played, history.Won, history.Drawn, history.Lost));
        Assert.Equal(50d, history.WinPercentage);
        Assert.Equal(7 / 4d, history.PointsPerGame);
        Assert.Equal((3, 4, 2), (history.GoalsFor, history.GoalsAgainst, history.CleanSheets));
    }

    [Theory]
    [InlineData(2026, 8, 1, "2026/27")]
    [InlineData(2027, 3, 1, "2026/27")]
    [InlineData(2027, 6, 30, "2026/27")]
    [InlineData(2099, 7, 1, "2099/00")]
    public void ManagerHistory_SeasonLabelRunsJulyToJune(int year, int month, int day, string expected) =>
        Assert.Equal(expected, ManagerHistoryModel.SeasonLabel(new DateOnly(year, month, day)));

    [Theory]
    [InlineData(100, "Secure")]
    [InlineData(75, "Secure")]
    [InlineData(74, "Stable")]
    [InlineData(30, "Stable")]
    [InlineData(29, "Under pressure")]
    [InlineData(15, "Under pressure")]
    [InlineData(14, "At risk")]
    [InlineData(0, "At risk")]
    public void BoardConfidence_LabelsCoverTheirRanges(int confidence, string expected) =>
        Assert.Equal(expected, BoardConfidenceRules.Label(confidence));

    [Fact]
    public void BoardConfidence_StaysWithinZeroAndOneHundred()
    {
        Assert.Equal(100, BoardConfidenceRules.Apply(99, BoardConfidenceRules.Win));
        Assert.Equal(0, BoardConfidenceRules.Apply(1, BoardConfidenceRules.Loss));
    }

    [Fact]
    public void PopulateFirstLegScores_CopiesTheReversedFirstLeg()
    {
        var league = TestData.League(1, _state.Clubs);
        league.Fixtures.Add(new FixtureModel { HomeClub = Away, AwayClub = Home, GoalsHome = 3, GoalsAway = 1, Concluded = true, FixtureType = FixtureType.Playoff });
        var secondLeg = new FixtureModel { HomeClub = Home, AwayClub = Away, FixtureType = FixtureType.PlayoffSecondLeg };

        _simulator.PopulateFirstLegScores(secondLeg, league);

        Assert.Equal(3, secondLeg.FirstLegGoalsHome);
        Assert.Equal(1, secondLeg.FirstLegGoalsAway);
    }

    [Fact]
    public void ConcludeFixture_PlayoffSecondLegIsDecidedOnAggregate()
    {
        for (var i = 0; i < Iterations; i++)
        {
            var (secondLeg, competition) = NewFixture(CompetitionType.EnglishFootballLeague, FixtureType.PlayoffSecondLeg);
            competition.Fixtures.Add(new FixtureModel { HomeClub = Away, AwayClub = Home, GoalsHome = 2, GoalsAway = 0, Concluded = true, FixtureType = FixtureType.Playoff });

            _simulator.ConcludeFixture(secondLeg, competition);

            Assert.True(secondLeg.Concluded);
            var homeAggregate = secondLeg.GoalsHome + secondLeg.FirstLegGoalsAway;
            var awayAggregate = secondLeg.GoalsAway + secondLeg.FirstLegGoalsHome;
            if (secondLeg.Minute < 120) // decided without penalties
                Assert.NotEqual(homeAggregate, awayAggregate);
        }
    }

    [Fact]
    public void PrepareMatch_AutoPicksOpponentsButLeavesMyTeamAlone()
    {
        _state.MyClubId = Home.Id;
        Home.TacticSlots.ForEach(s => s.PlayerId = null); // my deliberately empty lineup
        Away.TacticSlots.ForEach(s => s.PlayerId = null);
        var (fixture, _) = NewFixture(CompetitionType.League);

        _simulator.PrepareMatch(fixture);

        Assert.All(Home.TacticSlots, s => Assert.Null(s.PlayerId));
        Assert.All(StarterIds(Away), id => Assert.NotNull(id));
    }
}
