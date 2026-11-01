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
    [InlineData(2_000, 10_000, 1)]
    [InlineData(5_000, 10_000, 1)]
    [InlineData(10_000, 15_000, 2)]
    [InlineData(17_300, 20_000, 3)]
    [InlineData(99_354, StadiumRules.MaxCapacity, 10)]
    public void NextTier_IsTheFirstTierAboveTheCurrentCapacity(int capacity, int nextCapacity, int tierNumber)
    {
        Assert.Equal(nextCapacity, StadiumRules.NextTier(capacity)!.Capacity);
        Assert.Equal(nextCapacity - capacity, StadiumRules.SeatsAdded(capacity));
        Assert.Equal(tierNumber, StadiumRules.TierNumber(capacity));
    }

    [Theory]
    [InlineData(10_000, 1_500_000, 6)]
    [InlineData(40_000, 15_000_000, 15)]
    [InlineData(90_000, 70_000_000, 24)]
    public void UpgradeCost_IsTheFullTierPriceFromATierBoundary(int capacity, int cost, int months)
    {
        Assert.Equal(cost, StadiumRules.UpgradeCost(capacity));
        Assert.Equal(months, StadiumRules.UpgradeMonths(capacity));
    }

    [Theory]
    [InlineData(17_300, 1_620_000)] // 2,700 of the tier's 5,000 seats
    [InlineData(99_354, 4_520_000)] // rounded to £10k
    [InlineData(19_900, StadiumRules.MinUpgradeCost)]
    public void UpgradeCost_ChargesOnlyForTheShareOfTheTierStillToBuild(int capacity, int cost)
    {
        Assert.Equal(cost, StadiumRules.UpgradeCost(capacity));
    }

    [Theory]
    [InlineData(2_000)]
    [InlineData(4_999)]
    public void UpgradeCost_IsTheFullFirstStepBelowTheFirstTier(int capacity)
    {
        Assert.Equal(750_000, StadiumRules.UpgradeCost(capacity));
    }

    [Fact]
    public void Tiers_RiseInCapacityAndInCostPerSeat()
    {
        var tiers = StadiumRules.Tiers;
        Assert.Equal(StadiumRules.MaxCapacity, tiers[^1].Capacity);

        var lastPerSeat = 0.0;
        for (var i = 1; i < tiers.Count; i++)
        {
            Assert.True(tiers[i].Capacity > tiers[i - 1].Capacity);
            Assert.True(tiers[i].Months > 0);

            var perSeat = (double)tiers[i].Cost / (tiers[i].Capacity - tiers[i - 1].Capacity);
            Assert.True(perSeat >= lastPerSeat, $"Tier {i + 1} is cheaper per seat than the one below");
            lastPerSeat = perSeat;
        }
    }

    [Fact]
    public void UpgradeCost_NeverExceedsTheTierPriceAndFallsAsTheStadiumFillsTheTier()
    {
        var previous = (Tier: (StadiumRules.Tier?)null, Cost: int.MaxValue);
        for (var capacity = 1_000; capacity < StadiumRules.MaxCapacity; capacity += 137)
        {
            var next = StadiumRules.NextTier(capacity)!;
            var cost = StadiumRules.UpgradeCost(capacity);

            Assert.InRange(cost, StadiumRules.MinUpgradeCost, next.Cost);
            if (next == previous.Tier)
                Assert.True(cost <= previous.Cost, $"{capacity:n0} seats costs more than a smaller stadium in the same tier");

            previous = (next, cost);
        }
    }

    [Theory]
    [InlineData(StadiumRules.MaxCapacity)]
    [InlineData(StadiumRules.MaxCapacity + 1_000)]
    public void NothingIsLeftToBuildAtTheMaximumCapacity(int capacity)
    {
        Assert.Null(StadiumRules.NextTier(capacity));
        Assert.Equal(0, StadiumRules.SeatsAdded(capacity));
        Assert.Equal(0, StadiumRules.UpgradeCost(capacity));
        Assert.Equal(StadiumRules.Tiers.Count, StadiumRules.TierNumber(capacity));
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
