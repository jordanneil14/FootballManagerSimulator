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
    public void ProcessMatch_AdvancesHalfByHalf()
    {
        var (fixture, competition) = NewFixture(CompetitionType.League);

        _simulator.ProcessMatch(fixture, competition);
        Assert.Equal(45, fixture.Minute);
        Assert.False(fixture.Concluded);
        Assert.All(fixture.HomeScorers.Concat(fixture.AwayScorers), g => Assert.InRange(g.Minute, 1, 55));

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
