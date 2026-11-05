using FootballManagerSimulator.Maui.Helpers;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Helpers;

public class ReputationHelperTests
{
    // Country 1: league 20 (rank 1) club 5, league 10 (rank 2) clubs 1-4, league 30 (rank 3) club 6.
    // Standings: club 4 = 0, 6 = 20, 3 = 40, 5 = 60, 2 = 80, 1 = 100. League 10 averages 55: a weight of 1.1.
    private static readonly Dictionary<int, int> Strengths = new() { [1] = 80, [2] = 70, [3] = 60, [4] = 50, [5] = 65, [6] = 55 };
    private const double Weight = 1.1;

    private readonly StateModel _state = new() { Date = new DateOnly(2027, 6, 30), MyClubId = 4, ManagerReputation = 40 };
    private readonly RecordingNotifications _notifications;
    private readonly ReputationHelper _helper;
    private readonly LeagueModel _league;

    public ReputationHelperTests()
    {
        _league = AddLeague(10, rank: 2, clubIds: [1, 2, 3, 4]);
        AddLeague(20, rank: 1, clubIds: [5]);
        AddLeague(30, rank: 3, clubIds: [6]);
        foreach (var (clubId, rating) in Strengths)
            _state.Players.Add(TestData.Player(clubId * 100, clubId, rating: rating));
        _notifications = new RecordingNotifications { Now = _state.Date };
        _helper = new ReputationHelper(_state, _notifications);
        StartJob(4, new DateOnly(2026, 7, 1), expectedFinish: 4);
    }

    private LeagueModel AddLeague(int id, int rank, int[] clubIds)
    {
        var clubs = clubIds.Select(c => TestData.Club(c, leagueId: id)).ToList();
        _state.Clubs.AddRange(clubs);
        var league = TestData.League(id, clubs);
        league.Rank = rank;
        _state.Competitions.Add(league);
        return league;
    }

    private ClubModel Club(int id) => _state.Clubs.Single(p => p.Id == id);

    private void StartJob(int clubId, DateOnly from, int expectedFinish)
    {
        _state.ManagerHistory = new ManagerHistoryModel();
        _state.MyClubId = clubId;
        _state.ManagerHistory.StartJob(Club(clubId), from);
        _state.ManagerHistory.CurrentJob!.ExpectedFinish = expectedFinish;
    }

    private void Play(int home, int away, int goalsHome, int goalsAway, DateOnly date)
    {
        var fixture = TestData.Result(Club(home), Club(away), goalsHome, goalsAway);
        fixture.Date = date;
        _league.Fixtures.Add(fixture);
    }

    // Club 4 beats everyone, on 1, 8 and 15 August; the rest draw with each other.
    private void Club4WinsTheLeague()
    {
        Play(4, 1, 1, 0, new DateOnly(2026, 8, 1));
        Play(4, 2, 1, 0, new DateOnly(2026, 8, 8));
        Play(4, 3, 1, 0, new DateOnly(2026, 8, 15));
        Play(1, 2, 0, 0, new DateOnly(2026, 8, 1));
        Play(1, 3, 0, 0, new DateOnly(2026, 8, 8));
        Play(2, 3, 0, 0, new DateOnly(2026, 8, 15));
    }

    private void EndSeason() => _helper.CompleteSeason(_helper.ReviewSeason());

    [Fact]
    public void WinningTheLeagueFromLastInExpectations_RaisesReputation()
    {
        Club4WinsTheLeague();

        EndSeason();

        // 3 places above expectations (4.5) and the title (8), both weighted by the league.
        var change = 4.5 * Weight + 8 * Weight;
        Assert.Equal(40 + change, _state.ManagerReputation!.Value, 6);
        Assert.Equal(change, _state.LastSeasonReputationChange!.Value, 6);
        var notification = Assert.Single(_notifications.Sent);
        Assert.Equal("Your Reputation", notification.Subject);
        Assert.Contains("risen to 54", notification.Message);
        Assert.Contains("Finished 1st in League 10, 3 places above expectations.", notification.Message);
        Assert.Contains("Won League 10.", notification.Message);
    }

    [Fact]
    public void FinishingBelowExpectations_LowersReputation_ButABigClubPullsItUp()
    {
        StartJob(1, new DateOnly(2026, 7, 1), expectedFinish: 1);
        Play(2, 1, 1, 0, new DateOnly(2026, 8, 1));
        Play(3, 1, 1, 0, new DateOnly(2026, 8, 8));
        Play(4, 1, 1, 0, new DateOnly(2026, 8, 15));
        Play(2, 3, 0, 0, new DateOnly(2026, 8, 1));
        Play(2, 4, 0, 0, new DateOnly(2026, 8, 8));
        Play(3, 4, 0, 0, new DateOnly(2026, 8, 15));

        EndSeason();

        // 3 places below (-4.5 weighted), plus a tenth of the gap up to club 1's standing of 100.
        Assert.Equal(40 - 4.5 * Weight + (100 - 40) * JobRules.DriftShare, _state.ManagerReputation!.Value, 6);
        Assert.Contains("3 places below expectations", _notifications.Sent.Single().Message);
    }

    [Fact]
    public void ArrivingPartwayThrough_OnlyCountsTheGamesIManaged()
    {
        Club4WinsTheLeague();
        StartJob(4, new DateOnly(2026, 8, 5), expectedFinish: 4); // missed the first of 3 games

        EndSeason();

        Assert.Equal(40 + 4.5 * Weight * 2 / 3 + 8 * Weight, _state.ManagerReputation!.Value, 6);
    }

    [Fact]
    public void WinningACup_RaisesReputationByTheCupsWeight()
    {
        Club4WinsTheLeague();
        var cup = new CupModel { Id = 50, Name = "Test Cup" };
        cup.Fixtures.Add(TestData.Result(Club(1), Club(2), 1, 0)); // standings 100 and 80: weight capped at 1.5
        _state.Competitions.Add(cup);
        _state.ManagerHistory.Trophies.Add(new ManagerTrophyModel { Name = "Test Cup", Season = "2026/27" });
        _state.ManagerHistory.Trophies.Add(new ManagerTrophyModel { Name = "League 10 Play-off Final", Season = "2026/27" });
        _state.ManagerHistory.Trophies.Add(new ManagerTrophyModel { Name = "Test Cup", Season = "2025/26" });

        EndSeason();

        Assert.Equal(40 + 4.5 * Weight + 8 * Weight + 5 * JobRules.WeightMax, _state.ManagerReputation!.Value, 6);
        Assert.Contains("Won Test Cup.", _notifications.Sent.Single().Message);
    }

    [Theory]
    [InlineData(20, 5, "Promoted.")]
    [InlineData(30, -6, "Relegated.")]
    public void PromotionAndRelegation_CountOnceTheClubsHaveMoved(int newLeagueId, double change, string reason)
    {
        Club4WinsTheLeague();
        var review = _helper.ReviewSeason();

        Club(4).LeagueId = newLeagueId;
        _helper.CompleteSeason(review);

        Assert.Equal(40 + 4.5 * Weight + 8 * Weight + change, _state.ManagerReputation!.Value, 6);
        Assert.Contains(reason, _notifications.Sent.Single().Message);
    }

    [Fact]
    public void OutOfWork_TheSeasonChangesNothing()
    {
        Club4WinsTheLeague();
        _state.ManagerHistory.EndJob(_state.Date);
        _state.MyClubId = null;
        _state.LastSeasonReputationChange = 3;

        EndSeason();

        Assert.Equal(40, _state.ManagerReputation);
        Assert.Null(_state.LastSeasonReputationChange);
        Assert.Empty(_notifications.Sent);
    }

    [Fact]
    public void ReputationNeverGoesAbove100()
    {
        _state.ManagerReputation = 99;
        Club4WinsTheLeague();

        EndSeason();

        Assert.Equal(100, _state.ManagerReputation);
    }

    [Fact]
    public void SetExpectedFinish_RanksTheSquadWithinItsLeague()
    {
        _state.ManagerHistory.CurrentJob!.ExpectedFinish = null;

        _helper.SetExpectedFinish();

        Assert.Equal(4, _state.ManagerHistory.CurrentJob!.ExpectedFinish);
    }

    [Theory]
    [InlineData(10, 1, 1, 1, 10)] // 13.5 capped at 10
    [InlineData(1, 10, 1, 1, -10)]
    [InlineData(5, 3, 1.5, 0.5, 2.25)]
    [InlineData(3, 3, 1, 1, 0)]
    public void FinishChange(int expected, int actual, double weight, double share, double change) =>
        Assert.Equal(change, JobRules.FinishChange(expected, actual, weight, share), 6);

    [Theory]
    [InlineData(10, 0.5)]
    [InlineData(50, 1)]
    [InlineData(90, 1.5)]
    public void LeagueWeight_IsTheAverageStandingOver50(double averageStanding, double weight) =>
        Assert.Equal(weight, JobRules.Weight(averageStanding), 6);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(20, 1)]
    [InlineData(20.1, 2)]
    [InlineData(54, 3)]
    [InlineData(100, 5)]
    public void Stars(double value, int stars) => Assert.Equal(stars, JobRules.Stars(value));
}
