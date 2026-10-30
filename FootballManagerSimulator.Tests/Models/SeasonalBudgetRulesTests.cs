using FootballManagerSimulator.Maui.Models;
using FootballManagerSimulator.Tests.Support;

namespace FootballManagerSimulator.Tests.Models;

public class SeasonalBudgetRulesTests
{
    // Country 1 has a top flight (league 1) and a second tier (league 2); country 2 only a top flight (league 3).
    private static readonly List<CompetitionModel> Leagues =
    [
        new() { Id = 1, IsLeague = true, Rank = 1, CountryId = 1 },
        new() { Id = 2, IsLeague = true, Rank = 2, CountryId = 1 },
        new() { Id = 3, IsLeague = true, Rank = 1, CountryId = 2 },
        new() { Id = 9, IsLeague = false, Rank = 1, CountryId = 1 }
    ];

    private static ClubModel Club(int id, int leagueId, int stadium)
    {
        var club = TestData.Club(id, leagueId: leagueId);
        club.StadiumSize = stadium;
        return club;
    }

    [Fact]
    public void AnAverageClubInAReferenceSizedTopFlight_GetsTheDivisionBase()
    {
        var clubs = new[] { Club(1, 1, SeasonalBudgetRules.ReferenceStadiumSize), Club(2, 2, 10_000) };

        var budgets = SeasonalBudgetRules.Budgets(clubs, Leagues);

        Assert.Equal(SeasonalBudgetRules.DivisionBase(1), budgets[1]);
        Assert.Equal(SeasonalBudgetRules.DivisionBase(2), budgets[2]); // the only club, so average for its league
    }

    [Fact]
    public void BiggerStadiums_GetBiggerBudgetsWithinTheirLeague_UpToTheLimits()
    {
        // League average 38,000: 19,000 is half, 76,000 is double, the tiny ground is held at half.
        var clubs = new[] { Club(1, 1, 19_000), Club(2, 1, 76_000), Club(3, 1, 1_000), Club(4, 1, 56_000) };

        var budgets = SeasonalBudgetRules.Budgets(clubs, Leagues);

        Assert.True(budgets[2] > budgets[4] && budgets[4] > budgets[1]);
        Assert.Equal(budgets[1], budgets[3]); // both at the minimum club factor
    }

    [Fact]
    public void SmallerFootballCountries_HaveSmallerBudgets()
    {
        var clubs = new[] { Club(1, 1, 38_000), Club(2, 3, 19_000) };

        var budgets = SeasonalBudgetRules.Budgets(clubs, Leagues);

        // Half the crowds in its top flight, so a quarter of the budget.
        Assert.Equal(SeasonalBudgetRules.DivisionBase(1) / 4, budgets[2]);
    }

    [Fact]
    public void TheSameClub_GetsMoreAfterPromotion()
    {
        var club = Club(1, 2, 20_000);
        var topFlight = Club(2, 1, 38_000);
        var secondTier = Club(3, 2, 20_000);
        var before = SeasonalBudgetRules.Budgets([club, topFlight, secondTier], Leagues)[1];

        club.LeagueId = 1;
        var after = SeasonalBudgetRules.Budgets([club, topFlight, secondTier], Leagues)[1];

        Assert.True(after > before);
    }

    [Theory]
    [InlineData(0, 1_000_000, 1_000_000)]           // spent everything: just the allocation
    [InlineData(300_000, 1_000_000, 1_300_000)]     // below the cap: keeps it all
    [InlineData(500_000, 1_000_000, 1_500_000)]     // exactly reaches the cap
    [InlineData(1_200_000, 1_000_000, 1_500_000)]   // never spends: held at one and a half times the allocation
    [InlineData(20_000_000, 4_500_000, 6_750_000)]  // relegated with a big budget: cut to the new league's cap
    [InlineData(2_000_000, 4_500_000, 6_500_000)]   // promoted: keeps its savings under the higher cap
    public void NextBudget_AIClubsAreCappedAtOneAndAHalfTimesTheirAllocation(int current, int allocation, int expected) =>
        Assert.Equal(expected, SeasonalBudgetRules.NextBudget(current, allocation, isAIClub: true));

    [Theory]
    [InlineData(20_000_000, 1_000_000)]  // saved a lot: the board takes it back
    [InlineData(300_000, 1_000_000)]     // a little left over: taken back too
    [InlineData(0, 1_000_000)]
    public void NextBudget_TheUsersClubStartsEachSeasonOnJustItsAllocation(int current, int allocation) =>
        Assert.Equal(allocation, SeasonalBudgetRules.NextBudget(current, allocation, isAIClub: false));

    [Fact]
    public void NextBudget_NeverOverflows() =>
        Assert.Equal(int.MaxValue, SeasonalBudgetRules.NextBudget(int.MaxValue - 5, 2_000_000_000, isAIClub: true));

    [Fact]
    public void BudgetsAreRoundedToThreeFigures_AndClubsOutsideALeagueAreLeftOut()
    {
        var clubs = new[] { Club(1, 1, 38_000), Club(2, 1, 38_123), Club(3, 99, 10_000) };

        var budgets = SeasonalBudgetRules.Budgets(clubs, Leagues);

        Assert.All(budgets.Values, b => Assert.Equal(0, b % 100_000));
        Assert.False(budgets.ContainsKey(3));
    }
}
