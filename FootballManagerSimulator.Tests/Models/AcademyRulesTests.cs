using FootballManagerSimulator.Maui.Models;

namespace FootballManagerSimulator.Tests.Models;

public class AcademyRulesTests
{
    [Theory]
    [InlineData(1, 5, 7)]
    [InlineData(2, 4, 6)]
    [InlineData(3, 3, 5)]
    [InlineData(4, 2, 4)]
    [InlineData(5, 2, 3)]
    [InlineData(9, 2, 3)]
    public void StartingRating_IsHigherInBetterLeaguesAndNeverAboveSeven(int leagueRank, int min, int max)
    {
        var ratings = Enumerable.Range(0, 500).Select(_ => AcademyRules.StartingRating(leagueRank)).ToList();

        Assert.All(ratings, r => Assert.InRange(r, min, max));
        Assert.All(ratings, r => Assert.True(r <= AcademyRules.StartingMax));
        Assert.Equal(max - min + 1, ratings.Distinct().Count()); // every value in the range turns up
    }

    [Fact]
    public void UpgradeCost_RisesWithEveryLevel()
    {
        var costs = Enumerable.Range(AcademyRules.Min + 1, AcademyRules.Max - AcademyRules.Min)
            .Select(AcademyRules.UpgradeCost)
            .ToList();

        Assert.Equal(costs.OrderBy(c => c).Distinct(), costs);
        Assert.Equal(250_000, costs.First());
        Assert.Equal(18_000_000, costs.Last());
    }
}
