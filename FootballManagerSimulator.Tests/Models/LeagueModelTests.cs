using FootballManagerSimulator.Maui.Enums;
using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Models;

public class LeagueModelTests
{
    private readonly ClubModel _a = TestData.Club(1, "Alpha");
    private readonly ClubModel _b = TestData.Club(2, "Bravo");
    private readonly ClubModel _c = TestData.Club(3, "Charlie");
    private readonly LeagueModel _league;

    public LeagueModelTests() => _league = TestData.League(1, [_a, _b, _c]);

    [Fact]
    public void GenerateLeagueTable_AwardsThreePointsForAWinAndOneForADraw()
    {
        _league.Fixtures.Add(TestData.Result(_a, _b, 2, 1));
        _league.Fixtures.Add(TestData.Result(_c, _a, 1, 1));
        _league.Fixtures.Add(TestData.Result(_b, _c, 0, 3));

        var table = _league.GenerateLeagueTable().ToList();

        Assert.Equal(["Charlie", "Alpha", "Bravo"], table.Select(p => p.Club.Name));
        var alpha = table.Single(p => p.Club == _a);
        Assert.Equal((4, 3, 2, 2), (alpha.Points, alpha.GoalsScored, alpha.GoalsConceded, alpha.Played));
        var charlie = table.Single(p => p.Club == _c);
        Assert.Equal((4, 4, 1), (charlie.Points, charlie.GoalsScored, charlie.GoalsConceded));
    }

    [Fact]
    public void GenerateLeagueTable_IgnoresUnplayedAndPlayoffFixtures()
    {
        _league.Fixtures.Add(new FixtureModel { HomeClub = _a, AwayClub = _b, GoalsHome = 5, GoalsAway = 0, Concluded = false });
        _league.Fixtures.Add(TestData.Result(_b, _a, 4, 0, FixtureType.PlayoffFinal));

        Assert.All(_league.GenerateLeagueTable(), p => Assert.Equal((0, 0), (p.Points, p.Played)));
    }

    [Fact]
    public void GenerateLeagueTable_BreaksEqualPointsAndGoalDifferenceOnGoalsScoredThenName()
    {
        var d = TestData.Club(4, "Delta");
        _league.Clubs.Insert(0, d);
        // Every club has one point and a goal difference of zero.
        _league.Fixtures.Add(TestData.Result(d, _c, 1, 1));
        _league.Fixtures.Add(TestData.Result(_b, _a, 2, 2));

        var table = _league.GenerateLeagueTable().ToList();

        Assert.Equal(["Alpha", "Bravo", "Charlie", "Delta"], table.Select(p => p.Club.Name));
    }

    [Theory]
    [InlineData(5, 2, "+3")]
    [InlineData(2, 5, "-3")]
    [InlineData(1, 1, "0")]
    public void LeaguePosition_FormatsGoalDifference(int scored, int conceded, string expected) =>
        Assert.Equal(expected, new LeaguePositionModel { GoalsScored = scored, GoalsConceded = conceded }.GoalDifference);

    [Fact]
    public void FriendlyStrings()
    {
        Assert.Equal($"£{1500000:n0}", new ClubModel { TransferBudget = 1_500_000 }.TransferBudgetFriendly);
        Assert.Equal($"£{250000:n0}", new TransferListItemModel { AskingPrice = 250_000 }.AskingPriceFriendly);
        var notification = new NotificationModel { Date = new DateOnly(2026, 8, 1), Recipient = "Chairman", Subject = "Hi", Message = "Body" };
        Assert.StartsWith("From: Chairman\n", notification.ToString());
        Assert.EndsWith("Subject: Hi\nMessage: Body", notification.ToString());
    }
}
