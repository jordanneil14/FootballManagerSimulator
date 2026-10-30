using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Models;

public class TransferBudgetRequestRulesTests
{
    [Theory]
    [InlineData(1, 10_000_000)]
    [InlineData(2, 2_500_000)]
    [InlineData(3, 1_000_000)]
    [InlineData(4, 150_000)]
    [InlineData(5, 50_000)]
    [InlineData(null, 50_000)]
    public void AmountForLeagueRank_GrantsLessTheLowerTheLeague(int? rank, int expected)
    {
        Assert.Equal(expected, TransferBudgetRequestRules.AmountForLeagueRank(rank));
    }
}

public class StadiumRulesTests
{
    [Theory]
    [InlineData(10_000, 2_000)]
    [InlineData(10_004, 2_000)]
    [InlineData(50_000, 10_000)]
    public void SeatsAdded_GrowsTheStadiumByAFifth(int capacity, int expected)
    {
        Assert.Equal(expected, StadiumRules.SeatsAdded(capacity));
        Assert.Equal(expected * StadiumRules.CostPerSeat, StadiumRules.UpgradeCost(capacity));
    }

    [Theory]
    [InlineData(90_000, 10_000)]
    [InlineData(99_500, 500)]
    [InlineData(StadiumRules.MaxCapacity, 0)]
    [InlineData(StadiumRules.MaxCapacity + 1_000, 0)]
    public void SeatsAdded_StopsAtTheMaximumCapacity(int capacity, int expected)
    {
        Assert.Equal(expected, StadiumRules.SeatsAdded(capacity));
        Assert.Equal(expected * StadiumRules.CostPerSeat, StadiumRules.UpgradeCost(capacity));
    }
}

public class LeaguePositionModelTests
{
    [Theory]
    [InlineData(5, 2, "+3")]
    [InlineData(2, 5, "-3")]
    [InlineData(4, 4, "0")]
    [InlineData(0, 0, "0")]
    public void GoalDifference_HasAPlusSignOnlyWhenPositive(int scored, int conceded, string expected)
    {
        var position = new LeaguePositionModel { GoalsScored = scored, GoalsConceded = conceded };

        Assert.Equal(expected, position.FormatGoalDifference());
        Assert.Equal(expected, position.GoalDifference);
    }
}

public class ManagerHistoryModelTests
{
    private readonly ManagerHistoryModel _history = new();
    private readonly ClubModel _club = TestData.Club(1);
    private readonly ClubModel _otherClub = TestData.Club(2);

    [Fact]
    public void StartJob_OpensAJobAtTheClub()
    {
        _history.StartJob(_club, new DateOnly(2026, 7, 1));

        var job = Assert.Single(_history.Jobs);
        Assert.Same(job, _history.CurrentJob);
        Assert.Equal((1, "Club 1", new DateOnly(2026, 7, 1)), (job.ClubId, job.ClubName, job.StartDate));
        Assert.Null(job.EndDate);
    }

    [Fact]
    public void StartJob_AtTheSameClubKeepsTheCurrentJob()
    {
        _history.StartJob(_club, new DateOnly(2026, 7, 1));
        _history.StartJob(_club, new DateOnly(2027, 7, 1));

        Assert.Equal(new DateOnly(2026, 7, 1), Assert.Single(_history.Jobs).StartDate);
    }

    [Fact]
    public void StartJob_AtANewClubEndsThePreviousJob()
    {
        _history.StartJob(_club, new DateOnly(2026, 7, 1));
        _history.StartJob(_otherClub, new DateOnly(2027, 2, 1));

        Assert.Equal(new DateOnly(2027, 2, 1), _history.Jobs[0].EndDate);
        Assert.Equal(2, _history.CurrentJob!.ClubId);
    }

    [Fact]
    public void EndJob_ClosesTheCurrentJobAndLeavesNone()
    {
        _history.StartJob(_club, new DateOnly(2026, 7, 1));

        _history.EndJob(new DateOnly(2026, 11, 3));

        Assert.Equal(new DateOnly(2026, 11, 3), _history.Jobs[0].EndDate);
        Assert.Null(_history.CurrentJob);
    }

    [Fact]
    public void EndJob_WithNoJobDoesNothing()
    {
        _history.EndJob(new DateOnly(2026, 11, 3));

        Assert.Empty(_history.Jobs);
    }

    [Fact]
    public void EndJob_DoesNotRewriteTheEndOfAnEarlierJob()
    {
        _history.StartJob(_club, new DateOnly(2026, 7, 1));
        _history.EndJob(new DateOnly(2026, 11, 3));

        _history.EndJob(new DateOnly(2027, 1, 1));

        Assert.Equal(new DateOnly(2026, 11, 3), _history.Jobs[0].EndDate);
    }

    [Fact]
    public void RecordResult_CountsWinsDrawsLossesGoalsAndCleanSheets()
    {
        _history.StartJob(_club, new DateOnly(2026, 7, 1));

        _history.RecordResult(3, 0, won: true);
        _history.RecordResult(1, 1, won: null);
        _history.RecordResult(0, 2, won: false);
        _history.RecordResult(0, 0, won: null);

        var job = _history.CurrentJob!;
        Assert.Equal((4, 1, 2, 1), (job.Played, job.Won, job.Drawn, job.Lost));
        Assert.Equal((4, 3, 2), (job.GoalsFor, job.GoalsAgainst, job.CleanSheets));
    }

    [Fact]
    public void RecordResult_WhileUnemployedIsIgnored()
    {
        _history.StartJob(_club, new DateOnly(2026, 7, 1));
        _history.EndJob(new DateOnly(2026, 11, 3));

        _history.RecordResult(3, 0, won: true);

        Assert.Equal(0, _history.Played);
    }

    [Fact]
    public void Totals_AddUpEveryJob()
    {
        _history.StartJob(_club, new DateOnly(2026, 7, 1));
        _history.RecordResult(2, 0, won: true);
        _history.RecordResult(0, 1, won: false);
        _history.StartJob(_otherClub, new DateOnly(2027, 1, 1));
        _history.RecordResult(1, 1, won: null);
        _history.RecordResult(4, 0, won: true);

        Assert.Equal((4, 2, 1, 1), (_history.Played, _history.Won, _history.Drawn, _history.Lost));
        Assert.Equal((7, 2, 2), (_history.GoalsFor, _history.GoalsAgainst, _history.CleanSheets));
        Assert.Equal(50.0, _history.WinPercentage);
        Assert.Equal(7.0 / 4, _history.PointsPerGame);
    }

    [Fact]
    public void Ratios_AreZeroBeforeAnyMatchIsPlayed()
    {
        Assert.Equal(0, _history.WinPercentage);
        Assert.Equal(0, _history.PointsPerGame);
        Assert.Equal(0, ManagerJobModel.PerGame(5, 0));
        Assert.Equal(0, ManagerJobModel.Percentage(5, 0));
    }

    [Theory]
    [InlineData(3, 2, 1.5)]
    [InlineData(10, 4, 2.5)]
    public void PerGame_DividesTheTotalByMatchesPlayed(int total, int played, double expected)
    {
        Assert.Equal(expected, ManagerJobModel.PerGame(total, played));
    }

    [Fact]
    public void AddTrophy_RecordsTheCurrentClubAndSeason()
    {
        _history.StartJob(_club, new DateOnly(2026, 7, 1));

        _history.AddTrophy("League Cup", new DateOnly(2027, 3, 14));

        var trophy = Assert.Single(_history.Trophies);
        Assert.Equal(("League Cup", "Club 1", "2026/27", new DateOnly(2027, 3, 14)),
            (trophy.Name, trophy.ClubName, trophy.Season, trophy.Date));
    }

    [Fact]
    public void AddTrophy_WithNoCurrentJobLeavesTheClubBlank()
    {
        _history.AddTrophy("League Cup", new DateOnly(2027, 3, 14));

        Assert.Equal("", Assert.Single(_history.Trophies).ClubName);
    }

    [Theory]
    [InlineData(2026, 7, 1, "2026/27")]
    [InlineData(2027, 6, 30, "2026/27")]
    [InlineData(2099, 8, 1, "2099/00")]
    [InlineData(2030, 1, 1, "2029/30")]
    public void SeasonLabel_RunsFromJulyToJune(int y, int m, int d, string expected)
    {
        Assert.Equal(expected, ManagerHistoryModel.SeasonLabel(new DateOnly(y, m, d)));
    }
}
