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
        _simulator = new MatchSimulatorHelper(new ClubHelper(_state), _tactics, players, _state, new RecordingNotifications { Now = _state.Date });
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
            CompetitionType.Cup or CompetitionType.EnglishCommunityShield or CompetitionType.EnglishFACup => new CupModel { Id = 50, Type = type },
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

    // Everyone who took the pitch for a side: its starters and any substitutes (an AI side replaces the injured).
    private static HashSet<int?> PlayedIds(FixtureModel fixture, bool isHome) =>
        (isHome ? fixture.HomePlayerIds : fixture.AwayPlayerIds).Select(id => (int?)id).ToHashSet();

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
            Assert.All(fixture.HomeScorers, g => Assert.Contains(g.PlayerId, PlayedIds(fixture, true)));
            Assert.All(fixture.AwayScorers, g => Assert.Contains(g.PlayerId, PlayedIds(fixture, false)));
        }
    }

    [Theory]
    [InlineData(CompetitionType.Cup)]
    [InlineData(CompetitionType.EnglishCommunityShield)]
    [InlineData(CompetitionType.EnglishFACup)]
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
            Assert.All(fixture.HomeCards, c => Assert.Contains(c.PlayerId, PlayedIds(fixture, true)));
        }
    }

    [Fact]
    public void Cards_GoalkeepersAreBookedAtMostOnceAndNeverSentOff()
    {
        var keeperIds = _state.Clubs
            .SelectMany(c => c.TacticSlots)
            .Where(s => s.TacticSlotType == TacticSlotType.GK)
            .Select(s => s.PlayerId!.Value)
            .ToHashSet();
        var keeperBookings = 0;

        for (var i = 0; i < 2000; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.League);
            _simulator.PreGenerateMatch(fixture);

            var keeperCards = fixture.HomeCards.Concat(fixture.AwayCards).Where(c => keeperIds.Contains(c.PlayerId)).ToList();
            Assert.DoesNotContain(keeperCards, c => c.IsRed);
            Assert.All(keeperCards.GroupBy(c => c.PlayerId), g => Assert.Single(g));
            keeperBookings += keeperCards.Count;
        }

        Assert.True(keeperBookings > 0, "Goalkeepers should still be booked sometimes.");
    }

    // The user's live matches are pre-generated, so they must roll injuries like simulated matches do.
    [Fact]
    public void PreGenerateMatch_GeneratesInjuriesAtTheSimulatedRate()
    {
        const int matches = 500;
        int preGenerated = 0, simulated = 0;

        for (var i = 0; i < matches; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.League);
            _simulator.PreGenerateMatch(fixture);

            Assert.All(fixture.HomeInjuries.Concat(fixture.AwayInjuries), injury => Assert.InRange(injury.Minute, 1, 90));
            Assert.All(fixture.HomeInjuries, injury => Assert.Contains(injury.PlayerId, PlayedIds(fixture, true)));
            preGenerated += fixture.HomeInjuries.Count + fixture.AwayInjuries.Count;

            var (other, competition) = NewFixture(CompetitionType.League);
            _simulator.ConcludeFixture(other, competition);
            simulated += other.HomeInjuries.Count + other.AwayInjuries.Count;
        }

        Assert.InRange(preGenerated, simulated / 2, simulated * 2);
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

    // ----- The side on the pitch at each minute ---------------------------------------------

    private (TacticSlotModel Starter, int BenchPlayerId) HomeStrikerAndBenchPlayer()
    {
        var starter = Home.TacticSlots[9];
        var bench = Home.TacticSlots.First(s => s.TacticSlotType == TacticSlotType.SUB && s.PlayerId != null);
        return (starter, bench.PlayerId!.Value);
    }

    [Fact]
    public void KickOff_KeepsTheAISidesStartingSlotsButNotMine()
    {
        var mine = _state.Clubs[2];
        _tactics.ResetTacticForClub(mine);
        _tactics.FillEmptyTacticSlotsByClubId(mine.Id);
        var fixture = new FixtureModel { HomeClub = Home, AwayClub = mine, Date = _state.Date };

        _simulator.PreGenerateMatch(fixture);

        Assert.Equal(StarterIds(Home), fixture.HomeStartingSlots.Where(s => s.TacticSlotType != TacticSlotType.SUB).Select(s => s.PlayerId).ToHashSet());
        Assert.Equal(
            Home.TacticSlots.Where(s => s.TacticSlotType == TacticSlotType.SUB).Select(s => s.PlayerId).ToHashSet(),
            fixture.HomeStartingSlots.Where(s => s.TacticSlotType == TacticSlotType.SUB).Select(s => s.PlayerId).ToHashSet());
        Assert.Empty(fixture.AwayStartingSlots);
    }

    [Fact]
    public void LineupAt_AppliesAnAISubstitutionOnlyAfterItsMinute()
    {
        var (fixture, _) = NewFixture(CompetitionType.League);
        _simulator.PreGenerateMatch(fixture);
        fixture.HomeSubstitutions.Clear(); // any made for injuries at kick-off
        var (starter, benchId) = HomeStrikerAndBenchPlayer();
        var striker = starter.PlayerId!.Value;
        fixture.HomeSubstitutions.Add(new SubstitutionModel { Minute = 60, OffId = striker, OnId = benchId });

        var before = _simulator.LineupAt(fixture, true, 60);
        var after = _simulator.LineupAt(fixture, true, 61);

        Assert.Contains(before, s => s.PlayerId == striker);
        Assert.DoesNotContain(before, s => s.PlayerId == benchId);
        Assert.DoesNotContain(after, s => s.PlayerId == striker);
        Assert.Equal(starter.TacticSlotType, after.Single(s => s.PlayerId == benchId).TacticSlotType);
        Assert.Equal(11, after.Count);
    }

    [Fact]
    public void LineupAt_AnAISideIsItsKickOffXINotWhateverItsSlotsSayLater()
    {
        var (fixture, _) = NewFixture(CompetitionType.League);
        _simulator.PreGenerateMatch(fixture);
        fixture.HomeSubstitutions.Clear(); // any made for injuries at kick-off
        var kickOff = StarterIds(Home);
        var (starter, benchId) = HomeStrikerAndBenchPlayer();

        // Something changes the club's own slots mid-match (the next day's team selection, say).
        Home.TacticSlots.First(s => s.PlayerId == benchId).PlayerId = starter.PlayerId;
        starter.PlayerId = benchId;

        Assert.Equal(kickOff, _simulator.LineupAt(fixture, true, 70).Select(s => s.PlayerId).ToHashSet());
    }

    // Regenerating keeps the AI's substitutions up to the minute it starts from and plays on from there with
    // them made. (Its later ones are redone, so a substitution part-way through what's regenerated is tested
    // with the injury substitutions below.) 63 is not a decision minute.
    [Theory]
    [InlineData(45)]
    [InlineData(60)]
    [InlineData(63)]
    public void ReGenerateFromMinute_PlaysOnWithTheAISubstitutionsMadeSoFar(int subMinute)
    {
        var (starter, benchId) = HomeStrikerAndBenchPlayer();
        var striker = starter.PlayerId!.Value;
        var benchScored = false;

        for (var i = 0; i < Iterations * 5; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.League);
            _simulator.PreGenerateMatch(fixture);
            fixture.HomeSubstitutions.Clear(); // any made for injuries at kick-off
            fixture.HomeSubstitutions.Add(new SubstitutionModel { Minute = subMinute, OffId = striker, OnId = benchId });
            fixture.HomePlayerIds.Add(benchId);

            _simulator.ReGenerateFromMinute(fixture, subMinute);

            Assert.DoesNotContain(fixture.HomeScorers, g => g.PlayerId == striker && g.Minute > subMinute);
            Assert.DoesNotContain(fixture.HomeCards, c => c.PlayerId == striker && c.Minute > subMinute);
            benchScored |= fixture.HomeScorers.Any(g => g.PlayerId == benchId);
        }

        Assert.True(benchScored, "the substitute never scored after coming on");
    }

    [Fact]
    public void FullTime_ClearsTheKeptStartingSlots()
    {
        var (fixture, competition) = NewFixture(CompetitionType.League);

        _simulator.ConcludeFixture(fixture, competition);

        Assert.Empty(fixture.HomeStartingSlots);
        Assert.Empty(fixture.AwayStartingSlots);
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
    public void ReGenerateFromMinute_InTheFirstHalfRegeneratesBothHalves()
    {
        var firstHalfGoals = 0;
        for (var i = 0; i < Iterations; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.League);
            var early = new GoalModel { Minute = 10, PlayerId = Home.TacticSlots[9].PlayerId!.Value };
            fixture.HomeScorers.Add(early);

            _simulator.ReGenerateFromMinute(fixture, 15);

            var regenerated = fixture.HomeScorers.Concat(fixture.AwayScorers).Where(g => g != early).ToList();
            Assert.Contains(early, fixture.HomeScorers);
            Assert.All(regenerated, g => Assert.InRange(g.Minute, 16, 90));
            firstHalfGoals += regenerated.Count(g => g.Minute <= 45);
        }

        Assert.True(firstHalfGoals > 0, $"Expected a regenerated first-half goal across {Iterations} matches");
    }

    [Fact]
    public void ReGenerateFromMinute_DuringExtraTimeRegeneratesTheRestOfExtraTime()
    {
        var lateGoals = 0;
        for (var i = 0; i < Iterations; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.Cup);
            var kept = new GoalModel { Minute = 95, PlayerId = Home.TacticSlots[9].PlayerId!.Value };
            var dropped = new GoalModel { Minute = 110, PlayerId = Away.TacticSlots[9].PlayerId!.Value };
            fixture.HomeScorers.Add(kept);
            fixture.AwayScorers.Add(dropped);

            _simulator.ReGenerateFromMinute(fixture, 100);

            Assert.Contains(kept, fixture.HomeScorers);
            Assert.DoesNotContain(dropped, fixture.AwayScorers);
            var regenerated = fixture.HomeScorers.Concat(fixture.AwayScorers).Where(g => g != kept).ToList();
            Assert.All(regenerated, g => Assert.InRange(g.Minute, 101, 120));
            lateGoals += regenerated.Count;
        }

        Assert.True(lateGoals > 0, $"Expected a regenerated extra-time goal across {Iterations} matches");
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(120, false)]
    public void ReGenerateFromMinute_AddsNoExtraTimeOnceItIsOverOrTheMatchHasFinished(int fromMinute, bool concluded)
    {
        for (var i = 0; i < Iterations; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.Cup);
            fixture.Concluded = concluded;

            _simulator.ReGenerateFromMinute(fixture, fromMinute);

            Assert.Empty(fixture.HomeScorers.Concat(fixture.AwayScorers));
        }
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
    [InlineData(0, 2, 47.5)]
    public void FinaliseMatch_MyCompetitiveResultMovesBoardConfidence(int homeGoals, int awayGoals, double expected)
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

        Assert.Equal((53, 47.5), (Home.JobSecurity, Away.JobSecurity));
    }

    [Fact]
    public void FinaliseMatch_AiClubDroppingBelowTenSacksItsManagerAndGoesVacant()
    {
        _state.ShowManagerNotifications = true;
        Away.JobSecurity = 12;

        for (var i = 0; i < 2; i++)
        {
            var (fixture, competition) = NewFixture(CompetitionType.League); // my club (3) is not playing
            fixture.GoalsHome = 1;
            fixture.GoalsAway = 0;
            _simulator.FinaliseMatch(fixture, competition);
        }

        Assert.Equal(7, Away.JobSecurity); // not reset while the job is vacant
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
        Away.JobSecurity = 2;
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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void FinaliseMatch_WinningAPlayoffFinalIsATrophyButLosingOneIsNot(bool iWin)
    {
        _state.MyClubId = Home.Id;
        _state.ManagerHistory.StartJob(Home, _state.Date);
        var (fixture, competition) = NewFixture(CompetitionType.League, FixtureType.PlayoffFinal);
        fixture.GoalsHome = iWin ? 2 : 0;
        fixture.GoalsAway = iWin ? 0 : 2;

        _simulator.FinaliseMatch(fixture, competition);

        if (iWin)
        {
            var trophy = Assert.Single(_state.ManagerHistory.Trophies);
            Assert.Equal(($"{competition.Name} Play-off Final", Home.Name, _state.Date), (trophy.Name, trophy.ClubName, trophy.Date));
        }
        else
        {
            Assert.Empty(_state.ManagerHistory.Trophies);
        }
    }

    [Fact]
    public void FinaliseMatch_OtherClubsWinningACupFinalIsNotMyTrophy()
    {
        _state.MyClubId = 3;
        _state.ManagerHistory.StartJob(_state.Clubs[2], _state.Date);
        var (fixture, competition) = NewFixture(CompetitionType.Cup);
        fixture.GoalsHome = 1;
        fixture.GoalsAway = 0;

        _simulator.FinaliseMatch(fixture, competition);

        Assert.Empty(_state.ManagerHistory.Trophies);
        Assert.Equal(0, _state.ManagerHistory.Played);
    }

    [Theory]
    [InlineData(FACupRules.Rounds, true)]
    [InlineData(FACupRules.Rounds - 1, false)]
    public void FinaliseMatch_OnlyWinningTheFACupFinalIsATrophy(int round, bool trophy)
    {
        _state.MyClubId = Home.Id;
        _state.ManagerHistory.StartJob(Home, _state.Date);
        var (fixture, competition) = NewFixture(CompetitionType.EnglishFACup);
        competition.DrawSettings.AddRange(Enumerable.Range(1, FACupRules.Rounds).Select(r => new DrawSettingsModel { Round = r }));
        fixture.Round = round;
        fixture.GoalsHome = 1;
        fixture.GoalsAway = 0;

        _simulator.FinaliseMatch(fixture, competition);

        Assert.Equal(trophy, _state.ManagerHistory.Trophies.Any(t => t.Name == competition.Name));
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

    [Theory]
    [InlineData(50, "50/100")]
    [InlineData(47.5, "47.5/100")]
    public void BoardConfidence_FormatsHalfPoints(double confidence, string expected) =>
        Assert.Equal(expected, BoardConfidenceRules.Format(confidence));

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
            Assert.Equal(homeAggregate > awayAggregate ? Home : Away, secondLeg.ClubWon);
        }
    }

    [Theory]
    [InlineData(1, 0, false)] // won the second leg but lost 1-3 on aggregate
    [InlineData(4, 0, true)]  // overturned the first-leg deficit
    [InlineData(3, 1, false)] // 3-3 on aggregate, the away side's shootout goal settles it
    public void FinaliseMatch_PlayoffSecondLegWinnerIsTheAggregateWinner(int homeGoals, int awayGoals, bool homeGoesThrough)
    {
        var (secondLeg, competition) = NewFixture(CompetitionType.EnglishFootballLeague, FixtureType.PlayoffSecondLeg);
        competition.Fixtures.Add(new FixtureModel { HomeClub = Away, AwayClub = Home, GoalsHome = 3, GoalsAway = 0, Concluded = true, FixtureType = FixtureType.Playoff });
        secondLeg.GoalsHome = homeGoals;
        secondLeg.GoalsAway = awayGoals;

        _simulator.FinaliseMatch(secondLeg, competition);

        Assert.Equal(homeGoesThrough ? Home : Away, secondLeg.ClubWon);
    }

    [Fact]
    public void ConcludeFixture_GeneratesInjuriesOverManyMatches()
    {
        var anyInjury = false;
        for (var i = 0; i < 200; i++)
        {
            var (fixture, competition) = NewFixture(CompetitionType.League);
            _simulator.ConcludeFixture(fixture, competition);

            if (fixture.HomeInjuries.Count > 0 || fixture.AwayInjuries.Count > 0)
            {
                anyInjury = true;
                Assert.All(fixture.HomeInjuries, inj =>
                {
                    Assert.InRange(inj.Minute, 1, 90);
                    Assert.NotEmpty(inj.Description);
                    Assert.True(inj.DaysOut > 0);
                    Assert.Contains(inj.PlayerId, PlayedIds(fixture, true));
                });
                Assert.All(fixture.AwayInjuries, inj =>
                {
                    Assert.InRange(inj.Minute, 1, 90);
                    Assert.NotEmpty(inj.Description);
                    Assert.True(inj.DaysOut > 0);
                    Assert.Contains(inj.PlayerId, PlayedIds(fixture, false));
                });
            }
        }
        Assert.True(anyInjury, "Expected at least one injury across 200 matches");
    }

    [Fact]
    public void FinaliseMatch_AppliesInjuriesToPlayerModels()
    {
        var (fixture, competition) = NewFixture(CompetitionType.League);
        var playerId = Home.TacticSlots[5].PlayerId!.Value;
        fixture.HomeInjuries.Add(new InjuryModel { Minute = 30, PlayerId = playerId, Description = "Hamstring injury", DaysOut = 21 });
        fixture.GoalsHome = 1;
        fixture.GoalsAway = 0;

        _simulator.FinaliseMatch(fixture, competition);

        var player = _state.Players.First(p => p.Id == playerId);
        Assert.Equal(_state.Date.AddDays(21), player.InjuryEndDate);
        Assert.Equal("Hamstring injury", player.InjuryDescription);
        Assert.True(player.IsInjured(_state.Date));
    }

    [Fact]
    public void FinaliseMatch_InjuryNotificationOnlySentForMyClubPlayers()
    {
        _state.MyClubId = Home.Id;
        var (fixture, competition) = NewFixture(CompetitionType.League);
        var myPlayerId = Home.TacticSlots[5].PlayerId!.Value;
        var opponentPlayerId = Away.TacticSlots[5].PlayerId!.Value;
        fixture.HomeInjuries.Add(new InjuryModel { Minute = 30, PlayerId = myPlayerId, Description = "Minor knock", DaysOut = 5 });
        fixture.AwayInjuries.Add(new InjuryModel { Minute = 55, PlayerId = opponentPlayerId, Description = "Fracture", DaysOut = 60 });
        fixture.GoalsHome = 1;
        fixture.GoalsAway = 0;

        _simulator.FinaliseMatch(fixture, competition);

        var injuryNotifications = _state.Notifications.Where(n => n.Subject.Contains("injured")).ToList();
        Assert.Single(injuryNotifications);
        Assert.Contains(_state.Players.First(p => p.Id == myPlayerId).Name, injuryNotifications[0].Subject);
    }

    private void HirePhysios(int count)
    {
        for (var i = 1; i <= count; i++)
            _state.Physios.Add(new PhysioModel { Id = i, Name = $"Physio {i}", HiredDate = _state.Date });
    }

    private (int myPlayerId, int opponentPlayerId) InjureOnePlayerEachSide(int daysOut)
    {
        _state.MyClubId = Home.Id;
        var (fixture, competition) = NewFixture(CompetitionType.League);
        var myPlayerId = Home.TacticSlots[5].PlayerId!.Value;
        var opponentPlayerId = Away.TacticSlots[5].PlayerId!.Value;
        fixture.HomeInjuries.Add(new InjuryModel { Minute = 30, PlayerId = myPlayerId, Description = "Hamstring injury", DaysOut = daysOut });
        fixture.AwayInjuries.Add(new InjuryModel { Minute = 55, PlayerId = opponentPlayerId, Description = "Hamstring injury", DaysOut = daysOut });
        fixture.GoalsHome = 1;
        fixture.GoalsAway = 0;

        _simulator.FinaliseMatch(fixture, competition);
        return (myPlayerId, opponentPlayerId);
    }

    [Theory]
    [InlineData(0, 20)]
    [InlineData(1, 17)]
    [InlineData(2, 15)]
    [InlineData(3, 14)]
    public void FinaliseMatch_PhysiosShortenOnlyMyPlayersInjuries(int physios, int expectedDays)
    {
        HirePhysios(physios);

        var (mine, theirs) = InjureOnePlayerEachSide(daysOut: 20);

        Assert.Equal(_state.Date.AddDays(expectedDays), _state.Players.First(p => p.Id == mine).InjuryEndDate);
        Assert.Equal(_state.Date.AddDays(20), _state.Players.First(p => p.Id == theirs).InjuryEndDate);
    }

    [Fact]
    public void FinaliseMatch_InjuryNotificationExplainsThePhysioReduction()
    {
        HirePhysios(2);

        InjureOnePlayerEachSide(daysOut: 20);

        var notification = Assert.Single(_state.Notifications, n => n.Subject.Contains("injured"));
        Assert.Equal("Medical Team", notification.Recipient);
        Assert.Contains("out for 15 days", notification.Message);
        Assert.Contains("(reduced from 20 days by your medical team)", notification.Message);
    }

    [Fact]
    public void FinaliseMatch_InjuryNotificationWithoutPhysiosMentionsNoReduction()
    {
        InjureOnePlayerEachSide(daysOut: 20);

        var notification = Assert.Single(_state.Notifications, n => n.Subject.Contains("injured"));
        Assert.Contains("out for 20 days", notification.Message);
        Assert.DoesNotContain("reduced", notification.Message);
    }

    [Fact]
    public void FinaliseMatch_PhysiosNeverClearAnInjuryEntirely()
    {
        HirePhysios(PhysioRules.MaxPhysios);

        var (mine, _) = InjureOnePlayerEachSide(daysOut: 1);

        var player = _state.Players.First(p => p.Id == mine);
        Assert.Equal(_state.Date.AddDays(1), player.InjuryEndDate);
        Assert.True(player.IsInjured(_state.Date));
    }

    [Fact]
    public void ReGenerateFromMinute_ClearsInjuriesPastTheGivenMinute()
    {
        var (fixture, _) = NewFixture(CompetitionType.League);
        var early = new InjuryModel { Minute = 20, PlayerId = Home.TacticSlots[5].PlayerId!.Value, Description = "Minor knock", DaysOut = 5 };
        var late = new InjuryModel { Minute = 70, PlayerId = Home.TacticSlots[6].PlayerId!.Value, Description = "Fracture", DaysOut = 60 };
        fixture.HomeInjuries.AddRange([early, late]);

        _simulator.ReGenerateFromMinute(fixture, 50);

        // The rest of the match is re-simulated, so a new injury after minute 50 is possible.
        Assert.Contains(early, fixture.HomeInjuries);
        Assert.DoesNotContain(late, fixture.HomeInjuries);
        Assert.All(fixture.HomeInjuries.Where(i => i != early), i => Assert.InRange(i.Minute, 51, 90));
    }

    // Home's share of the goals scored after minute 10, over many re-simulations of the rest of the match.
    private double HomeGoalShareAfterMinute10(FixtureModel fixture)
    {
        int home = 0, away = 0;
        for (var i = 0; i < 3000; i++)
        {
            _simulator.ReGenerateFromMinute(fixture, 10);
            home += fixture.HomeScorers.Count(g => g.Minute > 10);
            away += fixture.AwayScorers.Count(g => g.Minute > 10);
        }
        return (double)home / (home + away);
    }

    [Fact]
    public void ReGenerateFromMinute_MyInjuredPlayerLeftOnThePitchNeverScores()
    {
        _state.MyClubId = Home.Id;
        var (fixture, _) = NewFixture(CompetitionType.League);
        var injured = Home.TacticSlots[9].PlayerId!.Value;
        fixture.HomeInjuries.Add(new InjuryModel { Minute = 10, PlayerId = injured, Description = "Minor knock", DaysOut = 5 });

        for (var i = 0; i < Iterations * 5; i++)
        {
            _simulator.ReGenerateFromMinute(fixture, 10);
            Assert.DoesNotContain(fixture.HomeScorers, g => g.PlayerId == injured && g.Minute > 10);
        }
    }

    [Fact]
    public void ReGenerateFromMinute_MyInjuredPlayerLeftOnThePitchWeakensTheSide()
    {
        _state.MyClubId = Home.Id;
        var (fixture, _) = NewFixture(CompetitionType.League);
        var fullStrength = HomeGoalShareAfterMinute10(fixture);

        fixture.HomeInjuries.Add(new InjuryModel { Minute = 10, PlayerId = Home.TacticSlots[9].PlayerId!.Value, Description = "Minor knock", DaysOut = 5 });
        var playerShort = HomeGoalShareAfterMinute10(fixture);

        // One player short costs 20% of the rating, which takes several points off the goal share.
        Assert.True(playerShort < fullStrength - 0.03, $"{playerShort:P1} with an injured player on vs {fullStrength:P1} at full strength");
    }

    [Fact]
    public void ReGenerateFromMinute_AnAIInjuredPlayerWithNoSubstitutionsLeftWeakensTheSide()
    {
        var (fixture, _) = NewFixture(CompetitionType.League);
        _simulator.PreGenerateMatch(fixture);
        // All five used early (on nobody, so the side is unchanged); nothing left for an injury.
        for (var i = 0; i < MatchSimulatorHelper.AIMaxSubstitutions; i++)
            fixture.HomeSubstitutions.Add(new SubstitutionModel { Minute = 5, OffId = -1, OnId = -1 });
        var fullStrength = HomeGoalShareAfterMinute10(fixture);

        fixture.HomeInjuries.Add(new InjuryModel { Minute = 10, PlayerId = Home.TacticSlots[9].PlayerId!.Value, Description = "Minor knock", DaysOut = 5 });
        var playerShort = HomeGoalShareAfterMinute10(fixture);

        Assert.True(playerShort < fullStrength - 0.03, $"{playerShort:P1} with an injured player on vs {fullStrength:P1} at full strength");
    }

    [Fact]
    public void ReGenerateFromMinute_AnInjuredPlayerOnAnUnrecordedAISidePlaysOn()
    {
        // Never kicked off here, so nothing is kept for Home: it's treated like a match saved before AI
        // substitutions, which can't replace anyone, so its injured play on as they used to.
        var (fixture, _) = NewFixture(CompetitionType.League);
        var fullStrength = HomeGoalShareAfterMinute10(fixture);

        fixture.HomeInjuries.Add(new InjuryModel { Minute = 10, PlayerId = Home.TacticSlots[9].PlayerId!.Value, Description = "Minor knock", DaysOut = 5 });
        var injured = HomeGoalShareAfterMinute10(fixture);

        Assert.InRange(injured, fullStrength - 0.03, fullStrength + 0.03);
    }

    // ----- AI injury substitutions ------------------------------------------------------------

    [Fact]
    public void PreGenerateMatch_AnAISideReplacesInjuredAndTiredPlayersInTheirSlots()
    {
        int replaced = 0, tired = 0, sides = 0;
        for (var i = 0; i < Iterations * 10; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.League);
            _simulator.PreGenerateMatch(fixture);
            var bench = fixture.HomeStartingSlots.Concat(fixture.AwayStartingSlots)
                .Where(s => s.TacticSlotType == TacticSlotType.SUB).Select(s => s.PlayerId).ToHashSet();

            foreach (var isHome in new[] { true, false })
            {
                var subs = isHome ? fixture.HomeSubstitutions : fixture.AwaySubstitutions;
                var injuries = isHome ? fixture.HomeInjuries : fixture.AwayInjuries;
                var scorers = isHome ? fixture.HomeScorers : fixture.AwayScorers;
                var cards = isHome ? fixture.HomeCards : fixture.AwayCards;
                var playerIds = isHome ? fixture.HomePlayerIds : fixture.AwayPlayerIds;

                var decisionMinutes = MatchSimulatorHelper.DecisionMinutes(fixture).Append(45).ToHashSet();
                for (var k = 0; k < subs.Count; k++)
                {
                    var sub = subs[k];
                    var forInjury = injuries.Any(inj => inj.PlayerId == sub.OffId && inj.Minute == sub.Minute);
                    if (forInjury)
                        replaced++;
                    else
                    {
                        // A change of the AI's choosing: only at half-time or a decision minute, never the keeper,
                        // a few at a time, and one kept back for injuries until late on.
                        tired++;
                        Assert.Contains(sub.Minute, decisionMinutes);
                        Assert.NotEqual(TacticSlotType.GK, _simulator.LineupAt(fixture, isHome, sub.Minute).Single(s => s.PlayerId == sub.OffId).TacticSlotType);
                        if (sub.Minute < MatchSimulatorHelper.AIReserveUntil)
                            Assert.True(k < MatchSimulatorHelper.AIMaxSubstitutions - MatchSimulatorHelper.AIReserveSubstitutions);
                    }
                    Assert.Contains((int?)sub.OnId, bench);
                    Assert.Contains(sub.OnId, playerIds);
                    var slotBefore = _simulator.LineupAt(fixture, isHome, sub.Minute).Single(s => s.PlayerId == sub.OffId);
                    var slotAfter = _simulator.LineupAt(fixture, isHome, sub.Minute + 1).Single(s => s.PlayerId == sub.OnId);
                    Assert.Equal(slotBefore.TacticSlotType, slotAfter.TacticSlotType);
                    Assert.DoesNotContain(scorers, g => g.PlayerId == sub.OffId && g.Minute > sub.Minute);
                    Assert.DoesNotContain(cards, c => c.PlayerId == sub.OffId && c.Minute > sub.Minute);
                    Assert.DoesNotContain(scorers, g => g.PlayerId == sub.OnId && g.Minute <= sub.Minute);
                }

                // Never more than five, never the same player twice, in at most three stoppages besides half-time.
                Assert.InRange(subs.Count, 0, MatchSimulatorHelper.AIMaxSubstitutions);
                Assert.Equal(subs.Count, subs.Select(s => s.OnId).Distinct().Count());
                Assert.InRange(SubstitutionRules.StoppagesUsed(subs.Select(s => s.Minute)), 0, MatchSimulatorHelper.AIMaxSubstitutionStoppages);
                foreach (var minute in decisionMinutes)
                    Assert.InRange(subs.Count(s => s.Minute == minute && !injuries.Any(inj => inj.PlayerId == s.OffId && inj.Minute == minute)),
                        0, MatchSimulatorHelper.AIMaxChangesPerStoppage);
                sides++;
            }
        }

        Assert.True(replaced > 0, "no AI side was ever injured");
        // Fresh legs most matches: a side brings on a few tired players' replacements.
        Assert.InRange(tired / (double)sides, 1.5, MatchSimulatorHelper.AIMaxSubstitutions);
    }

    [Fact]
    public void ReplaceInjuredAIPlayer_AKeeperIsReplacedByTheBenchKeeper()
    {
        var (fixture, _) = NewFixture(CompetitionType.League);
        _simulator.PreGenerateMatch(fixture);
        fixture.HomeSubstitutions.Clear();
        var keeper = _simulator.LineupAt(fixture, true, 30).Single(s => s.TacticSlotType == TacticSlotType.GK).PlayerId!.Value;

        Assert.True(_simulator.ReplaceInjuredAIPlayer(fixture, true, keeper, 30));

        var on = _state.Players.Single(p => p.Id == fixture.HomeSubstitutions.Single().OnId);
        Assert.Contains("GK", on.PreferredPosition);
    }

    [Fact]
    public void ReplaceInjuredAIPlayer_NeverForMySide()
    {
        var mine = _state.Clubs[2];
        _tactics.ResetTacticForClub(mine);
        _tactics.FillEmptyTacticSlotsByClubId(mine.Id);
        var fixture = new FixtureModel { HomeClub = mine, AwayClub = Away, Date = _state.Date };
        _simulator.PreGenerateMatch(fixture);
        fixture.HomeSubstitutions.Clear();

        Assert.False(_simulator.ReplaceInjuredAIPlayer(fixture, true, mine.TacticSlots[9].PlayerId!.Value, 30));
        Assert.Empty(fixture.HomeSubstitutions);
    }

    [Fact]
    public void AICanSubstitute_FiveSubstitutionsInThreeStoppagesBesidesHalfTime()
    {
        var (fixture, _) = NewFixture(CompetitionType.League);
        _simulator.PreGenerateMatch(fixture);
        fixture.HomeSubstitutions.Clear();
        void Sub(int minute) => fixture.HomeSubstitutions.Add(new SubstitutionModel { Minute = minute, OffId = -1, OnId = -1 });

        Sub(45); Sub(55); Sub(65); Sub(75);
        Assert.False(_simulator.AICanSubstitute(fixture, true, 80));  // a fourth stoppage
        Assert.True(_simulator.AICanSubstitute(fixture, true, 65));   // one already used
        Sub(65);
        Assert.False(_simulator.AICanSubstitute(fixture, true, 65));  // all five made
        Assert.False(_simulator.AICanSubstitute(fixture, true, 45));
    }

    [Fact]
    public void AICanSubstitute_AFriendlyHasNoLimitButTheBench()
    {
        var (fixture, _) = NewFixture(CompetitionType.Friendly);
        _simulator.PreGenerateMatch(fixture);
        for (var minute = 50; minute < 90; minute += 5)
            fixture.HomeSubstitutions.Add(new SubstitutionModel { Minute = minute, OffId = -1, OnId = -1 });

        Assert.True(_simulator.AICanSubstitute(fixture, true, 89));
    }

    [Fact]
    public void ReGenerateFromMinute_RedoesTheAISidesLaterSubstitutionsOnly()
    {
        var (fixture, _) = NewFixture(CompetitionType.League);
        _simulator.PreGenerateMatch(fixture);
        fixture.HomeSubstitutions.Clear();
        var bench = fixture.HomeStartingSlots.Where(s => s.TacticSlotType == TacticSlotType.SUB).Select(s => s.PlayerId!.Value).ToList();
        var lineup = _simulator.LineupAt(fixture, true, 1);
        var early = new SubstitutionModel { Minute = 40, OffId = lineup[9].PlayerId!.Value, OnId = bench[0] };
        var late = new SubstitutionModel { Minute = 70, OffId = lineup[8].PlayerId!.Value, OnId = bench[1] };
        fixture.HomeSubstitutions.AddRange([early, late]);
        fixture.HomePlayerIds.AddRange([bench[0], bench[1]]);

        _simulator.ReGenerateFromMinute(fixture, 60);

        Assert.Contains(early, fixture.HomeSubstitutions);
        Assert.DoesNotContain(late, fixture.HomeSubstitutions);
        Assert.Contains(bench[0], fixture.HomePlayerIds);
        // Any substitution after 60 now is one made again in the regenerated part: for an injury, or at a decision minute.
        var decisionMinutes = MatchSimulatorHelper.DecisionMinutes(fixture).ToHashSet();
        Assert.All(fixture.HomeSubstitutions.Where(s => s.Minute > 60), s => Assert.True(
            decisionMinutes.Contains(s.Minute) || fixture.HomeInjuries.Any(i => i.PlayerId == s.OffId && i.Minute == s.Minute)));
        Assert.Equal(fixture.HomeSubstitutions.Any(s => s.OnId == bench[1]), fixture.HomePlayerIds.Contains(bench[1]));
    }

    // ----- AI tiredness substitutions ---------------------------------------------------------

    [Fact]
    public void DecisionMinutes_AreWithinAFewMinutesOfTheirMarksAndStayPutForAFixture()
    {
        var seen = new HashSet<int>();
        for (var id = 0; id < 50; id++)
        {
            var fixture = new FixtureModel { Id = id, HomeClub = Home, AwayClub = Away };
            var minutes = MatchSimulatorHelper.DecisionMinutes(fixture).ToList();

            Assert.Equal(minutes, MatchSimulatorHelper.DecisionMinutes(fixture).ToList());
            for (var i = 0; i < minutes.Count; i++)
                Assert.InRange(minutes[i], MatchSimulatorHelper.AIDecisionMinutes[i] - MatchSimulatorHelper.AIDecisionJitter,
                    MatchSimulatorHelper.AIDecisionMinutes[i] + MatchSimulatorHelper.AIDecisionJitter);
            seen.Add(minutes[0]);
        }

        Assert.True(seen.Count > 3, "the first decision minute hardly varies between fixtures");
    }

    [Fact]
    public void PreGenerateMatch_AnAISideTakesOffAWornOutPlayerAtItsFirstDecision()
    {
        var takenOff = 0;
        const int matches = 100;
        for (var i = 0; i < matches; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.League);
            var worn = _state.Players.Single(p => p.Id == Home.TacticSlots[9].PlayerId);
            worn.Fitness = 40;
            _simulator.PreGenerateMatch(fixture);
            worn.Fitness = FitnessRules.Max;

            var first = MatchSimulatorHelper.DecisionMinutes(fixture).First();
            if (fixture.HomeSubstitutions.Any(s => s.OffId == worn.Id && s.Minute <= first))
                takenOff++;
        }

        // Every time, unless he went off injured or was sent off first, or other changes used up the stoppage.
        Assert.True(takenOff > matches * 0.8, $"taken off by the first decision in {takenOff} of {matches}");
    }

    // ----- AI changes for the state of the game, bookings and friendlies ----------------------

    private static bool IsForInjury(FixtureModel fixture, bool isHome, SubstitutionModel sub) =>
        (isHome ? fixture.HomeInjuries : fixture.AwayInjuries).Any(i => i.PlayerId == sub.OffId && i.Minute == sub.Minute);

    [Fact]
    public void ReGenerateFromMinute_ABookedDefenderIsSometimesTakenOffAtHalfTime()
    {
        const int matches = 300;
        int takenOff = 0, otherChanges = 0;
        for (var i = 0; i < matches; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.League);
            _simulator.PreGenerateMatch(fixture);
            fixture.HomeSubstitutions.Clear();
            fixture.HomeCards.Clear();
            var centreBack = _simulator.LineupAt(fixture, true, 1).First(s => s.TacticSlotType == TacticSlotType.CB).PlayerId!.Value;
            fixture.HomeCards.Add(new CardModel { Minute = 20, PlayerId = centreBack });

            _simulator.ReGenerateFromMinute(fixture, 20);

            var atHalfTime = fixture.HomeSubstitutions.Where(s => s.Minute == 45 && !IsForInjury(fixture, true, s)).ToList();
            if (atHalfTime.Any(s => s.OffId == centreBack)) takenOff++;
            otherChanges += atHalfTime.Count(s => s.OffId != centreBack);
        }

        // AIBookedSubChance, less the times a second yellow or an injury comes first.
        Assert.InRange(takenOff / (double)matches, 0.2, 0.5);
        // Nobody else is fresh enough to need changing at half-time.
        Assert.True(otherChanges < matches * 0.05, $"{otherChanges} other half-time changes in {matches} matches");
    }

    [Fact]
    public void ReGenerateFromMinute_ALosingSideChangesMoreOfItsAttackersAfterTheHour()
    {
        const int matches = 500;

        double AttackingChanges(bool losing)
        {
            var changes = 0;
            for (var i = 0; i < matches; i++)
            {
                var (fixture, _) = NewFixture(CompetitionType.League);
                _simulator.PreGenerateMatch(fixture);
                fixture.HomeSubstitutions.Clear();
                fixture.HomeScorers.Clear();
                fixture.AwayScorers.Clear();
                // Three goals up or down by 30 minutes.
                var scorers = losing ? fixture.AwayScorers : fixture.HomeScorers;
                var scorer = _simulator.LineupAt(fixture, !losing, 1)[9].PlayerId!.Value;
                foreach (var minute in new[] { 10, 20, 30 })
                    scorers.Add(new GoalModel { Minute = minute, PlayerId = scorer });

                _simulator.ReGenerateFromMinute(fixture, 30);

                changes += fixture.HomeSubstitutions.Count(s =>
                    s.Minute >= MatchSimulatorHelper.AIChasingFrom && s.Minute < MatchSimulatorHelper.AIHoldingFrom
                    && !IsForInjury(fixture, true, s)
                    && MatchSimulatorHelper.IsAttackingSlot(_simulator.LineupAt(fixture, true, s.Minute).Single(slot => slot.PlayerId == s.OffId).TacticSlotType));
            }
            return changes / (double)matches;
        }

        var chasing = AttackingChanges(losing: true);
        var winning = AttackingChanges(losing: false);
        Assert.True(chasing > winning, $"{chasing:F2} attacking changes a match chasing the game vs {winning:F2} three up");
    }

    [Fact]
    public void PreGenerateMatch_AFriendlySideRotatesAtHalfTimeAndAgainLater()
    {
        var total = 0;
        for (var i = 0; i < Iterations; i++)
        {
            var (fixture, _) = NewFixture(CompetitionType.Friendly);
            _simulator.PreGenerateMatch(fixture);

            Assert.Equal(MatchSimulatorHelper.AIFriendlyHalfTimeChanges,
                fixture.HomeSubstitutions.Count(s => s.Minute == 45 && !IsForInjury(fixture, true, s)));
            total += fixture.HomeSubstitutions.Count;
        }

        // Most of the bench gets a run-out: more than a competitive match's limit.
        Assert.True(total / (double)Iterations > MatchSimulatorHelper.AIMaxSubstitutions, $"{total / (double)Iterations:F1} substitutions a friendly");
    }

    [Fact]
    public void PreGenerateMatch_MySideMakesNoChangesOfItsOwn()
    {
        var mine = _state.Clubs[2];
        _tactics.ResetTacticForClub(mine);
        _tactics.FillEmptyTacticSlotsByClubId(mine.Id);

        for (var i = 0; i < Iterations; i++)
        {
            var fixture = new FixtureModel { HomeClub = mine, AwayClub = Away, Date = _state.Date };
            _simulator.PreGenerateMatch(fixture);
            Assert.Empty(fixture.HomeSubstitutions);
        }
    }

    [Fact]
    public void FinaliseMatch_SackingNotificationRespectsShowManagerNotificationsSetting()
    {
        Away.JobSecurity = 3;
        _state.ShowManagerNotifications = false;

        for (var i = 0; i < 2; i++)
        {
            var (fixture, competition) = NewFixture(CompetitionType.League);
            fixture.GoalsHome = 1;
            fixture.GoalsAway = 0;
            _simulator.FinaliseMatch(fixture, competition);
        }

        Assert.DoesNotContain(_state.Notifications, n => n.Subject.Contains("sack their manager"));
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

    [Fact]
    public void PrepareMatch_DropsASuspendedStarterEvenWhenTheFixtureHoldsACopyOfTheClub()
    {
        _state.MyClubId = Home.Id;
        var suspendedId = Away.TacticSlots[9].PlayerId!.Value;
        _state.Players.First(p => p.Id == suspendedId).SuspensionMatches = 1;

        // After a save/load the fixture's clubs are separate objects from State.Clubs.
        var (fixture, _) = NewFixture(CompetitionType.League);
        fixture.AwayClub = new ClubModel { Id = Away.Id, Name = Away.Name, Formation = Away.Formation };

        _simulator.PrepareMatch(fixture);

        Assert.DoesNotContain(suspendedId, StarterIds(Away));
        Assert.All(StarterIds(Away), id => Assert.NotNull(id));
    }

    // ----- Penalties during play -------------------------------------------------------------

    private static PlayerModel Taker(int id, int penalties = 75, int composure = 75, int finishing = 75) =>
        new() { Id = id, Name = $"Taker {id}", Penalties = penalties, Composure = composure, Finishing = finishing };

    private static PlayerModel Keeper(int diving, int reflexes) =>
        new() { Id = 999, Name = "Keeper", GKDiving = diving, GKReflexes = reflexes };

    private static List<(TacticSlotModel Slot, PlayerModel Player)> OnPitch(params (TacticSlotType Type, PlayerModel Player)[] players) =>
        players.Select(p => (new TacticSlotModel { TacticSlotType = p.Type, PlayerId = p.Player.Id }, p.Player)).ToList();

    private List<PlayerModel> SquadOf(ClubModel club) => _state.Players.Where(p => p.Contract?.ClubId == club.Id).ToList();

    [Fact]
    public void PickPenaltyTaker_TakesTheFirstListedPlayerWhoIsOnThePitch()
    {
        var (benched, second, third) = (Taker(1), Taker(2), Taker(3));
        var club = new ClubModel { PenaltyTakerIds = [benched.Id, second.Id, third.Id] };

        var taker = MatchSimulatorHelper.PickPenaltyTaker(club, OnPitch((TacticSlotType.ST, third), (TacticSlotType.CM, second)));

        Assert.Same(second, taker);
    }

    [Fact]
    public void PickPenaltyTaker_WithNobodyListedOnThePitchTakesTheBestOutfieldPenaltyTaker()
    {
        var keeper = Taker(1, penalties: 95);
        var best = Taker(2, penalties: 80);
        var club = new ClubModel { PenaltyTakerIds = [50] };

        var taker = MatchSimulatorHelper.PickPenaltyTaker(club,
            OnPitch((TacticSlotType.GK, keeper), (TacticSlotType.CM, Taker(3, penalties: 60)), (TacticSlotType.ST, best)));

        Assert.Same(best, taker);
        Assert.Same(best, MatchSimulatorHelper.PickPenaltyTaker(new ClubModel(),
            OnPitch((TacticSlotType.GK, keeper), (TacticSlotType.ST, best))));
    }

    [Fact]
    public void PrepareClub_GivesAnAIClubItsThreeBestPenaltyTakersFromTheStartingEleven()
    {
        foreach (var (player, i) in SquadOf(Away).Select((p, i) => (p, i)))
            player.Penalties = 40 + i * 3;
        Away.PenaltyTakerIds = [];

        _simulator.PrepareClub(Away.Id);

        var expected = Away.TacticSlots
            .Where(s => s.TacticSlotType is not (TacticSlotType.SUB or TacticSlotType.RES or TacticSlotType.GK))
            .Select(s => _state.Players.First(p => p.Id == s.PlayerId))
            .OrderByDescending(p => p.Penalties)
            .Take(MatchSimulatorHelper.AIPenaltyTakers)
            .Select(p => p.Id);
        Assert.Equal(expected, Away.PenaltyTakerIds);
    }

    [Fact]
    public void PrepareClub_LeavesMyPenaltyTakersAlone()
    {
        _state.MyClubId = Home.Id;
        var mine = SquadOf(Home).Last().Id;
        Home.PenaltyTakerIds = [mine];

        _simulator.PrepareClub(Home.Id);

        Assert.Equal([mine], Home.PenaltyTakerIds);
    }

    [Fact]
    public void ScoringChance_IsTheTargetRateForAnAverageTakerAndKeeper()
    {
        var averageTaker = Taker(1, (int)MatchSimulatorHelper.TakerReference, (int)MatchSimulatorHelper.TakerReference, (int)MatchSimulatorHelper.TakerReference);
        var averageKeeper = Keeper(72, 73);
        var expected = MatchSimulatorHelper.PenaltyConversionRate
            + MatchSimulatorHelper.TakerWeight * (MatchSimulatorHelper.TakerScore(averageTaker) - MatchSimulatorHelper.TakerReference)
            - MatchSimulatorHelper.KeeperWeight * (MatchSimulatorHelper.KeeperScore(averageKeeper) - MatchSimulatorHelper.KeeperReference);

        Assert.Equal(expected, MatchSimulatorHelper.ScoringChance(averageTaker, averageKeeper), 6);
        Assert.InRange(MatchSimulatorHelper.ScoringChance(averageTaker, averageKeeper), 0.77, 0.79);
        Assert.InRange(MatchSimulatorHelper.ScoringChance(averageTaker, null), 0.77, 0.79);
    }

    [Fact]
    public void ScoringChance_RisesWithTheTakersStatsAndFallsWithTheKeepers()
    {
        var keeper = Keeper(72, 73);

        Assert.True(MatchSimulatorHelper.ScoringChance(Taker(1, penalties: 90), keeper) > MatchSimulatorHelper.ScoringChance(Taker(1), keeper));
        Assert.True(MatchSimulatorHelper.ScoringChance(Taker(1, composure: 90), keeper) > MatchSimulatorHelper.ScoringChance(Taker(1), keeper));
        Assert.True(MatchSimulatorHelper.ScoringChance(Taker(1, finishing: 90), keeper) > MatchSimulatorHelper.ScoringChance(Taker(1), keeper));
        Assert.True(MatchSimulatorHelper.ScoringChance(Taker(1), Keeper(90, 72)) < MatchSimulatorHelper.ScoringChance(Taker(1), keeper));
        Assert.True(MatchSimulatorHelper.ScoringChance(Taker(1), Keeper(72, 90)) < MatchSimulatorHelper.ScoringChance(Taker(1), keeper));
    }

    [Fact]
    public void ScoringChance_StaysWithinItsLimits()
    {
        Assert.Equal(MatchSimulatorHelper.MaxScoringChance, MatchSimulatorHelper.ScoringChance(Taker(1, 99, 99, 99), Keeper(1, 1)));
        Assert.Equal(MatchSimulatorHelper.MinScoringChance, MatchSimulatorHelper.ScoringChance(Taker(1, 1, 1, 1), Keeper(99, 99)));
    }

    [Fact]
    public void PenaltyChanceForMinutes_GivesTheTargetMatchRateOverTwoHalvesAndScalesForPartHalves()
    {
        var perHalf = MatchSimulatorHelper.PenaltyChanceForMinutes(45);
        var noPenalty = Math.Pow(1 - perHalf, 2);

        Assert.Equal(MatchSimulatorHelper.PenaltyMatchRate, 1 - noPenalty, 6);
        Assert.Equal(0, MatchSimulatorHelper.PenaltyChanceForMinutes(0));
        Assert.Equal(0, MatchSimulatorHelper.PenaltyChanceForMinutes(-5));
        // Rolling the first 15 minutes and the last 30 separately is the same as rolling the half once.
        var split = 1 - (1 - MatchSimulatorHelper.PenaltyChanceForMinutes(15)) * (1 - MatchSimulatorHelper.PenaltyChanceForMinutes(30));
        Assert.Equal(perHalf, split, 6);
    }

    [Fact]
    public void OpenPlayGoalChance_LeavesRoomForTheGoalsPenaltiesScore()
    {
        var penaltyGoals = 2 * MatchSimulatorHelper.PenaltyChancePerHalf * MatchSimulatorHelper.PenaltyConversionRate;
        var openPlayGoals = MatchSimulatorHelper.BaseGoalsPerMatch * MatchSimulatorHelper.OpenPlayGoalChance / MatchSimulatorHelper.BaseGoalChance;

        Assert.Equal(MatchSimulatorHelper.BaseGoalsPerMatch, openPlayGoals + penaltyGoals, 6);
    }

    [Fact]
    public void BaseGoalsPerMatch_MatchesTheGoalLoopStepSizes()
    {
        // Expected landings of a walk with steps of 1..maxStep minutes in a 45-minute half.
        static double Landings(int maxStep)
        {
            var expected = new double[46];
            for (var d = 1; d <= 45; d++)
                expected[d] = Enumerable.Range(1, Math.Min(maxStep, d)).Sum(s => 1 + expected[d - s]) / maxStep;
            return expected[45];
        }

        var goals = (Landings(MatchSimulatorHelper.FirstHalfMaxStep) + Landings(MatchSimulatorHelper.LaterMaxStep)) * MatchSimulatorHelper.BaseGoalChance;

        Assert.Equal(MatchSimulatorHelper.BaseGoalsPerMatch, goals, 0.0001);
    }

    // Each kind of event carries its next step over a period's end, so a match cut into many short periods
    // has as many goals, cards, injuries and penalties as one with only the usual few periods.
    [Fact]
    public void SplittingAMatchIntoPeriodsChangesNoEventRates()
    {
        const int matches = 10_000;
        // Home is mine, so regenerating keeps its substitutions (an AI side's later ones would be redone).
        _state.MyClubId = Home.Id;

        (double Goals, double Cards, double Injuries, double Penalties) Rates(bool split)
        {
            int goals = 0, cards = 0, injuries = 0, penalties = 0;
            for (var i = 0; i < matches; i++)
            {
                var (fixture, _) = NewFixture(CompetitionType.League);
                _simulator.PreGenerateMatch(fixture);
                if (split)
                {
                    // Substitutions of nobody: they change neither side but end a period every 3 minutes.
                    for (var minute = 3; minute < 90; minute += 3)
                        fixture.HomeSubstitutions.Add(new SubstitutionModel { Minute = minute, OffId = -1, OnId = -1 });
                }
                _simulator.ReGenerateFromMinute(fixture, 0);

                goals += fixture.HomeScorers.Count + fixture.AwayScorers.Count;
                cards += fixture.HomeCards.Count + fixture.AwayCards.Count;
                injuries += fixture.HomeInjuries.Count + fixture.AwayInjuries.Count;
                penalties += fixture.HomePenalties.Count + fixture.AwayPenalties.Count;
            }
            return (goals / (double)matches, cards / (double)matches, injuries / (double)matches, penalties / (double)matches);
        }

        var halves = Rates(split: false);
        var periods = Rates(split: true);

        Assert.InRange(periods.Goals, halves.Goals - 0.07, halves.Goals + 0.07);
        Assert.InRange(periods.Cards, halves.Cards * 0.95, halves.Cards * 1.05);
        Assert.InRange(periods.Injuries, halves.Injuries * 0.9, halves.Injuries * 1.1);
        Assert.InRange(periods.Penalties, halves.Penalties - 0.025, halves.Penalties + 0.025);
    }

    [Fact]
    public void ConcludeFixture_PenaltiesHitTheirTargetsWithoutChangingGoalsPerMatch()
    {
        // Every taker and keeper at the reference values, so penalties go in at the target rate.
        foreach (var player in _state.Players)
        {
            (player.Penalties, player.Composure, player.Finishing) = (75, 75, 75);
            (player.GKDiving, player.GKReflexes) = (72, 73);
        }

        const int matches = 20_000;
        int goals = 0, matchesWithPenalty = 0, penalties = 0, scored = 0;

        for (var i = 0; i < matches; i++)
        {
            var (fixture, competition) = NewFixture(CompetitionType.League);
            _simulator.ConcludeFixture(fixture, competition);

            goals += fixture.GoalsHome!.Value + fixture.GoalsAway!.Value;
            var all = fixture.HomePenalties.Select(p => (p, Home: true)).Concat(fixture.AwayPenalties.Select(p => (p, Home: false))).ToList();
            if (all.Count > 0) matchesWithPenalty++;
            penalties += all.Count;

            foreach (var (penalty, home) in all)
            {
                Assert.InRange(penalty.Minute, 1, 90);
                var side = home ? Home : Away;
                Assert.Contains((int?)penalty.TakerId, PlayedIds(fixture, home));
                var sentOff = (home ? fixture.HomeCards : fixture.AwayCards).Where(c => c.IsRed && c.Minute < penalty.Minute).Select(c => c.PlayerId);
                Assert.DoesNotContain(penalty.TakerId, sentOff);

                var penaltyGoal = (home ? fixture.HomeScorers : fixture.AwayScorers)
                    .Any(g => g.IsPenalty && g.Minute == penalty.Minute && g.PlayerId == penalty.TakerId);
                Assert.Equal(penalty.Outcome == PenaltyOutcome.Scored, penaltyGoal);
                if (penalty.Outcome == PenaltyOutcome.Scored) scored++;
            }
        }

        var goalsPerMatch = goals / (double)matches;
        Assert.InRange(goalsPerMatch, MatchSimulatorHelper.BaseGoalsPerMatch - 0.05, MatchSimulatorHelper.BaseGoalsPerMatch + 0.05);
        Assert.InRange(matchesWithPenalty / (double)matches, 0.21, 0.24);
        Assert.InRange(scored / (double)penalties, 0.75, 0.81);
    }

    [Fact]
    public void ReGenerateFromMinute_KeepsEarlierPenaltiesAndClearsLaterOnes()
    {
        var (fixture, _) = NewFixture(CompetitionType.League);
        var taker = StarterIds(Home).First(id => id != null)!.Value;
        var early = new PenaltyModel { Minute = 10, TakerId = taker, Outcome = PenaltyOutcome.Scored };
        var late = new PenaltyModel { Minute = 70, TakerId = taker, Outcome = PenaltyOutcome.Saved };
        fixture.HomePenalties.AddRange([early, late]);
        fixture.HomeScorers.Add(new GoalModel { Minute = 10, PlayerId = taker, IsPenalty = true });

        _simulator.ReGenerateFromMinute(fixture, 50);

        Assert.Contains(early, fixture.HomePenalties);
        Assert.DoesNotContain(late, fixture.HomePenalties);
        Assert.Contains(fixture.HomeScorers, g => g.IsPenalty && g.Minute == 10);
        Assert.All(fixture.HomePenalties.Concat(fixture.AwayPenalties).Where(p => p != early), p => Assert.InRange(p.Minute, 51, 90));
    }

    [Fact]
    public void PositionScoringWeight_FavoursStrikersAndBarelyCountsTheKeeper()
    {
        var outfield = Enum.GetValues<TacticSlotType>()
            .Where(t => t is not (TacticSlotType.ST or TacticSlotType.LS or TacticSlotType.LF
                or TacticSlotType.GK or TacticSlotType.SUB or TacticSlotType.RES));

        Assert.All(outfield, t => Assert.True(
            MatchSimulatorHelper.PositionScoringWeight(TacticSlotType.ST) > MatchSimulatorHelper.PositionScoringWeight(t)));
        Assert.True(MatchSimulatorHelper.PositionScoringWeight(TacticSlotType.GK)
            < MatchSimulatorHelper.PositionScoringWeight(TacticSlotType.CB));
    }

    // ----- Opponent's plan (analyst report) -------------------------------------------------

    private ClubModel Mine => _state.Clubs[2];

    [Fact]
    public void PlanOpponentInstructions_PlansOnlyTheAISide()
    {
        foreach (var player in _state.Players)
            player.Rating = player.Contract?.ClubId == Mine.Id ? 85 : 45;
        var fixture = new FixtureModel { HomeClub = Mine, AwayClub = Home, Date = _state.Date.AddDays(1) };

        _simulator.PlanOpponentInstructions(fixture);

        Assert.Null(fixture.HomePlannedInstructions);
        Assert.NotNull(fixture.AwayPlannedInstructions);
        // A clear underdog always drops off (only its mentality is ever softened).
        Assert.Equal(Pressing.Low, fixture.AwayPlannedInstructions!.Pressing);
    }

    [Fact]
    public void PlanOpponentInstructions_IgnoresVenueAndMyHiddenBoost()
    {
        foreach (var player in _state.Players)
            player.Rating = 70;
        var fixture = new FixtureModel { HomeClub = Mine, AwayClub = Home, Date = _state.Date.AddDays(1) };

        _simulator.PlanOpponentInstructions(fixture);

        // An even match-up away at the user's ground isn't treated as an underdog's game.
        Assert.NotEqual(Mentality.Defensive, fixture.AwayPlannedInstructions!.Mentality);
        Assert.NotEqual(Pressing.Low, fixture.AwayPlannedInstructions.Pressing);
    }

    [Fact]
    public void ChooseAIInstructions_SticksToTheDayBeforePlan()
    {
        var plan = new TeamInstructionsModel { Mentality = Mentality.Attacking, Pressing = Pressing.High };
        var fixture = new FixtureModel { HomeClub = Mine, AwayClub = Home, Date = _state.Date, AwayPlannedInstructions = plan };

        _simulator.ChooseAIInstructions(fixture);

        Assert.Equal(Mentality.Attacking, Home.Instructions.Mentality);
        Assert.Equal(Pressing.High, Home.Instructions.Pressing);
        Assert.NotSame(plan, Home.Instructions);
    }

    [Fact]
    public void ChooseAIInstructions_NeverTouchesMyClub()
    {
        Mine.Instructions = new TeamInstructionsModel { Mentality = Mentality.Defensive };
        var fixture = new FixtureModel { HomeClub = Mine, AwayClub = Home, Date = _state.Date };

        _simulator.ChooseAIInstructions(fixture);

        Assert.Equal(Mentality.Defensive, Mine.Instructions.Mentality);
    }

    // ----- Fitness ---------------------------------------------------------------------------

    private List<PlayerModel> StartersOf(ClubModel club) => club.TacticSlots
        .Where(s => s.TacticSlotType is not (TacticSlotType.SUB or TacticSlotType.RES) && s.PlayerId != null)
        .Select(s => _state.Players.First(p => p.Id == s.PlayerId))
        .ToList();

    [Fact]
    public void FullTime_ChargesEveryStarterForTheMatchAndLeavesTheBenchFresh()
    {
        foreach (var player in _state.Players) player.Stamina = 65;
        var (fixture, competition) = NewFixture(CompetitionType.League);
        var bench = Home.TacticSlots.Where(s => s.TacticSlotType == TacticSlotType.SUB && s.PlayerId != null)
            .Select(s => _state.Players.First(p => p.Id == s.PlayerId)).ToList();

        _simulator.ConcludeFixture(fixture, competition);

        // Each is charged for the minutes he was on: a starter until he was taken off (the AI changes
        // tired and injured players), a substitute from when he came on, and an unused substitute nothing.
        var sentOff = fixture.HomeCards.Concat(fixture.AwayCards).Where(c => c.IsRed).Select(c => c.PlayerId).ToHashSet();
        var subs = fixture.HomeSubstitutions.Concat(fixture.AwaySubstitutions).ToList();
        var offAt = subs.ToDictionary(s => s.OffId, s => s.Minute);
        var onAt = subs.ToDictionary(s => s.OnId, s => s.Minute);
        Assert.All(StartersOf(Home).Concat(StartersOf(Away)).Where(p => !sentOff.Contains(p.Id)),
            p => Assert.Equal(FitnessRules.Max - FitnessRules.Drain(65, offAt.GetValueOrDefault(p.Id, 90), 0, FitnessRules.RoleDrainMultiplier(p)), p.Fitness, 6));
        Assert.All(bench.Where(p => !sentOff.Contains(p.Id)), p => Assert.Equal(
            onAt.TryGetValue(p.Id, out var on) ? FitnessRules.Max - FitnessRules.Drain(65, 90 - on, 0, FitnessRules.RoleDrainMultiplier(p)) : FitnessRules.Max,
            p.Fitness, 6));
        // Keepers tire at a third of the rate.
        var keeper = StartersOf(Home).First(p => p.PreferredPosition == "GK");
        var outfielder = StartersOf(Home).First(p => FitnessRules.RoleDrainMultiplier(p) == 1 && !sentOff.Contains(p.Id) && !offAt.ContainsKey(p.Id));
        if (!offAt.ContainsKey(keeper.Id))
            Assert.Equal((FitnessRules.Max - outfielder.Fitness) / 3, FitnessRules.Max - keeper.Fitness, 6);
        Assert.True(fixture.FitnessApplied);
    }

    [Fact]
    public void FullTime_ASidePressingHighTiresMore()
    {
        foreach (var player in _state.Players) player.Stamina = 65;
        Home.Instructions = new TeamInstructionsModel { Pressing = Pressing.High };
        var (fixture, competition) = NewFixture(CompetitionType.League);

        _simulator.ConcludeFixture(fixture, competition);

        var homeKeeper = StartersOf(Home).First(p => p.PreferredPosition == "GK");
        var awayKeeper = StartersOf(Away).First(p => p.PreferredPosition == "GK");
        Assert.Equal(FitnessRules.Max - FitnessRules.Drain(65, 90, 90, FitnessRules.GoalkeeperDrainMultiplier), homeKeeper.Fitness, 6);
        Assert.Equal(FitnessRules.Max - FitnessRules.Drain(65, 90, 0, FitnessRules.GoalkeeperDrainMultiplier), awayKeeper.Fitness, 6);
    }

    [Fact]
    public void FullTime_ASidePressingLowTiresLess()
    {
        foreach (var player in _state.Players) player.Stamina = 65;
        Home.Instructions = new TeamInstructionsModel { Pressing = Pressing.Low };
        var (fixture, competition) = NewFixture(CompetitionType.League);

        _simulator.ConcludeFixture(fixture, competition);

        var homeKeeper = StartersOf(Home).First(p => p.PreferredPosition == "GK");
        var awayKeeper = StartersOf(Away).First(p => p.PreferredPosition == "GK");
        Assert.Equal(FitnessRules.Max - FitnessRules.Drain(65, 90, 0, FitnessRules.GoalkeeperDrainMultiplier, 90), homeKeeper.Fitness, 6);
        Assert.Equal(FitnessRules.Max - FitnessRules.Drain(65, 90, 0, FitnessRules.GoalkeeperDrainMultiplier), awayKeeper.Fitness, 6);
    }

    [Fact]
    public void FitnessAt_ASubstituteOnlyTiresFromWhenHeCameOn()
    {
        foreach (var player in _state.Players) player.Stamina = 65;
        var (fixture, _) = NewFixture(CompetitionType.League);
        _simulator.PreGenerateMatch(fixture);
        fixture.HomeSubstitutions.Clear(); // the AI's own changes
        var starter = StartersOf(Home).First(p => FitnessRules.RoleDrainMultiplier(p) == 1);
        var sub = Home.TacticSlots.Where(s => s.TacticSlotType == TacticSlotType.SUB && s.PlayerId != null)
            .Select(s => _state.Players.First(p => p.Id == s.PlayerId))
            .First(p => FitnessRules.RoleDrainMultiplier(p) == 1);
        fixture.HomeSubstitutions.Add(new SubstitutionModel { Minute = 60, OffId = starter.Id, OnId = sub.Id });
        fixture.HomePlayerIds.Add(sub.Id);

        Assert.Equal(FitnessRules.Max - FitnessRules.Drain(65, 60, 0), _simulator.FitnessAt(fixture, starter.Id, 90), 6);
        Assert.Equal(FitnessRules.Max - FitnessRules.Drain(65, 30, 0), _simulator.FitnessAt(fixture, sub.Id, 90), 6);
        Assert.Equal(FitnessRules.Max, _simulator.FitnessAt(fixture, sub.Id, 50));
    }

    [Fact]
    public void FitnessAt_StartsFromThePlayersFitnessGoingIntoTheMatch()
    {
        foreach (var player in _state.Players) player.Stamina = 65;
        var (fixture, _) = NewFixture(CompetitionType.League);
        var tired = StartersOf(Home).First(p => FitnessRules.RoleDrainMultiplier(p) == 1);
        tired.Fitness = 70;
        _simulator.PreGenerateMatch(fixture);

        Assert.Equal(70, _simulator.FitnessAt(fixture, tired.Id, 0), 6);
        Assert.Equal(70 - FitnessRules.Drain(65, 45, 0), _simulator.FitnessAt(fixture, tired.Id, 45), 6);
    }
}
